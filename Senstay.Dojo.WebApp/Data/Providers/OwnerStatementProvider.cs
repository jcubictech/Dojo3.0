using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;
using System.Web;
using System.Globalization;

namespace Senstay.Dojo.Data.Providers
{
    public class OwnerStatementProvider : CrudProviderBase<OwnerStatement>
    {
        private readonly DojoDbContext _context;
        private const string GRONDSKEEPING_CATEGORY = "Groundskeeping";
        private const string CLEANING_CATEGORY = "Cleaning";
        private const string LAUNDRY_CATEGORY = "Laundry";
        private const string CONSUMABLES_CATEGORY = "Consumables";
        private DateTime GRONDSKEEPING_EFFECTIVE_DATE = (new DateTime(2017, 12, 1)).Date;
        private DateTime CLEANINGRULE_EFFECTIVE_DATE = (new DateTime(2018, 1, 1)).Date;

        public OwnerStatementProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        #region Owner Statement and Summary methods

        public OwnerStatement GetOwnerStatement(string propertyCode, int month, int year)
        {
            try
            {
                return _context.OwnerStatements.Where(s => s.Month == month && s.Year == year && s.PropertyCode == propertyCode)
                                               .FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public OwnerStatementViewModel GetOwnerStatement(DateTime month, string propertyCode)
        {
            try
            {
                var ownerStatement = new OwnerStatementViewModel();

                var utcMonth = ConversionHelper.EnsureUtcDate(month.AddDays(1));
                var endMonth = ConversionHelper.MonthWithLastDay(month);
                var lastMonth = ConversionHelper.MonthWithLastDay(month.AddMonths(-1));

                var propertyProvider = new PropertyProvider(_context);
                var property = propertyProvider.Retrieve(propertyCode);

                // remove these words from statement display
                if (property.Address != null)
                    property.Address = property.Address.Replace("COMBO:", "").Replace("ROOM:", "").Replace("MULTI:", "");
                else
                    property.Address = string.Empty;

                var feeProvider = new PropertyFeeProvider(_context);
                var propertyFee = feeProvider.Retrieve(propertyCode, utcMonth);
                var isFixedCostModel = IsFixedCostModel(propertyFee) && FixedCostEffectiveDate(month);

                var balanceProvider = new PropertyBalanceProvider(_context);
                double carryOver = 0;
                if (lastMonth.Month < 8 && lastMonth.Year <= 2017)
                {
                    var propertyBalance = balanceProvider.Retrieve(propertyCode, month.Month, month.Year);
                    carryOver = propertyBalance == null ? 0 : propertyBalance.AdjustedBalance.Value;
                }
                else
                {
                    var propertyBalance = balanceProvider.RetrieveCarryOvers(lastMonth.Month, lastMonth.Year, propertyCode).FirstOrDefault();
                    carryOver = propertyBalance == null ? 0 : propertyBalance.CarryOver;
                }

                var entityProvider = new PropertyEntityProvider(_context);
                var entityName = entityProvider.GetEntityName(propertyCode, endMonth);

                // banner data
                ownerStatement.StatementMonth = month;
                ownerStatement.OwnerName = entityName;
                ownerStatement.PropertyName = string.Format("{0} | {1}", property.PropertyCode, property.Address);
                ownerStatement.PropertyNameWithProduct = string.Format("{0}-{1} | {2}", property.PropertyCode, property.Vertical, property.Address);
                ownerStatement.IsFinalized = IsFinalized(month, propertyCode);
                ownerStatement.ApprovalSummary = GetApprovalStateText(month, propertyCode);

                // resolution data
                var resolutions = GetResolutionStatement(month, propertyCode);
                ownerStatement.ResolutionDetails = resolutions;
                ownerStatement.ResolutionsTotal = resolutions.Sum(x => x.TotalRevenue);

                // reservation data
                var reservations = GetReservationStatement(month, propertyCode);
                ownerStatement.ReservationDetails = reservations;
                ownerStatement.ReservationsTotal = reservations.Sum(x => x.TotalRevenue) + ownerStatement.ResolutionsTotal;
                var ExcludedTaxRevenue = GetExcludedTaxRevenue(reservations);

                // off-airbnb reservations for tax calculation
                ownerStatement.TaxCollected = reservations.Where(x => x.Channel != "Airbnb")
                                                          .Sum(x => (x.TotalRevenue - ExcludedTaxRevenue) * (x.TaxRate == null ? 0 : x.TaxRate.Value));

                // advance payment section
                ownerStatement.AdvancePaymentDetails = GetAdvancePayments(month, propertyCode);

                // owner & unit expenses section
                // special rule: maintenace expenses are rolled up to be with fixed cost if applicable
                var fixedUnitExpenses = new List<UnitExpenseStatement>();
                ownerStatement.UnitExpenseDetails = GetUnitExpenses(month, propertyCode, isFixedCostModel);
                if (isFixedCostModel)
                {
                    fixedUnitExpenses = GetUnitExpenses(month, GetFixedCostModelCount(reservations, month), propertyFee);
                    if (fixedUnitExpenses.Count > 0)
                    {
                        if (UseGroundKeepingRule(month))
                        {
                            if (UseCleaningCountRule(month))
                                MergeExpenses(ownerStatement.UnitExpenseDetails, fixedUnitExpenses);
                            else
                                MergeGroundskeeping(ownerStatement.UnitExpenseDetails, fixedUnitExpenses);
                        }
                    }
                }
                if (fixedUnitExpenses.Count > 0) ownerStatement.UnitExpenseDetails.AddRange(fixedUnitExpenses);

                // footer section
                ownerStatement.IsProductRS = (property.Vertical == "RS");

                // statement section
                ownerStatement.NightsBooked = GetNightCount(reservations);
                ownerStatement.ReservationCount = reservations.Distinct().Count();
                ownerStatement.TotalRevenue = ownerStatement.ReservationsTotal;
                ownerStatement.BeginBalance = carryOver;
                ownerStatement.CityTaxRate = propertyFee != null && propertyFee.CityTax != null ? propertyFee.CityTax.Value : 0;

                double managementFeeRate = (propertyFee == null || propertyFee.ManagementFee == null) ? 0.0 : propertyFee.ManagementFee.Value;
                ownerStatement.ManagementFeePercentage = managementFeeRate.ToString("P1", new NumberFormatInfo { PercentPositivePattern = 1, PercentNegativePattern = 1 });

                if (ownerStatement.IsProductRS)
                {
                    ownerStatement.CleaningFees = 0;
                    ownerStatement.ManagementFees = -ownerStatement.TotalRevenue * managementFeeRate;
                }
                else
                {
                    // special rule: cleaning fee for fixed cost also include special cleaning fees from expense table
                    ownerStatement.CleaningFees = -GetCleanFees(month, propertyCode); // cleaning fee for one-off cleaning cost including 10% surcharge
                    int fixedCostCount = GetFixedCostModelCount(reservations, month); // filter reservations that do not need cleaning
                    if (isFixedCostModel && propertyFee.Cleanings != null && fixedCostCount > 0)
                        ownerStatement.CleaningFees += -Math.Round(fixedCostCount * propertyFee.Cleanings.Value * 1.1, 2); // mark up 10% on fixed clean fee

                    // all cleaning fees are accounted for by fixed cleaning rule above, so we remove it from unit expenses
                    RemoveCleaningExpenses(ownerStatement.UnitExpenseDetails);

                    // special rule: management fee = 0 if there is no revenue but has cleaning fee
                    if ((ConversionHelper.ZeroMoneyValue(ownerStatement.TotalRevenue) && ownerStatement.CleaningFees < 0) ||
                        (ownerStatement.TotalRevenue + ownerStatement.CleaningFees) < 0.01)
                        ownerStatement.ManagementFees = 0;
                    else
                        ownerStatement.ManagementFees = -(ownerStatement.TotalRevenue + ownerStatement.CleaningFees) * managementFeeRate;
                }

                ownerStatement.AdvancementPaymentsTotal = -ownerStatement.AdvancePaymentDetails.Sum(x => x.Amount);
                ownerStatement.UnitExpensesTotal = -ownerStatement.UnitExpenseDetails.Sum(x => x.Amount);
                ownerStatement.EndingBalance = Math.Round(ownerStatement.TotalRevenue, 2) +
                                               Math.Round(ownerStatement.BeginBalance, 2) +
                                               Math.Round(ownerStatement.TaxCollected, 2) +
                                               Math.Round(ownerStatement.CleaningFees, 2) +
                                               Math.Round(ownerStatement.ManagementFees, 2) +
                                               Math.Round(ownerStatement.UnitExpensesTotal, 2) +
                                               Math.Round(ownerStatement.AdvancementPaymentsTotal, 2);

                var note = _context.OwnerStatements.Where(x => x.Month == month.Month && x.Year == month.Year && x.PropertyCode == property.PropertyCode)
                                                   .Select(x => x.StatementNotes)
                                                   .FirstOrDefault();

                var finalizedStatement = GetOwnerStatement(property.PropertyCode, month.Month, month.Year);
                if (finalizedStatement != null)
                {
                    ownerStatement.StatementNotes = finalizedStatement.StatementNotes;
                    ownerStatement.IsModified = IsStatementModified(finalizedStatement, ownerStatement);
                }

                return ownerStatement;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool FinalizeStatement(DateTime date, string propertyCode, string note, double endingBalance, bool isFinalized)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];
                sqlParams[0] = new SqlParameter("@Month", SqlDbType.Int);
                sqlParams[0].Value = date.Month;
                sqlParams[1] = new SqlParameter("@Year", SqlDbType.Int);
                sqlParams[1].Value = date.Year;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;
                sqlParams[3] = new SqlParameter("@EndingBalance", SqlDbType.Float);
                sqlParams[3].Value = endingBalance;
                sqlParams[4] = new SqlParameter("@IsFinalized", SqlDbType.Bit);
                sqlParams[4].Value = isFinalized;

                // create/update/delete from the property balance table depending on IsFinalized flag for next month
                var data = _context.Database.SqlQuery<SqlResultModel>("FinalizePropertyStatement @Month, @Year, @PropertyCode, @EndingBalance, @IsFinalized", sqlParams).FirstOrDefault();

                // create/update owner statement record
                var model = GetOwnerStatement(date, propertyCode);
                model.IsFinalized = isFinalized;
                model.PropertyName = model.PropertyName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                var entity = Retrieve(model);
                if (entity != null)
                {
                    MapData(model, ref entity);
                    entity.IsSummary = false;
                    entity.StatementNotes = note;
                    Update(entity.OwnerStatementId, entity);
                }
                else
                {
                    entity = new OwnerStatement();
                    MapData(model, ref entity);
                    entity.IsSummary = false;
                    entity.StatementNotes = note;
                    Create(entity);
                }
                Commit();

                return data == null || data.Result == null ? false : true;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public OwnerStatementSummaryModel GetOwnerSummary(DateTime month, string payoutMethod, bool redo)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@PayoutMethod", SqlDbType.NVarChar);
                sqlParams[0].Value = payoutMethod;
                var ownerProperties = _context.Database.SqlQuery<OwnerPropertyModel>("GetPropertiesForOwnerSummary @PayoutMethod", sqlParams).ToList();

                // get paid payout amount
                var paymentProvider = new OwnerPaymentProvider(_context);
                double? paidPayout = paymentProvider.GetMonthlyPayout(month, payoutMethod);

                var statementProvider = new OwnerStatementProvider(_context);
                var ownerSummary = new OwnerStatementSummaryModel();

                // banner data
                ownerSummary.StatementMonth = month;
                ownerSummary.OwnerName = payoutMethod;
                ownerSummary.PaidPayout = paidPayout == null ? 0 : paidPayout.Value;
                ownerSummary.TotalPayout = 0;

                // sum up owner statement belonging to the summary if the statement has been finalized
                int signFlag = 0;
                var entityProvider = new PropertyEntityProvider(_context);
                foreach (var property in ownerProperties)
                {
                    var model = new OwnerStatementViewModel(_context)
                    {
                        PropertyName = property.PropertyCode,
                        StatementMonth = month
                    };                 

                    var ownerStatement = statementProvider.Retrieve(model);

                    if (ownerStatement != null && ownerStatement.StatementStatus == StatementStatus.Finalized)
                    {
                        var summaryItem = MapOwnerStatementToSummaryItem(ownerStatement);
                        summaryItem.Address = property.Address;
                        summaryItem.PropertyID = property.PropertyCode + '-' + property.Vertical;

                        summaryItem.EntityName = entityProvider.GetEntityName(property.PropertyCode, model.StatementMonth);
                        summaryItem.PayoutMethod = payoutMethod;

                        ownerSummary.SummaryItems.Add(summaryItem);

                        signFlag |= summaryItem.EndingBalance > 0 ? 0x0001 : (summaryItem.EndingBalance < 0 ? 0x0002 : 0);
                    }
                }

                // retrive statement summary row if it exist; otherwise compute it
                var summaryRowModel = new OwnerStatementViewModel(_context)
                {
                    PropertyName = payoutMethod,
                    StatementMonth = month
                };
                var summaryRow = statementProvider.Retrieve(summaryRowModel);

                if(summaryRow != null) ownerSummary.SummaryNotes = summaryRow.StatementNotes;

                if (!redo && summaryRow != null) // get it from the record stored in ownerstatement table if exists
                {
                    ownerSummary.SetSumTotal(summaryRow);
                    ownerSummary.IsFinalized = summaryRow.StatementStatus == StatementStatus.Finalized;
                }
                else // compute it
                {
                    ownerSummary.SetSumTotal();
                    if (!redo) ownerSummary.IsFinalized = false;
                }

                // if the summary has been finalized, we check if the total of all statements matches
                if (ownerSummary.IsFinalized)
                {
                    var itemTotal = ownerSummary.SumTotal(ownerSummary.SummaryItems); // the total of all the statements
                    ownerSummary.IsModified = IsSummaryModified(ownerSummary.ItemTotal, itemTotal);
                }

                ownerSummary.IsRebalanced = !(ownerSummary.TotalPayout < 0 && (signFlag & 0x0003) == 0x0003);

                return ownerSummary;
            }
            catch(Exception ex)
            {
                throw; // let caller handle the error
            }
        }

        public void FinalizeSummary(DateTime month, string payoutMethod, string note, bool isFinalized)
        {
            try
            {
                var summary = GetOwnerSummary(month, payoutMethod, true);
                var model = new OwnerStatement();
                MapData(summary.ItemTotal, ref model, month, payoutMethod);
                model.StatementStatus = isFinalized ? StatementStatus.Finalized : StatementStatus.Approved;
                model.IsSummary = true;
                var entity = Retrieve(model);
                if (entity != null)
                {
                    model.OwnerStatementId = entity.OwnerStatementId;
                    model.StatementNotes = note;
                    Update(model.OwnerStatementId, model);
                }
                else
                {
                    model.StatementNotes = note;
                    Create(model);
                }
                Commit();
            }
            catch
            {
                throw;
            }
        }

        public bool IsFinalized(DateTime month, string propertyCode)
        {
            var count = _context.OwnerStatements.Where(x => x.PropertyCode == propertyCode &&
                                                            x.Month == month.Month &&
                                                            x.Year == month.Year &&
                                                            x.StatementStatus == StatementStatus.Finalized)
                                                    .Count();
            return count > 0;
        }

        public bool IsStatementModified(OwnerStatement finalizedStatement, OwnerStatementViewModel currentStatement)
        {
            return !ConversionHelper.MoneyEqual(finalizedStatement.TotalRevenue, currentStatement.TotalRevenue) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.BeginBalance, currentStatement.BeginBalance) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.CleaningFees, currentStatement.CleaningFees) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.AdvancePayments, currentStatement.AdvancementPaymentsTotal) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.Balance, currentStatement.EndingBalance) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.ManagementFees, currentStatement.ManagementFees) ||
                   !ConversionHelper.MoneyEqual(finalizedStatement.TaxCollected, currentStatement.TaxCollected);
        }

        public bool IsSummaryModified(OwnerStatementSummaryItem finalizedSummary, OwnerStatementSummaryItem currentSummary)
        {
            return !ConversionHelper.MoneyEqual(finalizedSummary.TotalRevenue, currentSummary.TotalRevenue) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.BeginBalance, currentSummary.BeginBalance) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.CleaningFees, currentSummary.CleaningFees) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.AdvancePayments, currentSummary.AdvancePayments) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.EndingBalance, currentSummary.EndingBalance) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.ManagementFees, currentSummary.ManagementFees) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.UnitExpenseItems, currentSummary.UnitExpenseItems) ||
                   !ConversionHelper.MoneyEqual(finalizedSummary.TaxCollected, currentSummary.TaxCollected);
        }

        public bool IsFixedCostModel(PropertyFee fee)
        {
            if (fee == null) return false;

            return (fee.Cleanings != null && fee.Cleanings != 0) ||
                   (fee.Consumables != null && fee.Consumables != 0) ||
                   (fee.Laundry != null && fee.Laundry != 0) ||
                   (fee.Landscaping != null && fee.Landscaping != 0) ||
                   (fee.PoolService != null && fee.PoolService != 0) ||
                   (fee.TrashService != null && fee.TrashService != 0) ||
                   (fee.PestService != null && fee.PestService != 0);
        }

        private bool FixedCostEffectiveDate(DateTime date)
        {
            return date.Year > 2017 || (date.Year == 2017 && date.Month > 10); // fixed cost model is effective on 11/2017
        }

        private string GetApprovalStateText(DateTime month, string propertyCode)
        {
            DateTime startDate = new DateTime(month.Year, month.Month, 1);
            DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
            var resevationCount = _context.Reservations.Where(x => x.PropertyCode == propertyCode && x.IsDeleted == false &&
                                                              x.TransactionDate >= startDate && x.TransactionDate <= endDate &&
                                                              (x.ApprovalStatus == RevenueApprovalStatus.Reviewed || x.ApprovalStatus == RevenueApprovalStatus.NotStarted))
                                                       .Count();

            var expenseCount = _context.Expenses.Where(x => x.PropertyCode == propertyCode && x.IsDeleted == false &&
                                                       x.ExpenseDate >= startDate && x.ExpenseDate <= endDate &&
                                                       x.ExpenseId == x.ParentId &&
                                                       (x.ApprovalStatus == RevenueApprovalStatus.Reviewed || x.ApprovalStatus == RevenueApprovalStatus.NotStarted))
                                                .Count();

            var resolutionCount = _context.Resolutions.Where(x => x.PropertyCode == propertyCode && x.IsDeleted == false &&
                                                             x.ResolutionDate >= startDate && x.ResolutionDate <= endDate &&
                                                             (x.ApprovalStatus == RevenueApprovalStatus.Reviewed || x.ApprovalStatus == RevenueApprovalStatus.NotStarted))
                                                      .Count();

            var otherRevenueCount = _context.OtherRevenues.Where(x => x.PropertyCode == propertyCode && x.IsDeleted == false &&
                                                                 x.OtherRevenueDate >= startDate && x.OtherRevenueDate <= endDate &&
                                                                 (x.ApprovalStatus == RevenueApprovalStatus.Reviewed || x.ApprovalStatus == RevenueApprovalStatus.NotStarted))
                                                          .Count();

            string approvalSummay = string.Empty;
            if (resevationCount > 0 || expenseCount > 0 || resolutionCount > 0)
                approvalSummay = string.Format("Waiting for approval count: Reservations: {0:d},  Expenses: {1:d},  Resolutions: {2:d},  Other Revenue: {3:d}",
                                               resevationCount, expenseCount, resolutionCount, otherRevenueCount);

            return approvalSummay;
        }

        private List<ReservationStatement> GetReservationStatement(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var reservations = _context.Database.SqlQuery<ReservationStatement>("GetReservationStatement @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();

                // consolidate splited reservations
                List<ReservationStatement> consolidatedReservtions = new List<ReservationStatement>();
                ReservationStatement previousReservation = null;
                foreach (var reservation in reservations)
                {
                    if (previousReservation != null && 
                        (reservation.Arrival != previousReservation.Arrival ||
                         reservation.Departure != previousReservation.Departure ||
                         reservation.Guest != previousReservation.Guest))
                    {
                        consolidatedReservtions.Add(previousReservation);
                        previousReservation = reservation;
                    }
                    else if (previousReservation == null)
                    {
                        previousReservation = reservation;
                    }
                    else
                    {
                        previousReservation.TotalRevenue += reservation.TotalRevenue;
                    }
                }
                if (previousReservation != null)
                {
                    consolidatedReservtions.Add(previousReservation);
                }

                return consolidatedReservtions;
            }
            catch(Exception ex)
            {
                throw; // let caller handle the error
            }
        }

        private List<ResolutionStatement> GetResolutionStatement(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var resolutions = _context.Database.SqlQuery<ResolutionStatement>("GetResolutionStatement @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();

                // combine resolutions that has same arrival date, nights, guest, and type
                List<ResolutionStatement> consolidatedResolutions = new List<ResolutionStatement>();
                ResolutionStatement previousResolution = null;
                foreach (var resolution in resolutions)
                {
                    if (previousResolution != null &&
                        (resolution.Arrival != previousResolution.Arrival || resolution.Nights != previousResolution.Nights ||
                         resolution.Guest != previousResolution.Guest || resolution.Type != previousResolution.Type))
                    {
                        consolidatedResolutions.Add(previousResolution);
                        previousResolution = resolution;
                    }
                    else if (previousResolution == null)
                    {
                        previousResolution = resolution;
                    }
                    else
                    {
                        previousResolution.TotalRevenue += resolution.TotalRevenue;
                        if (!string.IsNullOrEmpty(previousResolution.ApprovedNote)) previousResolution.ApprovedNote += ";";
                        previousResolution.ApprovedNote += resolution.ApprovedNote;
                    }
                }
                if (previousResolution != null)
                {
                    consolidatedResolutions.Add(previousResolution);
                }

                return consolidatedResolutions;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        private List<AdvancePaymentStatement> GetAdvancePayments(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var data = _context.Database.SqlQuery<AdvancePaymentStatement>("GetAdvancePaymentStatement @StartDate, @EndDate, @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        private List<UnitExpenseStatement> GetUnitExpenses(DateTime month, string propertyCode, bool isFixedCost)
        {
            try
            {
                if (UseGroundKeepingRule(month))
                {
                    DateTime startDate = new DateTime(month.Year, month.Month, 1);
                    DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                    SqlParameter[] sqlParams = new SqlParameter[4];
                    sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                    sqlParams[0].Value = startDate;
                    sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                    sqlParams[1].Value = endDate;
                    sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                    sqlParams[2].Value = propertyCode;
                    sqlParams[3] = new SqlParameter("@FixedCostCategory", SqlDbType.NVarChar);
                    sqlParams[3].Value = isFixedCost ? GRONDSKEEPING_CATEGORY : string.Empty;

                    var data = _context.Database.SqlQuery<UnitExpenseStatement>("GetUnitExpenses @StartDate, @EndDate, @PropertyCode, @FixedCostCategory", sqlParams).ToList();
                    return data;
                }
                else
                {
                    DateTime startDate = new DateTime(month.Year, month.Month, 1);
                    DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                    SqlParameter[] sqlParams = new SqlParameter[4];
                    sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                    sqlParams[0].Value = startDate;
                    sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                    sqlParams[1].Value = endDate;
                    sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                    sqlParams[2].Value = propertyCode;
                    sqlParams[3] = new SqlParameter("@FixedCost", SqlDbType.Int);
                    sqlParams[3].Value = isFixedCost ? 1 : 0;

                    var data = _context.Database.SqlQuery<UnitExpenseStatement>("GetUnitExpensesOld @StartDate, @EndDate, @PropertyCode, @FixedCost", sqlParams).ToList();
                    return data;
                }
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        private List<UnitExpenseStatement> GetUnitExpenses(DateTime month, int reservationCount, PropertyFee fee)
        {
            List<UnitExpenseStatement> unitExpenses = new List<UnitExpenseStatement>();
            try
            {
                if (UseGroundKeepingRule(month))
                {
                    double groundsKeepingFee = 0;
                    if (reservationCount > 0)
                    {
                        if (fee.Consumables != null && fee.Consumables != 0) unitExpenses.Add(MakeUnitExpense(Math.Round(fee.Consumables.Value * reservationCount, 2), "Consumables"));
                        if (fee.Laundry != null && fee.Laundry != 0) unitExpenses.Add(MakeUnitExpense(Math.Round(fee.Laundry.Value * reservationCount, 2), "Laundry Service"));
                        if (fee.Landscaping != null && fee.Landscaping != 0) groundsKeepingFee += fee.Landscaping.Value;
                    }
                    if (fee.PoolService != null && fee.PoolService != 0) groundsKeepingFee += fee.PoolService.Value;
                    if (fee.TrashService != null && fee.TrashService != 0) groundsKeepingFee += fee.TrashService.Value;
                    if (fee.PestService != null && fee.PestService != 0) groundsKeepingFee += fee.PestService.Value;

                    if (groundsKeepingFee > 0) unitExpenses.Add(MakeUnitExpense(Math.Round(groundsKeepingFee, 2), GRONDSKEEPING_CATEGORY));
                }
                else
                {
                    if (reservationCount > 0)
                    {
                        if (fee.Consumables != null && fee.Consumables != 0) unitExpenses.Add(MakeUnitExpense(Math.Round(fee.Consumables.Value * reservationCount, 2), "Consumables"));
                        if (fee.Landscaping != null && fee.Landscaping != 0) unitExpenses.Add(MakeUnitExpense(fee.Landscaping.Value, "Landscaping"));
                        if (fee.Laundry != null && fee.Laundry != 0) unitExpenses.Add(MakeUnitExpense(Math.Round(fee.Laundry.Value * reservationCount, 2), "Laundry Service"));
                    }
                    if (fee.PoolService != null && fee.PoolService != 0) unitExpenses.Add(MakeUnitExpense(fee.PoolService.Value, "Pool Service"));
                    if (fee.TrashService != null && fee.TrashService != 0) unitExpenses.Add(MakeUnitExpense(fee.TrashService.Value, "Trash Service"));
                    if (fee.PestService != null && fee.PestService != 0) unitExpenses.Add(MakeUnitExpense(fee.PestService.Value, "Pest Control Service"));
                }
            }
            catch
            {
                throw; // let caller handle the error
            }
            return unitExpenses;
        }

        private int GetFixedCostModelCount(List<ReservationStatement> reservations, DateTime month)
        {
            return reservations.Where(x => x.Type != "Maintenance").Count();
        }

        private UnitExpenseStatement MakeUnitExpense(double amount, string category)
        {
            var unitExpense = new UnitExpenseStatement();
            unitExpense.Category = category;
            unitExpense.Amount = (float)amount;
            return unitExpense;
        }

        private void MergeExpenses(List<UnitExpenseStatement> unitExpenseDetails, List<UnitExpenseStatement> fixedUnitExpenses)
        {
            foreach (UnitExpenseStatement expense in unitExpenseDetails)
            {
                UnitExpenseStatement matchedExpense = null;
                foreach (UnitExpenseStatement fixedExpense in fixedUnitExpenses)
                {
                    if (fixedExpense.Category == expense.Category)
                    {
                        expense.Amount += fixedExpense.Amount;
                        matchedExpense = fixedExpense;
                        break;
                    }
                }
                if (matchedExpense != null) fixedUnitExpenses.Remove(matchedExpense);
            }
        }

        private void RemoveCleaningExpenses(List<UnitExpenseStatement> unitExpenseDetails)
        {
            var cleaningExpenses = unitExpenseDetails.RemoveAll(x => x.Category == "Cleaning");
        }

        private void MergeGroundskeeping(List<UnitExpenseStatement> unitExpenseDetails, List<UnitExpenseStatement> fixedUnitExpenses)
        {
            foreach (UnitExpenseStatement expense in unitExpenseDetails)
            {
                if (expense.Category == GRONDSKEEPING_CATEGORY)
                {
                    UnitExpenseStatement matchedExpense = null;
                    foreach (UnitExpenseStatement fixedExpense in fixedUnitExpenses)
                    {
                        if (fixedExpense.Category == GRONDSKEEPING_CATEGORY)
                        {
                            expense.Amount += fixedExpense.Amount;
                            matchedExpense = fixedExpense;
                            break;
                        }
                    }
                    if (matchedExpense != null) fixedUnitExpenses.Remove(matchedExpense);
                }
            }
        }

        private bool UseGroundKeepingRule(DateTime month)
        {
            // use ground keeping service for consolidated pool, pest, landscape, and trash expenses
            return month >= GRONDSKEEPING_EFFECTIVE_DATE;
        }

        private bool UseCleaningCountRule(DateTime month)
        {
            // use ground keeping service for consolidated pool, pest, landscape, and trash expenses
            return month >= CLEANINGRULE_EFFECTIVE_DATE;
        }

        private float GetExcludedTaxRevenue(List<ReservationStatement> reservations)
        {
            var stringsToCheck = new List<string> { "notax" };
            var excluded = reservations.Where(x => x.Channel != "Airbnb" && stringsToCheck.Any(s => x.ApprovedNote.Contains(s))).FirstOrDefault();
            if (excluded != null)
                return excluded.TotalRevenue;
            else
                return 0;
        }

        private int GetNightCount(List<ReservationStatement> reservations)
        {
            //var nights = reservations.GroupBy(x => new { x.Arrival, x.Departure, x.Nights })
            //                         .Where(g => g.Count() > 1)
            //                         .Select(y => y.Key.Nights)
            //                         .Sum();
            var nights = reservations.Select(x => x.Nights)
                                     .Sum();
            return nights;
        }

        private double GetCleanFees(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var data = _context.Database.SqlQuery<TotalAmountModel>("GetCleaningFee @StartDate, @EndDate, @PropertyCode", sqlParams).FirstOrDefault();
                return data == null || data.Amount == null ? 0 : data.Amount.Value;
            }
            catch(Exception)
            {
                throw; // let caller handle the error
            }
        }

        private OwnerStatementSummaryItem MapOwnerStatementToSummaryItem(OwnerStatementViewModel ownerStatement)
        {
            var summaryItem = new OwnerStatementSummaryItem();
            summaryItem.BeginBalance = ownerStatement.BeginBalance;
            summaryItem.CleaningFees = ownerStatement.CleaningFees;
            summaryItem.ManagementFees = ownerStatement.ManagementFees;
            summaryItem.UnitExpenseItems = ownerStatement.UnitExpensesTotal;
            summaryItem.TaxCollected = ownerStatement.TaxCollected;
            summaryItem.AdvancePayments = ownerStatement.AdvancementPaymentsTotal;
            summaryItem.TotalRevenue = ownerStatement.TotalRevenue;
            summaryItem.Payout = ownerStatement.Payout;
            summaryItem.EndingBalance = ownerStatement.EndingBalance;

            return summaryItem;
        }

        private OwnerStatementSummaryItem MapOwnerStatementToSummaryItem(OwnerStatement ownerStatement)
        {
            var summaryItem = new OwnerStatementSummaryItem();
            summaryItem.BeginBalance = ownerStatement.BeginBalance;
            summaryItem.CleaningFees = ownerStatement.CleaningFees;
            summaryItem.ManagementFees = ownerStatement.ManagementFees;
            summaryItem.UnitExpenseItems = ownerStatement.UnitExpenseItems;
            summaryItem.TaxCollected = ownerStatement.TaxCollected;
            summaryItem.AdvancePayments = ownerStatement.AdvancePayments;
            summaryItem.TotalRevenue = ownerStatement.TotalRevenue;
            summaryItem.Payout = 0; // no individual property payout
            summaryItem.EndingBalance = ownerStatement.Balance;

            return summaryItem;
        }

        private double GetResolutionTotal(DateTime month, string propertyCode)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@PropertyCode", SqlDbType.NVarChar);
                sqlParams[2].Value = propertyCode;

                var data = _context.Database.SqlQuery<TotalAmountModel>("GetResolutionTotal @StartDate, @EndDate, @PropertyCode", sqlParams).FirstOrDefault();
                return data == null || data.Amount == null ? 0 : data.Amount.Value;
            }
            catch (Exception ex)
            {
                throw; // let caller handle the error
            }
        }

        private bool StatementExist(OwnerStatementViewModel model)
        {
            return _context.OwnerStatements.Where(s => s.PropertyCode == model.PropertyName &&
                                                       s.Month == model.StatementMonth.Month &&
                                                       s.Year == model.StatementMonth.Year)
                                            .Count() > 0;
        }

        #endregion

        #region override to delegate CRUD operations to databae's OwnerStatement entity

        public OwnerStatement Retrieve(OwnerStatementViewModel model)
        {
            return _context.OwnerStatements.Where(s => s.PropertyCode == model.PropertyName &&
                                                       s.Month == model.StatementMonth.Month &&
                                                       s.Year == model.StatementMonth.Year)
                                           .FirstOrDefault();
        }

        public OwnerStatement Retrieve(OwnerStatement model)
        {
            return _context.OwnerStatements.Where(s => s.PropertyCode == model.PropertyCode &&
                                                       s.Month == model.Month &&
                                                       s.Year == model.Year)
                                           .FirstOrDefault();
        }

        public void MapData(OwnerStatementViewModel from, ref OwnerStatement to)
        {
            to.TotalRevenue = Math.Round(from.TotalRevenue, 2);
            to.TaxCollected = Math.Round(from.TaxCollected, 2);
            to.CleaningFees = Math.Round(from.CleaningFees, 2);
            to.ManagementFees = Math.Round(from.ManagementFees, 2);
            to.UnitExpenseItems = Math.Round(from.UnitExpensesTotal, 2);
            to.AdvancePayments = Math.Round(from.AdvancementPaymentsTotal, 2);
            to.BeginBalance = Math.Round(from.BeginBalance, 2);
            to.Balance = Math.Round(from.EndingBalance, 2);

            to.PropertyCode = from.PropertyName;
            to.Month = from.StatementMonth.Month;
            to.Year = from.StatementMonth.Year;
            to.StatementStatus = from.IsFinalized ? StatementStatus.Finalized : StatementStatus.Approved;
            to.FinalizedBy = HttpContext.Current.User.Identity.Name;
            to.FinalizedDate = from.IsFinalized ? ConversionHelper.EnsureUtcDate(new DateTime(to.Year, to.Month, 15).AddMonths(1)) : (DateTime?)null;
        }

        public void MapData(OwnerStatementSummaryItem from, ref OwnerStatement to, DateTime statementMonth, string payoutMethod)
        {
            to.TotalRevenue = Math.Round(from.TotalRevenue, 2);
            to.TaxCollected = Math.Round(from.TaxCollected, 2);
            to.CleaningFees = Math.Round(from.CleaningFees, 2);
            to.ManagementFees = Math.Round(from.ManagementFees, 2);
            to.UnitExpenseItems = Math.Round(from.UnitExpenseItems, 2);
            to.AdvancePayments = Math.Round(from.AdvancePayments, 2);
            to.BeginBalance = Math.Round(from.BeginBalance, 2);
            to.Balance = Math.Round(from.EndingBalance, 2);

            to.PropertyCode = payoutMethod;
            to.Month = statementMonth.Month;
            to.Year = statementMonth.Year;
            to.StatementStatus = StatementStatus.Finalized;
            to.FinalizedBy = HttpContext.Current.User.Identity.Name;
            to.FinalizedDate = ConversionHelper.EnsureUtcDate(DateTime.Today); // (new DateTime(to.Year, to.Month, 15).AddMonths(1));
        }

        #endregion

        #region not implemented interfaces

        public override IQueryable<OwnerStatement> GetAll()
        {
            throw new NotImplementedException();
        }

        public override void Update(string id, OwnerStatement model)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Delete(OwnerStatement model)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class TotalAmountModel
    {
        public double? Amount { get; set; }
    }

    public class SqlResultModel
    {
        public int? Result { get; set; }
    }

    public class OwnerPropertyModel
    {
        public string OwnerName { get; set; }

        public string PropertyCode { get; set; }

        public string Vertical { get; set; }

        public string Address { get; set; }
    }
}

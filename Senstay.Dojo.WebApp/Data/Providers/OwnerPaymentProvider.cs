using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class OwnerPaymentProvider
    {
        private readonly DojoDbContext _context;

        public OwnerPaymentProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        #region custom methods for owner payment

        public List<PayoutMethod> All()
        {
            try
            {
                return _context.PayoutMethods.Where(x => x.PayoutMethodName != null).OrderBy(x => x.PayoutMethodName).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(int id)
        {
            return _context.PayoutMethods.FirstOrDefault(x => x.PayoutMethodId == id) != null;
        }

        public int GetKey(PayoutMethod model)
        {
            var entity = _context.PayoutMethods.Where(x => x.PayoutMethodId == model.PayoutMethodId)
                                               .OrderBy(x => x.PayoutMethodName)
                                               .FirstOrDefault();

            return entity != null ? entity.PayoutMethodId : 0;
        }

        public PayoutMethodPaymentEditModel GetPayoutBalancesForMonth(DateTime month, int id = 0)
        {
            var paymentEditor = new PayoutMethodPaymentEditModel();
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@Month", SqlDbType.Int);
                sqlParams[0].Value = month.Month;
                sqlParams[1] = new SqlParameter("@Year", SqlDbType.Int);
                sqlParams[1].Value = month.Year;
                sqlParams[2] = new SqlParameter("@PayoutMethodId", SqlDbType.Int);
                sqlParams[2].Value = id;

                var payoutBalances = _context.Database.SqlQuery<PayoutPaymentItem>("GetPayoutBalancesForMonth @Month, @Year, @PayoutMethodId", sqlParams).ToList();
                paymentEditor.Payments = payoutBalances;
            }
            catch
            {
                throw;
            }

            return paymentEditor;
        }

        public RebalanceEditModel GetBalancesForPayoutMethod(int id, DateTime month)
        {
            var rebalanceModel = new RebalanceEditModel();
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@Month", SqlDbType.Int);
                sqlParams[0].Value = month.Month;
                sqlParams[1] = new SqlParameter("@Year", SqlDbType.Int);
                sqlParams[1].Value = month.Year;
                sqlParams[2] = new SqlParameter("@PayoutMethodId", SqlDbType.Int);
                sqlParams[2].Value = id;

                var propertyBalances = _context.Database.SqlQuery<PropertyBalanceEditModel>("GetBalancesForPayoutMethod @Month, @Year, @PayoutMethodId", sqlParams).ToList();
                if (propertyBalances != null) rebalanceModel.Balances = propertyBalances;
            }
            catch(Exception ex)
            {
                throw;
            }

            return rebalanceModel;
        }

        public object Retrieve(DateTime month)
        {
            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                var payments = _context.Database.SqlQuery<PayoutMethodPaymentViewModel>("RetrievePayoutMethodPayments @StartDate, @EndDate", sqlParams).ToList();

                // group query result into owner payout and reservation/ resolution relation
                var payoutPayments = (from p in payments
                                      group p by new // group by payout method
                                      {
                                          p.PayoutMethodId,
                                          p.PayoutMethodName,
                                          p.EffectiveDate,
                                          p.PayoutAccount,
                                          p.PayoutMethodType,
                                          p.BeginBalance,
                                          p.PayoutTotal,
                                          p.TotalBalance,
                                          p.SelectedProperties
                                      } into g
                                      orderby g.Key.PayoutMethodName
                                      select new OwnerPayoutModel(g.Key.SelectedProperties)
                                      {
                                          PayoutMethodId = g.Key.PayoutMethodId,
                                          PayoutMethodName = g.Key.PayoutMethodName,
                                          EffectiveDate = g.Key.EffectiveDate,
                                          PayoutAccount = g.Key.PayoutAccount,
                                          PayoutMethodType = g.Key.PayoutMethodType,
                                          BeginBalance = g.Key.BeginBalance,
                                          PayoutTotal = g.Key.PayoutTotal,
                                          TotalBalance = g.Key.TotalBalance,
                                          SelectedProperties = g.Key.SelectedProperties,
                                          Children = g.Select(x => new OwnerPaymentModel
                                                       {
                                                           PayoutMethodId = x.PayoutMethodId,
                                                           PayoutPaymentId = x.PayoutPaymentId,
                                                           PaymentDate = x.PaymentDate,
                                                           PaymentAmount = x.PaymentAmount,
                                                           PaymentMonth = x.PaymentMonth,
                                                           PaymentYear = x.PaymentYear,
                                                       })
                                                      .OrderByDescending(x => x.PaymentDate)
                                                      .ToList()
                                      }).ToList();

                return payoutPayments;
            }
            catch(Exception ex)
            {
                throw; // let caller handle the error
            }
        }

        public double? GetMonthlyPayout(DateTime month, string payoutMethod)
        {
            var payments = (from p in _context.PayoutPayments.Where(p => p.PaymentMonth == month.Month && p.PaymentYear == month.Year)
                            join m in _context.PayoutMethods.Where(m => m.PayoutMethodName == payoutMethod)
                            on p.PayoutMethodId equals m.PayoutMethodId
                            select p.PaymentAmount).ToList();

            return (payments != null) ? payments.Sum() : 0;
        }

        #endregion

        #region override to delegate CRUD operations to databae's Expense entity

        public PayoutMethod Retrieve(int id)
        {
            try
            {
                if (id == 0) return new PayoutMethod();

                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@PayoutMethodId", SqlDbType.Int);
                sqlParams[0].Value = id;

                return _context.Database.SqlQuery<PayoutMethod>("RetrievePayoutMethodById @PayoutMethodId", sqlParams).FirstOrDefault();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        #endregion
    }

    public class CarryOverModel
    {
        public double BeginBalance { get; set; }
        public double Balance { get; set; }
        public double PaymentAmount { get; set; }
        public double CarryOver { get; set; }
    }
}

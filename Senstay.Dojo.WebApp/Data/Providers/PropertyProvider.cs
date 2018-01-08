using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class PropertyProvider : CrudProviderBase<CPL>
    {
        private readonly DojoDbContext _context;

        public PropertyProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<CPL> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PropertyCode).OrderBy(x => x.CreatedDate).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool PropertyExist(string propertyCode)
        {
            return Retrieve(propertyCode) != null;
        }

        public List<SelectListItem> GetPropertyCodes()
        {
            return GetAll().Select(x => new SelectListItem() { Text = x.PropertyCode, Value = x.PropertyCode })
                           .OrderBy(x => x.Text)
                           .ToList();
        }

        public List<OwnerPayoutAccount> GetOwnerPayoutAccounts(DateTime? month)
        {
            try
            {
                DateTime startDate = new DateTime(2016, 1, 1);
                DateTime endDate = new DateTime(2017, 12, 31);
                if (month != null && month > DateTime.Now.AddYears(-2))
                {
                    startDate = new DateTime(month.Value.Year, month.Value.Month, 1);
                    endDate = new DateTime(month.Value.Year, month.Value.Month, DateTime.DaysInMonth(month.Value.Year, month.Value.Month));
                }
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                return _context.Database.SqlQuery<OwnerPayoutAccount>("GetOwnerPayoutAccounts @StartDate, @EndDate", sqlParams).ToList();
            }
            catch
            {
                return new List<OwnerPayoutAccount>();
            }
        }

        public List<SelectListItem> StatementPropertyCodes()
        {
            return GetAll().Where(x => x.PropertyStatus.IndexOf("Active") == 0 || x.PropertyStatus == "Inactive" || x.PropertyStatus == "Pending-Onboarding")
                           .Select(x => new SelectListItem() { Text = x.PropertyCode, Value = x.PropertyCode })
                           .ToList();
        }

        public List<PropertyWithStatus> GetPropertyCodeWithAddress(string tableName = "Reservation", DateTime? month = null)
        {
            try
            {
                DateTime startDate = new DateTime(2016, 1, 1);
                DateTime endDate = new DateTime(2050, 12, 31);
                if (month != null && month > DateTime.Now.AddYears(-2))
                {
                    startDate = new DateTime(month.Value.Year, month.Value.Month, 1);
                    endDate = new DateTime(month.Value.Year, month.Value.Month, DateTime.DaysInMonth(month.Value.Year, month.Value.Month));
                }
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@TableName", SqlDbType.NVarChar);
                sqlParams[0].Value = tableName;
                sqlParams[1] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[1].Value = startDate;
                sqlParams[2] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[2].Value = endDate;
                return _context.Database.SqlQuery<PropertyWithStatus>("GetPropertyCodeWithAddress @TableName, @StartDate, @EndDate", sqlParams).ToList();
            }
            catch
            {
                return new List<PropertyWithStatus>();
            }
        }

        public List<ConfirmationWithProperty> GetConfirmationWithPropertyCode(string text)
        {
            try
            {
                var data = _context.Reservations.Where(x => (x.ConfirmationCode.StartsWith(text) || text == "") && x.PropertyCode != AppConstants.DEFAULT_PROPERTY_CODE)
                                                .Select(x => new ConfirmationWithProperty
                                                    {
                                                        ConfirmationCode = x.ConfirmationCode,
                                                        PropertyCode = x.PropertyCode
                                                    }
                                                 )
                                                .ToList();

                return data;
                //SqlParameter[] sqlParams = new SqlParameter[1];
                //return _context.Database.SqlQuery<ConfirmationWithProperty>("GetConfirmationWithPropertyCode", sqlParams).ToList();
            }
            catch
            {
                return new List<ConfirmationWithProperty>();
            }
        }

        public List<OwnerStatementPropertyList> GetOwnerStatementPropertyList(DateTime? month)
        {
            try
            {
                DateTime startDate = new DateTime(2016, 1, 1);
                DateTime endDate = new DateTime(2017, 12, 31);
                if (month != null && month > DateTime.Now.AddYears(-2))
                {
                    startDate = new DateTime(month.Value.Year, month.Value.Month, 1);
                    endDate = new DateTime(month.Value.Year, month.Value.Month, DateTime.DaysInMonth(month.Value.Year, month.Value.Month));
                }
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                return _context.Database.SqlQuery<OwnerStatementPropertyList>("GetOwnerStatementPropertyList @StartDate, @EndDate", sqlParams).ToList();
            }
            catch
            {
                return new List<OwnerStatementPropertyList>();
            }
        }

        public List<OwnerSummaryPayoutMethodListModel> PayoutMethods(DateTime? month)
        {
            try
            {
                int year = 2017;
                int monthOfYear = 6;
                if (month != null && month > DateTime.Now.AddYears(-2))
                {
                    year = month.Value.Year;
                    monthOfYear = month.Value.Month;
                }
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@Year", SqlDbType.Int);
                sqlParams[0].Value = year;
                sqlParams[1] = new SqlParameter("@Month", SqlDbType.Int);
                sqlParams[1].Value = monthOfYear;
                return _context.Database.SqlQuery<OwnerSummaryPayoutMethodListModel>("GetOwnerSummaryPayoutMethodList @Year, @Month", sqlParams).ToList();
            }
            catch
            {
                return new List<OwnerSummaryPayoutMethodListModel>();
            }
        }

        public List<SelectListItem> AggregatedProperties()
        {
            return GetAll().Select(x => new SelectListItem() { Text = "[" + x.PropertyCode + "] " + x.AirBnBHomeName, Value = x.PropertyCode }).ToList();
        }

        public AirbnbAccountRelatedProperties GetAirbnbAccountRelatedProperties(string email)
        {
            AirbnbAccountRelatedProperties properties = new AirbnbAccountRelatedProperties();
            IQueryable<CPL> cpls = _context.CPLs.Where(o => o.Account == email);
            properties.ActiveListings = cpls.Count(o => o.PropertyStatus.IndexOf("Active") == 0);
            properties.InactiveListings = cpls.Count(o => o.PropertyStatus == "Inactive");
            properties.ListingsInLAMarket = cpls.Where(o => o.PropertyStatus.IndexOf("Active") == 0).Count(o => o.Market == "Los Angeles");
            properties.ListingsInNYCMarket = cpls.Where(o => o.PropertyStatus.IndexOf("Active") == 0).Count(o => o.Market == "New York");
            properties.PendingOnboarding = cpls.Count(o => o.PropertyStatus == "Pending - Onboarding");
            return properties;
        }

        public List<CPL> Retrieve(DateTime beginDate, DateTime endDate, bool isActive, bool isPending, bool isDead)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@IsActive", SqlDbType.Bit);
                sqlParams[2].Value = isActive;
                sqlParams[3] = new SqlParameter("@IsPending", SqlDbType.Bit);
                sqlParams[3].Value = isPending;
                sqlParams[4] = new SqlParameter("@IsDead", SqlDbType.Bit);
                sqlParams[4].Value = isDead;

                List<CPL> data = _context.Database.SqlQuery<CPL>("RetrieveSelectedProperties @StartDate, @EndDate, @IsActive, @IsPending, @IsDead", sqlParams).ToList();
                return data;
            }
            catch(Exception ex)
            {
                throw; // let caller handle the error
            }
        }

        public List<CPL> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<CPL> data = _context.Database.SqlQuery<CPL>("RetrieveProperties @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch(Exception ex)
            {
                throw ex; // let caller handle the error
            }
        }

        public string GetPropertyCodeByListing(string account, string listing)
        {
            string propertyCode = string.Empty;
            CPL property = _context.CPLs.Where(p => p.AirBnBHomeName.Contains(listing)).FirstOrDefault();
            if (property != null)
                propertyCode = property.PropertyCode;
            else // try PropertyTitleHistory table
            {
                var entity = _context.PropertyTitleHistories.FirstOrDefault(p => p.PropertyTitle.ToLower() == listing.ToLower());
                if (entity != null) propertyCode = entity.PropertyCode;
            }
            return propertyCode;
        }
    }

    public class AirbnbAccountRelatedProperties
    {
        public int? ActiveListings { get; set; }
        public int? InactiveListings { get; set; }
        public int? ListingsInLAMarket { get; set; }
        public int? ListingsInNYCMarket { get; set; }
        public int? PendingOnboarding { get; set; }
    }

    public class PropertyWithStatus
    {
        public string PropertyCode { get; set; }
        public string PropertyCodeAndAddress { get; set; }
        public int Finalized { get; set; }
        public int AllApproved { get; set; }
        public int AllReviewed { get; set; }
        public int Empty { get; set; }
    }

    public class OwnerStatementPropertyList
    {
        public string PropertyCode { get; set; }
        public string PropertyCodeWithPayoutMethod { get; set; }
        public int Finalized { get; set; }
        public int ReservationApproved { get; set; }
        public int ResolutionApproved { get; set; }
        public int ExpenseApproved { get; set; }
        public int OtherRevenueApproved { get; set; }
        public int Empty { get; set; }
    }

    public class OwnerSummaryPayoutMethodListModel
    {
        public string PayoutMethod { get; set; }
        public string PayoutMethodAndPropertyCode { get; set; }
        public int StatementFinalized { get; set; }
        public int StatementPartialFinalized { get; set; }
        public int SummaryFinalized { get; set; }
        public int Empty { get; set; }
    }

    public class ConfirmationWithProperty
    {
        public string ConfirmationCode { get; set; }
        public string PropertyCode { get; set; }
    }

    public class OwnerPayoutAccount
    {
        public string Account { get; set; }
        public int Count { get; set; }
    }
}

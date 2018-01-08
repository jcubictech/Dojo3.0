using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class InquiryProvider : CrudProviderBase<InquiriesValidation>
    {
        private readonly DojoDbContext _context;

        public InquiryProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<InquiriesValidation> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.InquiryCreatedTimestamp).OrderBy(x => x.InquiryTeam).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<InquiryViewModel> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<InquiryViewModel> data = _context.Database.SqlQuery<InquiryViewModel>("RetrieveInquiries @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<InquiryViewModel> Search(string propertyCode)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@PropertyCode", SqlDbType.VarChar);
                sqlParams[0].Value = propertyCode;


                List<InquiryViewModel> data = _context.Database.SqlQuery<InquiryViewModel>("SearchInquiries @PropertyCode", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }
        public List<InquiryViewModel> SearchID(int id)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[1];
                sqlParams[0] = new SqlParameter("@ID", SqlDbType.Int);
                sqlParams[0].Value = id;


                List<InquiryViewModel> data = _context.Database.SqlQuery<InquiryViewModel>("SearchInquiryID @ID", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(string propertyCode, string guestName, DateTime checkin, DateTime checkout)
        {
            bool exist = false;
            var count = _context.InquiriesValidations.Where(x => x.PropertyCode == propertyCode &&
                                                                 x.GuestName.ToLower() == guestName.ToLower() &&
                                                                 DbFunctions.TruncateTime(x.Check_inDate.Value) == checkin.Date &&
                                                                 DbFunctions.TruncateTime(x.Check_outDate.Value) == checkout.Date)
                                                     .Count();
            exist = count > 0;
            return exist;
        }
    }
}

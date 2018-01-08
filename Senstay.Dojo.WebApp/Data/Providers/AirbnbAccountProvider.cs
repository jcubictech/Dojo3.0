using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class AirbnbAccountProvider : CrudProviderBase<AirbnbAccount>
    {
        private readonly DojoDbContext _context;

        public AirbnbAccountProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<AirbnbAccount> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.DateAdded).OrderBy(x => x.Email).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<string> ActiveAccounts()
        {
            return GetAll().Where(x => x.Status == "Active")
                           .OrderBy(x => x.Email)
                           .Select(x => x.Email)
                           .Distinct()
                           .ToList();
        }

        public List<SelectListItem> AggregatedAccounts()
        {
            return GetAll().Select(x => new SelectListItem() { Text = "[" + x.Email + "] " + x.Name, Value = x.Email }).ToList();
        }

        public List<AirbnbAccount> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<AirbnbAccount> data = _context.Database.SqlQuery<AirbnbAccount>("RetrieveAirbnbAccounts @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class FutureRevenueProvider //: CrudProviderBase(FutureReservation)
    {
        private readonly DojoDbContext _context;

        public FutureRevenueProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<FutureRevenueViewModel> Retrieve(DateTime reportDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = reportDate;
                sqlParams[1] = new SqlParameter("@Account", SqlDbType.NVarChar);
                sqlParams[1].Value = string.Empty;

                return _context.Database.SqlQuery<FutureRevenueViewModel>("GetFutureRevenueReport @StartDate, @Account", sqlParams).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class OtherRevenueTableProvider : CrudProviderBase<OtherRevenue>
    {
        private readonly DojoDbContext _context;

        public OtherRevenueTableProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<OtherRevenue> All()
        {
            try
            {
                return _context.OtherRevenues.OrderBy(x => x.PropertyCode)
                                             .ThenByDescending(x => x.OtherRevenueDate)
                                             .ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<OtherRevenue> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                return _context.Database.SqlQuery<OtherRevenue>("RetrieveOtherRevenueFromTable @StartDate, @EndDate", sqlParams).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }
    }
}

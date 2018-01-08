using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using OfficeOpenXml;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Data.Providers
{
    public class ResolutionProvider : CrudProviderBase<Resolution>
    {
        private readonly DojoDbContext _context;

        public ResolutionProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<Resolution> All()
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[0];
                List<Resolution> data = _context.Database.SqlQuery<Resolution>("RetrieveResolutions", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public List<Resolution> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<Resolution> data = _context.Database.SqlQuery<Resolution>("RetrieveResolutions @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        #region private methods

        #endregion
    }
}

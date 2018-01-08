using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Data.Providers
{
    public class StatementReportProvider
    {
        private readonly DojoDbContext _context;

        public StatementReportProvider(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        public List<StatementReportViewModel> Retrieve(DateTime month, StatementReportType type)
        {
            var reportModel = new List<StatementReportViewModel>();

            try
            {
                DateTime startDate = new DateTime(month.Year, month.Month, 1);
                DateTime endDate = new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = startDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;
                sqlParams[2] = new SqlParameter("@ReportType", SqlDbType.Int);
                sqlParams[2].Value = (int)type;
                reportModel = _context.Database.SqlQuery<StatementReportViewModel>("RetrieveStatementReport @StartDate, @EndDate, @ReportType", sqlParams).ToList();

                return reportModel;
            }
            catch(Exception ex)
            {
                throw;
            }
        }
    }
}

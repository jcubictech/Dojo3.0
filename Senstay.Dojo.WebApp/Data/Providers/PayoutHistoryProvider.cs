using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class PayoutHistoryProvider : CrudProviderBase<PayoutHistory>
    {
        private readonly DojoDbContext _context;

        public PayoutHistoryProvider(DojoDbContext dbContext) : base(dbContext)
        {
            _context = dbContext;
        }

        public List<PayoutHistory> All()
        {
            try
            {
                return GetAll().OrderBy(x => x.PayoutMethod).ToList();
            }
            catch
            {
                throw; // let caller handle the error
            }
        }

        public bool Exist(int id)
        {
            return _context.PayoutHistories.FirstOrDefault(p => p.PayoutHistoryId == id) != null;
        }

        public double? TotalPayout(PayoutHistory model)
        {
            var payout = _context.PayoutHistories.Where(x => x.PayoutMethod == model.PayoutMethod &&
                                                        x.Month == model.Month &&
                                                        x.Year == model.Year &&
                                                        x.IsFinalized == true)
                                                 .OrderByDescending(x => x.PayoutHistoryId)
                                                 .FirstOrDefault();
            return payout != null ? payout.Amount : null;
        }

        public double? GetBeginningBalance(PayoutHistory model)
        {
            // get the previous month
            var currentMonth = new DateTime(model.Year, model.Month, 1);
            var lastMonth = currentMonth.AddMonths(-1);
            var payout = _context.PayoutHistories.Where(x => x.PayoutMethod == model.PayoutMethod &&
                                                        x.Month == lastMonth.Month &&
                                                        x.Year == lastMonth.Year &&
                                                        x.IsFinalized == true)
                                                 .OrderByDescending(x => x.PayoutHistoryId)
                                                 .FirstOrDefault();
            return payout.Amount > 0 ? 0 : payout.Amount;
        }

        public List<PayoutHistory> Retrieve(DateTime beginDate, DateTime endDate)
        {
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[2];
                sqlParams[0] = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParams[0].Value = beginDate;
                sqlParams[1] = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParams[1].Value = endDate;

                List<PayoutHistory> data = _context.Database.SqlQuery<PayoutHistory>("RetrievePayoutHistories @StartDate, @EndDate", sqlParams).ToList();
                return data;
            }
            catch
            {
                throw; // let caller handle the error
            }
        }
    }
}

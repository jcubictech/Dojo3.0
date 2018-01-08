using System;
using System.Linq;
using System.Data;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Data.Providers
{
    public class StatementCompletionProvider : CrudProviderBase<StatementCompletion>
    {
        private readonly DojoDbContext _context;

        public StatementCompletionProvider(DojoDbContext dbContext) : base(dbContext, true)
        {
            _context = dbContext;
        }

        public bool IsCompleted(DateTime month)
        {
            var entity = _context.StatementCompletions.Where(x => x.Month == month.Month && x.Year == month.Year).FirstOrDefault();
            return entity != null && entity.Completed == true;
        }

        public bool IsEditFreezed(DateTime month)
        {
            // get the edit freeze flag
            bool canFreeze = AuthorizationProvider.CanFreezeEditing(); // only FinancialAdmin can do this
            var lastMonth = DateTime.Today.AddMonths(-1);
            bool isStatementMonth = month >= (new DateTime(lastMonth.Year, lastMonth.Month, 1));
            if (canFreeze)
            {
                return !isStatementMonth && IsCompleted(month);
            }
            else
                return !isStatementMonth;
        }

        public int FreezeEditing(DateTime date, bool freeze)
        {
            var entity = _context.StatementCompletions.Where(x => x.Month == date.Month && x.Year == date.Year).FirstOrDefault();
            if (entity == null)
            {
                entity.Month = date.Month;
                entity.Year = date.Year;
                entity.Completed = freeze;
                this.Create(entity);
            }
            else
            {
                entity.Completed = freeze;
                this.Update(entity.StatementCompletionId, entity);
            }
            this.Commit();

            return entity.StatementCompletionId;
        }
    }
}
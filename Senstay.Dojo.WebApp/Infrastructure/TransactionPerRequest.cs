using System.Data.Entity;
using System.Web;
using Senstay.Dojo.Infrastructure.Tasks;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Infrastructure
{
    public class TransactionPerRequest : IRunOnEachRequest, IRunOnError, IRunAfterEachRequest
    {
        private readonly DojoDbContext _dbContext;
        private readonly HttpContextBase _httpContext;

        public TransactionPerRequest(DojoDbContext dbContext, HttpContextBase httpContext)
        {
            _dbContext = dbContext;
            _httpContext = httpContext;
        }

        void IRunOnEachRequest.Execute()
        {
            //_httpContext.Items[AppConstants.TRANSACTION_KEY] =
            //    _dbContext.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
        }

        void IRunOnError.Execute()
        {
            //_httpContext.Items[AppConstants.TRANSACTION_ERROR_KEY] = true;
        }

        void IRunAfterEachRequest.Execute()
        {
            //var transaction = (DbContextTransaction)_httpContext.Items[AppConstants.TRANSACTION_KEY];

            //if (_httpContext.Items[AppConstants.TRANSACTION_ERROR_KEY] != null)
            //{
            //    transaction.Rollback();
            //}
            //else
            //{
            //    transaction.Commit();
            //}
        }
    }
}
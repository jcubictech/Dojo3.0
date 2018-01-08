using System;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class StatementReportController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public StatementReportController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            return View();
        }

        #region client services

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(DateTime month, StatementReportType type)
        {
            var provider = new StatementReportProvider(_dbContext);
            var model = provider.Retrieve(month, type);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}

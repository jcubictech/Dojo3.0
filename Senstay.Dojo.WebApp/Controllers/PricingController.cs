using System;
using System.Web;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PricingController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PricingController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();
            return View(new AirbnbPricingViewModel());
        }

        [HttpPost]
        public ActionResult UploadPrices(AirbnbPricingViewModel form, HttpPostedFileBase attachedPricingFile)
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                if (attachedPricingFile != null)
                {
                    var provider = new AirbnbPricingProvider(_dbContext);
                    provider.UploadPrices(attachedPricingFile.InputStream);
                }
                return Json(0, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(-1, JsonRequestBehavior.AllowGet);
            }
        }

        #region helper methods

        private JsonResult Forbidden(object model = null)
        {
            string message = string.Format("User '{0}' does not have permission to access this feature.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }

        private JsonResult InternalError(string logMsg, string returnMsg, Exception ex = null, object model = null)
        {
            string message = string.Empty;
            if (ex != null)
                message = string.Format("{0} - {1}", logMsg, ex.Message + ex.StackTrace);
            else
                message = string.Format("{0}", logMsg);

            DojoLogger.Error(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
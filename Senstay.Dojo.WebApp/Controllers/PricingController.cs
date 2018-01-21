using System;
using System.Web;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Fantastic;

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
        public ActionResult UpdatePrices(AirbnbPricingViewModel form, HttpPostedFileBase attachedPricingFile)
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                int total = 0, good = 0, bad = 0;
                string message = string.Empty;
                if (attachedPricingFile != null)
                {
                    var provider = new AirbnbPricingProvider(_dbContext);
                    var priceModels = provider.ImportPricing(attachedPricingFile.InputStream);
                    var apiService = new FantasticService();
                    total = priceModels.Count;
                    foreach (var model in priceModels)
                    {
                        var response = apiService.PricePush(model);
                        if (response.success == "false")
                        {
                            bad++;
                            message = "Fantastic API call error: " + response.error;
                        }
                        else
                            good++;
                    }
                }

                var result = new { total = total, good = good, bad = bad, imported = 1, message = message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                var result = new { imported = 0, message = ex.Message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult UpdateCustomStays(AirbnbPricingViewModel form, HttpPostedFileBase attachedPricingFile)
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                int total = 0, good = 0, bad = 0;
                string message = string.Empty;
                if (attachedPricingFile != null)
                {
                    var provider = new AirbnbCustomStayProvider(_dbContext);
                    var models = provider.ImportCustomStays(attachedPricingFile.InputStream);
                    var apiService = new FantasticService();
                    total = models.Count;
                    foreach (var model in models)
                    {
                        var response = apiService.CustomStay(model);
                        if (response.success == "false")
                        {
                            bad++;
                            message = "Fantastic API call error: " + response.error;
                        }
                        else
                            good++;
                    }
                }

                var result = new { total = total, good = good, bad = bad, imported = 1, message = message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var result = new { imported = 0, message = ex.Message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        #region Testing

        public ActionResult PropertyListing()
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                var apiService = new FantasticService();
                var listing = apiService.PropertyListing(1157); // SD011
                return Json(listing, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult PriceListing()
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                var apiService = new FantasticService();
                var listing = apiService.PriceListing(1157, new DateTime(2018, 12, 17), new DateTime(2018, 12, 20)); // SD011
                return Json(listing, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult PricePush()
        {
            if (!AuthorizationProvider.CanEditPricing()) return Forbidden();

            try
            {
                var apiService = new FantasticService();
                var result = apiService.PricePush(1157, new DateTime(2018, 12, 17), new DateTime(2018, 12, 20), true, 1150, "Dojo Api call"); // SD011
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

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
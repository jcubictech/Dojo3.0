using System;
using System.Web.Mvc;
using NLog;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PropertyCodeController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyCodeController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();
            return View();
        }

        #region CRUD operations implementation

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            try
            {
                var provider = new PropertyCodeProvider(_dbContext);
                var propertyCodes = provider.GetPropertyCodeInfo();
                return Json(propertyCodes, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Retrieve Property Code fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datab source
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var codeModel = JsonConvert.DeserializeObject<PropertyCodeViewModel>(model);

            try
            {
                var dataProvider = new PropertyCodeProvider(_dbContext);
                dataProvider.Update(codeModel);
                return Json(codeModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Code Info {0:d} fails. {1} - {2}", codeModel.PropertyCode, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Property Account.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            return Json(new OtherRevenueModel(), JsonRequestBehavior.AllowGet);
        }

        private JsonResult InternalError(string logMsg, string returnMsg, Exception ex = null)
        {
            string message = string.Empty;
            if (ex != null)
                message = string.Format("{0} - {1}", logMsg, ex.Message + ex.StackTrace);
            else
                message = string.Format("{0}", logMsg);

            DojoLogger.Error(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
            return Json(new OtherRevenueModel(), JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
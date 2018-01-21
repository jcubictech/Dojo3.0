using System;
using System.Web.Mvc;
using System.Data.Entity.Migrations;
using NLog;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;
using Senstay.Dojo.Fantastic;
using System.Linq;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PropertyFantasticMapController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyFantasticMapController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();
            return RedirectToAction("Index", "PropertyAccount");
        }


        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult SyncMap()
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();

            try
            {
                var apiService = new FantasticService();
                var listingJson = apiService.PropertyListing();
                if (listingJson.total > 0)
                {
                    int changeCount = 0;
                    var dataProvider = new PropertyFantasticMapProvider(_dbContext);
                    foreach (var map in listingJson.listings)
                    {
                        changeCount += dataProvider.AddOrUpdate(map, false) == true ? 1: 0;
                    }

                    var sync = 1;
                    var message = "Total of " + changeCount.ToString() + " listing IDs are updated.";
                    if (changeCount > 0)
                    {
                        dataProvider.Commit();
                    }
                    else
                    {
                        sync = 2;
                        message = "Sync is completed. No change is needed.";
                    }
                    var result = new { sync = sync, message = message };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var result = new { sync = 3, message = "No property is available from Fantastic API service." };
                    return Json(result, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                var result = new { sync = 0, message = ex.Message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        #region CRUD operations implementation

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();

            try
            {
                var provider = new PropertyFantasticMapProvider(_dbContext);
                var data = provider.All();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Retrieve Property Title fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();

            var dataModel = JsonConvert.DeserializeObject<PropertyFantasticMap>(model);

            try
            {
                var map = new PropertyFantasticMap();
                var dataProvider = new PropertyFantasticMapProvider(_dbContext);
                map.PropertyCode = dataModel.PropertyCode;
                map.ListingId = dataModel.ListingId;
                dataProvider.Create(map);
                dataProvider.Commit();

                return Json(dataModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Creating Property Fantastic Map fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datab source
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();

            var dataModel = JsonConvert.DeserializeObject<PropertyFantasticMap>(model);

            try
            {
                var dataProvider = new PropertyFantasticMapProvider(_dbContext);
                var entity = dataProvider.Retrieve(dataModel.PropertyFantasticMapId);
                entity.PropertyCode = dataModel.PropertyCode;
                entity.ListingId = dataModel.ListingId;
                dataProvider.Update(entity.PropertyFantasticMapId, entity);
                dataProvider.Commit();

                return Json(dataModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Fantastic Map {0:d} fails. {1} - {2}", dataModel.PropertyFantasticMapId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsPricingAdmin()) return Forbidden();

            var dataModel = JsonConvert.DeserializeObject<PropertyFantasticMap>(model);

            try
            {
                var dataProvider = new PropertyFantasticMapProvider(_dbContext);
                dataProvider.Delete(dataModel.PropertyFantasticMapId);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Property Fantastic Map {0:d} fails.", dataModel.PropertyFantasticMapId), "fail", ex);
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
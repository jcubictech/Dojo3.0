﻿using System;
using System.Web.Mvc;
using NLog;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PropertyTitleController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyTitleController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();
            return RedirectToAction("Index", "PropertyAccount");
        }

        #region CRUD operations implementation

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            try
            {
                var provider = new PropertyTitleHistoryProvider(_dbContext);
                var propertyTitles = provider.All();
                return Json(propertyTitles, JsonRequestBehavior.AllowGet);
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
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var titleModel = JsonConvert.DeserializeObject<PropertyTitleHistoryRow>(model);

            try
            {
                PropertyTitleHistory titleHistory = new PropertyTitleHistory();
                var dataProvider = new PropertyTitleHistoryProvider(_dbContext);
                titleHistory.PropertyCode = titleModel.PropertyCode;
                titleHistory.PropertyTitle = titleModel.PropertyTitle.Substring(0, Math.Min(200, titleModel.PropertyTitle.Length));
                titleHistory.EffectiveDate = ConversionHelper.EnsureUtcDate(titleModel.EffectiveDate);
                dataProvider.Create(titleHistory);
                dataProvider.Commit();

                return Json(titleModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Creating Property Title fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datab source
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var titleModel = JsonConvert.DeserializeObject<PropertyTitleHistoryRow>(model);

            try
            {
                var dataProvider = new PropertyTitleHistoryProvider(_dbContext);
                var entity = dataProvider.Retrieve(titleModel.PropertyTitleHistoryId);
                entity.PropertyCode = titleModel.PropertyCode;
                entity.PropertyTitle = titleModel.PropertyTitle.Substring(0, Math.Min(200, titleModel.PropertyTitle.Length));
                entity.EffectiveDate = ConversionHelper.EnsureUtcDate(titleModel.EffectiveDate);
                dataProvider.Update(entity.PropertyTitleHistoryId, entity);
                dataProvider.Commit();

                return Json(titleModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Title {0:d} fails. {1} - {2}", titleModel.PropertyTitleHistoryId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var titleModel = JsonConvert.DeserializeObject<PropertyTitleHistory>(model);

            try
            {
                var dataProvider = new PropertyTitleHistoryProvider(_dbContext);
                dataProvider.Delete(titleModel.PropertyTitleHistoryId);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Property Title {0:d} fails.", titleModel.PropertyTitleHistoryId), "fail", ex);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using NLog;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PropertyEntityController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyEntityController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();
            return View(new List<PropertyEntityViewModel>());
        }

        #region CRUD operations implementation

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult PayoutEntityList()
        {
            var provider = new PropertyEntityProvider(_dbContext);
            var list = provider.GetAll().Select(x => x.EntityName)
                               //.Select(x => new SelectListItem
                               //{
                               //    Text = x.EntityName,
                               //    Value = x.PropertyEntityId.ToString()
                               //})
                               .ToList();

            list.Insert(0, "");
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            try
            {
                var provider = new PropertyEntityProvider(_dbContext);
                var propertyEntities = provider.All();
                return Json(propertyEntities, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Retrieving Property Entities fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var entityModel = JsonConvert.DeserializeObject<PropertyEntityViewModel>(model);

            try
            {
                var dataProvider = new PropertyEntityProvider(_dbContext);
                var entity = new PropertyEntity();
                dataProvider.MapData(entityModel, ref entity);
                dataProvider.Create(entity);
                dataProvider.Commit(); // need to commit to get the newly inserted PropertyEntityId

                entityModel.PropertyEntityId = entity.PropertyEntityId; // set the created Id to return to kendo grid

                foreach (var m in entityModel.SelectedPropertyCodes)
                {
                    _dbContext.PropertyCodePropertyEntities.Add(new PropertyCodePropertyEntity
                    {
                        PropertyEntityId = entity.PropertyEntityId,
                        PropertyCode = m.Value,
                    });
                }
                dataProvider.Commit();

                return Json(entityModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property entity {0:d} fails. {1} - {2}", entityModel.PropertyEntityId, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datab source
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var clientModel = JsonConvert.DeserializeObject<PropertyEntityViewModel>(model);

            try
            {
                var dataProvider = new PropertyEntityProvider(_dbContext);
                var entity = dataProvider.Retrieve(clientModel.PropertyEntityId);
                dataProvider.MapData(clientModel, ref entity);
                dataProvider.Update(entity.PropertyEntityId, entity);

                // update properties if changed
                var propertyToUpdate = dataProvider.PropertyToUpdate(entity.PropertyEntityId, clientModel.SelectedPropertyCodes);
                if (propertyToUpdate != null)
                {
                    _dbContext.PropertyCodePropertyEntities.RemoveRange(propertyToUpdate);
                    foreach (var m in clientModel.SelectedPropertyCodes)
                    {
                        _dbContext.PropertyCodePropertyEntities.Add(new PropertyCodePropertyEntity
                        {
                            PropertyEntityId = entity.PropertyEntityId,
                            PropertyCode = m.Value,
                        });
                    }
                }

                dataProvider.Commit();

                return Json(clientModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Entity {0:d} fails. {1} - {2}", clientModel.PropertyEntityId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var clientModel = JsonConvert.DeserializeObject<PropertyEntityViewModel>(model);

            try
            {
                var dataProvider = new PropertyEntityProvider(_dbContext);
                dataProvider.Delete(clientModel.PropertyEntityId); // will do cascade deletion of relation table
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Property Entity {0:d} fails.", clientModel.PropertyEntityId), "fail", ex);
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
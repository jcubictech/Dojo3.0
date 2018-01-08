using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
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
    public class PropertyAccountController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyAccountController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();
            return View(new List<PropertyAccountViewModel>());
        }

        #region CRUD operations implementation

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult PropertyOwnerList()
        {
            var provider = new PropertyAccountProvider(_dbContext);
            var list = provider.GetAll().Select(x => x.OwnerName)
                               //.Select(x => new SelectListItem
                               //{
                               //    Text = x.OwnerName,
                               //    Value = x.PropertyAccountId.ToString()
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
                var provider = new PropertyAccountProvider(_dbContext);
                var propertyAccounts = provider.All();
                return Json(propertyAccounts, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Retrieving Property Accounts fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var accountModel = JsonConvert.DeserializeObject<PropertyAccountViewModel>(model);

            try
            {
                var dataProvider = new PropertyAccountProvider(_dbContext);
                var entity = new PropertyAccount();
                dataProvider.MapData(accountModel, ref entity);
                dataProvider.Create(entity);
                dataProvider.Commit(); // need to commit to get the newly inserted PropertyAccountId

                accountModel.PropertyAccountId = entity.PropertyAccountId; // set the created Id to return to kendo grid

                foreach (var m in accountModel.SelectedPayoutMethods)
                {
                    _dbContext.PropertyAccountPayoutMethods.Add(new PropertyAccountPayoutMethod
                    {
                        PayoutMethodId = Int32.Parse(m.Value),
                        PropertyAccountId = entity.PropertyAccountId
                    });
                }
                dataProvider.Commit();

                return Json(accountModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Creating Property Account fails. {1} - {2}", ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datab source
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var accountModel = JsonConvert.DeserializeObject<PropertyAccountViewModel>(model);

            try
            {
                var dataProvider = new PropertyAccountProvider(_dbContext);
                var entity = dataProvider.Retrieve(accountModel.PropertyAccountId);
                dataProvider.MapData(accountModel, ref entity);
                dataProvider.Update(entity.PropertyAccountId, entity);

                // update properties if changed
                if (dataProvider.IsPayoutMethodLinkChanged(accountModel))
                {
                    // ad-hoc property codes PropertyPayoutMethods replacement
                    var oldPayoutMethods = _dbContext.PropertyAccountPayoutMethods.Where(x => x.PropertyAccountId == entity.PropertyAccountId).ToList();
                    _dbContext.PropertyAccountPayoutMethods.RemoveRange(oldPayoutMethods);
                    foreach (var m in accountModel.SelectedPayoutMethods)
                    {
                        _dbContext.PropertyAccountPayoutMethods.Add(new PropertyAccountPayoutMethod
                        {
                            PropertyAccountId = accountModel.PropertyAccountId,
                            PayoutMethodId = Int32.Parse(m.Value),
                        });
                    }
                }

                dataProvider.Commit();

                return Json(accountModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Account {0:d} fails. {1} - {2}", accountModel.PropertyAccountId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var accountModel = JsonConvert.DeserializeObject<PropertyAccountViewModel>(model);

            try
            {
                var dataProvider = new PropertyAccountProvider(_dbContext);
                dataProvider.Delete(accountModel.PropertyAccountId);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Property Account {0:d} fails.", accountModel.PropertyAccountId), "fail", ex);
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
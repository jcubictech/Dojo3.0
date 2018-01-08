using System;
using System.Linq;
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
    public class PayoutMethodController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PayoutMethodController(DojoDbContext context)
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
        public JsonResult PayoutMethodNames()
        {
            var provider = new PayoutMethodProvider(_dbContext);
            var names = provider.PayoutMethodList().Select(x => x.Text).ToList();
            names.Insert(0, "");
            return Json(names, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult PayoutMethodList()
        {
            var provider = new PayoutMethodProvider(_dbContext);
            var list = provider.PayoutMethodList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            try
            {
                var provider = new PayoutMethodProvider(_dbContext);
                var payoutMethods = provider.All();
                return Json(payoutMethods, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Retrieving payout Methods fails. {0} - {1}", ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var payoutMethodModel = JsonConvert.DeserializeObject<PayoutMethodViewModel>(model);

            try
            {
                var dataProvider = new PayoutMethodProvider(_dbContext);
                PayoutMethod entity = new PayoutMethod();
                dataProvider.MapData(payoutMethodModel, ref entity);
                dataProvider.Create(entity);
                dataProvider.Commit();

                payoutMethodModel.PayoutMethodId = entity.PayoutMethodId; // set the created Id to return to kendo grid

                foreach (var m in payoutMethodModel.SelectedPropertyCodes)
                {
                    _dbContext.PropertyPayoutMethods.Add(new PropertyPayoutMethod
                    {
                        PayoutMethodId = entity.PayoutMethodId,
                        PropertyCode = m.Value,
                    });
                }
                dataProvider.Commit();

                return Json(payoutMethodModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Expense {0:d} fails. {1} - {2}", 0, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model) // parameter must be the same json object defined in parameterMap in kendo's datasource
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var clientModel = JsonConvert.DeserializeObject<PayoutMethodViewModel>(model);

            try
            {
                var dataProvider = new PayoutMethodProvider(_dbContext);
                PayoutMethod entity = dataProvider.Retrieve(clientModel.PayoutMethodId);
                dataProvider.MapData(clientModel, ref entity);
                dataProvider.Update(entity.PayoutMethodId, entity);

                // update properties if changed
                var propertyToUpdate = dataProvider.PropertyToUpdate(entity.PayoutMethodId, clientModel.SelectedPropertyCodes);
                if (propertyToUpdate != null)
                {
                    _dbContext.PropertyPayoutMethods.RemoveRange(propertyToUpdate);
                    foreach (var newProperty in clientModel.SelectedPropertyCodes)
                    {
                        _dbContext.PropertyPayoutMethods.Add(new PropertyPayoutMethod
                        {
                            PropertyCode = newProperty.Value,
                            PayoutMethodId = entity.PayoutMethodId
                        });
                    }
                }

                dataProvider.Commit();

                return Json(clientModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Expense {0:d} fails. {1} - {2}", 0, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            var entity = JsonConvert.DeserializeObject<PayoutMethodViewModel>(model);

            try
            {
                var dataProvider = new PayoutMethodProvider(_dbContext);
                dataProvider.Delete(entity.PayoutMethodId); // will do cascade deletion of relation table
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Expense {0:d} fails.", 0), "fail", ex);
            }
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Payout Method.", this.User.Identity.Name);
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
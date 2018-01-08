using System;
using System.Web.Mvc;
using Newtonsoft.Json;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class ExpenseController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public ExpenseController(DojoDbContext context)
        {
            _dbContext = context;
        }

        #region Custom methods

        public ActionResult Index()
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            ViewBag.ReviewerClass = AuthorizationProvider.CanReviewRevenue() ? "revenue-grid-reviewer" : string.Empty;
            ViewBag.ApproverClass = AuthorizationProvider.CanApproveRevenue() ? "revenue-grid-approver" : string.Empty;
            ViewBag.FinalizerClass = AuthorizationProvider.CanFinalizeRevenue() ? "revenue-grid-finalizer" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.CanEditRevenue() ? string.Empty : " revenue-field-readonly";
            ViewBag.AdminClass = AuthorizationProvider.IsRevenueAdmin() ? "revenue-grid-remover" : string.Empty;

            return View();
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetPropertyCodeWithAddress(DateTime month)
        {
            var provider = new PropertyProvider(_dbContext);
            var data = provider.GetPropertyCodeWithAddress("Expense", month);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Combine()
        {
            return PartialView("_CombineExpensesPartial");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ExpenseRevenueProvider(_dbContext);
            var view = provider.Retrieve(month, propertyCode);
            return Json(view, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetConfirmationCodeList(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ExpenseRevenueProvider(_dbContext);
            var data = provider.GetConfirmationCodeList(month, propertyCode);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetCategoryList()
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new LookupProvider(_dbContext);
            var data = provider.GetLookupText(LookupType.ExpenseCategory);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetCombinedExpenseTree(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ExpenseRevenueProvider(_dbContext);
            var data = provider.GetCombinedExpenseTree(month, propertyCode);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateCombinedExpense(int sourceId, int targetId)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ExpenseRevenueProvider(_dbContext);
                var id = dataProvider.UpdateCombinedExpense(sourceId, targetId);
                return Json(id, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Combine Expense {0:d} into {1:d} fails. {2} - {3}", sourceId, targetId, ex.Message, innerErrorMessage);
                return InternalError(message, "-1", ex);
            }
        }

        [HttpPost]
        public JsonResult UpdateWorkflowAll(DateTime month, string propertyCode, int state, int direction)
        {
            RevenueApprovalStatus workflowState = (RevenueApprovalStatus)state;
            if (!((AuthorizationProvider.CanReviewRevenue() && workflowState == RevenueApprovalStatus.Reviewed) ||
                  (AuthorizationProvider.CanApproveRevenue() && workflowState == RevenueApprovalStatus.Approved) ||
                  (AuthorizationProvider.CanFinalizeRevenue() && workflowState == RevenueApprovalStatus.Finalized)))
            {
                return Forbidden();
            }

            try
            {
                var dataProvider = new ExpenseRevenueProvider(_dbContext);
                var nextState = dataProvider.MoveWorkflowAll(month, propertyCode, workflowState, direction);

                if (nextState != null)
                    return Json(nextState, JsonRequestBehavior.AllowGet);
                else
                    return Json("-1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change {0} Expense workflow fails for property {1}. {2}", month.ToString("MM/dd/yyyy"), propertyCode, ex.Message + ex.StackTrace);
                return InternalError(message, "-1", ex);
            }
        }

        [HttpPost]
        public JsonResult UpdateWorkflow(int id, int state, int direction)
        {
            RevenueApprovalStatus workflowState = (RevenueApprovalStatus)state;
            if (!((AuthorizationProvider.CanReviewRevenue() && workflowState == RevenueApprovalStatus.Reviewed) ||
                  (AuthorizationProvider.CanApproveRevenue() && workflowState == RevenueApprovalStatus.Approved) ||
                  (AuthorizationProvider.CanFinalizeRevenue() && workflowState == RevenueApprovalStatus.Finalized)))
            {
                return Forbidden();
            }

            try
            {
                var dataProvider = new ExpenseRevenueProvider(_dbContext);
                RevenueApprovalStatus? nextState = null;
                if (direction > 0)
                {
                    nextState = dataProvider.MoveWorkflow(id, workflowState);
                }
                else
                {
                    nextState = dataProvider.BacktrackWorkflow(id, workflowState);
                }

                if (nextState != null)
                    return Json(nextState, JsonRequestBehavior.AllowGet);
                else
                    return Json("-1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change Reservation {0} workflow fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                return InternalError(message, "-1", ex);
            }
        }

        [HttpPost]
        public JsonResult UpdateFieldStatus(int id, string field, int included)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var provider = new ExpenseRevenueProvider(_dbContext);
                var ok = provider.SetFieldStatus(id, field, (included == 1 ? true : false));
                if (ok)
                    return Json(id, JsonRequestBehavior.AllowGet);
                else
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change Expense 'IncludeInStatement' for ID = {0:d} fails. {1}", id, ex.Message + ex.StackTrace);
                return InternalError(message, string.Empty, ex);
            }
        }

        #endregion

        #region CRUD operations implementation

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var entity = JsonConvert.DeserializeObject<ExpenseRevenueModel>(model);

            try
            {
                // parameter is passed in as a model with Json string
                var dataProvider = new ExpenseRevenueProvider(_dbContext);
                dataProvider.Create(entity);
                dataProvider.Commit();

                if (entity.ExpenseId == 0) entity.ExpenseId = dataProvider.GetKey(entity);

                entity.ParentId = entity.ExpenseId;
                dataProvider.Update(entity.ExpenseId, entity);
                dataProvider.Commit();

                return Json(entity, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Expense {0:d} fails. {1} - {2}", entity.ExpenseId, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            // parameter is passed in as a model with Json string
            var entity = JsonConvert.DeserializeObject<ExpenseRevenueModel>(model);

            try
            {
                var dataProvider = new ExpenseRevenueProvider(_dbContext);
                dataProvider.Update(entity.ExpenseId, entity);
                dataProvider.Commit();
                return Json(entity, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Expense {0:d} fails. {1} - {2}", entity.ExpenseId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            // parameter is passed in as a model with Json string
            var entity = JsonConvert.DeserializeObject<ExpenseRevenueModel>(model);

            try
            {
                if (entity.Children.Count == 0)
                {
                    var dataProvider = new ExpenseRevenueProvider(_dbContext);
                    dataProvider.Delete(entity.ExpenseId);
                    dataProvider.Commit();
                }
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Expense {0:d} fails.", entity.ExpenseId), "fail", ex);
            }
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Expenses.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            return Json(new ExpenseRevenueModel(), JsonRequestBehavior.AllowGet);
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
            return Json(new ExpenseRevenueModel(), JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
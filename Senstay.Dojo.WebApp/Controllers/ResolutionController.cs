using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Newtonsoft.Json;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.Grid;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class ResolutionController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public ResolutionController(DojoDbContext context)
        {
            _dbContext = context;
        }

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

        #region Custom methods

        public JsonResult GetConfirmationWithPropertyCode(string filter)
        {
            var provider = new PropertyProvider(_dbContext);
            List<ConfirmationWithProperty> data = new List<ConfirmationWithProperty>();
            if (filter != null)
            {
                data = provider.GetConfirmationWithPropertyCode(filter);
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewRevenue(int Id)
        {
            var model = new ResolutionRevenueModel();
            model.OwnerPayoutId = Id;
            model.PropertyCode = string.Empty;
            ViewBag.Title = "New Resolution";
            ViewBag.ButtonText = "Create New Resolution";
            ViewBag.NewResolution = true;
            return PartialView("_ResolutionEditPartial", model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult EditRevenue(int Id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var provider = new ResolutionRevenueProvider(_dbContext);
            var resolution = provider.Retrieve(Id);
            ViewBag.Title = "Edit Resolution";
            ViewBag.ButtonText = "Update Resolution";
            ViewBag.NewResolution = false;
            return PartialView("_ResolutionEditPartial", resolution);
        }

        [HttpPost]
        public JsonResult SaveRevenue(ResolutionRevenueModel form)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                // Impact list is defined in lookup table; Advance Payout does not have a confirmation code
                if (form.Impact == "Advance Payout") form.ConfirmationCode = string.Empty;

                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                if (form.ResolutionId == 0) // new
                {
                    dataProvider.Create(form);
                }
                else // update
                {
                    dataProvider.Update(form.ResolutionId, form);
                }

                dataProvider.Commit(); // ReservationId will be filled for new reservation by EF

                // get the resolution ID
                if (form.ResolutionId == 0) form.ResolutionId = dataProvider.GetKey(form);

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(form.OwnerPayoutId);

                return Json(form.ResolutionId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Resolution {0} fails. {1},{2}", form.ResolutionId.ToString(), ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult DeleteRevenue(int id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(id);

                dataProvider.Delete(id);
                dataProvider.Commit();

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(entity.OwnerPayoutId);

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Delete Resolution {0} fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                return InternalError(message, "fail", ex);
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveView(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ResolutionRevenueProvider(_dbContext);
            var view = provider.Retrieve(month, propertyCode);
            return Json(view, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateWorkflowAll(DateTime month, int state, int direction)
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
                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                var nextState = dataProvider.MoveWorkflowAll(month, workflowState, direction);

                if (nextState != null)
                    return Json(nextState, JsonRequestBehavior.AllowGet);
                else
                    return Json("-1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change {0} Resolution workflow bulk update from {1:d} fails. {2}", month.ToString("MM/dd/yyyy"), state, ex.Message + ex.StackTrace);
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
                var dataProvider = new ResolutionRevenueProvider(_dbContext);
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
                var provider = new ResolutionRevenueProvider(_dbContext);
                var ok = provider.SetFieldStatus(id, field, (included == 1 ? true : false));
                if (ok)
                    return Json(id, JsonRequestBehavior.AllowGet);
                else
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change Resolution 'IncludeInStatement' for ID = {0:d} fails. {1}", id, ex.Message + ex.StackTrace);
                return InternalError(message, string.Empty, ex);
            }
        }

        #endregion

        #region CRUD operations implementation

        [HttpPost]
        public JsonResult Create(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var entity = JsonConvert.DeserializeObject<ResolutionRevenueModel>(model);

            try
            {
                if (!string.IsNullOrEmpty(entity.ConfirmationCode))
                {
                    var provider = new ReservationRevenueProvider(_dbContext);
                    var propertycode = provider.GetPropertyCodeByConfirmationCode(entity.ConfirmationCode);
                    if (!string.IsNullOrEmpty(propertycode)) entity.PropertyCode = propertycode;
                }

                // parameter is passed in as a model with Json string
                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                dataProvider.Create(entity);
                dataProvider.Commit();

                if (entity.ResolutionId == 0) entity.ResolutionId = dataProvider.GetKey(entity);

                return Json(entity, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Resolution {0:d} fails. {1} - {2}", entity.ResolutionId, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty);
            }
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            // parameter is passed in as a model with Json string
            var entity = JsonConvert.DeserializeObject<ResolutionRevenueModel>(model);

            try
            {
                if (!string.IsNullOrEmpty(entity.ConfirmationCode))
                {
                    var provider = new ReservationRevenueProvider(_dbContext);
                    var propertycode = provider.GetPropertyCodeByConfirmationCode(entity.ConfirmationCode);
                    if (!string.IsNullOrEmpty(propertycode)) entity.PropertyCode = propertycode;
                }

                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                dataProvider.Update(entity.ResolutionId, entity);
                dataProvider.Commit();

                return Json(entity, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Resolution {0:d} fails. {1} - {2}", entity.ResolutionId, ex.Message, innerErrorMessage);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            // parameter is passed in as a model with Json string
            var entity = JsonConvert.DeserializeObject<ResolutionRevenueModel>(model);
            var ownerPayoutId = entity.OwnerPayoutId;

            try
            {
                var dataProvider = new ResolutionRevenueProvider(_dbContext);
                dataProvider.Delete(entity.ResolutionId);
                dataProvider.Commit();

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(ownerPayoutId);

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return InternalError(string.Format("Delete Resolution {0:d} fails.", entity.ResolutionId), "fail", ex);
            }
        }

        #endregion

        #region report

        public ActionResult ResolutionReport()
        {
            return View();
        }

        public JsonResult Retrieve(DateTime beginDate, DateTime endDate)
        {
            ResolutionProvider dataProvider = new ResolutionProvider(_dbContext);
            var resolutions = dataProvider.Retrieve(beginDate, endDate);
            return Json(resolutions, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Resolutions.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            return Json(new ResolutionRevenueModel(), JsonRequestBehavior.AllowGet);
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
            return Json(new ResolutionRevenueModel(), JsonRequestBehavior.AllowGet);
        }

        #endregion
    }


}
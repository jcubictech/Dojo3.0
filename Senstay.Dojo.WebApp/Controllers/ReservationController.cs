using System;
using System.Web.Mvc;
using NLog;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.Grid;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class ReservationController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public ReservationController(DojoDbContext context)
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
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new PropertyProvider(_dbContext);
            var data = provider.GetPropertyCodeWithAddress("Reservation", month);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetConfirmationCode(string account)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ReservationRevenueProvider(_dbContext);
            var data = provider.GetConfirmationCode(account);
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetDuplicateReservations(DateTime month)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ReservationRevenueProvider(_dbContext);
            var model = provider.GetDuplicateReservations(month);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetMissingPropertyCodes(DateTime month)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            var provider = new ReservationRevenueProvider(_dbContext);
            var model = provider.GetMissingPropertyCodes(month);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RevenueView(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            ReservationRevenueProvider provider = new ReservationRevenueProvider(_dbContext);
            var viewModel = provider.Retrieve(month, propertyCode);

            return Json(viewModel, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult EditRevenue(int Id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var provider = new ReservationRevenueProvider(_dbContext);
            var reservation = provider.Retrieve(Id);
            ViewBag.Title = "Edit Reservation";
            ViewBag.ButtonText = string.Format("Update Reservation for Property {0}", reservation.PropertyCode);
            return PartialView("_ReservationEditPartial", reservation);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult TetrisRevenue(int Id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var model = new ResevationTetrisModel();
            var provider = new ReservationRevenueProvider(_dbContext);
            model.OldPropertyCode = provider.GetPropertyCodeById(Id);
            model.ReservationId = Id;
            ViewBag.Title = "Change Reservation";
            return PartialView("_ReservationTetrisPartial", model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult SplitRevenue(int Id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var model = new ResevationSplitModel();
            try
            {
                var provider = new ReservationRevenueProvider(_dbContext);
                var entity = provider.Retrieve(Id);
                if (entity != null)
                {
                    model.ReservationId = Id;
                    model.PropertyCode = entity.PropertyCode;
                    model.ConfirmationCode = entity.ConfirmationCode;
                    model.ReservationAmount = entity.TotalRevenue;
                    ViewBag.Title = "Split Reservation";
                }
            }
            catch
            {

            }
            return PartialView("_ReservationSplitPartial", model);
        }

        [HttpPost]
        public JsonResult SaveRevenue(ReservationRevenueModel form)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                // treat checkin and checkout date as Hawaii time zone and covert it to UTC by adding 11 hours.
                if (form.PayoutDate != null) form.PayoutDate = ConversionHelper.ToUtcFromUs(form.PayoutDate.Value);
                if (form.CheckinDate != null) form.CheckinDate = ConversionHelper.ToUtcFromUs(form.CheckinDate.Value);

                var dataProvider = new ReservationRevenueProvider(_dbContext);
                if (form.ReservationId == 0) // new reservation
                {
                    if (dataProvider.GetKey(form) != 0)
                    {
                        Response.StatusCode = (int)System.Net.HttpStatusCode.Conflict; // code = 409
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        dataProvider.Create(form);
                    }
                }
                else // updating reservation
                {
                    dataProvider.Update(form.ReservationId, form);
                }

                dataProvider.Commit(); // ReservationId will be filled for new reservation by EF

                // get the reservation ID
                if (form.ReservationId == 0) form.ReservationId = dataProvider.GetKey(form);

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(form.OwnerPayoutId);

                return Json(form.ReservationId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Reservation {0} fails. {1},{2}", form.ReservationId.ToString(), ex.Message , innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult DeleteRevenue(int id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(id);

                dataProvider.Delete(id);
                dataProvider.Commit();

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(entity.OwnerPayoutId);

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Delete Reservation {0} fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult ChangePropertyCode(ResevationTetrisModel form)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(form.ReservationId);
                entity.PropertyCode = form.NewPropertyCode;
                dataProvider.Update(form.ReservationId, entity);
                dataProvider.Commit();
                return Json(form.ReservationId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Changing Reservation {0:d} property code to {1} fails. {2},{3}", form.ReservationId, form.NewPropertyCode, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult SplitReservation(ResevationSplitModel form)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var result = dataProvider.SplitReservation(form);
                if (result != null)
                    return Json(result.Value.ToString(), JsonRequestBehavior.AllowGet);
                else
                    return Json("-1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Splitting Reservation {0:d} for property code {1} fails. {2},{3}", form.ReservationId, form.PropertyCode, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult ConvertRevenue(int id)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var reservationProvider = new ReservationRevenueProvider(_dbContext);
                var entity = reservationProvider.Retrieve(id);

                // create resolution entity
                var resolutionModel = new ResolutionRevenueModel();
                resolutionModel.ResolutionDate = entity.PayoutDate;
                resolutionModel.OwnerPayoutId = entity.OwnerPayoutId;
                resolutionModel.ConfirmationCode = entity.ConfirmationCode;
                resolutionModel.PropertyCode = entity.PropertyCode;
                resolutionModel.ResolutionType = "Cancellation";
                resolutionModel.ResolutionDescription = "Converted from Reservation";
                resolutionModel.ResolutionAmount = entity.TotalRevenue;
                resolutionModel.IncludeOnStatement = true;
                resolutionModel.Impact = string.Empty;
                resolutionModel.ApprovalStatus = RevenueApprovalStatus.NotStarted;

                var resolutionProvider = new ResolutionRevenueProvider(_dbContext);
                resolutionProvider.Create(resolutionModel);
                resolutionProvider.Commit();

                // set reservation revenue to 0 and excluded from statement
                entity.TotalRevenue = 0;
                entity.IncludeOnStatement = false;
                reservationProvider.Update(id, entity);
                reservationProvider.Commit();

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(entity.OwnerPayoutId);

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Delete Reservation {0} fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                return InternalError(message, "fail", ex);
            }
        }

        [HttpPost]
        public JsonResult UpdateFieldStatus(int id, string field, int included, double taxrate)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var provider = new ReservationRevenueProvider(_dbContext);
                var ok = provider.SetFieldStatus(id, field, taxrate, (included == 1 ? true : false));                  
                if (ok)
                    return Json(id, JsonRequestBehavior.AllowGet);
                else
                    return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change Reservation 'IncludeInStatement' for ID = {0:d} fails. {1}", id, ex.Message + ex.StackTrace);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult SaveNote(int id, string note)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            try
            {
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(id);
                entity.ApprovedNote = note;
                dataProvider.Update(id, entity);
                dataProvider.Commit();
                return Json(id.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Reservation {0:d} fails. {1},{2}", id, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            if (!AuthorizationProvider.CanEditRevenue()) return Forbidden();

            var codeModel = JsonConvert.DeserializeObject<MissingPropertyCodesModel>(model);

            try
            {
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(codeModel.ReservationId);
                entity.PropertyCode = codeModel.PropertyCode;
                dataProvider.Update(codeModel.ReservationId, entity);
                dataProvider.Commit();
                return Json(codeModel, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Property Code for Reservation {0:d} fails. {1},{2}", codeModel.ReservationId, ex.Message, innerErrorMessage);
                return InternalError(message, string.Empty, ex);
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult DetailInfo(int id)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            ViewBag.EditClass = " app-field-readonly";
            var details = new ReservationRevenueModel();
            try
            {
                var provider = new ReservationRevenueProvider(_dbContext);

                ViewBag.Title = "View Reservation Details";
                details = provider.Retrieve(id);
                if (details == null) details = new ReservationRevenueModel();
                // append property code to confirmation code for view-only mode
                if (!string.IsNullOrEmpty(details.PropertyCode))
                {
                    if (string.IsNullOrEmpty(details.ConfirmationCode)) details.ConfirmationCode = string.Empty;
                    details.ConfirmationCode += " | " + details.PropertyCode;
                }

                return PartialView("_ReservationEditPartial", details);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Reservation Details fails. {0}", ex.Message);
                DojoLogger.Error(message, typeof(PropertyController));
            }

            return PartialView("_ReservationEditPartial", details);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult DetailInfoByConfirmationCode(int id, string data)
        {
            var provider = new ReservationRevenueProvider(_dbContext);
            int reservationId = provider.GetIdByConfirmationCode(data);
            return DetailInfo(reservationId);
        }

        #endregion

        #region Approval workflow

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
                var dataProvider = new ReservationRevenueProvider(_dbContext);
                var nextState = dataProvider.MoveWorkflowAll(month, propertyCode, workflowState, direction);

                if (nextState != null)
                    return Json(nextState, JsonRequestBehavior.AllowGet);
                else
                    return Json("-1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Change {0} Reservation workflow fails for property {1}. {2}", month.ToString("MM/dd/yyyy"), propertyCode, ex.Message + ex.StackTrace);
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
                var dataProvider = new ReservationRevenueProvider(_dbContext);
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

        #endregion

        #region report

        public ActionResult ReservationReport()
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();
            return View();
        }

        public JsonResult Retrieve(DateTime beginDate, DateTime endDate)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            ReservationProvider dataProvider = new ReservationProvider(_dbContext);
            var reservations = dataProvider.Retrieve(beginDate, endDate);
            return Json(reservations, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region private methods

        private void SetRelatedFields(ReservationRevenueModel form)
        {
            // these are fields that initialized by app internally
            //form.OwnerPayoutId = 0;
            form.ApprovalStatus = (int)RevenueApprovalStatus.NotStarted;
            form.Reviewed = false;
            form.Approved = false;
            form.Finalized = false;
        }

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Reservation.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            return Json(new ReservationRevenueModel(), JsonRequestBehavior.AllowGet);
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
            return Json(new ReservationRevenueModel(), JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web;
using Newtonsoft.Json;
using NLog;
using Google.Apis.Drive.v3;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Infrastructure.Alerts;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.App.Helpers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.HelperClass;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.Grid;
using Senstay.Dojo.FtpClient;
using Senstay.Dojo.Airbnb.Import;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class OwnerPayoutController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public OwnerPayoutController(DojoDbContext context)
        {
            _dbContext = context;
        }

        #region Owner Payout Revenue

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
        public ActionResult OwnerPayoutView(DateTime month, string source, int ownerPayoutId)
        {
            if (!AuthorizationProvider.CanViewRevenue()) return Forbidden();

            ViewBag.ReviewerClass = AuthorizationProvider.CanReviewRevenue() ? "revenue-grid-reviewer" : string.Empty;
            ViewBag.ApproverClass = AuthorizationProvider.CanApproveRevenue() ? "revenue-grid-approver" : string.Empty;
            ViewBag.FinalizerClass = AuthorizationProvider.CanFinalizeRevenue() ? "revenue-grid-finalizer" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.CanEditRevenue() ? string.Empty : " revenue-field-readonly";
            ViewBag.AdminClass = AuthorizationProvider.IsRevenueAdmin() ? "revenue-grid-remover" : string.Empty;

            var model = new ReservationRevenueModel();
            model.Month = month;
            model.Source = source;
            model.OwnerPayoutId = ownerPayoutId;

            return View(model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RevenueView(DateTime month, string source)
        {
            OwnerPayoutRevenueProvider provider = new OwnerPayoutRevenueProvider(_dbContext);
            var view = provider.Retrieve(month, source);
            return Json(view, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddReservation(int Id)
        {
            var model = new ReservationRevenueModel();
            model.OwnerPayoutId = Id;
            model.ReservationId = 0;
            model.PropertyCode = string.Empty;
            model.InputSource = "system";
            return PartialView("_NewReservationPartial", model);
        }

        public JsonResult SaveReservation(ReservationRevenueModel form)
        {
            // delegate to reservation controller service to save
            var delegatedController = DependencyResolver.Current.GetService<ReservationController>();
            delegatedController.ControllerContext = ControllerContext;
            var result = delegatedController.SaveRevenue(form);

            // return the owner payout Id instead of reservation Id
            try
            {
                int id = int.Parse(result.Data.ToString());
                return Json(form.OwnerPayoutId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return result;
            }
        }

        public JsonResult SaveResolution(ResolutionRevenueModel form)
        {
            // delegate to resolution controller service to save
            var delegatedController = DependencyResolver.Current.GetService<ResolutionController>();
            delegatedController.ControllerContext = ControllerContext;
            var result = delegatedController.SaveRevenue(form);

            // return the owner payout Id instead of resolution Id
            try
            {
                int id = int.Parse(result.Data.ToString());
                return Json(form.OwnerPayoutId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return result;
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult EditRevenue(int Id)
        {
            if (!AuthorizationProvider.CanEditRevenue())
            {
                string message = string.Format("User '{0}' does not have permission to edit Owner Payout.", this.User.Identity.Name);
                DojoLogger.Warn(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            var provider = new OwnerPayoutRevenueProvider(_dbContext);
            var entity = provider.Retrieve(Id);
            ViewBag.Title = "Edit Owner Payout";
            ViewBag.ButtonText = Id == 0 ? "Create Owner Payout" : "Update Owner Payout";
            return PartialView("_OwnerPayoutEditPartial", entity);
        }

        [HttpPost]
        public JsonResult SaveRevenue(OwnerPayoutRevenueModel form)
        {
            if (!AuthorizationProvider.CanEditRevenue())
            {
                string message = string.Format("User '{0}' does not have permission to save Owner Payout {1}.", this.User.Identity.Name, form.OwnerPayoutId.ToString());
                DojoLogger.Warn(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // treat checkin and checkout date as Hawaii time zone and covert it to UTC by adding 11 hours.
                if (form.PayoutDate != null) form.PayoutDate = ConversionHelper.ToUtcFromUs(form.PayoutDate.Value);

                var dataProvider = new OwnerPayoutRevenueProvider(_dbContext);

                if (form.OwnerPayoutId == 0) // new OwnerPayout
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
                else // updating OwnerPayout
                {
                    dataProvider.Update(form.OwnerPayoutId, form);
                }

                dataProvider.Commit(); // OwnerPayoutId will be filled for new OwnerPayout by EF

                // get the OwnerPayout ID
                if (form.OwnerPayoutId == 0) form.OwnerPayoutId = dataProvider.GetKey(form);

                var provider = new OwnerPayoutProvider(_dbContext);
                provider.UpdateOwnerPayoutMatchStatus(form.OwnerPayoutId);

                return Json(form.OwnerPayoutId.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Owner Payout {0} fails. {1},{2}", form.OwnerPayoutId.ToString(), ex.Message, innerErrorMessage);
                DojoLogger.Error(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult DeleteRevenue(int id)
        {
            if (!AuthorizationProvider.CanEditRevenue())
            {
                string message = string.Format("User '{0}' does not have permission to delete Owner Payout {1:d}.", this.User.Identity.Name, id);
                DojoLogger.Warn(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var dataProvider = new OwnerPayoutRevenueProvider(_dbContext);
                dataProvider.Delete(id);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Delete Owner Payout {0} fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("fail", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SavePayoutAmount(int id, float amount)
        {
            if (!AuthorizationProvider.CanEditRevenue())
            {
                string message = string.Format("User '{0}' does not have permission to save Owner Payout {1}.", this.User.Identity.Name, id.ToString());
                DojoLogger.Warn(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var dataProvider = new OwnerPayoutRevenueProvider(_dbContext);
                var entity = dataProvider.Retrieve(id);
                entity.PayoutAmount = amount;
                dataProvider.Update(id, entity);

                dataProvider.Commit(); // OwnerPayoutId will be filled for new OwnerPayout by EF

                return Json(id.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var innerErrorMessage = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                string message = string.Format("Saving Owner Payout {0} fails. {1},{2}", id.ToString(), ex.Message, innerErrorMessage);
                DojoLogger.Error(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ShowOwnerPayoutById(int id)
        {
            if (!AuthorizationProvider.CanViewRevenue())
            {
                string message = string.Format("User '{0}' does not have permission to view Owner Payout.", this.User.Identity.Name);
                DojoLogger.Warn(message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            var provider = new OwnerPayoutRevenueProvider(_dbContext);
            var entity = provider.Retrieve(id);
            ViewBag.Title = "View Owner Payout";
            ViewBag.ButtonText = "View Owner Payout";
            return PartialView("_OwnerPayoutEditPartial", entity);
        }

        #endregion

        #region owner payout report

        public ActionResult OwnerPayoutReport()
        {
            return View();
        }

        public JsonResult Retrieve(DateTime beginDate, DateTime endDate)
        {
            OwnerPayoutProvider dataProvider = new OwnerPayoutProvider(_dbContext);
            var ownerPayouts = dataProvider.Retrieve(beginDate, endDate);
            return Json(ownerPayouts, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Owner Payout.", this.User.Identity.Name);
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

    class ImportResult
    {
        public int OK { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
    }
}
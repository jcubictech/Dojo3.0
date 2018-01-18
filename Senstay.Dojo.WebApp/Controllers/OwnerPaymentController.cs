using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Models.Grid;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class OwnerPaymentController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public OwnerPaymentController(DojoDbContext context)
        {
            _dbContext = context;
        }

        #region Custom methods

        public ActionResult Index()
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            ViewBag.ReviewerClass = AuthorizationProvider.CanReviewRevenue() ? "revenue-grid-reviewer" : string.Empty;
            ViewBag.ApproverClass = AuthorizationProvider.CanApproveRevenue() ? "revenue-grid-approver" : string.Empty;
            ViewBag.FinalizerClass = AuthorizationProvider.CanFinalizeRevenue() ? "revenue-grid-finalizer" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.CanEditStatement() ? string.Empty : " revenue-field-readonly";
            ViewBag.AdminClass = AuthorizationProvider.IsRevenueAdmin() ? "revenue-grid-remover" : string.Empty;
            ViewBag.CanFreezeEditing = AuthorizationProvider.CanFreezeEditing() ? true : false;
            ViewBag.StatementCompleted = (new StatementCompletionProvider(_dbContext)).IsCompleted(DateTime.Today);

            return View();
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult PaymentEditor(DateTime month)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();
            try
            {
                var provider = new OwnerPaymentProvider(_dbContext);
                var editor = provider.GetPayoutBalancesForMonth(month);
                return PartialView("_PayoutMethodPaymentPartial", editor);
            }
            catch
            {
                return PartialView("_PayoutMethodPaymentPartial", new PayoutMethodPaymentEditModel());
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult RebalanceEditor(int id, DateTime data)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();
            try
            {
                var provider = new OwnerPaymentProvider(_dbContext);
                var editor = provider.GetBalancesForPayoutMethod(id, data);
                return PartialView("_RebalancePartial", editor);
            }
            catch
            {
                return PartialView("_RebalancePartial", new RebalanceEditModel());
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrievePayoutMethodPayments(DateTime month)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var provider = new OwnerPaymentProvider(_dbContext);
                var model = provider.Retrieve(month);
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new PayoutMethodPaymentViewModel(), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult RedistributeBalance(List<PropertyBalance> balances)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var provider = new PropertyBalanceProvider(_dbContext);
                provider.RedistributeBalance(balances);
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult UpdateBalances(DateTime month)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var provider = new PropertyBalanceProvider(_dbContext);
                provider.UpdateNextMonthBalances(month);
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult IsEditFreezed(DateTime month)
        {
            try
            {
                // get the edit freeze flag
                var freezeProvider = new StatementCompletionProvider(_dbContext);
                int editFreezedFlag = !freezeProvider.Exist(month) ? -1 : freezeProvider.IsEditFreezed(month) ? 1 : 0;
                return Json(editFreezedFlag, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult FreezeEditing(DateTime month, bool freeze)
        {
            if (!AuthorizationProvider.CanFreezeEditing()) return Forbidden();

            try
            {
                var provider = new StatementCompletionProvider(_dbContext);
                provider.FreezeEditing(month, freeze);
                return Json(freeze ? "0" : "1", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region CRUD operations implementation

        public JsonResult SavePayoutMethodPayments(List<PayoutPayment> payments)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                // create/update payments
                var provider = new PayoutPaymentProvider(_dbContext);
                provider.SavePayoutMethodPayments(payments);

                // add a statement completion record for the payment month
                if (payments != null && payments.Count > 0)
                {
                    var nextMonth = (new DateTime(payments[0].PaymentYear, payments[0].PaymentMonth, 1)).AddMonths(1);
                    var completionProvider = new StatementCompletionProvider(_dbContext);
                    completionProvider.New(nextMonth);
                }

                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult RedistributeBalances(List<PropertyBalance> balances)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var provider = new PropertyBalanceProvider(_dbContext);
                provider.RedistributeBalance(balances);
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden()
        {
            string message = string.Format("User '{0}' does not have permission to access Owner Payment.", this.User.Identity.Name);
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
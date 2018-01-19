using System;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Infrastructure.ModelMetadata;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class OwnerStatementController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public OwnerStatementController(DojoDbContext context)
        {
            _dbContext = context;
            ViewBag.EditClass = AuthorizationProvider.CanEditStatement() ? string.Empty : " revenue-field-readonly";
            bool isFreezed = (new StatementCompletionProvider(_dbContext)).IsCompleted(DateTime.Today);
            ViewBag.EditFreezed = isFreezed ? "edit-freezed" : string.Empty;
        }

        #region Owner Statement Views

        public ActionResult Index()
        {
            return RedirectToAction("Statement", "OwnerStatement");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Statement()
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden();
            return View("OwnerStatement", new OwnerStatementViewModel(_dbContext));
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult StatementLink(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden();

            var tokens = propertyCode.Split(new char[] { '-' });
            if (tokens.Length > 0) propertyCode = tokens[0].Trim();
            var provider = new OwnerStatementProvider(_dbContext);
            var viewModel = provider.GetOwnerStatement(month, propertyCode);
            return View("OwnerStatement", viewModel);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult OwnerStatement(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden();

            try
            {
                var provider = new OwnerStatementProvider(_dbContext);
                var viewModel = provider.GetOwnerStatement(month, propertyCode);

                // get the edit freeze flag
                viewModel.IsEditFreezed = (new StatementCompletionProvider(_dbContext)).IsEditFreezed(month);

                // statement owner can only see own statement and summary
                if (AuthorizationProvider.IsStatementOwner() && !AuthorizationProvider.IsStatementAdmin() && !AuthorizationProvider.IsStatementViewer())
                {
                    // TODO: filter the viewModel for the owner account
                }
                return PartialView("_StatementPartial", viewModel);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(false, JsonRequestBehavior.AllowGet);

            }
        }

        #endregion

        #region Owner Statement Methods

        [HttpPost]
        public JsonResult Finalize(DateTime month, string propertyCode, string note, bool isFinalized = true)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var provider = new OwnerStatementProvider(_dbContext);
                var viewModel = provider.GetOwnerStatement(month, propertyCode);
                var result = provider.FinalizeStatement(month, propertyCode, note, viewModel.EndingBalance, isFinalized);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult IsFinalized(DateTime date, string propertyCode)
        {
            try
            {
                var statementProvider = new OwnerStatementProvider(_dbContext);
                var finalizeFlag = statementProvider.IsFinalized(date, propertyCode) == true ? "1" : "0";
                return Json(finalizeFlag, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("0", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [LayoutInjecter("_Print")]
        public ActionResult PrintStatement(DateTime month, string propertyCode)
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden();

            try
            {
                var provider = new OwnerStatementProvider(_dbContext);
                var viewModel = provider.GetOwnerStatement(month, propertyCode);
                viewModel.IsPrint = true;
                return View(viewModel);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Owner Summary Views

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Summary()
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden();

            ViewBag.AllowEditClass = !AuthorizationProvider.CanViewStatement() ? "hide" : string.Empty;
            return View("OwnerSummary");
        }

        public ActionResult OwnerSummaryView(DateTime month, string payoutMethod)
        {
            var model = new OwnerStatementSummaryModel();
            model.Month = month;
            model.PayoutMethod = payoutMethod;

            return View(model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult SummaryRebalance(DateTime month, string payoutMethod)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            var model = new OwnerSummaryRebalanceModel
            {
                PayoutMethod = payoutMethod,
                PayoutMonth = month.Month,
                PayoutYear = month.Year,
                RebalanceDate = month,
                SummaryProperties = new PayoutMethodProvider(_dbContext).PropertyList(payoutMethod, month),
                PropertyBalances = new PayoutMethodProvider(_dbContext).PropertyBalances(payoutMethod, month)
            };

            return PartialView("_RebalancePartial", model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult OwnerSummary(DateTime month, string payoutMethod)
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden(new OwnerStatementSummaryModel());

            var provider = new OwnerStatementProvider(_dbContext);
            var viewModel = provider.GetOwnerSummary(month, payoutMethod, false);

            // get the edit freeze flag
            viewModel.IsEditFreezed = (new StatementCompletionProvider(_dbContext)).IsEditFreezed(month);

            return PartialView("_SummaryPartial", viewModel);
        }

        #endregion

        #region Owner Summary Methods

        [HttpPost]
        public JsonResult RebalanceSummary(SummaryRebalanceTransactionModel model)
        {
            if (!AuthorizationProvider.IsStatementAdmin()) return Forbidden();

            try
            {
                var dataProvider = new RebalanceProvider(_dbContext);
                dataProvider.RebalanceSummaryWithNoGroup(model);
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("fail", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult FinalizeSummary(DateTime month, string payoutMethod, string note, bool isFinalized = true)
        {
            if (!AuthorizationProvider.CanEditStatement()) return Forbidden();

            try
            {
                var statementProvider = new OwnerStatementProvider(_dbContext);
                statementProvider.FinalizeSummary(month, payoutMethod, note, isFinalized);
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("fail", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [LayoutInjecter("_Print")]
        public ActionResult PrintSummary(DateTime month, string payoutMethod)
        {
            if (!AuthorizationProvider.CanViewStatement()) return Forbidden(new OwnerStatementSummaryModel());

            try
            {
                var provider = new OwnerStatementProvider(_dbContext);
                var viewModel = provider.GetOwnerSummary(month, payoutMethod, false);
                viewModel.IsPrint = true;
                return View(viewModel);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(new OwnerStatementSummaryModel(), JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region Imports

        public ActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public JsonResult BackFillStatements(DateTime month)
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            try
            {
                var statementProvider = new OwnerStatementProvider(_dbContext);
                var propertyProvider = new PropertyProvider(_dbContext);
                var properties = propertyProvider.All();
                foreach (CPL property in properties)
                {
                    if (property.PropertyStatus != "Dead")
                    {
                        try
                        {
                            // create/update owner statement record
                            var model = statementProvider.GetOwnerStatement(month, property.PropertyCode);
                            model.PropertyName = model.PropertyName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                            var entity = statementProvider.Retrieve(model);
                            if (entity != null)
                            {
                                statementProvider.MapData(model, ref entity);
                                statementProvider.Update(entity.OwnerStatementId, entity);
                            }
                            else
                            {
                                entity = new OwnerStatement();
                                statementProvider.MapData(model, ref entity);
                                statementProvider.Create(entity);
                            }
                            statementProvider.Commit();
                        }
                        catch(Exception ex)
                        {
                            return Json(ex.Message, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                return Json("success-Statement", JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult BackFillOwnerSummaries(DateTime month)
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            try
            {
                var summaryProvider = new OwnerStatementProvider(_dbContext);
                var paymentProvider = new OwnerPaymentProvider(_dbContext);
                var payoutMethods = paymentProvider.All();
                foreach (PayoutMethod method in payoutMethods)
                {
                    if (method.PayoutMethodName != null)
                    {
                        try
                        {
                            var summary = summaryProvider.GetOwnerSummary(month, method.PayoutMethodName, true);
                            var model = new OwnerStatement();
                            summaryProvider.MapData(summary.ItemTotal, ref model, month, method.PayoutMethodName);

                            var entity = summaryProvider.Retrieve(model);
                            if (entity != null)
                            {
                                model.OwnerStatementId = entity.OwnerStatementId;
                                summaryProvider.Update(model.OwnerStatementId, model);
                            }
                            else
                            {
                                summaryProvider.Create(model);
                            }
                            summaryProvider.Commit();
                        }
                        catch (Exception ex)
                        {
                            return Json(ex.Message, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                return Json("success-Summary", JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden(object model = null)
        {
            string message = string.Format("User '{0}' does not have permission to perofrm this action for Owner Statement.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }

        private JsonResult InternalError(string logMsg, string returnMsg, Exception ex = null, object model = null)
        {
            string message = string.Empty;
            if (ex != null)
                message = string.Format("{0} - {1}", logMsg, ex.Message + ex.StackTrace);
            else
                message = string.Format("{0}", logMsg);

            DojoLogger.Error(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
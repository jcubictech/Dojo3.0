using System;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class ReportController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public ReportController(DojoDbContext context)
        {
            _dbContext = context;
        }

        #region Report views

        public ActionResult Index()
        {
            return RedirectToAction("Property");
        }

        public ActionResult Property()
        {
            return View();
        }

        public ActionResult AirbnbAccount()
        {
            return View();
        }

        public ActionResult Inquiry()
        {
            return View();
        }

        public ActionResult FutureRevenue()
        {
            return View();
        }

        #endregion

        #region Client side ajax methods

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveProperties(DateTime beginDate, DateTime endDate)
        {
            PropertyProvider dataProvider = new PropertyProvider(_dbContext);
            var Properties = dataProvider.Retrieve(beginDate, endDate);
            return Json(Properties, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveAirbnbAccounts(DateTime beginDate, DateTime endDate)
        {
            AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
            var AirbnbAccounts = dataProvider.Retrieve(beginDate, endDate);
            return Json(AirbnbAccounts, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveInquiries(DateTime beginDate, DateTime endDate)
        {
            InquiryProvider dataProvider = new InquiryProvider(_dbContext);
            var Inquiries = dataProvider.Retrieve(beginDate, endDate);
            return Json(Inquiries, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveFutureRevenue()
        {
            var dataProvider = new FutureRevenueProvider(_dbContext);
            var futureRevenue = dataProvider.Retrieve(DateTime.Today.Date);
            return Json(futureRevenue, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}

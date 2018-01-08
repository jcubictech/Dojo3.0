using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using AutoMapper;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Infrastructure.Alerts;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class SearchInquiryController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger RDTLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public SearchInquiryController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            return View();
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Search(string propertyCode)
        {
            InquiryProvider dataProvider = new InquiryProvider(_dbContext);
            var inquiries = dataProvider.Search(propertyCode);
            return Json(inquiries, JsonRequestBehavior.AllowGet);
        }
        public JsonResult SearchID(int id)
        {
            InquiryProvider dataProvider = new InquiryProvider(_dbContext);
            var inquiries = dataProvider.SearchID(id);
            return Json(inquiries, JsonRequestBehavior.AllowGet);
        }
    }
}

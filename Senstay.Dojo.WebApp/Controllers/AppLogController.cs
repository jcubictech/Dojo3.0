using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class AppLogController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;

        public AppLogController(DojoDbContext context)
        {
            _dbContext = context;
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            var logs = _dbContext.DojoLogs.OrderByDescending(l => l.EventDateTime).Take(500).ToList();
            return Json(logs, JsonRequestBehavior.AllowGet);
        }
    }
}
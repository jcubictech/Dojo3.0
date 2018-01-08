using System.Web.Mvc;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class AdminController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;

        public AdminController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Users()
        {
            return View("UserManagement");
        }

        [Authorize(Roles = AppConstants.SUPER_ADMIN_ROLE)]
        public ActionResult Roles()
        {
            return View("RoleManagement");
        }

        public ActionResult UserRoles()
        {
            return View("UserRoleManagement");
        }

        public ActionResult UserInvitation()
        {
            return View();
        }

        public ActionResult LookupTables()
        {
            return View();
        }

        public ActionResult ApplicationLog()
        {
            return View("AppLog");
        }

        // Google API test menu
        public ActionResult GoogleConnection()
        {
            return View();
        }
    }
}
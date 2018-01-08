using System.Web.Mvc;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Controllers
{
    public class HomeController : AppBaseController
    {
        public ActionResult Index()
        {
            if (!AuthorizationProvider.IsAuthenticated())
                return RedirectToAction("Login", "Account", "/");
            else
            {
                return View();
            }
        }

        public ActionResult Maintenance()
        {
            return View(new MaintenanceViewModel());
        }
    }
}
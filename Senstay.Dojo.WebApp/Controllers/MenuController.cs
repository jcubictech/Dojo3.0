using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Controllers
{
    public class MenuController : AppBaseController
    {
        private readonly DojoDbContext _context;

        public MenuController(DojoDbContext context)
        {
            _context = context;
        }

        public ActionResult Index()
        {
            return RedirectToAction("Route", "Error"); // not supported
        }

        [OutputCache(Duration = 3600)]
        public ActionResult Install()
        {
            IDataProvider xmlRetriever = new MenuContentProvider();
            object xmlData = xmlRetriever.Read();
            if (xmlData is List<MenuViewModel>)
            {
                List<MenuViewModel> viewModel = (List<MenuViewModel>)xmlData;
                return PartialView("_MenuPartial", viewModel);
            }
            else
                return PartialView(string.Empty);
        }
    }
}
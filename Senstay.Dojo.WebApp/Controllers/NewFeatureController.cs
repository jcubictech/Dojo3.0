using System;
using System.Web.Mvc;
using System.Linq;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    public class NewFeatureController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;

        public NewFeatureController(DojoDbContext context)
        {
            _dbContext = context;
        }

        // need to allow annonymous user to access to this method for javascript in menu to work without hard refreach
        [AllowAnonymous]
        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Announcement(string deployDate = null)
        {
            // return a empty feature for annonymous user; client side needs to have logic to handle this
            if (!AuthorizationProvider.IsAuthenticated())
                return Json(new NewFeature(), JsonRequestBehavior.AllowGet);

            NewFeatureProvider provider = new NewFeatureProvider(_dbContext);
            NewFeature newFeature = provider.Get(deployDate);
            return Json(newFeature, JsonRequestBehavior.AllowGet);
        }
    }
}
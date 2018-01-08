using System;
using System.Web.Mvc;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    public class ProxyController : Controller
    {
        public ProxyController()
        {
        }

        [HttpPost]
        public ActionResult Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);
            return File(fileContents, contentType, fileName);
        }
    }
}
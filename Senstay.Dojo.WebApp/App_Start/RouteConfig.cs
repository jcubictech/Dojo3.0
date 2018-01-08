using System.Web.Mvc;
using System.Web.Routing;

namespace Senstay.Dojo
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "CPL",
                url: "property/{action}",
                defaults: new { action = "Index", controller = "Property" }
            );

            routes.MapRoute(
                name: "InquiriesValidation",
                url: "inquiry/{action}",
                defaults: new { action = "Index", controller = "Inquiry" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );


        }
    }
}

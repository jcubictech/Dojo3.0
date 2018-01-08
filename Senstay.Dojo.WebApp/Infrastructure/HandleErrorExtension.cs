using System;
using System.Web;
using System.Web.Mvc;

namespace Senstay.Dojo.Infrastructure
{
    public class CustomHandleErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled) return; // || !filterContext.HttpContext.IsCustomErrorEnabled

            if (new HttpException(null, filterContext.Exception).GetHttpCode() != 500)
            {
                filterContext.HttpContext.Response.Redirect(String.Format("~/Error/Application"));
            }

            if (!ExceptionType.IsInstanceOfType(filterContext.Exception))
            {
                filterContext.HttpContext.Response.Redirect(String.Format("~/Error/Application"));
            }

            // if the request is AJAX return JSON else view.
            if (IsAjaxRequest(filterContext))
            {
                // return Json to client as it is a ajax request
                filterContext.Result = new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new
                    {
                        action = "redirect",
                        controller = "error",
                        page = "application"
                    }
                };

                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.Clear();
            }
            else
            {
                // Normal Exception
                base.OnException(filterContext);

            }

            // TODO: Write error logging code ...

            //if want to get to the source of the request
            //var currentController = (string)filterContext.RouteData.Values["controller"];
            //var currentActionName = (string)filterContext.RouteData.Values["action"];
        }

        private bool IsAjaxRequest(ExceptionContext filterContext)
        {
            return filterContext.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Net;
using System.Configuration;
using Microsoft.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure.ActionResults;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Infrastructure
{
    public abstract class AppBaseController : Controller
    {
        public AppBaseController()
        {
        }

        protected ActionResult RedirectToAction<TController>(Expression<Action<TController>> action)
            where TController : Controller
        {
            return ControllerExtensions.RedirectToAction(this, action);
        }

        [Obsolete("Do not use the standard Json helpers to return JSON data to the client.  Use either JsonSuccess or JsonError instead.")]
        protected JsonResult Json<T>(T data)
        {
            throw new InvalidOperationException("Do not use the standard Json helpers to return JSON data to the client.  Use either JsonSuccess or JsonError instead.");
        }

        protected StandardJsonResult JsonValidationError()
        {
            var result = new StandardJsonResult();

            foreach (var validationError in ModelState.Values.SelectMany(v => v.Errors))
            {
                result.AddError(validationError.ErrorMessage);
            }
            return result;
        }

        protected StandardJsonResult JsonError(string errorMessage)
        {
            var result = new StandardJsonResult();

            result.AddError(errorMessage);

            return result;
        }

        protected StandardJsonResult<T> JsonSuccess<T>(T data)
        {
            return new StandardJsonResult<T> { Data = data };
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            // format MVC exceptions to a normalized JSON message for our AJAX calls
            if (filterContext.HttpContext.Request.IsAjaxRequest() && filterContext.Exception != null)
            {
                filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                filterContext.Result = new JsonResult
                {
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                    Data = new
                    {
                        filterContext.Exception.Message,
                        //filterContext.Exception.StackTrace
                    }
                };
                filterContext.ExceptionHandled = true;
            }

            //log unhandled exceptions in the audit log
            var expMessage = string.Format("SenStay Dojo App had an unhandled exception. Error: {0}", filterContext.Exception);
            Logger dojoLogger = NLog.LogManager.GetCurrentClassLogger();
            Senstay.Dojo.Helpers.EventLogger.Error(dojoLogger, filterContext.Exception, expMessage, typeof(AppBaseController));

            base.OnException(filterContext);
        }

        protected override void OnAuthentication(System.Web.Mvc.Filters.AuthenticationContext filterContext)
        {
            if (filterContext.Principal.Identity.IsAuthenticated)
            {
                ViewBag.UserName = filterContext.Principal.Identity.Name;
                ViewBag.UserEmail = string.Empty; // since name = email; we just show one of them
            }

            base.OnAuthentication(filterContext);

            //Logger dojoLogger = NLog.LogManager.GetCurrentClassLogger();
            //dojoLogger.Trace(string.Format("User {0} [{1}] is authenticated.", ViewBag.UserName, ViewBag.UserEmail));
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // check if we are in maintenance mode and redirect to notification page
            bool isOffline = Convert.ToBoolean(ConfigurationManager.AppSettings["IsOffline"]);
            ViewBag.IsOffLine = isOffline;
            // if IsOffline setting value is true,user is not admin and action name is not Maintenance,redirect to Maintenance view
            if (isOffline && filterContext.ActionDescriptor.ActionName != AppConstants.MAINTENANCE_ACTION_NAME)
            {
                filterContext.HttpContext.Response.Redirect("~/Home/Maintenance");
                filterContext.Result = HttpNotFound();
            }
            else if(!isOffline && filterContext.ActionDescriptor.ActionName == AppConstants.MAINTENANCE_ACTION_NAME)
            {
                filterContext.HttpContext.Response.Redirect("~/Home/Index");
            }
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = int.MaxValue,
            };
        }
    }

}
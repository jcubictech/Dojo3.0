using System.Collections.Generic;
using System.Web.Mvc;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Infrastructure.Alerts
{
    public static class AlertExtensions
    {
        public static List<Alert> GetAlerts(this TempDataDictionary tempData)
        {
            if (!tempData.ContainsKey(AppConstants.ALERT_KEY))
            {
                tempData[AppConstants.ALERT_KEY] = new List<Alert>();
            }

            return (List<Alert>)tempData[AppConstants.ALERT_KEY];
        }

        public static ActionResult WithSuccess(this ActionResult result, string message)
        {
            return new AlertDecoratorResult(result, "alert-success", message);
        }

        public static ActionResult WithInfo(this ActionResult result, string message)
        {
            return new AlertDecoratorResult(result, "alert-info", message);
        }

        public static ActionResult WithWarning(this ActionResult result, string message)
        {
            return new AlertDecoratorResult(result, "alert-warning", message);
        }

        public static ActionResult WithError(this ActionResult result, string message)
        {
            return new AlertDecoratorResult(result, "alert-danger", message);
        }
    }
    
    public static class PartialAlertExtensions
	{
		public static List<Alert> GetPartialAlerts(this ViewDataDictionary viewData)
		{
            if (!viewData.ContainsKey(AppConstants.PARTIAL_ALERT_KEY))
			{
                viewData[AppConstants.PARTIAL_ALERT_KEY] = new List<Alert>();
			}

            return (List<Alert>)viewData[AppConstants.PARTIAL_ALERT_KEY];
		}

        public static PartialViewResult WithSuccess(this PartialViewResult result, string message)
        {
            return new AlertPartialDecoratorResult(result, "alert-success", message);
        }

        public static PartialViewResult WithInfo(this PartialViewResult result, string message)
        {
            return new AlertPartialDecoratorResult(result, "alert-info", message);
        }

        public static PartialViewResult WithWarning(this PartialViewResult result, string message)
        {
            return new AlertPartialDecoratorResult(result, "alert-warning", message);
        }

        public static PartialViewResult WithError(this PartialViewResult result, string message)
        {
            return new AlertPartialDecoratorResult(result, "alert-danger", message);
        }
    }

    public static class JsonAlertExtensions
    {
        public static List<Alert> GetJsonAlerts(this ViewDataDictionary viewData)
        {
            if (!viewData.ContainsKey(AppConstants.JSON_ALERT_KEY))
            {
                viewData[AppConstants.JSON_ALERT_KEY] = new List<Alert>();
            }

            return (List<Alert>)viewData[AppConstants.JSON_ALERT_KEY];
        }

        public static JsonResult WithSuccess(this JsonResult result, string message)
        {
            return new AlertJsonDecoratorResult(result, "alert-success", message);
        }

        public static JsonResult WithInfo(this JsonResult result, string message)
        {
            return new AlertJsonDecoratorResult(result, "alert-info", message);
        }

        public static JsonResult WithWarning(this JsonResult result, string message)
        {
            return new AlertJsonDecoratorResult(result, "alert-warning", message);
        }

        public static JsonResult WithError(this JsonResult result, string message)
        {
            return new AlertJsonDecoratorResult(result, "alert-danger", message);
        }
    }
}
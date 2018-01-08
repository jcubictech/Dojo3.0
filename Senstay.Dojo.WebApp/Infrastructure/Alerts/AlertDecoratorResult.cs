using System.Web.Mvc;

namespace Senstay.Dojo.Infrastructure.Alerts
{
	public class AlertDecoratorResult : ActionResult
	{
		public ActionResult InnerResult { get; set; }
		public string AlertClass { get; set; }
		public string Message { get; set; }

		public AlertDecoratorResult(ActionResult innerResult, string alertClass, string message)
		{
			InnerResult = innerResult;
			AlertClass = alertClass;
			Message = message;
		}

		public override void ExecuteResult(ControllerContext context)
		{
			var alerts = context.Controller.TempData.GetAlerts();
			alerts.Add(new Alert(AlertClass, Message));
			InnerResult.ExecuteResult(context);
		}
	}
    
    public class AlertPartialDecoratorResult : PartialViewResult
    {
        public PartialViewResult InnerResult { get; set; }
        public string AlertClass { get; set; }
        public string Message { get; set; }

        public AlertPartialDecoratorResult(PartialViewResult innerResult, string alertClass, string message)
        {
            InnerResult = innerResult;
            AlertClass = alertClass;
            Message = message;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var alerts = context.Controller.ViewData.GetPartialAlerts();
            alerts.Add(new Alert(AlertClass, Message));
            InnerResult.ExecuteResult(context);
        }
    }

    public class AlertJsonDecoratorResult : JsonResult
    {
        public JsonResult InnerResult { get; set; }
        public string AlertClass { get; set; }
        public string Message { get; set; }

        public AlertJsonDecoratorResult(JsonResult innerResult, string alertClass, string message)
        {
            InnerResult = innerResult;
            AlertClass = alertClass;
            Message = message;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var alerts = context.Controller.ViewData.GetPartialAlerts();
            alerts.Add(new Alert(AlertClass, Message));
            InnerResult.ExecuteResult(context);
        }
    }
}
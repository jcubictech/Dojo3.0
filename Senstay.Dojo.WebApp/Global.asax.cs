using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web;
using Heroic.Web.IoC;
using NLog;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Infrastructure.Tasks;

namespace Senstay.Dojo
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // sync up NLog configuration with web.config for db configuration + log level
            LogConfig.SyncWithAppConfig(SettingsHelper.GetSafeSetting(AppConstants.LOG_LEVEL, "Error"));

            // register all infrastructure related dependency using structuremap package
            StructureMapConfig.Configure();

            using (var container = IoC.Container.GetNestedContainer())
            {
                foreach (var task in container.GetAllInstances<IRunAtInit>())
                {
                    task.Execute();
                }

                foreach (var task in container.GetAllInstances<IRunAtStartup>())
                {
                    task.Execute();
                }
            }
        }

        public void Application_BeginRequest()
        {
            foreach (var task in IoC.Container.GetNestedContainer().GetAllInstances<IRunOnEachRequest>())
            {
                task.Execute();
            }

            if (SettingsHelper.GetSafeSetting(AppConstants.HTTPS_ONLY) == "true")
            {
                // redirect any http: request to https:
                // note that adding [RequireHttps] attribute to base controller class makes OWIN go into repeating login loop with http request.
                string dojoDevElasticIP = SettingsHelper.GetSafeSetting(AppConstants.DOJO_DEV_AWS_ELASTIC_IP, "ec2-35-166-50-76");
                if (!Context.Request.IsSecureConnection && 
                    !Request.Url.Host.Contains(AppConstants.DOJO_DEV_LOCAL) &&
                    !Request.Url.Host.Contains(dojoDevElasticIP))
                {
                    string url = Context.Request.Url.AbsoluteUri.Replace("http://", "https://");
                    Response.Redirect(url);
                }
            }
        }

        public void Application_Error()
        {
            foreach (var task in IoC.Container.GetNestedContainer().GetAllInstances<IRunOnError>())
            {
                task.Execute();
            }

            // if exception does not comes from IRunOnError(), we hand it to generic error page
            if (HttpContext.Current.Items[AppConstants.TRANSACTION_ERROR_KEY] == null)
            {
                Exception exception = Server.GetLastError();
                var dojoLogger = LogManager.GetCurrentClassLogger();
                string message = "System error detected. See ErrorMessage column.";
                EventLogger.Error(dojoLogger, exception, message, typeof(MvcApplication));
                Response.Redirect("/Error/Application");
            }
        }

        public void Application_EndRequest()
        {
            try
            {
                foreach (var task in IoC.Container.GetNestedContainer().GetAllInstances<IRunAfterEachRequest>())
                {
                    task.Execute();
                }
            }
            finally
            {
            }
        }
    }
}

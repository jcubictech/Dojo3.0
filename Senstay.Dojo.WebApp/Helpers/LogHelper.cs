using System.Web;
using NLog;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.App.Helpers
{
    public class LogHelper
    {
        public static void LogUserEnvironment(HttpRequestBase request, string userName)
        {
            var dojoLogger = LogManager.GetCurrentClassLogger();
            string browserInfo = OsHelper.GetUserBrowser(request);
            string osInfo = OsHelper.GetUserPlatform(request).ToString();
            string message = string.Format("User:{0} OS:{1} {2}", (userName != null ? userName : string.Empty), osInfo, browserInfo);
            dojoLogger.Info(message, typeof(OsHelper));
        }
    }
}
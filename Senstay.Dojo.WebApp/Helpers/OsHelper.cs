using System;
using System.Web;
using System.Security.Claims;
using NLog;

namespace Senstay.Dojo.Helpers
{
    public class OsHelper
    {
        public static void LogUserEnvironment(HttpRequestBase request, string userName)
        {
            var dojoLogger = LogManager.GetCurrentClassLogger();
            string browserInfo = OsHelper.GetUserBrowser(request);
            string osInfo = OsHelper.GetUserPlatform(request).ToString();
            string message = string.Format("User:{0} OS:{1} {2}", (userName != null ? userName : string.Empty), osInfo, browserInfo);
            dojoLogger.Info(message, typeof(OsHelper));
        }
        public static OSType GetUserPlatform(HttpRequestBase request)
        {
            var ua = request.UserAgent;

            if (ua.Contains("Android"))
                return OSType.Android; // string.Format("Android {0}", GetMobileVersion(ua, "Android"));

            if (ua.Contains("iPad"))
                return OSType.iPad; // string.Format("iPad OS {0}", GetMobileVersion(ua, "OS"));

            if (ua.Contains("iPhone"))
                return OSType.iPhone; // string.Format("iPhone OS {0}", GetMobileVersion(ua, "OS"));

            if (ua.Contains("Linux") && ua.Contains("KFAPWI"))
                return OSType.Kindle; // "Kindle Fire";

            if (ua.Contains("RIM Tablet") || (ua.Contains("BB") && ua.Contains("Mobile")))
                return OSType.BlackBerry; // "Black Berry";

            if (ua.Contains("Windows Phone"))
                return OSType.WindowsPhone; // string.Format("Windows Phone {0}", GetMobileVersion(ua, "Windows Phone"));

            if (ua.Contains("Mac OS"))
                return OSType.Mac; // "Mac OS";

            if (ua.Contains("Windows NT 5.1") || ua.Contains("Windows NT 5.2"))
                return OSType.WindowsXP; // "Windows XP";

            if (ua.Contains("Windows NT 6.0"))
                return OSType.WindowsVista; // "Windows Vista";

            if (ua.Contains("Windows NT 6.1"))
                return OSType.Windows7; // "Windows 7";

            if (ua.Contains("Windows NT 6.2"))
                return OSType.Windows8; // "Windows 8";

            if (ua.Contains("Windows NT 6.3"))
                return OSType.Windows81; // "Windows 8.1";

            if (ua.Contains("Windows NT 10"))
                return OSType.Windows10; // "Windows 10";

            //fallback to basic platform:
            return OSType.Other; // request.Browser.Platform + (ua.Contains("Mobile") ? " Mobile " : "");
        }

        public static string GetMobileVersion(string userAgent, string device)
        {
            var temp = userAgent.Substring(userAgent.IndexOf(device) + device.Length).TrimStart();
            var version = string.Empty;

            foreach (var character in temp)
            {
                var validCharacter = false;
                int test = 0;

                if (Int32.TryParse(character.ToString(), out test))
                {
                    version += character;
                    validCharacter = true;
                }

                if (character == '.' || character == '_')
                {
                    version += '.';
                    validCharacter = true;
                }

                if (validCharacter == false)
                    break;
            }

            return version;
        }

        public static string GetUserBrowser(HttpRequestBase request)
        {
            try
            {
                var browser = request.Browser;
                string browserName = browser.Browser;
                string version = browser.Version;
                // request.Browser does not detect Edge browser; we check with useragent
                if (request.UserAgent.Contains("Edge"))
                {
                    browserName = "Edge";
                    int start = request.UserAgent.IndexOf("Edge") + 5;
                    int end = request.UserAgent.IndexOf(" ", start);
                    if (end < 0) end = request.UserAgent.Length;
                    version = request.UserAgent.Substring(start, end - start);
                }
                return string.Format("browser:{0} {1}", browserName, version);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public enum OSType
    {
        Android,
        iPad,
        iPhone,
        Kindle,
        BlackBerry,
        WindowsPhone,
        Mac,
        WindowsXP,
        WindowsVista,
        Windows7,
        Windows8,
        Windows81,
        Windows10,
        Other
    }

    public enum BrowserType
    {
        Chrome,
        Edge,
        Firefox,
        IE,
        Opera,
        Safari,
        Other
    }
}
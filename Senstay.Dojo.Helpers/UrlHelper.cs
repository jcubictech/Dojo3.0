using System;
using System.Collections.Specialized;
using System.Web;

namespace Senstay.Dojo.Helpers
{
    public class UrlHelper
    {
        public static NameValueCollection ParseQueryStrings(string url)
        {
            if (!url.ToLower().StartsWith("http")) url = "http://" + url;
            url = url.Replace("///", "//"); // in case the given url starts with '/'
            url = HttpUtility.UrlDecode(url); // in case the url is encoded
            return HttpUtility.ParseQueryString(new Uri(url).Query);
        }

        public static string DataRootUrl()
        {
            try
            {
                return HttpContext.Current.Server.MapPath("~/App_Data");
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
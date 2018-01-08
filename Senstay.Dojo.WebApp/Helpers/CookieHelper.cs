using System;
using System.Linq;
using System.Web;

namespace Senstay.Dojo.Helpers
{
    public class CookieHelper
    {
        public static HttpCookie Create(string cookieName, string value)
        {
            HttpCookie cookie = new HttpCookie(cookieName, value);
            return cookie;
        }

        public static HttpCookie Update(HttpContextBase httpContext, string cookieName, string value)
        {
            if (httpContext.Request.Cookies.AllKeys.Contains(cookieName))
            {
                HttpCookie cookie = httpContext.Request.Cookies[cookieName];
                cookie.Value = value;
                return cookie;
            }
            return Create(cookieName, value);
        }

        public static HttpCookie Remove(HttpContextBase httpContext, string cookieName)
        {
            if (httpContext.Request.Cookies.AllKeys.Contains(cookieName))
            {
                HttpCookie cookie = httpContext.Request.Cookies[cookieName];
                cookie.Expires = DateTime.Now.AddDays(-1); // remove cookie by setting it to expire
                return cookie;
            }
            return null;
        }
    }
}
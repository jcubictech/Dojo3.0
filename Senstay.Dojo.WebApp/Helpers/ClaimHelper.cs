using System.Linq;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.App.Helpers
{
    public class ClaimHelper
    {
        public static string GetSafeClaim(ApplicationUser appUser, string type, string defaultValue = "")
        {
            try
            {
                var claim = appUser.Claims.Where(c => c.ClaimType == type).FirstOrDefault();
                if (claim == null)
                    return defaultValue;
                else
                    return claim.ClaimValue;
            }
            catch
            {
            }
            return defaultValue;
        }
    }
}
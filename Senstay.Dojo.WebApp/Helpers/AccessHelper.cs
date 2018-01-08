using System.Security.Claims;
//using System.Security.Principal;

namespace Senstay.Dojo.Helpers
{
    public class AccessHelper
    {
        public static string UserAlias
        {
            get
            {
                var identityName = ClaimsPrincipal.Current.Identity.Name;
                return identityName.Substring(0, identityName.IndexOf("@"));
            }
        }

        public static bool IsGoogleAccount()
        {
            string email = AccessHelper.UserAlias;
            if (!string.IsNullOrEmpty(email))
                return email.ToLower().Contains("@" + AppConstants.GOOGLE_EMAIL_DOMAIN);
            else
                return false;
        }

        public static bool IsAuthenticated()
        {
            return ClaimsPrincipal.Current.Identity.IsAuthenticated;
        }

        public static bool WaitingForRoleAssignment()
        {
            return ClaimsPrincipal.Current.Identity.IsAuthenticated &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.VIEWER_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.EDITOR_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.ADMIN_ROLE) &&
                   !ClaimsPrincipal.Current.IsInRole(AppConstants.SUPER_ADMIN_ROLE);
                    
        }
    }
}
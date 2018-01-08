using System.Collections.Generic;
using System.Reflection;
using System.Resources;

namespace Senstay.Dojo.Helpers
{
    public class Notification
    {
        public static void SendAssignRoleNotice(string email)
        {
            try
            {
                // send an email to SenStay admin to assign a role for the registered user
                var recipient = SettingsHelper.GetSafeSetting(AppConstants.EMAIL_SUPPORT_KEY, string.Empty);
                string cc = SettingsHelper.GetSafeSetting(AppConstants.EMAIL_DEVELOPER_KEY, string.Empty);
                List<string> Ccs = new List<string>();
                Ccs.Add(cc);
                if (!string.IsNullOrEmpty(recipient))
                {
                    ResourceManager rm = new ResourceManager("Senstay.Dojo.AppResources", Assembly.GetExecutingAssembly());
                    var subject = rm.GetString("RoleAssignmentEmailSubject");
                    var body = rm.GetString("RoleAssignmentEmailTemplate");
                    var content = string.Format(body, email);
                    Helpers.EmailHelper.SendEmail(recipient, subject, content, Ccs);
                }
            }
            catch
            {
                // TO-DO: log the error
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using System.Web.Mvc;
using System.Reflection;
using System.Resources;
using Newtonsoft.Json;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.App.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class UserInvitationController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DojoDbContext _dbContext;
        private readonly ApplicationUserManager _userManager;
        private readonly ApplicationRoleManager _roleManager;

        public UserInvitationController(DojoDbContext context, ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            _dbContext = context;
            _userManager = userManager;
            _roleManager = roleManager;

            // allow non-alphanumeric characters in user name
            _userManager.UserValidator = new UserValidator<ApplicationUser>(_userManager)
            {
                AllowOnlyAlphanumericUserNames = false
            };
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            try
            {
                UserInvitationViewModel invitation = JsonConvert.DeserializeObject<UserInvitationViewModel>(model);
                if (_userManager.FindByEmail(invitation.UserEmail) != null)
                {
                    Response.StatusCode = (int)System.Net.HttpStatusCode.Conflict;
                    return JsonError("User already exists.");
                }

                DojoLogger.Trace(string.Format("Inviting user '{0}', email '{1} to join Dojo.", invitation.UserName, invitation.UserEmail), typeof(UserRoleManagerController));

                ApplicationUser newUser = new ApplicationUser()
                {
                    UserName = invitation.UserName,
                    Email = invitation.UserEmail,
                };

                IdentityResult result = _userManager.Create(newUser, invitation.InvitationCode);
                DojoLogger.Trace(string.Format("Creating User '{0}', email '{1} in DB.", invitation.UserName, invitation.UserEmail), typeof(UserRoleManagerController));
                if (result == IdentityResult.Success)
                {
                    newUser = _userManager.FindByEmail(invitation.UserEmail);
                    _userManager.AddClaim(newUser.Id, new Claim(AppConstants.INVITATION_CLAIM, "1"));
                    _userManager.AddClaim(newUser.Id, new Claim(AppConstants.INVITATION_CODE_CLAIM, invitation.InvitationCode));
                    _userManager.AddClaim(newUser.Id, new Claim(AppConstants.EXPIRATION_DATE_CLAIM, invitation.ExpirationDate.ToString("MM/dd/yyyy")));
                    _userManager.AddClaim(newUser.Id, new Claim(AppConstants.ROLES_CLAIM, StringifyTuples(invitation.UserRoles)));

                    DojoLogger.Trace(string.Format("Sending invitation email to User '{0}', ID {1}.", invitation.UserName, newUser.Id), typeof(UserRoleManagerController));

                    // send invitation email to user
                    SendInvitationEmail(newUser.Id, invitation);

                    Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    return Json(invitation, JsonRequestBehavior.AllowGet); // keep claims out of it
                }
                else
                {
                    Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                    return Json("Unable to create invited user.", JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("Inviting user fails. {0}", ex.Message);
                DojoLogger.Warn(message, typeof(UserRoleManagerController));

                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (_userManager != null && _userManager.SupportsUserRole)
            {
                var appUsers = _userManager.Users.ToList();
                var filteredUsers = appUsers.Where(u => u.Claims.Any(t => t.ClaimType == AppConstants.INVITATION_CLAIM && t.ClaimValue == "1"));
                var invitedUsers = filteredUsers.Select(u =>
                        new UserInvitationViewModel
                        {
                            UserId = u.Id,
                            UserName = u.UserName,
                            UserEmail = u.Email,
                            InvitationCode = ClaimHelper.GetSafeClaim(u, AppConstants.INVITATION_CODE_CLAIM, string.Empty),
                            ExpirationDate = DateTime.Parse(ClaimHelper.GetSafeClaim(u, AppConstants.EXPIRATION_DATE_CLAIM, DateTime.Today.Date.AddDays(-1).ToShortDateString())),
                            UserRoles = TuplifyString(ClaimHelper.GetSafeClaim(u, AppConstants.ROLES_CLAIM, string.Empty)),
                            Password = string.Empty,
                            ConfirmPassword = string.Empty,
                        })
                        .ToList();

                string message = string.Format("Total of {0:d} users are retrieved for invitation.", invitedUsers.Count());
                DojoLogger.Trace(message, typeof(UserRoleManagerController));

                return Json(invitedUsers, JsonRequestBehavior.AllowGet);
            }
            else if (!_userManager.SupportsUserRole)
                return JsonError("User Role is not suported.");
            else
                return JsonError("Role manager does not exist");
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            try
            {
                UserInvitationViewModel invitation = JsonConvert.DeserializeObject<UserInvitationViewModel>(model);
                var user = _userManager.Users.Where(u => u.Id == invitation.UserId).First();
                if (user == null)
                {
                    Response.StatusCode = (int)System.Net.HttpStatusCode.Conflict;
                    return JsonError("User does not exist.");
                }
                else
                {
                    user.UserName = invitation.UserName;
                    user.Email = invitation.UserEmail;
                    _userManager.Update(user);

                    SafeRemoveClaim(user, AppConstants.INVITATION_CODE_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.INVITATION_CODE_CLAIM, string.Empty));
                    SafeRemoveClaim(user, AppConstants.EXPIRATION_DATE_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.EXPIRATION_DATE_CLAIM, string.Empty));
                    SafeRemoveClaim(user, AppConstants.ROLES_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.ROLES_CLAIM, string.Empty));
                    _userManager.AddClaim(user.Id, new Claim(AppConstants.INVITATION_CODE_CLAIM, invitation.InvitationCode));
                    _userManager.AddClaim(user.Id, new Claim(AppConstants.EXPIRATION_DATE_CLAIM, invitation.ExpirationDate.ToString("MM/dd/yyyy")));
                    _userManager.AddClaim(user.Id, new Claim(AppConstants.ROLES_CLAIM, StringifyTuples(invitation.UserRoles)));

                    // send invitation email to user
                    SendInvitationEmail(user.Id, invitation);

                    Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    return Json(invitation, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                string message = string.Format("Inviting user fails. {0}", ex.Message);
                DojoLogger.Warn(message, typeof(UserRoleManagerController));

                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            try
            {
                UserInvitationViewModel invitation = JsonConvert.DeserializeObject<UserInvitationViewModel>(model);
                var user = _userManager.FindByEmail(invitation.UserEmail);
                if (user != null)
                {
                    if (_userManager.Delete(user) == IdentityResult.Success)
                    {
                        Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                }

                Response.StatusCode = (int)System.Net.HttpStatusCode.Conflict;
                return JsonError("User does not exist or cannot be deleted.");
            }
            catch (Exception ex)
            {
                string message = string.Format("Deleting user fails. {0}", ex.Message);
                DojoLogger.Warn(message, typeof(UserRoleManagerController));

                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(message, JsonRequestBehavior.AllowGet);
            }
        }

        private void SafeRemoveClaim(ApplicationUser user, string claimType, string claimValue)
        {
            try
            {
                _userManager.RemoveClaim(user.Id, new Claim(claimType, claimValue));
            }
            catch // ignore if fail to remove claim
            {
            }
        }

        private string StringifyTuples(ICollection<CustomTuple> tuples)
        {
            string tupleString = string.Empty;
            foreach(CustomTuple tuple in tuples)
            {
                if (tupleString != string.Empty) tupleString += ",";
                tupleString += tuple.Text;
            }
            return tupleString;
        }

        private List<CustomTuple> TuplifyString(string roles)
        {
            string[] roleTokens = roles.Split(new char[] { ',' });
            List<CustomTuple> tuples = new List<CustomTuple>();
            foreach (string role in roleTokens)
            {
                var appRole = _roleManager.FindByName(role);
                if (appRole != null)
                {
                    tuples.Add(new CustomTuple()
                    {
                        Id = _roleManager.FindByName(role).Id,
                        Text = role
                    });
                };
            }
            return tuples;
        }

        private void SendInvitationEmail(string userId, UserInvitationViewModel invitation)
        {
            try 
            {
                var recipient = invitation.UserEmail;
                List<string> Ccs = new List<string>()
                {
                    SettingsHelper.GetSafeSetting(AppConstants.EMAIL_SUPPORT_KEY, string.Empty),
                    //SettingsHelper.GetSafeSetting(AppConstants.EMAIL_DEVELOPER_KEY, string.Empty),
                };
                if (!string.IsNullOrEmpty(recipient))
                {
                    string pageUrl = Request.Url.GetLeftPart(UriPartial.Authority);
                    string url = string.Format("{0}{1}?id={2}&invite={3}", pageUrl, "/Account/Accept", userId, invitation.InvitationCode);
                    ResourceManager rm = new ResourceManager("Senstay.Dojo.AppResources", Assembly.GetExecutingAssembly());
                    var subject = rm.GetString("InvitationEmailSubject");
                    var body = rm.GetString("InvitationEmailTemplate");
                    var content = string.Format(body, invitation.UserName, invitation.InvitationCode, url);
                    Helpers.EmailHelper.SendEmail(recipient, subject, content, Ccs);

                    string message = string.Format("Invitation email is sent to {0} on {1}", recipient, DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss zzz"));
                    DojoLogger.Info(message, typeof(UserInvitationController));
                }
                else
                {
                    string message = "Invitation recipient email is empty or null.";
                    DojoLogger.Warn(message, typeof(UserInvitationController));
                }
            }
            catch(Exception ex)
            {
                string message = string.Format("Send invitation email error. {0}", ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(UserInvitationController));
            }
        }
    }
}
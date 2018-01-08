using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Infrastructure.Alerts;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.App.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    public class AccountController : AppBaseController
    {
        private DojoDbContext _dbContext;
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController(DojoDbContext dbContext, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            _dbContext = dbContext;
            UserManager = userManager;
            SignInManager = signInManager;

            // allow non-alphanumeric characters in user name
            UserManager.UserValidator = new UserValidator<ApplicationUser>(_userManager)
            {
                AllowOnlyAlphanumericUserNames = false
            };
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        #region User favorites settings
        public JsonResult SetStartPage(string page)
        {
            try
            {
                ApplicationUser appUser = _userManager.FindByName(ClaimsPrincipal.Current.Identity.Name);
                if (appUser != null)
                {
                    // keep only one start page
                    string oldPage = ClaimHelper.GetSafeClaim(appUser, AppConstants.FAVORITE_PAGE);
                    _userManager.RemoveClaim(appUser.Id, new Claim(AppConstants.FAVORITE_PAGE, oldPage));
                    _userManager.AddClaim(appUser.Id, new Claim(AppConstants.FAVORITE_PAGE, page));
                    return Json("success", JsonRequestBehavior.AllowGet);
                }
            }
            catch
            {
            }
            return Json("fail", JsonRequestBehavior.AllowGet);
        }

        private string GetStartPage(string userName, bool useEmail = false)
        {
            ViewBag.StartPageClass = "fa-heart-o";
            try
            {
                ApplicationUser appUser = _userManager.FindByName(userName);
                if (useEmail) appUser = _userManager.FindByEmail(userName);
                if (appUser != null)
                {
                    string page = ClaimHelper.GetSafeClaim(appUser, AppConstants.FAVORITE_PAGE);
                    if (page != string.Empty) ViewBag.StartPageClass = "fa-heart red";
                    return page;
                }
            }
            catch
            {
            }
            return string.Empty;
        }
        #endregion

        #region Login, Logout, Register

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (Request.IsAuthenticated) AuthenticationManager.SignOut();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // To enable password failures to trigger account lockout, change to shouldLockout: true
            // PasswordSignIn uses UserName for sign in.
            if (string.IsNullOrEmpty(model.UserName)) model.UserName = model.Email;
            if (UserEmailMatch(model.UserName, model.Email))
            {
                var result = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
                switch (result)
                {
                    case SignInStatus.Success:
                        LogHelper.LogUserEnvironment(HttpContext.Request, model.UserName);
                        if (returnUrl == null)
                        {
                            string page = GetStartPage(model.UserName);
                            if (!string.IsNullOrEmpty(page)) return RedirectToLocal(page);
                        }
                        return RedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        return View("Lockout");
                    case SignInStatus.RequiresVerification:
                        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    case SignInStatus.Failure:
                    default:
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "User name and Email do not match.");
                return View(model);
            }
        }

        public ActionResult Logout()
        {
            if (Request.IsAuthenticated) AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            //return RedirectToAction("Login"); // Dojo uses Invitation model for new user
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(model.UserName)) model.UserName = model.Email;
                var user = new ApplicationUser { UserName = model.UserName, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    Helpers.Notification.SendAssignRoleNotice(model.Email);

                    return RedirectToAction("Index", "Home");
                    //.WithSuccess("SenStay Dojo App is working on assigning an application role for you. We will send you an email when this is done.");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region Invitation Acceptance
        //---------------------------------------------------------------------------------------------------
        // Mechanism of Invitation for membership of Dojo Web App:
        // 1. Dojo App Admin invite a Dojo team member (user name and email) to join them
        // 2. The invited Dojo team member receives an invitation email with a link to accept the membership
        // 3. The invited team member clicks the link to join the Dojo team (i.e. access to DOjo Web App)
        //    The invited team member can use either a social medial account or specify a password to join.
        //---------------------------------------------------------------------------------------------------
        [AllowAnonymous]
        public ActionResult Accept(string id, string invite)
        {
            try
            {
                //------------------------------------------------------------------------------------------------
                // present the user with the invitation form pre-popualated user name, email and invitation code.
                //------------------------------------------------------------------------------------------------
                var user = _userManager.FindById(id);
                if (user != null)
                {
                    var claimValue = ClaimHelper.GetSafeClaim(user, AppConstants.INVITATION_CODE_CLAIM, null);
                    if (claimValue != null)
                    {
                        var invitationCode = claimValue;
                        var expiredOn = ClaimHelper.GetSafeClaim(user, AppConstants.EXPIRATION_DATE_CLAIM, DateTime.Today.Date.AddDays(-1).ToShortDateString());
                        DateTime expirationDate;
                        if (DateTime.TryParse(expiredOn, out expirationDate) == true)
                        {
                            if (DateTime.Today.Date <= expirationDate && invitationCode == invite)
                            {
                                UserInvitationViewModel invitationModel = new UserInvitationViewModel();
                                invitationModel.UserId = id;
                                invitationModel.UserName = user.UserName;
                                invitationModel.UserEmail = user.Email;
                                invitationModel.InvitationCode = invite;
                                return View(invitationModel);
                            }
                        }
                    }
                    else // user has been here before, redirect it to login page
                    {
                        return RedirectToAction("Login");
                    }
                }
            }
            catch
            {
            }
            // redirect to login page if anything is wrong
            return RedirectToAction("Login");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Accept(UserInvitationViewModel model)
        {
            try
            {
                //----------------------------------------------------------------------------------------------------
                // when the user accepts the invitation, we remove the invitation claims and sign the user in;
                // temporary password is the invitation code; also confirm the email as user gets here by email link.
                //----------------------------------------------------------------------------------------------------
                var user = _userManager.FindById(model.UserId);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    user.PhoneNumber = model.MobilePhone;
                    _userManager.Update(user);

                    // change user password
                    IdentityResult result = _userManager.ChangePassword(user.Id, model.InvitationCode, model.Password);
                    if (result.Succeeded)
                    {
                        string roleClaim = ClaimHelper.GetSafeClaim(user, AppConstants.ROLES_CLAIM, string.Empty);
                        string[] roles = roleClaim.Split(new char[] { ',' });
                        if (roles.Length > 0) _userManager.AddToRoles(user.Id, roles);

                        // remove invitation claim for the user
                        SafeRemoveClaim(user, AppConstants.INVITATION_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.INVITATION_CLAIM, "1"));
                        SafeRemoveClaim(user, AppConstants.INVITATION_CODE_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.INVITATION_CODE_CLAIM, string.Empty));
                        SafeRemoveClaim(user, AppConstants.EXPIRATION_DATE_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.EXPIRATION_DATE_CLAIM, string.Empty));
                        SafeRemoveClaim(user, AppConstants.ROLES_CLAIM, ClaimHelper.GetSafeClaim(user, AppConstants.ROLES_CLAIM, string.Empty));

                        user = _userManager.FindById(model.UserId);
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToAction("Index", "Home", "/"); // redirect user to home page
                    }
                }
            }
            catch
            {
            }
            // redirect to login page if anything is wrong
            return RedirectToAction("Login", "Account", "/");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ExternalAccept(UserInvitationViewModel model)
        {
            string returnUrl = string.Format("/Account/ExternalInvitationRedirect/?id={0}&phone={1}", model.UserId, model.MobilePhone);
            return new ChallengeResult(model.Provider, Url.Action("ExternalInvitationLoginCallback", "Account",
                            new { ReturnUrl = returnUrl }));
        }

        [AllowAnonymous]
        public async Task<ActionResult> ExternalInvitationLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login")
                         .WithError("Dojo App cannot sign you in with your existing SenStay/Google account. Plesae try again.");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);

            NameValueCollection queryStrings = Helpers.UrlHelper.ParseQueryStrings(returnUrl);
            string id = queryStrings["id"];
            string phone = queryStrings["phone"];
            switch (result)
            {
                case SignInStatus.Success: // case of the use already exists in Dojo
                case SignInStatus.Failure: // case of first time login by social media user
                    bool ok = await CreateExternalInvitedUser(loginInfo, id, phone);
                    if (ok)
                    {
                        return RedirectToAction("Index", "Home")
                                .WithSuccess("Cool. You can start collaborating with your Dojo team. Thanks for your acceptance.");
                    }
                    else
                    {
                        return RedirectToAction("Login")
                            .WithError("Dojo App cannot create an external user account for your " + loginInfo.Login.LoginProvider + " account.");
                    }
                case SignInStatus.LockedOut: // case of too many login attempt
                    return View("Lockout");
                case SignInStatus.RequiresVerification: // case of 2-factor authentication
                    // TODO: If Dojo 2.0 enables 2-factor authentication, we will need to set the role after the verification 
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                default: // unknown login problem
                    return RedirectToAction("Login")
                        .WithError("Dojo App cannot sign you in with your SenStay/" + loginInfo.Login.LoginProvider + " account.");
            }
        }

        private async Task<bool> CreateExternalInvitedUser(ExternalLoginInfo loginInfo, string id, string phone)
        {
            try
            {
                var user = _userManager.FindById(id); // this is the internal user created for invitation
                if (user != null)
                {
                    // create the external login account using the invitation user ID and external login info
                    var result = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
                    if (result.Succeeded)
                    {
                        SetExternalInvitationRoles(user, phone);
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return true;
                    }
                }
            }
            catch // any error will return false
            {
            }
            return false;
        }

        private void SetExternalInvitationRoles(ApplicationUser externalUser, string phone)
        {
            //----------------------------------------------------------------------------------------------------
            // when the external user accepts the invitation, we remove the invitation and roles claims for the
            // internal user, confirm the email and add phone # for the external user.
            //----------------------------------------------------------------------------------------------------
            if (externalUser != null)
            {
                string roleClaim = ClaimHelper.GetSafeClaim(externalUser, AppConstants.ROLES_CLAIM, string.Empty);
                string[] roles = roleClaim.Split(new char[] { ',' });
                if (roles.Length > 0)
                {
                    _userManager.AddToRoles(externalUser.Id, roles);
                }

                externalUser.EmailConfirmed = true;
                externalUser.PhoneNumber = phone;
                _userManager.Update(externalUser);

                // remove invitation claim for the internal user
                SafeRemoveClaim(externalUser, AppConstants.INVITATION_CLAIM, ClaimHelper.GetSafeClaim(externalUser, AppConstants.INVITATION_CLAIM, "1"));
                SafeRemoveClaim(externalUser, AppConstants.EXPIRATION_DATE_CLAIM, ClaimHelper.GetSafeClaim(externalUser, AppConstants.EXPIRATION_DATE_CLAIM, string.Empty));
                SafeRemoveClaim(externalUser, AppConstants.ROLES_CLAIM, ClaimHelper.GetSafeClaim(externalUser, AppConstants.ROLES_CLAIM, string.Empty));
            }
        }

        #endregion

        #region External Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginCallbackRedirect(string returnUrl)
        {
            // in route mapping, map /signin-google to /Account/ExternalLoginCallback does not seem working for OAuth 2.0
            return RedirectPermanent("/Account/ExternalLoginCallback");
        }

        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login")
                         .WithError("Dojo App cannot sign you in with your SenStay/Google account. Please try again.");
            }

            // Sign in the user with this external login provider if the user already has a login with Dojo
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);

            switch (result)
            {
                case SignInStatus.Success: // case of the use already exists in Dojo
                    LogHelper.LogUserEnvironment(HttpContext.Request, loginInfo.ExternalIdentity.Name);
                    if (returnUrl == null)
                    {
                        string page = GetStartPage(loginInfo.Email, true);
                        if (!string.IsNullOrEmpty(page)) return RedirectToLocal(page);
                    }
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut: // case of too many login attempt
                    return View("Lockout");
                case SignInStatus.RequiresVerification: // case of 2-factor authentication
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure: // case of external user does not exist in Dojo 
                default: // unknown login problem
                    // for registration style external user login, enable the following code
                    //ViewBag.ReturnUrl = returnUrl;
                    //ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    //return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });

                    // for invitation style external user login, the user should already exist in Dojo via invitation link in email
                    return RedirectToAction("Login")
                        .WithError("Dojo App cannot sign you in with your SenStay/Google account. Please try again.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #endregion

        #region Two-Factor Authentication

        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        #endregion

        #region Password Management

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.UserName);
                var email = await UserManager.FindByEmailAsync(model.Email);
                if (user == null) user = email;
                if (email == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            // Dependency injection will take care of disposal automatically for _userManager and _signInManager
            //if (disposing)
            //{
            //    if (_userManager != null)
            //    {
            //        _userManager.Dispose();
            //        _userManager = null;
            //    }

            //    if (_signInManager != null)
            //    {
            //        _signInManager.Dispose();
            //        _signInManager = null;
            //    }
            //}

            base.Dispose(disposing);
        }

        #region Helpers

        public JsonResult LoginAccounts()
        {
            var loginAccounts = _userManager.Users.OrderBy(u => u.Email)
                                            .Select(u => new SelectListItem
                                                {
                                                    Text = u.Email + " | " + u.UserName,
                                                    Value = u.Email
                                            })
                                            .ToList();
            return Json(loginAccounts, JsonRequestBehavior.AllowGet);
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

        private bool UserEmailMatch(string userName, string email)
        {
            var count = _dbContext.Users.Where(u => u.UserName == userName && u.Email == email).Count();
            return count > 0;
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null) properties.Dictionary[XsrfKey] = UserId;
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}
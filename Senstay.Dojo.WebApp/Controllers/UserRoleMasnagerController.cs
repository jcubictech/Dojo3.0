using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using Newtonsoft.Json;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Data.Providers;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class UserRoleManagerController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DojoDbContext _dbContext;
        private readonly ApplicationUserManager _userManager;
        private readonly ApplicationRoleManager _roleManager;

        public UserRoleManagerController(DojoDbContext context, ApplicationUserManager userManager, ApplicationRoleManager roleManager)
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
            Response.StatusCode = (int)System.Net.HttpStatusCode.NotImplemented;
            return JsonError("User-Role creation is not applicable.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (_userManager != null && _userManager.SupportsUserRole)
            {
                var appUsers = _userManager.Users.ToList();

                // super user is a built-in user that cannot be changed
                var allUserRoles = appUsers.Where(u => u.UserName != AppConstants.SUPER_ADMIN_ROLE)
                                           .Select(u => new UserRoleManagementViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    UserRoles = u.Roles.Join(_dbContext.Roles,
                                                ur => ur.RoleId,
                                                dr => dr.Id,
                                                (ur, dr) => new CustomTuple
                                                {
                                                    Id = dr.Id,
                                                    Text = dr.Name
                                                })
                                        .ToList()
                });

                string message = string.Format("Total of {0:d} users are retrieved for role assignment.", appUsers.Count());
                DojoLogger.Info(message, typeof(UserRoleManagerController));

                return Json(allUserRoles, JsonRequestBehavior.AllowGet);
            }
            else if (!_userManager.SupportsUserRole)
                return JsonError("User Role is not suported.");
            else
                return JsonError("Role manager does not exist");
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            string userName = string.Empty;
            try
            {
                UserRoleManagementViewModel userRole = JsonConvert.DeserializeObject<UserRoleManagementViewModel>(model);
                var user = _userManager.Users.Where(u => u.Id == userRole.UserId).First();
                if (user != null)
                {
                    userName = user.UserName;
                    var oldRoles = _userManager.GetRoles(user.Id).ToArray();
                    IdentityResult result = _userManager.RemoveFromRoles(user.Id, oldRoles);
                    if (result == IdentityResult.Success)
                    {
                        var newRoles = userRole.UserRoles.Select(r => r.Text).ToArray();
                        result = _userManager.AddToRoles(user.Id, newRoles);
                        if (result == IdentityResult.Success)
                        {
                            string message = string.Format("Role '{0}' assigned to user '{1}'.", string.Join(", ", newRoles), userName);
                            DojoLogger.Info(message, typeof(UserRoleManagerController));

                            Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                            return Json(userRole, JsonRequestBehavior.AllowGet);
                        }
                    }
                    throw new System.Exception("Remove/Add user role from DB fails.");
                }
                throw new System.Exception(string.Format("User does not exist for user ID = '{0}'.", userRole.UserId));
            }
            catch (Exception ex)
            {
                string message = string.Format("Update user role for user '{0}' fails. {1}", userName, ex.Message);
                DojoLogger.Info(message, typeof(UserRoleManagerController));

                // assemble custom error for kendo CRID operation
                Response.StatusCode = (int)System.Net.HttpStatusCode.OK; // custom error return 200 code
                var exception = new // custom response body indicated by the 'errors' field
                {
                    errors = message
                };
                return Json(exception, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            Response.StatusCode = (int)System.Net.HttpStatusCode.NotImplemented;
            return JsonError("User-Role Deletion is not applicable.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult AvailableRoles()
        {
            if (_roleManager != null)
            {
                var roles = _roleManager.Roles.OrderBy(r => r.Name)
                                              .Select(r => new CustomTuple
                                                {
                                                    Id = r.Id,
                                                    Text = r.Name
                                                })
                                              .ToList();

                // super user role can only be assigned by SenstayAdmin
                if (!AuthorizationProvider.IsSuperAdmin())
                {
                    roles = roles.Where(r => r.Text != AppConstants.SUPER_ADMIN_ROLE).ToList();
                }

                return Json(roles, JsonRequestBehavior.AllowGet);
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetApprovers()
        {
            return Json(GetApproverList(), JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public List<SelectListItem> GetApproverList()
        {
            List<SelectListItem> approvers = new List<SelectListItem>();
            if (_userManager != null && _userManager.SupportsUserRole)
            {
                ApplicationRole approverRole = _roleManager.FindByName(AppConstants.APPROVER_ROLE);
                foreach (ApplicationUser user in _userManager.Users.ToList())
                {
                    // this code does not work
                    //var identityUserRole = new IdentityUserRole() { UserId = user.Id, RoleId = approverRole.Id };
                    //if (user.Roles.Contains<IdentityUserRole>(identityUserRole))
                    //    approvers.Add(user.UserName);

                    foreach (IdentityUserRole role in user.Roles)
                    {
                        if (role.RoleId == approverRole.Id)
                        {
                            approvers.Add(new SelectListItem { Text = user.UserName, Value = user.UserName });
                            break;
                        }
                    }
                }
            }
            return approvers;
        }

        private IEnumerable<UserRoleManagementViewModel> GetModel(string userId)
        {
            var appUsers = _userManager.Users.Where(u => u.Id == userId).ToList();
            var userRoles = appUsers.Select(u => new UserRoleManagementViewModel
                                            {
                                                UserId = u.Id,
                                                UserName = u.UserName,
                                                UserRoles = u.Roles.Join(_dbContext.Roles,
                                                                            ur => ur.RoleId,
                                                                            dr => dr.Id,
                                                                            (ur, dr) => new CustomTuple
                                                                            {
                                                                                Id = dr.Id,
                                                                                Text = dr.Name
                                                                            })
                                                                    .ToList()
                                            })
                                     .ToList();
            return userRoles;
        }
    }
}
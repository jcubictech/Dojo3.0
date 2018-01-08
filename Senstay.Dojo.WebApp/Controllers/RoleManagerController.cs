using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class RoleManagerController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;
        private readonly ApplicationRoleManager _roleManager;

        public RoleManagerController(DojoDbContext context, ApplicationRoleManager roleManager)
        {
            _dbContext = context;
            _roleManager = roleManager;
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            string roleName = string.Empty;
            try
            {
                RoleManagementViewModel role = JsonConvert.DeserializeObject<RoleManagementViewModel>(model);
                ApplicationRole newRole = new ApplicationRole() { Name = role.RoleName };

                IdentityResult result = _roleManager.Create(newRole);
                if (result == IdentityResult.Success)
                {
                    roleName = newRole.Name;
                    role.RoleId = _roleManager.Roles.Select(r => r.Id).First();
                    Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                    return Json(role, JsonRequestBehavior.AllowGet);
                }
                throw new System.Exception("New role cannot be created.");
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("Create role '" + roleName + "' fails. " + ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve()
        {
            if (_roleManager != null)
            {
                var allAppRoles = _roleManager.Roles.ToList();
                var allUsers = allAppRoles.Select(r => new RoleManagementViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name
                });

                return Json(allUsers, JsonRequestBehavior.AllowGet);
            }
            else
                return JsonError("Role manager does not exist");
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            string roleName = string.Empty;
            try
            {
                RoleManagementViewModel role = JsonConvert.DeserializeObject<RoleManagementViewModel>(model);
                var roleToUpdate = _roleManager.Roles.Where(r => r.Id == role.RoleId).First();
                if (roleToUpdate != null)
                {
                    roleName = roleToUpdate.Name;
                    roleToUpdate.Name = role.RoleName;
                    roleToUpdate.Id = role.RoleId;
                    IdentityResult result = _roleManager.Update(roleToUpdate);
                    if (result == IdentityResult.Success)
                    {
                        Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        return Json(role, JsonRequestBehavior.AllowGet);
                    }
                }
                throw new System.Exception("Role does not exist.");
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("Update role '" + roleName + "' fails. " + ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            string roleName = string.Empty;
            try
            {
                RoleManagementViewModel role = JsonConvert.DeserializeObject<RoleManagementViewModel>(model);
                var roleToDelete = _roleManager.Roles.Where(r => r.Id == role.RoleId).First();
                if (roleToDelete != null)
                {
                    roleName = roleToDelete.Name;
                    IdentityResult result = _roleManager.Delete(roleToDelete);
                    if (result == IdentityResult.Success)
                    {
                        Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                        return Json("success", JsonRequestBehavior.AllowGet);
                    }
                }
                throw new System.Exception("Role does not exist.");
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("Delete role '" + roleName + "' fails. " + ex.Message, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
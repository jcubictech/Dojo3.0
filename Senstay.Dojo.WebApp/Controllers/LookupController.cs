using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;
using Newtonsoft.Json;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Controllers
{
    [Authorize(Roles = AppConstants.ADMIN_ROLE + "," + AppConstants.SUPER_ADMIN_ROLE)]
    [CustomHandleError]
    public class LookupController : AppBaseController
    {
        private readonly DojoDbContext _dbContext;

        public LookupController(DojoDbContext context)
        {
            _dbContext = context;
        }

        [HttpPost]
        public JsonResult Create(string model)
        {
            string typeName = string.Empty;
            string reason = string.Empty;
            string message = string.Empty;
            try
            {
                Lookup item = JsonConvert.DeserializeObject<Lookup>(model);
                typeName = item.Type;
                LookupProvider provider = new LookupProvider(_dbContext);

                // check to make sure the lookup name is not used anywhere
                LookupType type;
                if (Enum.TryParse(typeName, out type))
                {
                    if (provider.Exist(type, item.Name) || provider.InUse(type, item.Name)) // new lookup name is in used; can't change it
                    {
                        reason = "Conflict";
                        message = InUseMessage(typeName, item.Name);
                    }
                    else
                    {
                        provider.Create(item);
                        provider.Commit();
                        if (item.Id == 0) item.Id = provider.GetKey(item);
                        return Json(item, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    reason = "Bad Type";
                    message = TypeNotFound(typeName);
                }

            }
            catch(Exception ex)
            {
                reason = "Server Error";
                message = ex.Message + " " + ex.InnerException.Message;
            }

            // assemble custom error for kendo CRID operation
            Response.StatusCode = (int)System.Net.HttpStatusCode.OK; // custom error return 200 code
            var exception = new // custom response body indicated by the 'errors' field
            {
                errors = CrudError("Create", typeName, reason, message)
            };
            return Json(exception, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(string type)
        {
            try
            {
                LookupProvider provider = new LookupProvider(_dbContext);
                var allItems = provider.All(type);
                return Json(allItems, JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json("Retrieve " + type + " lookup table fails. " + ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Update(string model)
        {
            string typeName = string.Empty;
            string reason = string.Empty;
            string message = string.Empty;
            try
            {
                Lookup item = JsonConvert.DeserializeObject<Lookup>(model);
                typeName = item.Type;
                LookupProvider provider = new LookupProvider(_dbContext);

                // check to make sure the lookup name is not used anywhere
                LookupType type;
                if (Enum.TryParse(typeName, out type))
                {
                    // make sure the original one is not in used
                    Lookup oldItem = provider.Retrieve(item.Id);
                    if (provider.InUse(type, oldItem.Name)) // old lookup name is in used; can't change it
                    {
                        reason = "Conflict";
                        message = string.Format("Cannot change {0} type of '{1}' that is still in used.", typeName, oldItem.Name);
                    }
                    else if (provider.Exist(type, item.Name) || provider.InUse(type, item.Name)) // new lookup name is in used; can't change it
                    {
                        reason = "Conflict";
                        message = InUseMessage(typeName, item.Name);
                    }
                    else
                    {
                        provider.Update(item.Id, item);
                        provider.Commit();
                        return Json(item, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    reason = "Bad Type";
                    message = TypeNotFound(typeName);
                }
            }
            catch (Exception ex)
            {
                reason = "Server Error";
                message = ex.Message + " " + ex.InnerException.Message;
            }

            // assemble custom error for kendo CRUD operation
            Response.StatusCode = (int)System.Net.HttpStatusCode.OK; // custom error return 200 code
            var exception = new // custom response body indicated by the 'errors' field
            {
                errors = CrudError("Update", typeName, reason, message)
            };
            return Json(exception, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Delete(string model)
        {
            string typeName = string.Empty;
            string reason = string.Empty;
            string message = string.Empty;
            try
            {
                Lookup item = JsonConvert.DeserializeObject<Lookup>(model);
                typeName = item.Type;
                LookupProvider provider = new LookupProvider(_dbContext);

                // check to make sure the lookup exists and is not used anywhere
                LookupType type;
                if (Enum.TryParse(typeName, out type))
                {
                    if (provider.InUse(type, item.Name))
                    {
                        reason = "Conflict";
                        message = InUseMessage(typeName, item.Name);
                    }
                    else
                    {
                        provider.Delete(item.Id);
                        provider.Commit();
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    reason = "Bad Type";
                    message = TypeNotFound(typeName);
                }
            }
            catch (Exception ex)
            {
                reason = "Server Error";
                message = ex.Message + " " + ex.InnerException.Message;
            }

            // assemble custom error for kendo CRID operation
            Response.StatusCode = (int)System.Net.HttpStatusCode.OK; // custom error return 200 code
            var exception = new // custom response body indicated by the 'errors' field
            {
                errors = CrudError("Delete", typeName, reason, message)
            };
            return Json(exception, JsonRequestBehavior.AllowGet);
        }

        private string InUseMessage(string typeName, string name)
        {
            return string.Format("{0} name of '{1}' is in used.", typeName, name);
        }

        private string TypeNotFound(string typeName)
        {
            return string.Format("Type '{0}' does not exist.", typeName);
        }

        private string CrudError(string action, string typeName, string reason, string message)
        {
            return string.Format("{0} {1} fails. {2}: {3}", action, typeName, reason, message);
        }
    }
}
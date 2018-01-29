using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Security.Claims;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Infrastructure.Alerts;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class PropertyController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public PropertyController(DojoDbContext context)
        {
            _dbContext = context;
            ViewBag.PermissionClass = AuthorizationProvider.IsPropertyEditor() ? "app-grid-edit" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.IsPropertyEditor() ? string.Empty : " app-field-readonly";
        }

        public ActionResult Index()
        {
            return View(new PropertyViewModel());
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetPropertyCodes()
        {
            var provider = new PropertyProvider(_dbContext);
            var propertyCodes = provider.GetPropertyCodes();
            return Json(propertyCodes, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetOwnerPayoutAccounts(DateTime month)
        {
            var provider = new PropertyProvider(_dbContext);
            var accounts = provider.GetOwnerPayoutAccounts(month);
            return Json(accounts, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetPayoutMethods(DateTime month)
        {
            var provider = new PropertyProvider(_dbContext);
            var payoutMethodList = provider.PayoutMethods(month);
            return Json(payoutMethodList, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetOwnerStatementPropertyList(DateTime month)
        {
            var provider = new PropertyProvider(_dbContext);
            var propertyList = provider.GetOwnerStatementPropertyList(month);
            return Json(propertyList, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(DateTime beginDate, DateTime endDate, bool isActive = true, bool isPending = true, bool isDead = false)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            PropertyProvider dataProvider = new PropertyProvider(_dbContext);
            var properties = dataProvider.Retrieve(beginDate, endDate, isActive, isPending, isDead);
            return Json(properties, JsonRequestBehavior.AllowGet);

            // need to use Newtonsoft.Json to handle object inside an object json serialization
            //var properties = JsonConvert.SerializeObject(model, Formatting.None, new JsonSerializerSettings()
            //                    {
            //                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            //                    });
            //return Content(properties, "application/json");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult New()
        {
            ViewBag.Title = "New Property";
            ViewBag.ButtonText = "Add Property";

            var property = new CPL();
            InitSelectFields(property);
            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            return PartialView("_PropertyFormPartial", property);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ModalDetails(string Id)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return PartialView("DetailsPartial", new CPL());
            }

            try
            {
                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);

                ViewBag.Title = "View Property Details";
                CPL details = propertyProvider.Retrieve(Id);
                if (details == null)
                {
                    string message = string.Format("Property '{0}' not found.", Id);
                    DojoLogger.Warn(message, typeof(PropertyController));
                    return RedirectToAction("NotFound", "Error");
                }
                return PartialView("DetailsPartial", details);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Property Details fails. {0}", ex.Message);
                DojoLogger.Error(message, typeof(PropertyController));
            }

            return RedirectToAction("Index", "Property")
                .WithError("The Property cannot be found.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ModalEdit(String Id)
        {
            if (!AuthorizationProvider.IsPropertyEditor() && !AuthorizationProvider.IsViewer())
            {
                return RedirectToAction("Index", "Property")
                    .WithError("It looks like you do not have permisssion to edit this property.");
            }

            try
            {
                ViewBag.Title = "Edit Property";
                ViewBag.ButtonText = "Update Property";

                PropertyProvider dataProvider = new PropertyProvider(_dbContext);
                CPL property = dataProvider.Retrieve(Id);
                if (property == null) return RedirectToAction("NotFound", "Error");

                InitSelectFields(property);
                ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
                return PartialView("_PropertyFormPartial", property);
            }
            catch (Exception)
            {
                // TODO: log
            }

            return RedirectToAction("Index", "Property")
                .WithError("The property item cannot be found.");
        }

        [HttpPost]
        public JsonResult ModalEdit(CPL form)
        {
            if (!AuthorizationProvider.IsPropertyEditor())
            {
                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    PropertyProvider dataProvider = new PropertyProvider(_dbContext);

                    // make the date to PST time zone to store in DB using UTC
                    if (form.ListingStartDate != null) form.ListingStartDate = form.ListingStartDate.Value.Date.AddHours(11);
                    if (form.PendingContractDate != null) form.PendingContractDate = form.PendingContractDate.Value.Date.AddHours(11);
                    if (form.PendingOnboardingDate != null) form.PendingOnboardingDate = form.PendingOnboardingDate.Value.Date.AddHours(11);

                    bool managementFeeChanged = false;
                    bool titleChanged = false;
                    if (!dataProvider.PropertyExist(form.PropertyCode)) // new property
                    {
                        titleChanged = true;
                        managementFeeChanged = true;
                        form.PropertyCode = form.PropertyCode.ToUpper();
                        // if entity state is EntityState.UnAttached, CreatedDate won't be created. we set it explicitly here just to be sure
                        form.CreatedDate = ConversionHelper.EnsureUtcDate(DateTime.Now.Date);
                        form.CreatedBy = ClaimsPrincipal.Current.Identity.Name;
                        dataProvider.Create(form);
                    }
                    else // updating property
                    {
                        var property = dataProvider.Retrieve(form.PropertyCode);
                        if (property != null)
                        {
                            titleChanged = string.Compare(property.AirBnBHomeName, form.AirBnBHomeName, true) != 0;
                            managementFeeChanged = property.Ownership != form.Ownership;

                            if (string.Compare(property.PropertyStatus, form.PropertyStatus, true) != 0)
                                setInactiveTimestamp(form);

                            dataProvider.Update(form.PropertyCode, form);
                        }
                    }
                    dataProvider.Commit();

                    // update PropertyTitleHistory table if title has changed
                    if (titleChanged)
                    {
                        var titleProvider = new PropertyTitleHistoryProvider(_dbContext);
                        if (!titleProvider.Exist(form.PropertyCode, form.AirBnBHomeName))
                        {
                            var titleHistory = new PropertyTitleHistory()
                            {
                                PropertyCode = form.PropertyCode,
                                PropertyTitle = form.AirBnBHomeName,
                                EffectiveDate = ConversionHelper.EnsureUtcDate(DateTime.UtcNow)
                            };
                            titleProvider.Create(titleHistory);
                            titleProvider.Commit();
                        }
                    }

                    // obsolete: Property fee has moved to property fee table
                    if (managementFeeChanged)
                    {
                        // TODO: add a record to PropertyFee table for new management fee
                    }

                    ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
                    return Json(form.PropertyCode, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                // TODO: log
                this.ModelState.AddModelError("", ex);
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Delete(string id)
        {
            if (!AuthorizationProvider.IsPropertyEditor())
            {
                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                PropertyProvider dataProvider = new PropertyProvider(_dbContext);
                dataProvider.Delete(id);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                // TODO: logging
            }
            return Json("fail", JsonRequestBehavior.AllowGet);
        }

        private void InitSelectFields(CPL cpl)
        {
            //if (cpl.NeedsOwnerApproval == null) cpl.NeedsOwnerApproval = false;
            if (cpl.PropertyStatus == null) cpl.PropertyStatus = string.Empty;
            if (cpl.BeltDesignation == null) cpl.BeltDesignation = string.Empty;
            if (cpl.Account == null) cpl.Account = string.Empty;
            if (cpl.Market == null) cpl.Market = string.Empty;
            if (cpl.Vertical == null) cpl.Vertical = string.Empty;
            if (cpl.Area == null) cpl.Area = string.Empty;
            if (cpl.Neighborhood == null) cpl.Neighborhood = string.Empty;
            if (cpl.City == null) cpl.City = string.Empty;
            if (cpl.Elevator == null) cpl.Elevator = string.Empty;
            if (cpl.Pool == null) cpl.Pool = string.Empty;
        }

        private void setInactiveTimestamp(CPL form)
        {
            // set inactive or dead timestamp if its value is changed
            if (form.PropertyStatus.ToLower() == "dead")
            {
                form.Dead = DateTime.UtcNow;
                form.Inactive = null;
            }
            else if (form.PropertyStatus.ToLower() == "inactive")
            {
                form.Inactive = DateTime.UtcNow;
                form.Dead = null;
            }
            else
            {
                form.Inactive = null;
                form.Dead = null;
            }
        }

        #region methods not used
        public ActionResult Info(string Id)
        {
            try
            {
                ViewBag.Title = "View Property";
                PropertyProvider dataProvider = new PropertyProvider(_dbContext);
                CPL property = dataProvider.Retrieve(Id);
                if (property == null) return RedirectToAction("NotFound", "Error");
                var account = new SelectListItem { Text = property.Account, Value = property.Account };
                ViewBag.Accounts = new List<SelectListItem>();
                ViewBag.Accounts.Add(account);
                return View("Entry", property);
            }
            catch (Exception ex)
            {
                // TODO: log
            }

            return RedirectToAction("Index", "Property")
                .WithError("The property item cannot be found.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Edit(String Id)
        {
            try
            {
                ViewBag.Title = "Edit Property";
                ViewBag.ButtonText = "Update Property";

                PropertyProvider dataProvider = new PropertyProvider(_dbContext);
                CPL property = dataProvider.Retrieve(Id);
                if (property == null) return RedirectToAction("NotFound", "Error");
                ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
                return View("Edit", property);
            }
            catch(Exception ex)
            {
                // TODO: log
            }

            return RedirectToAction("Index", "Property")
                .WithError("The property item cannot be found.");
        }

        [HttpPost]
        public ActionResult Edit(CPL cpl)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    PropertyProvider dataProvider = new PropertyProvider(_dbContext);
                    dataProvider.Update(cpl.PropertyCode, cpl);
                    dataProvider.Commit();
                    return RedirectToAction("Index");
                }
            }
            catch(Exception ex)
            {
                // TODO: log
            }

            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            return View("Entry", "Property", cpl)
                    .WithError("The property item cannot be updated.");
        }
        #endregion
    }
}

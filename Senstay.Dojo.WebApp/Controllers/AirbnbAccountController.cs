using System;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Infrastructure.Alerts;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class AirbnbAccountController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public AirbnbAccountController(DojoDbContext context)
        {
            _dbContext = context;
            ViewBag.PermissionClass = AuthorizationProvider.IsAccountEditor() ? "app-grid-edit" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.IsAccountEditor() ? string.Empty : " app-field-readonly";
        }

        public ActionResult Index()
        {
            return View(new AirbnbAccountViewModel());
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(DateTime beginDate, DateTime endDate)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
            var AirbnbAccounts = dataProvider.Retrieve(beginDate, endDate);
            return Json(AirbnbAccounts, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult New()
        {
            ViewBag.Title = "New Airbnb Account";
            ViewBag.ButtonText = "Add Airbnb Account";

            var account = new AirbnbAccount();
            account.Vertical = string.Empty;
            account.Status = string.Empty;
            return PartialView("_AirbnbAccountNewFormPartial", account);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ModalEdit(int Id)
        {
            if (!AuthorizationProvider.IsAccountEditor() && !AuthorizationProvider.IsViewer())
            {
                return RedirectToAction("Index", "AirbnbAccount")
                    .WithError("It looks like you do not have permisssion to edit this Airbnb account.");
            }

            try
            {
                ViewBag.Title = "Edit Airbnb Account";
                ViewBag.ButtonText = "Update Airbnb Account";

                AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
                AirbnbAccount account = dataProvider.Retrieve(Id);
                if (account == null) return RedirectToAction("NotFound", "Error");

                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);
                var relatedProperties = propertyProvider.GetAirbnbAccountRelatedProperties(account.Email);
                account.ActiveListings = relatedProperties.ActiveListings;
                account.In_activeListings = relatedProperties.InactiveListings;
                account.ofListingsinLAMarket = relatedProperties.ListingsInLAMarket;
                account.ofListingsinNYCMarket = relatedProperties.ActiveListings;
                account.Pending_Onboarding = relatedProperties.PendingOnboarding;
                if (account.Vertical == null) account.Vertical = string.Empty;
                if (account.Status == null) account.Status = string.Empty;

                return PartialView("_AirbnbAccountEditFormPartial", account);
            }
            catch (Exception ex)
            {
                // TODO: log
            }

            return RedirectToAction("Index", "AirbnbAccount")
                .WithError("The Airbnb Account item cannot be found.");
        }

        [HttpPost]
        public JsonResult ModalEdit(AirbnbAccount form)
        {
            if (!AuthorizationProvider.IsAccountEditor())
            {
                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);

                    // make the date to PST time zone to store in DB using UTC
                    if (form.DateAdded != null) form.DateAdded = form.DateAdded.Value.Date.AddHours(11);
                    if (form.DOB1 != null) form.DOB1 = form.DOB1.Value.Date.AddHours(11);
                    if (form.DOB2 != null) form.DOB2 = form.DOB2.Value.Date.AddHours(11);

                    if (form.Id == 0) // new airbnb account
                    {
                        //form.DateAdded = DateTime.Now.Date;
                        PropertyProvider propertyProvider = new PropertyProvider(_dbContext);
                        var relatedProperties = propertyProvider.GetAirbnbAccountRelatedProperties(form.Email);
                        form.ActiveListings = relatedProperties.ActiveListings;
                        form.In_activeListings = relatedProperties.InactiveListings;
                        form.ofListingsinLAMarket = relatedProperties.ListingsInLAMarket;
                        form.ofListingsinNYCMarket = relatedProperties.ActiveListings;
                        form.Pending_Onboarding = relatedProperties.PendingOnboarding;
                        dataProvider.Create(form);
                    }
                    else // updating airbnb account
                    {
                        var account = dataProvider.Retrieve(form.Id);
                        if (account != null)
                        {
                            // need to set CreatedBy and CreatedDate a they are not part of form
                            form.CreatedDate = account.CreatedDate;
                            form.CreatedBy = account.CreatedBy;
                            dataProvider.Update(form.Id, form);
                        }
                    }
                    dataProvider.Commit();

                    return Json(form.Id.ToString(), JsonRequestBehavior.AllowGet);
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
        public JsonResult Delete(int id)
        {
            if (!AuthorizationProvider.IsAccountEditor())
            {
                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
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

        #region for stand alone edit form page; not used as dialog form is used
        [HttpPost]
        public ActionResult Edit(AirbnbAccount AirbnbAccount)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
                    if (AirbnbAccount.Id == 0)
                    {
                        dataProvider.Create(AirbnbAccount);
                    }
                    else
                    {
                        dataProvider.Update(AirbnbAccount.Id, AirbnbAccount);
                    }
                    dataProvider.Commit();
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                // TODO: log
            }

            return View("Entry", "AirbnbAccount", AirbnbAccount)
                    .WithError("The Airbnb Account item cannot be updated.");
        }

        public ActionResult Edit(int id)
        {
            try
            {
                ViewBag.Title = "Edit Airbnb Account";
                ViewBag.ButtonText = "Update Airbnb Account";

                AirbnbAccountProvider dataProvider = new AirbnbAccountProvider(_dbContext);
                AirbnbAccount AirbnbAccount = dataProvider.Retrieve(id);
                if (AirbnbAccount == null) return RedirectToAction("NotFound", "Error");
                return View("Edit", AirbnbAccount);
            }
            catch(Exception ex)
            {
                // TODO: log
            }

            return RedirectToAction("Index", "AirbnbAccount")
                .WithError("The Airbnb Account item cannot be found.");
        }
        #endregion
    }
}

using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using AutoMapper;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Infrastructure.Alerts;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class InquiryController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public InquiryController(DojoDbContext context)
        {
            _dbContext = context;
            ViewBag.PermissionClass = AuthorizationProvider.IsInquiryEditor() ? "app-grid-edit" : string.Empty;
            ViewBag.ApprovalClass = AuthorizationProvider.IsAppover() ? "app-grid-approve" : string.Empty;
            ViewBag.EditClass = AuthorizationProvider.IsInquiryEditor() ? string.Empty : " app-field-readonly";

            // note: the following setting affect MVC serialization. Use Newtonsoft.Json to work around it.

            // if false, it will not get the associated certification and skills when we get the applicants.
            //_dbContext.Configuration.ProxyCreationEnabled = false;

            // Load navigation properties explicitly (avoid serialization trouble)
            //_dbContext.Configuration.LazyLoadingEnabled = false;

            // Because Web API will perform validation, we don't need/want EF to do so
            //_dbContext.Configuration.ValidateOnSaveEnabled = false;

            // We won't use this performance tweak because we don't need
            // the extra performance and, when autodetect is false,
            // we'd have to be careful. We're not being that careful.
            //_dbContext.Configuration.AutoDetectChangesEnabled = false;
        }

        public ActionResult Index(int id = 0)
        {
            var model = new InquiryViewModel();

            // old Dojo logic gets all inquiries if id is not found. so emulate it here.
            model.InquiryId = id;
            if (id != 0)
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                InquiriesValidation inquiry = inquiryProvider.Retrieve(id);
                if (inquiry == null) model.InquiryId = 0;
            }

            model.UserName = this.User.Identity.Name;
            // hack here: for admin role, we set the UserName to 'DelegateDeletion' to allow admin to delete other's inquiry
            if (AuthorizationProvider.IsAdmin()) model.UserName = "DelegateDeletion";

            return View(model);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult Retrieve(DateTime beginDate, DateTime endDate)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }

            InquiryProvider dataProvider = new InquiryProvider(_dbContext);
            var inquiries = dataProvider.Retrieve(beginDate, endDate);
            return Json(inquiries, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Property(int Id)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return PartialView("PropertyPartial", new InquiriesValidation());
            }

            try
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);

                ViewBag.Title = "View Property Information";
                InquiriesValidation inquiry = inquiryProvider.Retrieve(Id);
                if (inquiry == null)
                {
                    string message = string.Format("Inquiry {0} not found.", Id.ToString());
                    DojoLogger.Warn(message, typeof(InquiryController));

                    return RedirectToAction("NotFound", "Error");
                }

                // make up Airbnb property link if it does not already exist
                if (string.IsNullOrEmpty(inquiry.CPL.AirBnb) && !string.IsNullOrEmpty(inquiry.CPL.AIrBnBID))
                    inquiry.CPL.AirBnb = string.Format(AppConstants.AIRBNB_URL_TEMPLATE, inquiry.CPL.AIrBnBID);

                return PartialView("PropertyPartial", inquiry);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Inquiry Property Info fails. {0}", ex.Message);
                DojoLogger.Error(message, typeof(InquiryController));
            }

            return RedirectToAction("Index", "Inquiry")
                .WithError("The inquiry item cannot be found.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Info(int Id)
        {
            if (!AuthorizationProvider.IsViewer())
            {
                return PartialView("InfoPartial", new InquiriesValidation());
            }

            try
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);

                ViewBag.Title = "View Inquiry Information";
                InquiriesValidation inquiry = inquiryProvider.Retrieve(Id);
                if (inquiry == null)
                {
                    string message = string.Format("Inquiry {0} not found.", Id.ToString());
                    DojoLogger.Warn(message, typeof(InquiryController));

                    return RedirectToAction("NotFound", "Error");
                }

                // make up Airbnb property link if it does not already exist
                if (string.IsNullOrEmpty(inquiry.CPL.AirBnb) && !string.IsNullOrEmpty(inquiry.CPL.AIrBnBID))
                    inquiry.CPL.AirBnb = string.Format(AppConstants.AIRBNB_URL_TEMPLATE, inquiry.CPL.AIrBnBID);

                return PartialView("InfoPartial", inquiry);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Inquiry Info fails. {0}", ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(InquiryController));
            }

            return RedirectToAction("Index", "Inquiry")
                .WithError("The inquiry item cannot be found.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult New()
        {
            ViewBag.Title = "New Inquiry";
            ViewBag.ButtonText = "Add Inquiry";
            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            var inquiry = new InquiriesValidation();
            inquiry.InquiryTeam = ViewBag.UserName;
            return PartialView("EditPartial", inquiry);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Edit(int Id)
        {
            if (!AuthorizationProvider.IsInquiryEditor() && !AuthorizationProvider.IsViewer())
            {
                string message = string.Format("User '{0}' does not have permission to edit Inquiry {1}.", this.User.Identity.Name, Id.ToString());
                DojoLogger.Warn(message, typeof(InquiryController));

                return RedirectToAction("Index", "Inquiry")
                    .WithError("It looks like you do not have permisssion to edit this inquiry.");
            }

            try
            {
                ViewBag.Title = "Edit Inquiry";
                ViewBag.ButtonText = "Update Inquiry";

                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);
                InquiriesValidation inquiry = inquiryProvider.Retrieve(Id);
                if (inquiry == null) return RedirectToAction("NotFound", "Error");
                ViewBag.Properties = propertyProvider.AggregatedProperties();
                return PartialView("EditPartial", inquiry);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Inquiry {0} for Editing fails. {1}", Id.ToString(), ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(InquiryController));
            }

            return RedirectToAction("Index", "Inquiry")
                .WithError("The inquiry item cannot be found.");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Approve(int Id)
        {
            if (!AuthorizationProvider.IsInquiryEditor() && !AuthorizationProvider.IsViewer())
            {
                string message = string.Format("User '{0}' does not have permission to edit Inquiry {1}.", this.User.Identity.Name, Id.ToString());
                DojoLogger.Warn(message, typeof(InquiryController));

                return RedirectToAction("Index", "Inquiry")
                    .WithError("It looks like you do not have permisssion to edit this inquiry.");
            }

            try
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);

                ViewBag.Title = "Approve Inquiry";
                ViewBag.ButtonText = "Approve Inquiry";
                InquiriesValidation inquiry = inquiryProvider.Retrieve(Id);
                if (inquiry == null) return RedirectToAction("NotFound", "Error");

                // get the owner approval flag (string constant defined in ListProvider)
                inquiry.OwnerApprovalNeeded = (inquiry.CPL.NeedsOwnerApproval != null && inquiry.CPL.NeedsOwnerApproval.Value)
                                              ? (inquiry.CPL.NeedsOwnerApproval.Value ? "YES" : "NO")
                                              : "N/A";
                if (inquiry.PricingApprover1 == null) inquiry.PricingApprover1 = ViewBag.UserName;
                if (inquiry.PricingApprover2 == null) inquiry.PricingApprover2 = ViewBag.UserName;
                if (inquiry.PricingDecision1 == null) inquiry.PricingDecision1 = string.Empty;

                return PartialView("ApprovePartial", inquiry);
            }
            catch (Exception ex)
            {
                string message = string.Format("Retrieve Inquiry {0} for Approval fails. {1}", Id.ToString(), ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(InquiryController));
            }

            return PartialView("ApprovePartial", new InquiriesValidation());
        }

        [HttpPost]
        public JsonResult Save(InquiriesValidation form)
        {
            if (!AuthorizationProvider.IsInquiryEditor())
            {
                string message = string.Format("User '{0}' does not have permission to save Inquiry {1}.", this.User.Identity.Name, form.Id.ToString());
                DojoLogger.Warn(message, typeof(InquiryController));
                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                // treat checkin and checkout date as Hawaii time zone and covert it to UTC by adding 11 hours.
                if (form.Check_inDate != null) form.Check_inDate = form.Check_inDate.Value.Date.AddHours(11);
                if (form.Check_outDate != null) form.Check_outDate = form.Check_outDate.Value.Date.AddHours(11);

                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);

                string message = string.Format("saving inquiry: name: {0} team: {1} property: {2} checkin: {3} checkout: {4} payout: {5}",
                                                form.GuestName,
                                                form.InquiryTeam,
                                                (!string.IsNullOrEmpty(form.PropertyCode) ? form.PropertyCode : "Invalid"),
                                                (form.Check_inDate != null ? form.Check_inDate.Value.ToShortDateString() : "Invalid"),
                                                (form.Check_outDate != null ? form.Check_outDate.Value.ToShortDateString() : "Invalid"),
                                                (form.TotalPayout != null ? form.TotalPayout.Value.ToString() : "Invalid"));
                DojoLogger.Trace(message, typeof(InquiryController));

                if (form.Id == 0) // new inquiry
                {
                    if (inquiryProvider.Exist(form.PropertyCode, form.GuestName, form.Check_inDate.Value, form.Check_outDate.Value))
                    {
                        Response.StatusCode = (int)System.Net.HttpStatusCode.Conflict; // code = 409
                        return Json(string.Empty, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        SetRelatedFields(form);
                        inquiryProvider.Create(form);
                    }
                }
                else // updating inquiry
                {
                    InquiriesValidation inquiry = inquiryProvider.Retrieve(form.Id);
                    SetRelatedFields(inquiry, form);
                    // need to set CreatedBy and CreatedDate a they are not part of form
                    form.CreatedDate = inquiry.CreatedDate;
                    form.CreatedBy = inquiry.CreatedBy;
                    inquiryProvider.Update(inquiry.Id, inquiry);
                }

                inquiryProvider.Commit(); // Id will be filled for new inquiry by EF

                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);
                ViewBag.Properties = propertyProvider.AggregatedProperties();
                return Json(form.Id.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Saving Inquiry {0} fails. {1}", form.Id.ToString(), ex.Message);
                DojoLogger.Error(message, typeof(InquiryController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(string.Empty, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!AuthorizationProvider.IsInquiryEditor())
            {
                string message = string.Format("User '{0}' does not have permission to delete Inquiry {1}.", this.User.Identity.Name, id.ToString());
                DojoLogger.Warn(message, typeof(InquiryController));

                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                InquiryProvider dataProvider = new InquiryProvider(_dbContext);
                // TODO: need to check if the same user is deleting the inquiry
                dataProvider.Delete(id);
                dataProvider.Commit();
                return Json("success", JsonRequestBehavior.AllowGet);
            }
            catch(Exception ex)
            {
                string message = string.Format("Delete Inquiry {0} fails. {1}", id.ToString(), ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(InquiryController));
            }
            return Json("fail", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveApproveStatus(InquiriesValidation form)
        {
            if (!AuthorizationProvider.IsInquiryEditor())
            {
                string message = string.Format("User '{0}' does not have permission to save approval status for Inquiry {1}.", this.User.Identity.Name, form.Id.ToString());
                DojoLogger.Warn(message, typeof(InquiryController));

                return Json("denied", JsonRequestBehavior.AllowGet);
            }

            try
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                InquiriesValidation inquiry = inquiryProvider.Retrieve(form.Id);
                SetApproveFields(inquiry, form);
                inquiryProvider.Update(inquiry.Id, inquiry);
                inquiryProvider.Commit();

                return Json(form.Id.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                string message = string.Format("Save Apporval Ststus for Inquiry {0} fails. {1}", form.Id.ToString(), ex.Message + ex.StackTrace);
                DojoLogger.Error(message, typeof(InquiryController));
            }
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SearchID(string inquiryId)
        {
            RouteValueDictionary queryParam = new RouteValueDictionary();
            queryParam.Add("id", inquiryId);
            return RedirectToAction("Index", "Inquiry", queryParam);
        }

        private void SetRelatedFields(InquiriesValidation inquiry)
        {
            if (inquiry.CPL == null) inquiry.CPL = _dbContext.CPLs.Find(inquiry.PropertyCode);
            inquiry.InquiryCreatedTimestamp = DateTime.Now;
            inquiry.Check_InDay = inquiry.Check_inDate.HasValue ? inquiry.Check_inDate.Value.DayOfWeek.ToString() : string.Empty;
            inquiry.Check_OutDay = inquiry.Check_outDate.HasValue ? inquiry.Check_outDate.Value.DayOfWeek.ToString() : string.Empty;

            if (inquiry.Check_inDate.HasValue)
            {
                inquiry.DaysOut = (inquiry.Check_inDate.Value - inquiry.InquiryCreatedTimestamp.Value).Days;
                if (inquiry.Check_outDate.HasValue)
                    inquiry.LengthofStay = (inquiry.Check_outDate.Value - inquiry.Check_inDate.Value).Days;
            }

            inquiry.NightlyRate = ComputeNightlyRate(inquiry);

            if (inquiry.CPL != null)
            {
                if (inquiry.CPL.BeltDesignation != null && "black belt".Equals(inquiry.CPL.BeltDesignation.ToLower()))
                {
                    inquiry.PricingApprover2 = "Pending Review";
                }

                if (inquiry.CPL.NeedsOwnerApproval.HasValue && inquiry.CPL.NeedsOwnerApproval.Value)
                {
                    inquiry.ApprovedbyOwner = "PENDING";
                }
                else
                {
                    inquiry.ApprovedbyOwner = "N/A";
                }
            }
        }

        private void SetRelatedFields(InquiriesValidation inquiry, InquiriesValidation form)
        {
            // use inquiry object as form may not have all the fields
            inquiry.PropertyCode = form.PropertyCode;
            inquiry.GuestName = form.GuestName;
            inquiry.InquiryTeam = form.InquiryTeam;
            inquiry.Channel = form.Channel;
            inquiry.TotalPayout = form.TotalPayout;
            inquiry.Check_inDate = form.Check_inDate;
            inquiry.Check_outDate = form.Check_outDate;
            inquiry.Weekdayorphandays = form.Weekdayorphandays;
            inquiry.AdditionalInfo_StatusofInquiry = form.AdditionalInfo_StatusofInquiry;
            if (inquiry.CPL == null) inquiry.CPL = _dbContext.CPLs.Find(inquiry.PropertyCode);

            if (form.Check_inDate != null)
            {
                inquiry.Check_InDay = form.Check_inDate.Value.DayOfWeek.ToString();
            }

            if (form.Check_outDate != null)
            {
                inquiry.Check_OutDay = form.Check_outDate.Value.DayOfWeek.ToString();
                inquiry.DaysOut = (form.Check_outDate.Value - inquiry.InquiryCreatedTimestamp).Value.Days;
                if (form.Check_inDate != null)
                    inquiry.LengthofStay = (form.Check_outDate.Value - form.Check_inDate.Value).Days;
                else
                    inquiry.LengthofStay = null;
            }

            inquiry.NightlyRate = ComputeNightlyRate(inquiry);
        }

        private void SetApproveFields(InquiriesValidation inquiry, InquiriesValidation form)
        {
            inquiry.Doesitrequire2pricingteamapprovals = form.Doesitrequire2pricingteamapprovals;
            inquiry.PricingApprover1 = form.PricingApprover1;
            inquiry.PricingApprover2 = form.PricingApprover2;
            inquiry.PricingDecision1 = form.PricingDecision1;
            inquiry.PricingReason1 = form.PricingReason1;
            inquiry.ApprovedbyOwner = form.ApprovedbyOwner;
        }

        private decimal? ComputeNightlyRate(InquiriesValidation inquiry)
        {
            decimal? nightlyRate = null;
            if (inquiry.LengthofStay != null && inquiry.LengthofStay.Value != 0 && inquiry.CPL != null)
            {
                if (inquiry.CPL.CleaningFees != null)
                    nightlyRate = ((inquiry.TotalPayout.Value / (decimal)0.97) - inquiry.CPL.CleaningFees.Value) / inquiry.LengthofStay;
                else
                    nightlyRate = (inquiry.TotalPayout.Value / (decimal)0.97) / inquiry.LengthofStay;
            }

            return nightlyRate;
        }

        #region these methods are not used in this app
        [HttpPost]
        public PartialViewResult Edit(InquiriesValidation form)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                    inquiryProvider.Update(form.Id, form);
                    inquiryProvider.Commit();
                }
            }
            catch (Exception ex)
            {
                // TODO: Log error
                this.ModelState.AddModelError("", ex);
            }

            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            return PartialView("EditPartial", form);
        }

        [HttpPost]
        public PartialViewResult Approve(InquiriesValidation form)
        {
            try
            {
                InquiryProvider inquiryProvider = new InquiryProvider(_dbContext);
                PropertyProvider propertyProvider = new PropertyProvider(_dbContext);

                InquiriesValidation inquiry = inquiryProvider.Retrieve(form.Id);
                form.CPL = propertyProvider.Retrieve(inquiry.PropertyCode);
                inquiry.Doesitrequire2pricingteamapprovals = form.Doesitrequire2pricingteamapprovals;
                inquiry.PricingApprover1 = form.PricingApprover1;
                inquiry.PricingApprover2 = form.PricingApprover2;
                inquiry.PricingDecision1 = form.PricingDecision1;
                inquiry.PricingReason1 = form.PricingReason1;
                inquiry.ApprovedbyOwner = form.ApprovedbyOwner;

                inquiryProvider.Update(inquiry.Id, inquiry);
                inquiryProvider.Commit();
            }
            catch (Exception ex)
            {
                // TODO: Log error
                this.ModelState.AddModelError("", ex);
            }

            return PartialView("ApprovePartial", form);
        }

        public ActionResult Search(string term)
        {
            PropertyProvider propertyProvider = new PropertyProvider(_dbContext);

            var allProperties = propertyProvider.GetAll();
            var data1 = allProperties.Where(x => x.AirBnBHomeName.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Address, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var data2 = allProperties.Where(x => x.PropertyCode.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Address, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var data3 = allProperties.Where(x => x.Owner.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Address, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var result = data1;
            result.AddRange(data2);
            result.AddRange(data3);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}

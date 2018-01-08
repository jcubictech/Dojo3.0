using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.ComponentModel.DataAnnotations;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Parsers;
using Senstay.Dojo.Data.Providers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class InquiriesValidationController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger RDTLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public InquiriesValidationController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Search(string term)
        {
            var data1 = _dbContext.CPLs.Where(x => x.AirBnBHomeName.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Owner, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var data2 = _dbContext.CPLs.Where(x => x.PropertyCode.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Owner, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var data3 = _dbContext.CPLs.Where(x => x.Owner.Contains(term)).Select(x => new { label = x.AirBnBHomeName + "," + x.PropertyCode + "," + x.Owner, value = x.AirBnBHomeName, Bedrooms = x.Bedrooms, Account = x.Account, CleaningFee = x.CleaningFees, RevTeam2xApproval = x.RevTeam2xApproval, NeedsownerApproval = x.NeedsOwnerApproval, BookingGuidelines = x.BookingGuidelines, AirBnBHomeName = x.AirBnBHomeName, PropertyCode = x.PropertyCode, NeedsOwnerApproval = x.NeedsOwnerApproval }).ToList();
            var result = data1;
            result.AddRange(data2);
            result.AddRange(data3);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SearchID(string id)
        {
            RouteValueDictionary r = new RouteValueDictionary();
            r.Add("grid-filter", "Id__1__" + id);
            return RedirectToAction("Index", "InquiriesValidation", r);
        }

        public ActionResult ShortAdd()
        {
            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            var model = new InquiriesValidation();
            return View(model);
        }

        [HttpPost]
        public ActionResult ShortAdd(InquiriesValidation inquiriesValidation)
        {
            var errors = inquiriesValidation.Validate(new ValidationContext(inquiriesValidation, null, null));
            foreach (var error in errors)
                foreach (var memberName in error.MemberNames)
                    ModelState.AddModelError(memberName, error.ErrorMessage);

            if (ModelState.IsValid)
            {

                var selectedProperty = _dbContext.CPLs.Find(inquiriesValidation.PropertyCode);

                inquiriesValidation.CPL = selectedProperty;
                inquiriesValidation.InquiryCreatedTimestamp = DateTime.Now;
                inquiriesValidation.Check_InDay = inquiriesValidation.Check_inDate.Value.DayOfWeek.ToString();
                inquiriesValidation.Check_OutDay = inquiriesValidation.Check_outDate.Value.DayOfWeek.ToString();
                if (inquiriesValidation.Check_outDate.HasValue)
                {
                    inquiriesValidation.DaysOut = (inquiriesValidation.Check_inDate.Value - inquiriesValidation.InquiryCreatedTimestamp).Value.Days;
                }
                if (inquiriesValidation.Check_outDate.HasValue)
                {
                    inquiriesValidation.LengthofStay = (inquiriesValidation.Check_outDate.Value - inquiriesValidation.Check_inDate).Value.Days;
                    if (inquiriesValidation.LengthofStay == 0)
                    {
                        inquiriesValidation.LengthofStay = 1;
                    }
                }

                inquiriesValidation.NightlyRate = (((inquiriesValidation.TotalPayout.Value / (decimal)0.97) - inquiriesValidation.CPL.CleaningFees) / inquiriesValidation.LengthofStay);

                if (selectedProperty.BeltDesignation!=null && "black belt".Equals(selectedProperty.BeltDesignation.ToLower()))
                {
                    inquiriesValidation.PricingApprover2 = "Pending Review";
                }

                if (selectedProperty.NeedsOwnerApproval.HasValue && selectedProperty.NeedsOwnerApproval.Value)
                {
                    inquiriesValidation.ApprovedbyOwner = "PENDING";
                }
                else
                {
                    inquiriesValidation.ApprovedbyOwner = "N\\A";
                }

                _dbContext.InquiriesValidations.Add(inquiriesValidation);
                saveShowErrors(_dbContext);
                return RedirectToAction("Index");
            }

            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            return View(inquiriesValidation);
        }

        public ActionResult OwnerApprovalInfo(int Id = 0)
        {
            InquiriesValidation ctvid = _dbContext.InquiriesValidations.Find(Id);
            if (ctvid == null)
            {
                return RedirectToAction("NotFound", "Error");
            }
            return View(ctvid);
        }

        [HttpPost]
        public ActionResult OwnerApprovalInfo(InquiriesValidation inquiriesValidation)
        {
            try
            {
                InquiriesValidation strng = _dbContext.InquiriesValidations.Single(nc => nc.Id == inquiriesValidation.Id);

                var selectedProperty = _dbContext.CPLs.Find(strng.PropertyCode);
                strng.CPL = selectedProperty;
                strng.Doesitrequire2pricingteamapprovals = inquiriesValidation.Doesitrequire2pricingteamapprovals;
                strng.PricingApprover1 = inquiriesValidation.PricingApprover1;
                strng.PricingApprover2 = inquiriesValidation.PricingApprover2;
                strng.PricingDecision1 = inquiriesValidation.PricingDecision1;
                strng.PricingReason1 = inquiriesValidation.PricingReason1;
                strng.ApprovedbyOwner = inquiriesValidation.ApprovedbyOwner;
                _dbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                this.ModelState.AddModelError("", ex);
                return View();
            }
        }

        public ActionResult Information(int Id = 0)
        {
            ViewBag.PropertyCodes = _dbContext.CPLs.Select(x => new SelectListItem() { Text = x.PropertyCode, Value = x.PropertyCode }).ToList();
            InquiriesValidation ctvid = _dbContext.InquiriesValidations.Find(Id);
            if (ctvid == null)
            {
                return RedirectToAction("NotFound", "Error");
            }
            return PartialView(ctvid);
        }

        public ActionResult ShortEdit(int Id = 0)
        {
            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            InquiriesValidation model = _dbContext.InquiriesValidations.Find(Id);
            if (model == null)
            {
                return RedirectToAction("NotFound", "Error");
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult ShortEdit(InquiriesValidation inquiriesValidation)
        {
            if (ModelState.IsValid)
            {
                InquiriesValidation strng = _dbContext.InquiriesValidations.Single(nc => nc.Id == inquiriesValidation.Id);

                var selectedProperty = _dbContext.CPLs.Find(inquiriesValidation.PropertyCode);
                strng.CPL = selectedProperty;

                strng.GuestName = inquiriesValidation.GuestName;
                strng.InquiryTeam = inquiriesValidation.InquiryTeam;
                strng.Channel = inquiriesValidation.Channel;
                strng.Check_inDate = inquiriesValidation.Check_inDate;
                strng.Check_outDate = inquiriesValidation.Check_outDate;
                strng.Weekdayorphandays = inquiriesValidation.Weekdayorphandays;
                strng.TotalPayout = inquiriesValidation.TotalPayout;
                strng.AdditionalInfo_StatusofInquiry = inquiriesValidation.AdditionalInfo_StatusofInquiry;
                strng.Check_InDay = inquiriesValidation.Check_inDate.Value.DayOfWeek.ToString();
                strng.Check_OutDay = inquiriesValidation.Check_outDate.Value.DayOfWeek.ToString();
                if (inquiriesValidation.Check_outDate != null) { strng.DaysOut = (inquiriesValidation.Check_outDate.Value - strng.InquiryCreatedTimestamp).Value.Days; }
                if (inquiriesValidation.Check_outDate != null) { strng.LengthofStay = (inquiriesValidation.Check_outDate.Value - inquiriesValidation.Check_inDate).Value.Days; }
                strng.NightlyRate = (((inquiriesValidation.TotalPayout.Value / (decimal)0.97) - strng.CPL.CleaningFees) / strng.LengthofStay);

                _dbContext.SaveChanges();

                return RedirectToAction("Index");
            }
            ViewBag.Properties = (new PropertyProvider(_dbContext)).AggregatedProperties();
            return View(inquiriesValidation);
        }

        // GET: InquiriesValidation
        public ActionResult Upload()
        {
            return View();
        }

        public ActionResult Index()
        {
            var model = _dbContext.InquiriesValidations.OrderByDescending(x => x.InquiryCreatedTimestamp);
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            try
            {
                List<string[]> csvRows = new List<string[]>();
                if (file != null && file.ContentLength > 0)
                {
                    // extract only the filename
                    var fileName = Path.GetFileName(file.FileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + fileName);
                    file.SaveAs(path);
                    using (var reader = new StreamReader(System.IO.File.OpenRead(path)))
                    {
                        var parser = new NetCSVParser();
                        var standartCollLength = reader.ReadLine().Split(',').Length;
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadToEnd();
                            line.Replace(",#N/A,", ",null,");
                            List<string[]> values = new List<string[]>();
                            values = parser.Parse(path, standartCollLength, ",");
                            values.RemoveAt(0);
                            csvRows = values;
                        }
                    }
                }

                if (csvRows.Count > 0)
                {
                    int expectedLength = csvRows[0].Length;
                    foreach (var item in csvRows)
                    {
                        if (item.Length != expectedLength)
                        {
                            throw new ArgumentException("CSV file is invalid");
                        }
                    }
                }

                if (csvRows.Count() < 1)
                {
                    throw new ArgumentException("File is empty or invalid");
                }

                Dictionary<String, CPL> cplDictionary = _dbContext.CPLs.ToDictionary(x => x.PropertyCode);

                int skippedRows = 0;
                for (int i = 0; i < csvRows.Count; i++)
                {
                    InquiriesValidation str = csvRows[i].ToInquiriesValidation();

                    if (!cplDictionary.ContainsKey(str.PropertyCode))
                    {
                        skippedRows++;
                        continue;
                    }

                    str.CPL = cplDictionary[str.PropertyCode];
                    _dbContext.InquiriesValidations.Add(str);
                }
                saveShowErrors(_dbContext);
                ViewBag.UploadMessage = "Data is succesfully uploaded." + (csvRows.Count - skippedRows) + " rows added, " + skippedRows + " rows skipped (no property code found)";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while uploading data: " + ex.GetType().Name + ".";

                string message = ViewBag.ErrorMessage + ex.Message;
                RDTLogger.Info(message, typeof(InquiriesValidationController));
                return View();
            }
        }

        private void saveShowErrors(DbContext context)
        {
            try
            {
                context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var failure in ex.EntityValidationErrors)
                {
                    sb.AppendFormat("{0} failed validation\n", failure.Entry.Entity.GetType());
                    foreach (var error in failure.ValidationErrors)
                    {
                        sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                        sb.AppendLine();
                    }
                }

                throw new DbEntityValidationException(
                    "Entity Validation Failed - errors follow:\n" +
                    sb.ToString(), ex
                ); // Add the original exception as the innerException
            }
        }


    }
}
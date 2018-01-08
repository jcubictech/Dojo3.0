using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Models;
using Senstay.Dojo.Helpers;
using Senstay.Dojo.Parsers;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class AirbnbAccountsController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger RDTLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public AirbnbAccountsController(DojoDbContext context)
        {
            _dbContext = context;
        }

        // GET: AirbnbAccounts
        public ActionResult Index()
        {
            return View("Index", _dbContext.AirbnbAccounts.ToList());
        }

        public ActionResult Add()
        {
            var model = new AirbnbAccount();
            return View(model);
        }

        [HttpPost]
        public ActionResult Add(AirbnbAccount model)
        {
            if (ModelState.IsValid)
            {
                model.ActiveListings = _dbContext.CPLs.Where(o => o.Account == model.Email).Count(o => o.PropertyStatus == "Active");
                model.Pending_Onboarding = _dbContext.CPLs.Where(o => o.Account == model.Email).Count(o => o.PropertyStatus == "Pending - Onboarding");
                model.In_activeListings = _dbContext.CPLs.Where(o => o.Account == model.Email).Count(o => o.PropertyStatus == "Inactive");
                model.ofListingsinLAMarket = _dbContext.CPLs.Where(o => o.Account == model.Email).Where(o => o.PropertyStatus == "Active").Count(o => o.Market == "Los Angeles");
                model.ofListingsinNYCMarket = _dbContext.CPLs.Where(o => o.Account == model.Email).Where(o => o.PropertyStatus == "Active").Count(o => o.Market == "New York");
                _dbContext.AirbnbAccounts.Add(model);
                saveShowErrors(_dbContext);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult Edit(int Id = 0)
        {
            AirbnbAccount ctvid = _dbContext.AirbnbAccounts.Find(Id);
            if (ctvid == null)
            {
                return RedirectToAction("NotFound", "Error");
            }
            ctvid.ActiveListings = _dbContext.CPLs.Where(o => o.Account == ctvid.Email).Count(o => o.PropertyStatus == "Active");
            ctvid.Pending_Onboarding = _dbContext.CPLs.Where(o => o.Account == ctvid.Email).Count(o => o.PropertyStatus == "Pending - Onboarding");
            ctvid.In_activeListings = _dbContext.CPLs.Where(o => o.Account == ctvid.Email).Count(o => o.PropertyStatus == "Inactive");
            ctvid.ofListingsinLAMarket = _dbContext.CPLs.Where(o => o.Account == ctvid.Email).Where(o => o.PropertyStatus == "Active").Count(o => o.Market == "Los Angeles");
            ctvid.ofListingsinNYCMarket = _dbContext.CPLs.Where(o => o.Account == ctvid.Email).Where(o => o.PropertyStatus == "Active").Count(o => o.Market == "New York");
            return View(ctvid);
        }

        [HttpPost]
        public ActionResult Edit(AirbnbAccount model)
        {

            if (ModelState.IsValid)
            {
                _dbContext.Entry(model).State = EntityState.Modified;
                _dbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
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

        public ActionResult Upload()
        {
            return View();
        }

        public ActionResult Delete(int Id = 0)
        {
            AirbnbAccount airbnbAccount = _dbContext.AirbnbAccounts.Find(Id);
            _dbContext.AirbnbAccounts.Attach(airbnbAccount);
            _dbContext.AirbnbAccounts.Remove(airbnbAccount);
            saveShowErrors(_dbContext);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            try
            {
                List<string[]> csvRows = new List<string[]>();
                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + fileName);
                    file.SaveAs(path);

                    using (var reader = new StreamReader(System.IO.File.OpenRead(path)))
                    {
                        var colCount = reader.ReadLine().Split(',').Length;
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadToEnd();
                            line.Replace(",#N/A,", ",null,");
                            List<string[]> values = new List<string[]>();
                            var parser = new NetCSVParser();
                            values = parser.Parse(path, colCount, "\t");
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
                            throw new ArgumentException("TSV file is invalid");
                        }
                    }
                }

                if (csvRows.Count() < 1)
                {
                    throw new ArgumentException("File is empty or invalid");
                }

                for (int i = 0; i < csvRows.Count; i++)
                {
                    AirbnbAccount str = csvRows[i].ToAirbnbAccounts();
                    _dbContext.AirbnbAccounts.Add(str);
                }
                saveShowErrors(_dbContext);
                _dbContext.SaveChanges();
                ViewBag.UploadMessage = "Data is succesfully uploaded";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error while uploading data: " + ex.GetType().Name + ". ";

                string message = ViewBag.ErrorMessage + ex.Message;
                RDTLogger.Info(message, typeof(InquiriesValidationController));
                return View();
            }
        }
    }
}
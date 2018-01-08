using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.Entity.Migrations;
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
    public class CPLController : AppBaseController
    {
        // a looger object per class is the recommended way of using NLog
        private static Logger RDTLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public CPLController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Add()
        {
            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            var model = new CPL();
            return View(model);
        }

        [HttpPost]
        public ActionResult Add(CPL model)
        {
            model.CreatedDate = DateTime.UtcNow;
            try
            {
                if (String.IsNullOrWhiteSpace(model.PropertyCode))
                {
                    ModelState.AddModelError("PropertyCode", "Property Code must be filled");
                }
                else
                {
                    model.PropertyCode = model.PropertyCode.ToUpper();

                    if (_dbContext.CPLs.Find(model.PropertyCode) != null)
                    {
                        ModelState.AddModelError("PropertyCode", "Property with code " + model.PropertyCode + " is already exists");
                    }
                }
                if (ModelState.IsValid)
                {
                    _dbContext.CPLs.Add(model);
                    saveShowErrors(_dbContext);
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "New property was not added: " + ex.GetType().Name + ". " + ex.Message;
                return View();
            }
            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            return View();
        }

        public ActionResult Edit(String Id = "")
        {
            CPL cpl = _dbContext.CPLs.Find(Id);
            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            if (cpl == null)
            {
                return RedirectToAction("NotFound", "Error");
            }

            return View(cpl);
        }

        [HttpPost]
        public ActionResult Edit(CPL cpl)
        {          
            if (ModelState.IsValid)
            {
                _dbContext.Entry(cpl).State = EntityState.Modified;
                _dbContext.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Accounts = (new AirbnbAccountProvider(_dbContext)).AggregatedAccounts();
            return View(cpl);
        }

        public ActionResult Index()
        {
            return View("Index", _dbContext.CPLs.OrderBy(x => x.PropertyCode).AsQueryable());
        }
        // GET: api/CompletePropertyList/5
        public ActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase file)
        {
            try
            {
                List<string[]> csvRows = new List<string[]>();

                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    // extract only the filename
                    var fileName = Path.GetFileName(file.FileName);
                    // store the file inside ~/App_Data/uploads folder
                    var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), Guid.NewGuid().ToString() + "_" + fileName);
                    file.SaveAs(path);

                    using (var reader = new StreamReader(System.IO.File.OpenRead(path)))
                    {
                        var colCount = reader.ReadLine().Split(',').Length;
                        var parser = new NetCSVParser();
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadToEnd();
                            line.Replace(",#N/A,", ",null,");
                            List<string[]> values = new List<string[]>();
                            values = parser.Parse(path, colCount, ",");
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

                for (int i = 0; i < csvRows.Count; i++)
                {
                    CPL str = csvRows[i].ToCPL();
                    _dbContext.CPLs.AddOrUpdate(str);
                }
                saveShowErrors(_dbContext);
                _dbContext.SaveChanges();

                ViewBag.UploadMessage = "Data is succesfully uploaded. Existing rows are updated, new rows added. Total rows processed " + csvRows.Count;
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

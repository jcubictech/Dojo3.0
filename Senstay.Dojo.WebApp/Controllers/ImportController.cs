using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using Newtonsoft.Json;
using NLog;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Data.Providers;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;
using Senstay.Dojo.Models.HelperClass;
using Senstay.Dojo.FtpClient;
using Senstay.Dojo.Airbnb.Import;

namespace Senstay.Dojo.Controllers
{
    [Authorize]
    [CustomHandleError]
    public class ImportController : AppBaseController
    {
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private readonly DojoDbContext _dbContext;

        public ImportController(DojoDbContext context)
        {
            _dbContext = context;
        }

        public ActionResult Index()
        {
            return RedirectToAction("Airbnb", "Import");
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult AirbnbStatistics()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            var provider = new AirbnbImportProvider(_dbContext);
            var model = provider.GetImportStatistics();

            return View(model);
        }

        public ActionResult AirbnbImport()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();
            return View(new AirbnbImportFormModel());
        }

        public ActionResult AirbnbImportForm()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();
            return PartialView("_AirbnbImportPartial", new AirbnbImportFormModel());
        }

        public ActionResult NonAirbnb()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();
            var model = new ImportViewModel()
            {
                ImportDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult ImportAirbnb(AirbnbImportFormModel form)
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            try
            {
                List<ImportFile> completedTransactionFiles = JsonConvert.DeserializeObject<List<ImportFile>>(form.CompletedTransactionFiles);
                List<ImportFile> futureTransactionFiles = JsonConvert.DeserializeObject<List<ImportFile>>(form.FutureTransactionFiles);
                List<ImportFile> grossTransactionFiles = JsonConvert.DeserializeObject<List<ImportFile>>(form.GrossTransactionFiles);
                FtpTransactionType FileTransactionType = form.TransactionFileType;

                ProcessResult result = new ProcessResult();
                if (FileTransactionType == FtpTransactionType.Completed)
                {
                    result = ImportAirbnbTrasactions(completedTransactionFiles, form.ReportDate, FileTransactionType);
                }
                else if (FileTransactionType == FtpTransactionType.Future)
                {
                    result = ImportAirbnbTrasactions(futureTransactionFiles, form.ReportDate, FileTransactionType);
                }
                else if (FileTransactionType == FtpTransactionType.Gross)
                {
                    result = ImportAirbnbTrasactions(grossTransactionFiles, form.ReportDate, FileTransactionType);
                }
                else if (FileTransactionType == FtpTransactionType.LogMissingTransactions)
                {
                    var logDate = new DateTime(2017, 12, 30);
                    result = LogMissingAirbnbTrasactions(completedTransactionFiles, logDate, FileTransactionType);
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                ProcessResult result = new ProcessResult();
                result.Count = -1;
                result.Message = ex.Message;
                return Json(-1, JsonRequestBehavior.AllowGet);
            }
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult GetImportFileList(DateTime reportDate, FtpTransactionType reportType)
        {
            List<SelectListItem> fileList = new List<SelectListItem>();
            try
            {
                var fptService = new AirbnbFtpService();
                NameValueCollection urlList;
                if (reportType == FtpTransactionType.Completed)
                {
                    urlList = fptService.GetFileList(FtpTransactionType.Completed, reportDate);
                }
                else if (reportType == FtpTransactionType.Future)
                {
                    urlList = fptService.GetFileList(FtpTransactionType.Future, reportDate);
                }
                else
                {
                    urlList = fptService.GetFileList(FtpTransactionType.Gross, reportDate);
                }

                // map NameValueCollection to SelectListItem
                foreach (var key in urlList.AllKeys)
                {
                    fileList.Add(new SelectListItem
                    {
                        Text = key,
                        Value = key + ";" + urlList[key] // js uses this format to parse out the filename and url
                    });
                }

                return Json(fileList, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                DojoLogger.Error(ex.Message, typeof(OwnerPayoutController));
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError; // code = 500
                return Json(fileList, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ImportNonAirbnb(ImportViewModel form, HttpPostedFileBase attachedImportFile)
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                int errorCount = 0;
                if (attachedImportFile != null)
                {
                    switch (form.FileType)
                    {
                        case ImportFileType.Expenses:
                            bool newVersion = attachedImportFile.FileName.IndexOf("v3") > 0 || attachedImportFile.FileName.IndexOf("v4") > 0;
                            errorCount = ImportExpenses(attachedImportFile.InputStream, form.ImportDate, newVersion);
                            break;
                        case ImportFileType.OffAibnb:
                            errorCount = ImportOffAirbnbExcel(attachedImportFile.InputStream);
                            break;
                        case ImportFileType.PropertyFee:
                            errorCount = ImportPropertyFee(attachedImportFile.InputStream);
                            break;
                        case ImportFileType.BackfillTransaction:
                            errorCount = ImportBackfillTransactions(attachedImportFile.InputStream, attachedImportFile.FileName, form.ImportDate);
                            break;
                        case ImportFileType.Balance:
                            errorCount = ImportBalanceSweep(attachedImportFile.InputStream, form.ImportDate);
                            break;
                    }

                }
                return Json(errorCount.ToString(), JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("-1", JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult AirbnbImportLog()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();
            return View();
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public JsonResult RetrieveImportLog()
        {
            if (!AuthorizationProvider.IsDataImporter()) return Forbidden();

            try
            {
                var provider = new AirbnbImportProvider(_dbContext);
                var model = provider.RetrieveImportLog();
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult OffAirbnbUpload(HttpPostedFileBase excelFile)
        {
            var provider = new OffAirbnbImportProvider(_dbContext);
            var filename = "off-airbnb-import.xlsx";
            string excelPath = Path.Combine(Helpers.UrlHelper.DataRootUrl(), filename);
            int result = provider.ImportExcel(excelFile.InputStream);
            string resultMessage = string.Empty;
            if (result == 0)
                resultMessage = "There is no data in the file.";
            else if (result > 0)
                resultMessage = string.Format("There were {0:d} Reservations processed and saved to database.", result);
            else
                resultMessage = string.Format("There were {0:d} Errors found in the file. No data is saved to database.", -result);

            var resultJson = new
            {
                Status = result > 0 ? 1 : (result == 0 ? 0 : -1),
                Message = resultMessage
            };
            return Json(resultJson, JsonRequestBehavior.AllowGet);
        }

        #region Import methods

        private ProcessResult ImportAirbnbTrasactions(List<ImportFile> importFiles, DateTime importDate, FtpTransactionType fileTransactionType)
        {
            var result = new ProcessResult();
            string localFile = string.Empty;
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                string tempFolder = GetDownloadFolder();
                var ftpService = new AirbnbFtpService();
                var importService = new AirbnbImportService();

                DojoLogger.Info(string.Format("Temporary download folder is {0} for {1} files from FTP server.", tempFolder, importFiles.Count.ToString()));

                if (fileTransactionType == FtpTransactionType.Completed)
                {
                    var payoutProvider = new OwnerPayoutService(_dbContext);
                    foreach (var importFile in importFiles)
                    {
                        result.Count++;

                        // download file from ftp site
                        string csvFileUrl = importFile.Id;
                        localFile = Path.Combine(tempFolder, importFile.Name);
                        ftpService.Download(csvFileUrl, localFile);
                        //DojoLogger.Info(string.Format("Download file {0} completed.", localFile));

                        // process only those data that is later than the most recent imported payout date in db
                        DateTime? startPayoutDate = payoutProvider.GetMostRecentPayoutDate(importFile.Name);
                        var count = importService.ImportCompletedAirbnbTransactions(
                                                        localFile, importDate.ToString("yyyy-MM-dd"), startPayoutDate);

                        if (count == -1)
                            result.Skip++;
                        else
                            result.Bad += count;

                        importService.DeleteFileIfAllowed(localFile);
                    }
                }
                else if (fileTransactionType == FtpTransactionType.Future)
                {
                    var reservationProvider = new ReservationService(_dbContext);
                    string mostRecentDateString = reservationProvider.GetMostRecentFutureDate();

                    foreach (var importFile in importFiles)
                    {
                        result.Count++;

                        //string sortableDate = importService.ParseDateFromTransactionFileUrl(importFile.Id);
                        //if (string.Compare(sortableDate, mostRecentDateString) > 0)
                        //{
                        // download file from ftp site
                        string csvFileUrl = importFile.Id;
                        localFile = Path.Combine(tempFolder, importFile.Name);
                        ftpService.Download(csvFileUrl, localFile);
                        var count = importService.ImportFutureAirbnbTransactions(localFile, importDate.ToString("yyyy-MM-dd"));

                        if (count == -1)
                            result.Skip++;
                        else
                            result.Bad += count;

                        importService.DeleteFileIfAllowed(localFile);

                        //}
                        //else
                        //    result.Skip++;
                    }
                }
                else
                {
                    var reservationProvider = new ReservationService(_dbContext);
                    string mostRecentDateString = reservationProvider.GetMostRecentGrossDate();

                    foreach (var importFile in importFiles)
                    {
                        result.Count++;

                        //string sortableDate = importService.ParseDateFromTransactionFileUrl(importFile.Id);
                        //if (string.Compare(sortableDate, mostRecentDateString) > 0)
                        //{
                        // download file from ftp site
                        string csvFileUrl = importFile.Id;
                        localFile = Path.Combine(tempFolder, importFile.Name);
                        ftpService.Download(csvFileUrl, localFile);
                        var count = importService.ImportGrossEarningTransactions(localFile, importDate.ToString("yyyy-MM-dd"));

                        if (count == -1)
                            result.Skip++;
                        else
                            result.Bad += count;

                        importService.DeleteFileIfAllowed(localFile);
                    }
                }
            }
            catch (Exception ex)
            {
                DojoLogger.Error(string.Format("Import file {0} fails. Exception: {1}", localFile, ex.Message));
                // fall through
            }

            result.Good = result.Count - result.Bad - result.Skip;
            return result;
        }

        private ProcessResult LogMissingAirbnbTrasactions(List<ImportFile> importFiles, DateTime logDate, FtpTransactionType fileTransactionType)
        {
            var result = new ProcessResult();
            string localFile = string.Empty;
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                string tempFolder = GetDownloadFolder();
                var ftpService = new AirbnbFtpService();
                var importService = new AirbnbImportService();

                var payoutProvider = new OwnerPayoutService(_dbContext);
                foreach (var importFile in importFiles)
                {
                    result.Count++;

                    // download file from ftp site
                    string csvFileUrl = importFile.Id;
                    localFile = Path.Combine(tempFolder, importFile.Name);
                    ftpService.Download(csvFileUrl, localFile);
                    //DojoLogger.Info(string.Format("Download file {0} completed.", localFile));

                    // process only those data that is later than the most recent imported payout date in db
                    DateTime? startPayoutDate = payoutProvider.GetMostRecentPayoutDate(importFile.Name);
                    var count = importService.LogMissingCompletedAirbnbTransactions(
                                              localFile, logDate.ToString("yyyy-MM-dd"), startPayoutDate);

                    if (count == -1)
                        result.Skip++;
                    else
                        result.Bad += count;

                    importService.DeleteFileIfAllowed(localFile);
                }
            }
            catch (Exception ex)
            {
                DojoLogger.Error(string.Format("Import file {0} fails. Exception: {1}", localFile, ex.Message));
                // fall through
            }

            result.Good = result.Count - result.Bad - result.Skip;
            return result;
        }

        private int ImportExpenses(Stream dataStream, DateTime importDate, bool newVersion)
        {
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                int startJobCostId = _dbContext.JobCosts.Max(x => x.JobCostId); // expense creation will start from this JobCostId
                var dataProvider = new JobCostImportProvider(_dbContext);
                int result = dataProvider.ImportExcel(dataStream, newVersion);
                if (result > 0) // JobCost import successes; create and group expenses from it
                {
                    dataProvider.CreateExpenses(importDate.Month, importDate.Year, startJobCostId);
                }
                return result;
            }
            catch
            {
                throw;
            }
        }

        private int ImportOffAirbnbExcel(Stream dataStream)
        {
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                var dataProvider = new OffAirbnbImportProvider(_dbContext);
                return dataProvider.ImportExcel(dataStream);
            }
            catch
            {
                throw;
            }
        }

        private int ImportPropertyFee(Stream dataStream)
        {
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                var dataProvider = new PropertyFeeImportProvider(_dbContext);
                return dataProvider.ImportExcel(dataStream);

            }
            catch
            {
                throw;
            }
        }

        private int ImportBalanceSweep(Stream dataStream, DateTime importDate)
        {
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                // TODO: need to implement this import
                var dataProvider = new PropertyBalanceImportProvider(_dbContext);
                return dataProvider.ImportExcel(dataStream, importDate);

            }
            catch
            {
                throw;
            }
        }

        private int ImportBackfillTransactions(Stream dataStream, string filename, DateTime importDate)
        {
            try
            {
                // speed up bulk insertion
                _dbContext.Configuration.AutoDetectChangesEnabled = false;
                _dbContext.Configuration.ValidateOnSaveEnabled = false;

                // TODO: need to implement this import
                var dataProvider = new AirbnbImportService(_dbContext);
                return dataProvider.BackfillCompletedAirbnbTransactions(dataStream, filename, importDate);

            }
            catch
            {
                throw;
            }
        }

        public ActionResult ActionDashboard()
        {
            if (!AuthorizationProvider.IsDataImporter())
                return RedirectToAction("Login", "Account", "/");
            else
            {
                return View();
            }
        }

        public JsonResult UpdatePropertyContacts(HttpPostedFileBase excelFile)
        {
            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        private string GetDownloadFolder()
        {
            return Path.Combine(Server.MapPath("~/App_Data")); // Path.GetTempPath(); this does not work for web
        }

        #endregion

        #region helper methods

        private JsonResult Forbidden(object model = null)
        {
            string message = string.Format("User '{0}' does not have permission to access thsi feature.", this.User.Identity.Name);
            DojoLogger.Warn(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }

        private JsonResult InternalError(string logMsg, string returnMsg, Exception ex = null, object model = null)
        {
            string message = string.Empty;
            if (ex != null)
                message = string.Format("{0} - {1}", logMsg, ex.Message + ex.StackTrace);
            else
                message = string.Format("{0}", logMsg);

            DojoLogger.Error(message, this.GetType());
            Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            if (model == null)
                return Json(new OwnerStatementViewModel(), JsonRequestBehavior.AllowGet);
            else
                return Json(model, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
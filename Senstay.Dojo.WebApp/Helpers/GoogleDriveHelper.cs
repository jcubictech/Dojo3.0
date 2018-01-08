using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web.Mvc;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using NLog;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.HelperClass;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.App.Helpers
{
    public class GoogleDriveHelper
    {
        private const int FILE_PAGE_SIZE = 100;
        private const int MAZ_FILES_ALLOWED = 500;
        private const string APP_DATA_PATH = "~/App_Data";
        private const string GOOGLE_DRIVE_APPNAME = "GoogleDriveApplicationName";
        private const string GOOGLE_DRIVE_CREDENTIAL_FILE = "GoogleDriveSecretFilename";
        private const string FOLDER_MIME_TYPE = "application/vnd.google-apps.folder";
        private const string OAUTH_TOKEN_FOLDER = ".credentials/dojo-google-drive-token.json";
        private const string GOOGLE_DRIVE_ROOT_URL = "https://drive.google.com/a/bcubedllc.com/file/d/";

        private const string GOOGLE_DRIVE_SERVICE_EMAIL = "GoogleDriveServiceEmail";
        private const string GOOGLE_DRIVE_KEYFILE = "GoogleDriveKeyFile";

        public delegate double GoogleDriveFileProcessor(Google.Apis.Drive.v3.Data.File file);

        public string GoogleServiceAccountEmail = "id-85-372@senstay-1155.iam.gserviceaccount.com";
        public string X509KeyFile = "Key.p12";

        private static string AirbnbReportFolderId = string.Empty;
        private static string CompletedFolderId = string.Empty;
        private static string FutureFolderId = string.Empty;

        private readonly DojoDbContext _dbContext;
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();

        public GoogleDriveHelper(DojoDbContext dbContext)
        {
            _dbContext = dbContext;

            if (string.IsNullOrEmpty(AirbnbReportFolderId))
            {
                string folderFilter = string.Format("mimeType='{0}' and name = 'Airbnb Reports'", FOLDER_MIME_TYPE);
                var folders = GetFileList(folderFilter);
                if (folders.Count > 0) AirbnbReportFolderId = folders[0].Id;
            }

            if (string.IsNullOrEmpty(CompletedFolderId))
            {
                string folderFilter = string.Format("mimeType='{0}' and name = 'Airbnb Historical Data'", FOLDER_MIME_TYPE);
                var folders = GetFileList(folderFilter);
                if (folders.Count > 0) CompletedFolderId = folders[0].Id;
            }

            if (string.IsNullOrEmpty(FutureFolderId))
            {
                string folderFilter = string.Format("mimeType='{0}' and name = 'Revenue Forecasts'", FOLDER_MIME_TYPE);
                var folders = GetFileList(folderFilter);
                if (folders.Count > 0) FutureFolderId = folders[0].Id;
            }

            DojoLogger.Info(string.Format("report folder={0}, completed folder={1}, future folder={2}", 
                AirbnbReportFolderId, CompletedFolderId, FutureFolderId));
        }

        #region google service

        public DriveService GetDriveService()
        {
            try
            {
                return GoogleServiceHelper.GetDriveService(AuthAccountType.Developer);
            }
            catch(Exception ex)
            {
                DojoLogger.Error(ex.Message);
                throw;
            }
        }

        #endregion

        #region google drive folders and files

        public List<SelectListItem> GetAirbnbReportFolders(OwnerayoutFileType type)
        {
            List<SelectListItem> selectList = new List<SelectListItem>();
            try
            {
                string searchFilter = FormatFolderSearch(type);
                var folders = GetFileList(searchFilter);
                foreach (var folder in folders)
                {
                    selectList.Add(new SelectListItem
                    {
                        Text = folder.Name,
                        Value = folder.Id
                    });
                }
                return selectList.OrderBy(x => x.Text).ToList();
            }
            catch
            {
                throw;
            }
        }

        public List<SelectListItem> GetAirbnbReportFiles(OwnerayoutFileType type, DateTime reportDate)
        {
            List<SelectListItem> selectList = new List<SelectListItem>();
            try
            {
                string searchFilter = FormatFolderFileSearch(type, reportDate);
                DojoLogger.Info(string.Format("search filter={0}", searchFilter));
                var files = GetFileList(searchFilter);
                foreach (var file in files)
                {
                    selectList.Add(new SelectListItem
                    {
                        Text = file.Name,
                        Value = file.Name + ";" + file.Id
                    });
                }

                return selectList.OrderBy(x => x.Text).ToList();
            }
            catch(Exception ex)
            {
                DojoLogger.Error(ex.Message);
                throw;
            }
        }

        public List<Google.Apis.Drive.v3.Data.File> GetFileList(string searchFilter = null, DriveService service = null)
        {
            List<Google.Apis.Drive.v3.Data.File> files = new List<Google.Apis.Drive.v3.Data.File>();
            try
            {
                if (service == null) service = GetDriveService();
                if (service != null)
                {
                    string pageToken = null; // get all; use "nextPageToken" for paging
                    do
                    {
                        FilesResource.ListRequest request = service.Files.List();
                        request.Fields = "nextPageToken, files(id, name, fileExtension, mimeType, parents)";
                        if (searchFilter != null) request.Q = searchFilter;
                        request.PageSize = FILE_PAGE_SIZE;
                        request.Spaces = "drive";
                        var result = request.Execute();
                        files.AddRange(result.Files);
                        pageToken = result.NextPageToken;
                        if (files.Count > MAZ_FILES_ALLOWED) break;
                    } while (pageToken != null);

                    return files;
                }
            }
            catch (Google.GoogleApiException ex)
            {
                if (ex.HttpStatusCode != System.Net.HttpStatusCode.NotFound)
                    throw;
            }
            catch (Exception ex)
            {
                throw;
            }

            return files;
        }

        private string FormatFolderSearch(OwnerayoutFileType dojoType)
        {
            string parentId = (dojoType == OwnerayoutFileType.Future ? FutureFolderId : CompletedFolderId);
            return string.Format("'{0}' in parents and mimeType='{1}'", parentId, FOLDER_MIME_TYPE);
        }

        private string FormatFolderFileSearch(OwnerayoutFileType dojoType, DateTime reportDate)
        {
            // get the folder Id for the reportDate
            string parentId = string.Empty;
            string folderName = (dojoType == OwnerayoutFileType.Future ? 
                                    string.Format("Future Transactions - {0}", reportDate.ToString("MMMMM d yyyy")) :
                                    reportDate.ToString("MMMMM d yyyy")); // e.g. April 5 2017
            string folderFilter = string.Format("mimeType='{0}' and name='{1}'", FOLDER_MIME_TYPE, folderName);
            var folders = GetFileList(folderFilter);
            if (folders.Count > 0) parentId = folders[0].Id;

            // search query for reportDate files
            string filenameFilter = (dojoType == OwnerayoutFileType.Future ? "-airbnb_pending.csv" : "-airbnb.csv");
            return string.Format("'{0}' in parents and name contains '{1}'", parentId, filenameFilter);
        }

        private string ForamtFileSearch(OwnerayoutFileType dojoType, string mimeType)
        {
            string filenameFilter = (dojoType == OwnerayoutFileType.Future ? "-airbnb_pending.csv" : "-airbnb.csv");
            string beginOfLastMonth = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-01T12:00:00");
            return string.Format("name contains '{0}' and mimeType='{1}' and createdTime > '{2}'", filenameFilter, mimeType, beginOfLastMonth);
        }

        #endregion

        #region download files

        public bool DownloadFile(DriveService service, ImportFile importFile)
        {
            string saveTo = Path.Combine(Senstay.Dojo.Helpers.UrlHelper.DataRootUrl(), importFile.Name);
            return DownloadFile(service, importFile, saveTo);
        }

        public bool DownloadFile(DriveService service, ImportFile importFile, string saveTo)
        {
            if (!string.IsNullOrEmpty(importFile.Id))
            {
                try
                {
                    var request = service.Files.Get(importFile.Id);
                    var stream = new MemoryStream();
                    // monitor download progress
                    request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                    {
                        switch (progress.Status)
                        {
                            case DownloadStatus.Downloading: break;
                            case DownloadStatus.Failed: break;
                            case DownloadStatus.Completed:
                                {
                                    long length = stream.Length;
                                    byte[] content = stream.ToArray();
                                    File.WriteAllBytes(saveTo, content);
                                }
                                break;
                        }
                    };

                    request.Download(stream);

                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false; // The file doesn't have any content stored on Drive.
            }
        }

        public void ProcessFile(string filename, GoogleDriveFileProcessor fileProcessor)
        {
            ProcessFiles(GetFileList(), fileProcessor);

        }

        public void ProcessFiles(GoogleDriveFileProcessor fileProcessor)
        {
            ProcessFiles(GetFileList(), fileProcessor);

        }

        public void ProcessFiles(IList<Google.Apis.Drive.v3.Data.File> files, GoogleDriveFileProcessor fileProcessor)
        {
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    fileProcessor(file);
                }
            }
        }

        #endregion

        public static ServiceAccountCredential GetServiceCredential()
        {
            string[] scopes = new string[] { DriveService.Scope.Drive }; // Full access

            var keyFilePath = GOOGLE_DRIVE_KEYFILE; // Downloaded from https://console.developers.google.com
            var serviceAccountEmail = GOOGLE_DRIVE_SERVICE_EMAIL;  // found https://console.developers.google.com

            //loading the Key file
            var certificate = new X509Certificate2(keyFilePath, "notasecret", X509KeyStorageFlags.Exportable);
            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(serviceAccountEmail)
                                    {
                                        Scopes = scopes
                                    }
                                .FromCertificate(certificate));
            return credential;
        }

        // tries to figure out the mime type of the file.
        private static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }
    }

    public enum OwnerayoutFileType
    {
        Completed = 1,
        Future = 2,
        Streamline = 3,
        Expense = 4,
        Other = 5
    }
}

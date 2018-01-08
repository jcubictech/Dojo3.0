using System;
using System.IO;
using System.Threading;
using System.Security.Claims;
using NLog;
using Google.Apis.Drive.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using System.Security.Cryptography.X509Certificates;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Google.Apis.Sheets.v4;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.App.Helpers
{
    public class GoogleServiceHelper
    {
        private static Logger DojoLogger = NLog.LogManager.GetCurrentClassLogger();
        private const string SERVICE_ACCOUNT_KEYFILE = "jcubic-dojo-service-auth.json";
        private const string JCUBIC_WEB_APP_KEYFILE = "jcubic-web-app-auth.json";
        private const string GOOGLE_DRIVE_TOKEN_FOLDER = @".credentials\dojo-google-drive-token.json";

        private static DriveService _driveService = null;
        private static SpreadsheetsService _sheetervice = null;

        public static DriveService GetDriveService(AuthAccountType useServiceAccount = AuthAccountType.Service)
        {
            if (_driveService != null) return _driveService;
            try
            {
                string ApplicationName = GetApplicationName(AuthAccountType.Developer);
                string[] scopes = { DriveService.Scope.Drive };

                if (useServiceAccount == AuthAccountType.Service)
                {
                    var serviceAccountEmail = GetClientEmail(AuthAccountType.Service);
                    var x509File = SERVICE_ACCOUNT_KEYFILE;
                    var certificate = new X509Certificate2(x509File, "notasecret", X509KeyStorageFlags.Exportable);
                    var serviceAccountCredentialInitializer =
                        new ServiceAccountCredential
                            .Initializer(serviceAccountEmail) { Scopes = scopes }
                            .FromCertificate(certificate);

                    var credential = new ServiceAccountCredential(serviceAccountCredentialInitializer);

                    if (!credential.RequestAccessTokenAsync(CancellationToken.None).Result)
                    {
                        throw new InvalidOperationException("Access token request failed.");
                    }

                    DriveService service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });

                    return _driveService = service;
                }
                else
                {
                    string secretFile = GetAuthFilePath();

                    UserCredential credential;

                    using (var stream = new FileStream(secretFile, FileMode.Open, FileAccess.Read))
                    {
                        //string credPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                        // token folder to store user Oauth access token
                        string credPath = Path.Combine(AuthFolder(), GOOGLE_DRIVE_TOKEN_FOLDER);

                        //string userId = ClaimProvider.GetUserId(_dbContext).Replace("-", string.Empty);
                        string userId = ClaimsPrincipal.Current.Identity.Name.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries)[0];

                        DojoLogger.Info(string.Format("credential folder={0} for user {1}", credPath, ClaimsPrincipal.Current.Identity.Name));

                        // save credential file to credPath
                        GoogleWebAuthorizationBroker.Folder = credPath;
                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                        GoogleClientSecrets.Load(stream).Secrets,
                                        scopes,
                                        userId,
                                        CancellationToken.None,
                                        new FileDataStore(credPath, true))
                                     .Result;
                    }

                    // Create Google Drive API service
                    var service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName,
                    });

                    DojoLogger.Info("Drive service created.");

                    return _driveService = service;
                }
            }
            catch (Exception ex)
            {
                DojoLogger.Error(ex.Message);
                return null;
            }
        }

        public static SpreadsheetsService GetSpreadsheetService(AuthAccountType useServiceAccount = AuthAccountType.Service)
        {
            if (_driveService != null) return _sheetervice;
            try
            {
                string ApplicationName = GetApplicationName(AuthAccountType.Developer);
                string[] scopes = { SheetsService.Scope.Drive, SheetsService.Scope.Spreadsheets };

                if (useServiceAccount == AuthAccountType.Service)
                {
                    var serviceAccountEmail = GetClientEmail(AuthAccountType.Service);
                    var x509File = SERVICE_ACCOUNT_KEYFILE;
                    var certificate = new X509Certificate2(x509File, "notasecret", X509KeyStorageFlags.Exportable);

                    var serviceAccountCredentialInitializer = new ServiceAccountCredential
                            .Initializer(serviceAccountEmail) { Scopes = scopes }
                            .FromCertificate(certificate);

                    var credential = new ServiceAccountCredential(serviceAccountCredentialInitializer);

                    if (!credential.RequestAccessTokenAsync(CancellationToken.None).Result)
                    {
                        throw new InvalidOperationException("Access token request failed.");
                    }

                    var requestFactory = new GDataRequestFactory(null);
                    requestFactory.CustomHeaders.Add("Authorization: Bearer " + credential.Token.AccessToken);

                    var service = new SpreadsheetsService(ApplicationName)
                    {
                        RequestFactory = requestFactory
                    };

                    return _sheetervice = service;
                }
                else
                {
                    string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
                    string secretFile = GetAuthFilePath();

                    UserCredential credential;

                    using (var stream = new FileStream(secretFile, FileMode.Open, FileAccess.Read))
                    {
                        string credPath = GetAuthFilePath();
                        credPath = Path.Combine(AuthFolder(), GOOGLE_DRIVE_TOKEN_FOLDER);

                        string userId = ClaimsPrincipal.Current.Identity.Name.Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries)[0];

                        // save credential file to credPath
                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                        GoogleClientSecrets.Load(stream).Secrets,
                                        Scopes,
                                        "user",
                                        CancellationToken.None,
                                        new FileDataStore(credPath, true)).Result;
                    }

                    //var service = new SheetsService(new BaseClientService.Initializer()
                    //{
                    //    HttpClientInitializer = credential,
                    //    ApplicationName = ApplicationName,
                    //});

                    var requestFactory = new GDataRequestFactory(null);
                    requestFactory.CustomHeaders.Add("Authorization: Bearer " + credential.Token.AccessToken);

                    // Create Google Sheets API service
                    var service = new SpreadsheetsService(ApplicationName)
                    {
                        RequestFactory = requestFactory
                    };

                    return _sheetervice = service;
                }
            }
            catch (Exception ex)
            {
                DojoLogger.Error(ex.Message);
                return null;
            }
        }

        public static string AuthFolder()
        {
            try
            {
                return UrlHelper.DataRootUrl();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetAuthFilePath(AuthAccountType useServiceAccount = AuthAccountType.Service)
        {
            try
            {
                if (useServiceAccount == AuthAccountType.Service)
                    return Path.Combine(AuthFolder(), SERVICE_ACCOUNT_KEYFILE);
                else
                    return Path.Combine(AuthFolder(), JCUBIC_WEB_APP_KEYFILE);
            }
            catch
            {
            }
            return string.Empty;
        }

        public static string GetApplicationName(AuthAccountType useServiceAccount = AuthAccountType.Service)
        {
            try
            {
                string authFilePath = GetAuthFilePath(useServiceAccount);
                using (StreamReader sr = File.OpenText(authFilePath))
                {
                    string content = sr.ReadToEnd();
                    return ParseValue(content, "\"project_id\":");

                    // TODO: the following code only work for one-level json
                    //var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    //return dict["project_id"];
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        public static string GetClientEmail(AuthAccountType useServiceAccount = AuthAccountType.Service)
        {
            try
            {
                string authFilePath = GetAuthFilePath(useServiceAccount);
                using (StreamReader sr = File.OpenText(authFilePath))
                {
                    string content = sr.ReadToEnd();
                    return ParseValue(content, "\"client_email\":");

                    // TODO: the following code only work for one-level json
                    //var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
                    //return dict["client_email"];
                }
            }
            catch
            {
            }
            return string.Empty;
        }

        private static string ParseValue(string content, string keyToFind)
        {
            int index = content.IndexOf(keyToFind);
            if (index > 0)
            {
                int endIndex = content.IndexOf(',', index);
                if (endIndex > 0)
                {
                    int startIndex = index + keyToFind.Length;
                    string value = content.Substring(startIndex, endIndex - startIndex).Trim();
                    return value.TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
                }
            }
            return string.Empty;
        }
    }

    public enum AuthAccountType
    {
        Service = 1,
        Developer = 2
    }
}

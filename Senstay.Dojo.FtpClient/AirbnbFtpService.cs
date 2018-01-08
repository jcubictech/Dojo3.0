using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Senstay.Dojo.Helpers;
using System.Collections.Specialized;

namespace Senstay.Dojo.FtpClient
{
    public class AirbnbFtpService
    {
        private const string FTP_ACCOUNT_NAME = "Senstay.Dojo.FTP"; // default ftp user name
        private const string FTP_ACCOUNT_PASSWORD = "P7gZX&rpyA"; // default ftp user password
        private string _FtpServerUrl = string.Empty;
        private string _AirbnbServerUser = string.Empty;
        private string _AirbnbServerPassword = string.Empty;
        private int _BufferSize = 2048;

        public AirbnbFtpService(string user = null, string password = null)
        {
            // ftp server IP and account are configured in web.config or app.config
            _FtpServerUrl = AirbnbFtpConfig.RootFtpUrl;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                _AirbnbServerUser = SettingsHelper.GetSafeSetting("FtpAccount", FTP_ACCOUNT_NAME);
                _AirbnbServerPassword = SettingsHelper.GetSafeSetting("FtpSecret", FTP_ACCOUNT_PASSWORD);
            }
            else
            {
                _AirbnbServerUser = user;
                _AirbnbServerPassword = password;
            }
        }

        /// <summary>
        /// Get all folders under the well-form ftp url.
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <returns>the list of files under the given ftpUrl</returns>
        public NameValueCollection GetFolderList(string folderUrl)
        {
            NameValueCollection folderList = new NameValueCollection();
            try
            {
                string ftpUrl = folderUrl;
                if (!folderUrl.StartsWith("ftp://")) ftpUrl = Path.Combine(_FtpServerUrl, folderUrl) + "/";
                var folders = GetDataList(ftpUrl);

                //Loop through the resulting file names.
                foreach (var folderName in folders)
                {
                    var parentDirectory = string.Empty;

                    if (!Path.HasExtension(folderName))
                    {
                        folderList.Add(folderName, ftpUrl + folderName);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return folderList;
        }

        /// <summary>
        /// Get all files under the well-form ftp url.
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <returns>the list of files under the given ftpUrl</returns>
        public NameValueCollection GetFileList(string folderUrl)
        {
            NameValueCollection fileList = new NameValueCollection();
            try
            {
                string ftpUrl = folderUrl;
                if (!folderUrl.StartsWith("ftp://")) ftpUrl = Path.Combine(_FtpServerUrl, folderUrl) + "/";
                var files = GetDataList(ftpUrl);

                //Loop through the resulting file names.
                foreach (var fileName in files)
                {
                    if (Path.HasExtension(fileName))
                    {
                        fileList.Add(fileName, ftpUrl + "/" + fileName);
                    }
                }
            }
            catch
            {
                // no file available
            }

            return fileList;
        }

        /// <summary>
        /// Get all files for the transaction type and import date.
        /// </summary>
        /// <param name="type">trasnaction type</param>
        /// <param name="reportDate">import date</param>
        /// <returns>the list of files</returns>
        public NameValueCollection GetFileList(FtpTransactionType type, DateTime reportDate)
        {
            var fileList = new NameValueCollection();
            try
            {
                var folderUrl = string.Empty;
                if (type == FtpTransactionType.Completed)
                {
                    folderUrl = AirbnbFtpConfig.CompletedFtpUrl + "/" + reportDate.ToString("MMMM d yyyy");
                }
                else if (type == FtpTransactionType.Future) {
                    folderUrl = AirbnbFtpConfig.FutureFtpUrl + "/Future Transactions - " + reportDate.ToString("MMMM d yyyy");
                }
                else
                {
                    folderUrl = AirbnbFtpConfig.GrossEarningsFtpUrl + "/" + reportDate.ToString("MMMM d yyyy");
                }

                string ftpUrl = folderUrl;
                if (!folderUrl.StartsWith("ftp://")) ftpUrl = Path.Combine(_FtpServerUrl, folderUrl);
                var files = GetDataList(ftpUrl);

                //Loop through the resulting file names.
                foreach (var fileName in files)
                {
                    if (Path.HasExtension(fileName))
                    {
                        fileList.Add(fileName, ftpUrl + "/" + fileName);
                    }
                }
            }
            catch
            {
                // no file available 
            }

            return fileList;
        }

        /// <summary>
        /// upload a file to ftp server at the well-form ftp url location.
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <param name="localFile">the file to be uploaded</param>
        /// <returns>the result of the upload operation</returns>
        public void Upload(string ftpUrl, string localFile)
        {
            try
            {
                FtpWebRequest ftpRequest = GetFtpRequest(ftpUrl, WebRequestMethods.Ftp.UploadFile);
                var ftpStream = ftpRequest.GetRequestStream();
                FileStream localFileStream = new FileStream(localFile, FileMode.Create);

                byte[] byteBuffer = new byte[_BufferSize];
                int bytesSent = localFileStream.Read(byteBuffer, 0, _BufferSize);
                try
                {
                    while (bytesSent != 0)
                    {
                        ftpStream.Write(byteBuffer, 0, bytesSent);
                        bytesSent = localFileStream.Read(byteBuffer, 0, _BufferSize);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    localFileStream.Close();
                    ftpStream.Close();
                    ftpRequest = null;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// upload a file to ftp server at the well-form ftp url location.
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <param name="dataStream">the file content to be uploaded</param>
        /// <returns>the result of the upload operation</returns>
        public void Upload(string ftpUrl, Stream dataStream)
        {
            try
            {
                FtpWebRequest ftpRequest = GetFtpRequest(ftpUrl, WebRequestMethods.Ftp.UploadFile);
                var ftpStream = ftpRequest.GetRequestStream();
                byte[] byteBuffer = new byte[dataStream.Length];
                try
                {
                    int bytesSent = dataStream.Read(byteBuffer, 0, (int)dataStream.Length);
                    ftpStream.Write(byteBuffer, 0, (int)dataStream.Length);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    ftpStream.Close();
                    ftpRequest = null;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// download a file from a well-form ftp url
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <param name="localFile">local filename to be saved under</param>
        /// <returns>the list under the given ftpUrl</returns>
        public void Download(string ftpUrl, string localFile)
        {
            try
            {
                FtpWebRequest ftpRequest = GetFtpRequest(ftpUrl, WebRequestMethods.Ftp.DownloadFile);
                var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
                var ftpStream = ftpResponse.GetResponseStream();

                FileStream localFileStream = new FileStream(localFile, FileMode.Create);

                byte[] byteBuffer = new byte[_BufferSize];
                int bytesRead = ftpStream.Read(byteBuffer, 0, _BufferSize);
                try
                {
                    while (bytesRead > 0)
                    {
                        localFileStream.Write(byteBuffer, 0, bytesRead);
                        bytesRead = ftpStream.Read(byteBuffer, 0, _BufferSize);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    localFileStream.Close();
                    ftpStream.Close();
                    ftpResponse.Close();
                    ftpRequest = null;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// get the folder or file list from a well-form ftp url
        /// </summary>
        /// <param name="ftpUrl">a well-form ftp url including all the directories. e.g. http://myip/mydir/mysubdir </param>
        /// <returns>the list under the given ftpUrl</returns>
        private List<string> GetDataList(string ftpUrl)
        {
            List<string> dataList = new List<string>();
            try
            {
                FtpWebRequest ftpRequest = GetFtpRequest(ftpUrl, WebRequestMethods.Ftp.ListDirectory);
                FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                while (!reader.EndOfStream) dataList.Add(reader.ReadLine());
                reader.Close();
                responseStream.Dispose();
            }
            catch
            {
                throw;
            }

            return dataList;
        }

        private FtpWebRequest GetFtpRequest(string ftpUrl, string requestType)
        {
            try
            {
                FtpWebRequest ftpRequest = (FtpWebRequest)FtpWebRequest.Create(ftpUrl);
                ftpRequest.Method = requestType;
                ftpRequest.Credentials = new NetworkCredential(_AirbnbServerUser, _AirbnbServerPassword);
                ftpRequest.EnableSsl = false;
                ftpRequest.KeepAlive = false;
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;

                return ftpRequest;
            }
            catch
            {
                throw;
            }
        }
    }
}
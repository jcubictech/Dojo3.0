using System;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace DojoTools
{
    class GoogleDriveProvider
    {
        private const string GOOGLE_DRIVE_ROOT = "GoogleDriveRoot";
        private const string GOOGLE_DRIVE_ROOT_DEFAULT = "/";

        public GoogleDriveProvider()
        {
            GoogleDriveRoot = SettingsHelper.GetSafeSetting("SettingsHelper", GOOGLE_DRIVE_ROOT_DEFAULT);
        }

        public string GoogleDriveRoot { get; set; }

        public string GetFileUrl(string filename)
        {
            Console.Write("Enter a Excel filename: ");
            string excelFile = Console.ReadLine();
            return string.Format("{0}/{1}", GoogleDriveRoot, excelFile);
        }
    }
}

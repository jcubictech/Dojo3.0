using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.FtpClient
{
    public class AirbnbFtpConfig
    {
        private const string FTP_SERVER_URL = "ftp://54.191.89.89/";

        public AirbnbFtpConfig()
        {
        }

        public static string RootFtpUrl
        {
            get
            {
                return SettingsHelper.GetSafeSetting("AirbnbFtpUrl", FTP_SERVER_URL);
            }
        }

        public static string CompletedFtpUrl
        {
            get
            {
                return SettingsHelper.GetSafeSetting("CompletedUrl", "Transactions/Completed_Transactions");
            }
        }

        public static string FutureFtpUrl
        {
            get
            {
                return SettingsHelper.GetSafeSetting("FutureUrl", "Transactions/Future_Transactions");
            }
        }

        public static string GrossEarningsFtpUrl
        {
            get
            {
                return SettingsHelper.GetSafeSetting("GrossUrl", "Transactions/Gross_Earnings");
            }
        }

        public static string SteamlineFtpUrl
        {
            get
            {
                return "Transactions/Streamline";
            }
        }

        public static string JobCostFtpUrl
        {
            get
            {
                return "Transactions/JobCosts";
            }
        }
    }
}

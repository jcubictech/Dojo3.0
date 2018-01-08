using System.Configuration;

namespace DojoTools
{
    public class SettingsHelper
    {
       public static int GetSafeInteger(string key, int defaultValue = 0)
        {
            int value = 0;
            try
            {
                string v = ConfigurationManager.AppSettings[key];
                if (System.Int32.TryParse(v, out value) == false)
                {
                    value = defaultValue;
                }
            }
            catch
            {
                value = defaultValue;
            }
            return value;
        }

        public static string GetSafeSetting(string key, string defaultValue = "")
        {
            string value = string.Empty;
            try
            {
                value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value)) value = defaultValue;
            }
            catch
            {
                value = defaultValue;
            }
            return value;
        }

        public static string GetSafeConnectionString(string name, string defaultValue = "")
        {
            string value = string.Empty;
            try
            {
                value = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                if (string.IsNullOrEmpty(value)) value = defaultValue;
            }
            catch
            {
                value = defaultValue;
            }
            return value;
        }
    }
}
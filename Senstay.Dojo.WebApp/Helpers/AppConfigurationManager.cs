using System.Configuration;

namespace Senstay.Dojo.Helpers
{
    public class AppConfigurationManager : IConfigurationManger
    {
        public string GetConfiguration(string configKeyName)
        {
            return ConfigurationManager.AppSettings[configKeyName];
        }

        public string GetConnectionString(string dbConnectionName)
        {
            return ConfigurationManager.ConnectionStrings[dbConnectionName].ConnectionString;
        }
    }
    public interface IConfigurationManger
    {
        string GetConfiguration(string configKeyName);
        string GetConnectionString(string dbConnectionName);
    }
}
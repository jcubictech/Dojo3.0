using StructureMap;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Infrastructure
{
    public class ConfigurationRegistry : Registry
    {
        public interface IAppConfigurationManager : IConfigurationManger
        {
        }

        public ConfigurationRegistry()
        {
            For<IConfigurationManger>().Use<AppConfigurationManager>();
        }
    }
}
using Heroic.AutoMapper;
using Senstay.Dojo.Helpers;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Senstay.Dojo.AutoMapperConfig), "Configure")]
namespace Senstay.Dojo
{
	public static class AutoMapperConfig
	{
		public static void Configure()
		{
			//NOTE: By default, the current project and all referenced projects will be scanned.
			//		You can customize this by passing in a lambda to filter the assemblies by name,
			//		like so:
			HeroicAutoMapperConfigurator.LoadMapsFromCallerAndReferencedAssemblies(x => x.Name.StartsWith(AppConstants.APP_ASSEMBLY_PREFIX));
			//HeroicAutoMapperConfigurator.LoadMapsFromCallerAndReferencedAssemblies();
			//If you run into issues with the maps not being located at runtime, try using this method instead: 
			//HeroicAutoMapperConfigurator.LoadMapsFromAssemblyContainingTypeAndReferencedAssemblies<SomeControllerOrTypeInYourWebProject>();
		}
	}
}
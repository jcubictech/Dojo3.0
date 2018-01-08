using System.Web.Mvc;
using StructureMap;
using StructureMap.Graph;

namespace Senstay.Dojo.Infrastructure.ModelMetadata
{
	public class ModelMetadataRegistry : Registry
	{
		public ModelMetadataRegistry()
		{
			For<ModelMetadataProvider>().Use<ExtensibleModelMetadataProvider>();

			Scan(scan =>
			{
				scan.TheCallingAssembly();
				scan.AddAllTypesOf<IModelMetadataFilter>();
			});

            ModelValidatorProviders.Providers.Clear();
            ModelValidatorProviders.Providers.Add(new DisallowHtmlMetadataValidationProvider());
        }
	}
}
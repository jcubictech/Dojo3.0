using StructureMap;
using StructureMap.Graph;
using Senstay.Dojo.Helpers;

namespace Senstay.Dojo.Infrastructure.Tasks
{
	public class TaskRegistry : Registry
	{
		public TaskRegistry()
		{
			Scan(scan =>
			{
				scan.AssembliesFromApplicationBaseDirectory(
                    a => a.FullName.StartsWith(AppConstants.APP_ASSEMBLY_PREFIX));
				scan.AddAllTypesOf<IRunAtInit>();
				scan.AddAllTypesOf<IRunAtStartup>();
				scan.AddAllTypesOf<IRunOnEachRequest>();
				scan.AddAllTypesOf<IRunOnError>();
				scan.AddAllTypesOf<IRunAfterEachRequest>();
			});
		}
	}
}
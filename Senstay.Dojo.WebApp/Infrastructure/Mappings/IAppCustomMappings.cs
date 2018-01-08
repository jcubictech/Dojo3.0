using Heroic.AutoMapper;

namespace Senstay.Dojo.Infrastructure.Mappings
{
	public interface IAppCustomMappings : IHaveCustomMappings
	{
		T Map<T>(object source);
	}
}
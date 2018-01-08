using AutoMapper;
using Senstay.Dojo.Models;
using Senstay.Dojo.Models.View;

namespace Senstay.Dojo.Infrastructure.Mappings
{
    public class RegistrationMappings : IAppCustomMappings // the interface auto-registers during start-up by Heroic.AutoMapper
    {
        private static MapperConfiguration _configuration;
        private static IMapper _autoMapper;

        public object Configuration  { get { return _configuration; }  } // Accessor

        public T Map<T>(object source)
        {
            return _autoMapper.Map<T>(source);
        }

        public void CreateMappings(IMapperConfiguration configuration)
        {
            _configuration = new MapperConfiguration(config => { });
            var profileExpression = _configuration as IProfileExpression;

            // set app mappings
            profileExpression.CreateMap<InquiriesValidation, InquiryInfoViewModel>();

            _autoMapper = _configuration.CreateMapper();
        }
    }
}
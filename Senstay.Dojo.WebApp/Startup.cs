using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Senstay.Dojo.Startup))]
namespace Senstay.Dojo
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

using Heroic.Web.IoC;
using System.Web.Http;
using System.Web.Mvc;
using StructureMap.Graph;
using Senstay.Dojo.Infrastructure;
using Senstay.Dojo.Infrastructure.Tasks;
using Senstay.Dojo.Infrastructure.ModelMetadata;
using Senstay.Dojo.Helpers;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Senstay.Dojo.StructureMapConfig), "Configure")]
namespace Senstay.Dojo
{
    public static class StructureMapConfig
	{
		public static void Configure()
		{
			IoC.Container.Configure(cfg =>
			{
                cfg.Scan(scan =>
                {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
                });

                cfg.AddRegistry(new ControllerRegistry());
                cfg.AddRegistry(new MvcRegistry());
                cfg.AddRegistry(new DojoActionFilterRegistry(namespacePrefix: AppConstants.APP_ASSEMBLY_PREFIX));
                cfg.AddRegistry(new TaskRegistry());
                cfg.AddRegistry(new ModelMetadataRegistry());
                cfg.AddRegistry(new ConfigurationRegistry());
                cfg.AddRegistry(new UserStoreRegistry());

                // JJJ: These are included in UserStoreRegistry()

                //Are you using ASP.NET Identity?  If so, you'll probably need to configure some additional services:

                //1) Make IUserStore injectable. Replace 'ApplicationUser' with whatever your Identity user type is.
                //cfg.For<IUserStore<ApplicationUser>>().Use<UserStore<ApplicationUser>>();

                //2) Change AppDbContext to your application's Entity Framework context.
                //cfg.For<DbContext>().Use<AppDbContext>();

                //3) This will allow you to inject the IAuthenticationManager. You may not need this, but you will if you 
                //   used the default ASP.NET MVC project template as a starting point!
                //cfg.For<IAuthenticationManager>().Use(ctx => ctx.GetInstance<HttpRequestBase>().GetOwinContext().Authentication);

                //TODO: Add other registries and configure your container (if needed)
            });

			var resolver = new StructureMapDependencyResolver();
			DependencyResolver.SetResolver(resolver);
			GlobalConfiguration.Configuration.DependencyResolver = resolver;
		}
	}
}
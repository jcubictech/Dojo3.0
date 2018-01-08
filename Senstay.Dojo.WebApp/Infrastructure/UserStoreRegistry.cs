using System.Data.Entity;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using StructureMap;
using Senstay.Dojo.Models;

namespace Senstay.Dojo.Infrastructure
{
    public class UserStoreRegistry : Registry
    {
        public UserStoreRegistry()
        {
            For<IUserStore<ApplicationUser>>().Use<UserStore<ApplicationUser>>(); // Make IUserStore injectable
            For<IRoleStore<ApplicationRole, string>>().Use<RoleStore<ApplicationRole>>(); // Make IRoleStore injectable
            For<DbContext>().Use<DojoDbContext>(); // inject application's Entity Framework context
            For<IAuthenticationManager>().Use(ctx => ctx.GetInstance<HttpRequestBase>().GetOwinContext().Authentication); // inject IAuthenticationManager
        }
    }
}
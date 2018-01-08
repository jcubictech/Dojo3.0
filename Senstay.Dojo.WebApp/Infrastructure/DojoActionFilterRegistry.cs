using System.Web.Mvc;
using StructureMap;
using StructureMap.TypeRules;
using Heroic.Web.IoC;

namespace Senstay.Dojo.Infrastructure
{
    public class DojoActionFilterRegistry : Registry
    {
        public DojoActionFilterRegistry(string namespacePrefix)
        {
            For<IFilterProvider>().Use(new StructureMapFilterProvider());

            // this is the same code as in Heroic.Web.IoC ActionFilterRegistry().
            // modify it as you see fit
            Policies.SetAllProperties(x =>
                x.Matching(p =>
                    p.DeclaringType.CanBeCastTo(typeof(ActionFilterAttribute)) &&
                    p.DeclaringType.Namespace.StartsWith(namespacePrefix) &&
                    !p.PropertyType.IsPrimitive &&
                    p.PropertyType != typeof(string)));
        }
    }
}
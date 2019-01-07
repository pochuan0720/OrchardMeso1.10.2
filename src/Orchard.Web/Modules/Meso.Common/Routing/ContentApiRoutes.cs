using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Common.Routing
{
    public class ContentApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "CommonContentApiRoute",
                        Priority = 20,
                        RouteTemplate = "common/api/{Name}",
                        Defaults = new  { area = "Meso.Common", controller = "ContentApi"}
                    },
                   new HttpRouteDescriptor {
                        Name = "CommonLangContentApiRoute",
                        Priority = 20,
                        RouteTemplate = "common/api/{culture}/{Name}",
                        Defaults = new  { area = "Meso.Common", controller = "ContentApi"}
                    }
            };
        }

        public void GetRoutes(ICollection<Orchard.Mvc.Routes.RouteDescriptor> routes)
        {
            foreach (RouteDescriptor routeDescriptor in GetRoutes())
            {
                routes.Add(routeDescriptor);
            }
        }
    }
}
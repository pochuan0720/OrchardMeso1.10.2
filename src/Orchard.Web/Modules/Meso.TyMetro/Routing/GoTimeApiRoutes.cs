using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class GoTimeApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                    new HttpRouteDescriptor {
                        Name = "TyMetroGoTimeApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/GoTime/{Code}",
                        Defaults = new  { area = "Meso.TyMetro", controller = "GoTimeApi", Code = RouteParameter.Optional}
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
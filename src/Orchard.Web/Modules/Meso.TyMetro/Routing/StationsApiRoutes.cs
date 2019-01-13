using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class StationsApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                    new HttpRouteDescriptor {
                        Name = "TyMetroStationsApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/Stations",
                        Defaults = new  { area = "Meso.TyMetro", controller = "StationsApi"}
                    },
                    new HttpRouteDescriptor {
                        Name = "TyMetroStationsLangApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/{culture}/Stations",
                        Defaults = new  { area = "Meso.TyMetro", controller = "StationsApi", culture = RouteParameter.Optional}
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
using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class AccessibilityServiceItemsApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                    new HttpRouteDescriptor {
                        Name = "TyMetroAccessibilityServiceItemsApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/AccessibilityServiceItems",
                        Defaults = new  { area = "Meso.TyMetro", controller = "AccessibilityServiceItemsApi"}
                    },
                    new HttpRouteDescriptor {
                        Name = "TyMetroAccessibilityServiceItemsLangApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/{culture}/AccessibilityServiceItems",
                        Defaults = new  { area = "Meso.TyMetro", controller = "AccessibilityServiceItemsApi", culture = RouteParameter.Optional}
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
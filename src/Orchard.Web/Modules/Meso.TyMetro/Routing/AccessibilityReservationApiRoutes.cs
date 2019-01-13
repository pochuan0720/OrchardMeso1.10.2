using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class AccessibilityReservationApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "TyMetroAccessibilityReservationApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/AccessibilityReservation/{Id}",
                        Defaults = new  { area = "Meso.TyMetro", controller = "AccessibilityReservationApi", Id = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "TyMetroAccessibilityReservationDraftApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/{Version}/AccessibilityReservation/{Id}",
                        Defaults = new  { area = "Meso.TyMetro", controller = "AccessibilityReservationApi", Version = RouteParameter.Optional, Id = RouteParameter.Optional}
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
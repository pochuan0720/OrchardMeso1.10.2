using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class AccessibilityReservationNotificationApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                    new HttpRouteDescriptor {
                        Name = "TyMetroAccessibilityReservationNotificationApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/AccessibilityReservationNotification/{Condition}",
                        Defaults = new  { area = "Meso.TyMetro", controller = "AccessibilityReservationNotificationApi", Condition = RouteParameter.Optional}
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
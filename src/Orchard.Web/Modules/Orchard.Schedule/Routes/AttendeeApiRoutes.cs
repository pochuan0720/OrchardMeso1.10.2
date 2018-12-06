using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Schedule.Routing
{
    public class AttendeeApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "AttendeeActionApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/attendee/{action}",
                        Defaults = new  { area = "Orchard.Schedule", controller = "AttendeeApi"}
                    }
            };
        }

        public void GetRoutes(ICollection<Mvc.Routes.RouteDescriptor> routes)
        {
            foreach (RouteDescriptor routeDescriptor in GetRoutes())
            {
                routes.Add(routeDescriptor);
            }
        }
    }
}
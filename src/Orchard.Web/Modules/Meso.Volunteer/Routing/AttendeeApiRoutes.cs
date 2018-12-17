using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class AttendeeApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "VolunteerAttendeeApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/attendee/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "AttendeeApi"}
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
using System.Collections;
using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                 new HttpRouteDescriptor {
						Name = "VolunteerUserApiRoute",
						Priority = 20,
						RouteTemplate = "volunteer/api/users/{action}",
						Defaults = new	{ area = "Meso.Volunteer", controller = "AccountApi"}		
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
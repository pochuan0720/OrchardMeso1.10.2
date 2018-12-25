using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing.Api
{
    public class RoleApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                 new HttpRouteDescriptor {
											Name = "VolunteerRoleApiRoute",
											Priority = 20,
											RouteTemplate = "volunteer/api/roles/{action}",
											Defaults = new	{ area = "Meso.Volunteer", controller = "RoleApi"}		
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
using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.TyMetro.Routing
{
    public class UsersApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                 new HttpRouteDescriptor {
						Name = "VolunteerUserApiRoute",
						Priority = 20,
						RouteTemplate = "tymetro/api/users/{action}",
						Defaults = new	{ area = "Meso.TyMetro", controller = "AccountApi"}		
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
using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Users.Routing
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                  new HttpRouteDescriptor {
                        Name = "UserIndexAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/users/{id}",
                        Defaults = new  { area = "Orchard.Users", controller = "AdminApi", action = "index", id = RouteParameter.Optional}
                    },
                 new HttpRouteDescriptor {
						Name = "UserAdminApiRoute",
						Priority = 20,
						RouteTemplate = "api/admin/users/{action}/{id}",
						Defaults = new	{ area = "Orchard.Users", controller = "AdminApi", id = RouteParameter.Optional}		
					},
                new HttpRouteDescriptor {
                        Name = "UserApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/users/{action}",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "index"}
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
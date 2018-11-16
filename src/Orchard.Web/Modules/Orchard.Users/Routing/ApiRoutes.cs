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
                        Name = "UserListApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/users",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "list"}
                    },
                  new HttpRouteDescriptor {
                        Name = "UserRegisterApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/users/register",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "register"}
                    },
                  new HttpRouteDescriptor {
                        Name = "UserCreateApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/users/create",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "create"}
                    },
                  new HttpRouteDescriptor {
                        Name = "UserIndexApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/users/{id}",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "index", id = RouteParameter.Optional}
                    },
                 new HttpRouteDescriptor {
						Name = "UserApiRoute",
						Priority = 20,
						RouteTemplate = "api/users/{id}/{action}",
						Defaults = new	{ area = "Orchard.Users", controller = "AccountApi", id = RouteParameter.Optional}		
					},
                  new HttpRouteDescriptor {
                        Name = "UserSelfApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/user",
                        Defaults = new  { area = "Orchard.Users", controller = "AccountApi", action = "self"}
                    },
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
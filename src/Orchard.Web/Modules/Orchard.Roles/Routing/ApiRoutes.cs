using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Api.Routing.Api
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                 new HttpRouteDescriptor {
											Name = "RoleIndexApiRoute",
											Priority = 20,
											RouteTemplate = "api/roles",
											Defaults = new	{ area = "Orchard.Roles", controller = "AdminApi", action = "index"}		
										},
                 new HttpRouteDescriptor {
                                            Name = "RoleApiRoute",
                                            Priority = 20,
                                            RouteTemplate = "api/roles/{id}",
                                            Defaults = new  { area = "Orchard.Roles", controller = "AdminApi", id = RouteParameter.Optional}
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
using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;
//Meso New Add for Contents Api, 2018/11/18
namespace Orchard.Core.Contents.Routing
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                  new HttpRouteDescriptor {
                        Name = "ContentApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/contents/{id}",
                        Defaults = new  { area = "Orchard.Core", controller = "ContentApi", action = "index"}
                    },
                  new HttpRouteDescriptor {
                        Name = "ContentActionApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/contents/{id}/{action}",
                        Defaults = new  { area = "Orchard.Core", controller = "ContentApi"}
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
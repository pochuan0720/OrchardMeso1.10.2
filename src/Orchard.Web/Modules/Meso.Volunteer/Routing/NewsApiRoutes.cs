using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class NewsApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                  new HttpRouteDescriptor {
                        Name = "VolunteerNewsApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/news/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "NewsApi"}
                    },
                   new HttpRouteDescriptor {
                        Name = "VolunteerNewsPostApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/newsposts/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "NewsPostApi"}
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
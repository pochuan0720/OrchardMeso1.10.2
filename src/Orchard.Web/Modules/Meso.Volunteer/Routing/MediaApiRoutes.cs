using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class MediaApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "VolunteerMediaUploadApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/medias/upload/{*folderPath}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "MediaApi", action = "upload", folderPath = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "VolunteerMediaApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/medias/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "MediaApi"}
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
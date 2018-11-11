using System.Collections.Generic;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.MediaLibrary.Routing
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "MediaLibraryUploadApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/medias/upload/{*folderPath}",
                        Defaults = new  { area = "Orchard.MediaLibrary", controller = "ClientStorageApi", action = "upload", folderPath = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "MediaLibraryDeleteApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/medias/delete/{*mediaIds}",
                        Defaults = new  { area = "Orchard.MediaLibrary", controller = "ClientStorageApi", action = "delete", mediaIds = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "MediaLibraryIndexApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/medias/{*folderPath}",
                        Defaults = new  { area = "Orchard.MediaLibrary", action = "index", controller = "ClientStorageApi"}
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Blogs.Routing
{
    public class ApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "BlogCreateAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/blogs/create",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogAdminApi", action = "create"}
                    },
                  new HttpRouteDescriptor {
                        Name = "BlogIndexAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/blogs/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogAdminApi", action = "index", blogId = RouteParameter.Optional}
                    },
                  new HttpRouteDescriptor {
                        Name = "BlogAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/blogs/{action}/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogAdminApi", blogId = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "BlogPostIndexAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/blogposts/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogAdminApi", action = "posts", blogId = RouteParameter.Optional}
                    },
                new HttpRouteDescriptor {
                        Name = "BloIndexgApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/{action}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "index"}
                    },
                 new HttpRouteDescriptor {
                        Name = "BlogPostApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogposts",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "posts"}
                    },
                   new HttpRouteDescriptor {
                        Name = "BlogPostCreateAdminApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/admin/blogpost/{action}/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogPostAdminApi", blogId = RouteParameter.Optional}
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Blogs.Routing
{
    public class BlogPostApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                  new HttpRouteDescriptor {
                        Name = "BlogCreatePostApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blog/posts/create/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogPostApi"}
                    },
                  new HttpRouteDescriptor {
                        Name = "BlogPostIndexApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blog/posts/{blogId}/{postId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogPostApi", action = "index"}
                    },
                   new HttpRouteDescriptor {
                        Name = "BlogPostApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blog/posts/{action}/{blogId}/{postId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogPostApi"}
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
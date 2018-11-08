using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Orchard.Blogs.Routing
{
    public class BlogApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "BlogCreateApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/create",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "create"}
                    },
                  new HttpRouteDescriptor {
                        Name = "BlogIndexApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "index", blogId = RouteParameter.Optional}
                    },
                  new HttpRouteDescriptor {
                        Name = "BlogApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/{action}/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", blogId = RouteParameter.Optional}
                    },
                   new HttpRouteDescriptor {
                        Name = "BlogPostsApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/posts/{blogId}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "posts", blogId = RouteParameter.Optional}
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
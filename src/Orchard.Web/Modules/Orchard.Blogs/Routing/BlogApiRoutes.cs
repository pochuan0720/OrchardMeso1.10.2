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
                        Name = "BlogApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogs/{action}",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi"}
                    },
                   new HttpRouteDescriptor {
                        Name = "BlogPostsApiRoute",
                        Priority = 20,
                        RouteTemplate = "api/blogposts/query",
                        Defaults = new  { area = "Orchard.Blogs", controller = "BlogApi", action = "posts"}
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
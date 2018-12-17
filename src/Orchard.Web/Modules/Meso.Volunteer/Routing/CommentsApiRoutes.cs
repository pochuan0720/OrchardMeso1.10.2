using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class CommentsApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                   new HttpRouteDescriptor {
                        Name = "VolunteerCommentApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/comments/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "CommentApi"}
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
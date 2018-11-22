using Orchard.Environment.Extensions;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Routing;

namespace F2B.Orchard.Schedule.Routes
{
    [OrchardFeature("F2B.Orchard.Schedule")]
    public class ScheduleRoutes : IHttpRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (RouteDescriptor routeDescriptor in GetRoutes())
            {
                routes.Add(routeDescriptor);
            }
        }

        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[] {
                new HttpRouteDescriptor {
                    Name = "ScheduleDetails",
                    Priority = -10,
                    RouteTemplate = "_ScheduleDetails/{id}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "ScheduleDetail",
                    }
                },
                new HttpRouteDescriptor {
                    Name = "DeleteScheduleItem",
                    Priority = -5,
                    RouteTemplate = "_ScheduleDetails/{id}/{mode}/{date}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "ScheduleDetail",
                    },
                    Constraints = new { httpMethod = new HttpMethodConstraint(HttpMethod.Delete.Method)}
                }
            };
        }
    }
}
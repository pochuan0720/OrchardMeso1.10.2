using Orchard.Environment.Extensions;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Routes
{
    //[OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class CalendarLayoutRoutes: IHttpRouteProvider
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
                    Name = "CalendarApi",
                    Priority = -10,
                    RouteTemplate = "_Calendar/{id}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "CalendarLayout",
                    }
                },
                new HttpRouteDescriptor {
                    Name = "CalendarApiWithDate",
                    Priority = -10,
                    RouteTemplate = "_Calendar/{id}/{startDate}/{endDate}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "CalendarLayout",
                    }
                },
            };
        }
    }
}
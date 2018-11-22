﻿using Orchard.Environment.Extensions;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Routes
{
    [OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class UpcomingLayoutRoutes: IHttpRouteProvider
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
                    Name = "UpcomingApi",
                    Priority = -10,
                    RouteTemplate = "_Upcoming/{id}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "UpcomingLayout",
                    }
                },
                new HttpRouteDescriptor {
                    Name = "UpcomingApiWithPage",
                    Priority = -10,
                    RouteTemplate = "_Upcoming/{id}/{page}",
                    Defaults = new {
                        area = "F2B.Orchard.Schedule",
                        controller = "UpcomingLayout",
                    }
                },
            };
        }
    }
}
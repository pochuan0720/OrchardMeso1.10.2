﻿using System.Collections.Generic;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;

namespace Meso.Volunteer.Routing
{
    public class ReportApiRoutes : IHttpRouteProvider
    {
        public IEnumerable<Orchard.Mvc.Routes.RouteDescriptor> GetRoutes()
        {
            return new[]{
                  new HttpRouteDescriptor {
                        Name = "VolunteerReportApiRoute",
                        Priority = 20,
                        RouteTemplate = "volunteer/api/reports/{action}",
                        Defaults = new  { area = "Meso.Volunteer", controller = "ReportApi"}
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
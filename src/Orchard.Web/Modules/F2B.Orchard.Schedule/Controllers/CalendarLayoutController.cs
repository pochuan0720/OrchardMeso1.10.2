﻿using F2B.Orchard.Schedule.Models;
using F2B.Orchard.Schedule.Services;
using F2B.Orchard.Schedule.Settings;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Contents;
using Orchard.Core.Title.Models;
using Orchard.Environment.Extensions;
using Orchard.Projections.Services;
using Orchard.Tags.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Routing;

namespace F2B.Orchard.Schedule.Controllers
{
    using Occurrence = Dictionary<string, object>;

    //[OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class CalendarLayoutController : ApiController
    {
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;

        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);

        public CalendarLayoutController(
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            ISlugService slugService
            )
        {
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
            _orchardServices = orchardServices;
            _slugService = slugService;
        }
     
        public IEnumerable<Occurrence> Get(int id, DateTime startDate, DateTime endDate)
        {
            IEnumerable<ContentItem> contentItems = _scheduleLayoutService.GetProjectionContentItems(id);
            List<Occurrence> occurrences = new List<Occurrence>();

            if (contentItems == null) return occurrences;

            Dictionary<IContent, ScheduleData> ScheduleMap =
                contentItems
                .Select(c => new { k = (IContent)c, v = new ScheduleData(c, Url, _slugService, _orchardServices) })
                .ToDictionary(c => c.k, c => c.v);

            var scheduleOccurrences = contentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _scheduleLayoutService.DateInRange(s, startDate, endDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, startDate, endDate))
                .OrderBy(o => o.Start);

            occurrences = scheduleOccurrences.Select(o => _scheduleLayoutService.GetOccurrenceObject(o, ScheduleMap[o.Source])).ToList();

            return occurrences;
        }

        public IEnumerable<Dictionary<string, object>> Get(int id, int start, int end)
        {
            DateTime startDate = (start != 0) ? UnixEpochTime.AddSeconds(start) : DateTime.MinValue;
            DateTime endDate = (end != 0) ? UnixEpochTime.AddSeconds(end) : DateTime.MaxValue;

            return Get(id, startDate, endDate);
        }
    }
}
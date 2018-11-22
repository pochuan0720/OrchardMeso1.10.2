using F2B.Orchard.Schedule.Models;
using F2B.Orchard.Schedule.Services;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml.Serialization;

namespace F2B.Orchard.Schedule.Controllers
{
    [OrchardFeature("F2B.Orchard.Schedule")]
    public class ScheduleDetailController : ApiController
    {
        private readonly IContentManager _contentManager;
        private readonly IScheduleService _scheduleService;

        public ScheduleDetailController(IContentManager contentManager, IScheduleService scheduleService)
        {
            _contentManager = contentManager;
            _scheduleService = scheduleService;
        }

        public Dictionary<string, object> Get(int id)
        {
            var item = _contentManager.Get(id);
            if (item == null || !item.Has<SchedulePart>()) return new Dictionary<string, object>();
            var schedule = item.As<SchedulePart>();
            DateTime start = schedule.StartDate;
            DateTime end;

            if (!schedule.AllDay)
            {
                start += schedule.StartTime;
            }
            end = start + schedule.Duration;

            var result = new Dictionary<string, object>
            {
                {"start", start},
                {"end", end},
            };

            return result;

        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
            Delete(id, "all", DateTime.MinValue);
        }

        public void Delete(int id, string mode, DateTime date)
        {
            switch (mode)
            {
                case "all": _scheduleService.RemoveScheduleItem(id); break;
                case "single": _scheduleService.RemoveSingleDateForScheduleItem(id, date); break;
                case "following": _scheduleService.RemoveFollowingDatesForScheduleItem(id, date); break;
            }
        }
    }
}
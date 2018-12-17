using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;
using Orchard.Environment.Extensions;
using Orchard.ContentManagement;
using Orchard.Projections.Services;
using Orchard.Schedule.ViewModels;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Projections.Models;
using Orchard.Core.Containers.Services;
using Orchard.Core.Containers.Models;
using Orchard.Security;
using Orchard.Schedule.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard.PublishLater.Models;

namespace Meso.Volunteer.Services {

    using Occurrence = Dictionary<string, object>;   
        
    public class CalendarService : ICalendarService
    {
        private readonly IOrchardServices _services;
        private readonly IProjectionManager _projectionManager;
        private readonly IContainerService _containerService;
        private readonly IScheduleLayoutService _scheduleLayoutService;

        public CalendarService(IOrchardServices services, 
            IContainerService containerService, 
            IProjectionManager projectionManager, 
            IScheduleLayoutService scheduleLayoutService) {
            _services = services;
            _containerService = containerService;
            _projectionManager = projectionManager;
            _scheduleLayoutService = scheduleLayoutService;
        }

        public Occurrence GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData) {
            return _scheduleLayoutService.GetOccurrenceObject(scheduleEvent, scheduleData);
        }

        public object GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData, bool withAttendee)
        { 
            DateTime? publishLater = scheduleEvent.Source.As<PublishLaterPart>().ScheduledPublishUtc.Value;
            ContainerPart containerPart = scheduleEvent.Source.As<ContainerPart>();
            object obj = null;
            SchedulePart schedule = scheduleEvent.Source.As<SchedulePart>();
            if (containerPart != null && withAttendee)
            {
                IList<string> selectedItemContentTypes = containerPart.ItemContentTypes.Select(x => x.Name).ToList();


                IList<object> list = new List<object>();
                var contentItems = _containerService.GetContentItems(containerPart.Id);
                foreach (ContentItem _item in contentItems)
                {
                    CommonPart common = _item.As<CommonPart>();
                    IUser user = common.Owner;
                    var userModel = _services.ContentManager.BuildEditor(user);
                    var attendeeModel = _services.ContentManager.BuildEditor(_item);
                    JObject attendee = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), attendeeModel);
                    attendee.Add(new JProperty("Id", _item.Id));
                    attendee.Add(new JProperty("CreatedUtc", common.CreatedUtc));
                    attendee.Add(new JProperty("User", Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(user), userModel)));
                    list.Add(attendee);
                }

                scheduleEvent.Source.As<SchedulePart>();
                obj = new
                {
                    Id = scheduleData.Id,
                    Title = scheduleData.Title,
                    Body = scheduleData.Body,
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.Start, schedule.TimeZone),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.End, schedule.TimeZone),
                    IsPublished = schedule.IsPublished,
                    PublishLater = publishLater == null ? publishLater : (DateTime)publishLater,
                    Attendee = list.ToArray(),
                    Container = selectedItemContentTypes.ToArray<string>()
                };
                
            }
            else
            {
                obj = new
                {
                    Id = scheduleData.Id,
                    Title = scheduleData.Title,
                    Body = scheduleData.Body,
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.Start, schedule.TimeZone),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.End, schedule.TimeZone),
                    IsPublished = schedule.IsPublished,
                    PublishLater = publishLater == null ? publishLater : (DateTime)publishLater
                };
            }
            var model = _services.ContentManager.BuildEditor(schedule); ;
            return UpdateModelHandler.GetData(JObject.FromObject(obj), model);
        }

        public bool DateInRange(SchedulePart part, DateTime start, DateTime end) {
            return _scheduleLayoutService.DateInRange(part, start, end);
        }
        
        public bool DateInFuture(SchedulePart part) {
            return _scheduleLayoutService.DateInFuture(part);
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query)
        {
            IEnumerable<ContentItem> results = null;
            try
            {
                results = _projectionManager.GetContentItems(query);
                if (results == null) return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            results = results.Where(c => c.Has<SchedulePart>());
            if (results.Count() == 0) return null;

            return results;
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(int queryId) {
            return _scheduleLayoutService.GetProjectionContentItems(queryId);
        }
    }
}
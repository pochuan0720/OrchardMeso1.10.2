using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using F2B.Orchard.Schedule.Models;
using Orchard.Environment.Extensions;
using Orchard.ContentManagement;
using Orchard.Projections.Services;
using F2B.Orchard.Schedule.ViewModels;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Projections.Models;

namespace F2B.Orchard.Schedule.Services {

    using Occurrence = Dictionary<string, object>;   
        
    //[OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class ScheduleLayoutService : IScheduleLayoutService {

        private readonly IOrchardServices _services;
        private readonly IProjectionManager _projectionManager;

        public ScheduleLayoutService(IOrchardServices services, IProjectionManager projectionManager) {
            _services = services;
            _projectionManager = projectionManager;
        }

        public Occurrence GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData) {
            string[] emptyTags = new string[] { };

            return new Occurrence {
                { "id", scheduleData.Id },
                { "start", scheduleEvent.Start },
                { "end", scheduleEvent.End },
                { "allDay", scheduleData.AllDay },
                { "title", scheduleData.Title},
                { "url", scheduleData.DisplayUrl},
                { "deletable", scheduleData.DisplayUrl },
                { "defaultBackgroundColor", scheduleData.BackgroundColor },
                { "defaultBorderColor", scheduleData.BorderColor },
                { "defaultTextColor", scheduleData.TextColor },
                { "tags", scheduleData.Tags??emptyTags },
                { "className", scheduleData.Classes??emptyTags },
                //{ "backgroundColor", string.Format("#{0:X6}", settings.EventColor) },
            };
        }

        public ScheduleEditApiViewMode GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData)
        {
            return new ScheduleEditApiViewMode
            {
                Id = scheduleData.Id,
                Title = scheduleData.Title,
                StartDate = scheduleEvent.Start,
                EndDate = scheduleEvent.End,
                Contact = scheduleEvent.Source.As<CommonPart>().Owner.Id,
                Data = UpdateModelHandler.GetData(_services.ContentManager.BuildEditor(scheduleEvent.Source))
            };
        }

        public bool DateInRange(SchedulePart part, DateTime start, DateTime end) {
            if (part.ScheduleType == ScheduleRepeatType.Single) {
                return (part.StartDate + part.StartTime >= start && part.StartDate <= end);
            }
            else if (part.EndDate.HasValue) {
                return part.StartDate <= end && part.EndDate.Value >= start;
            }
            else {
                return part.StartDate <= end;
            }
        }
        
        public bool DateInFuture(SchedulePart part) {
            var start = DateTime.UtcNow;

            if (part.ScheduleType == ScheduleRepeatType.Single) {
                return (part.StartDate + part.Duration >= start);
            }
            else if (part.EndDate.HasValue) {
                return part.EndDate.Value >= start;
            }
            else {
                return true;
            }
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
            IEnumerable<ContentItem> results = null;
            try {
                results = _projectionManager.GetContentItems(queryId);
                if (results == null) return null;
            }
            catch (ArgumentException) {
                return null;
            }

            results = results.Where(c => c.Has<SchedulePart>());
            if (results.Count() == 0) return null;

            return results;
        }
    }
}
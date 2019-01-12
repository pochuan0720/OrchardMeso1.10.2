using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Projections.Models;
using System;
using System.Collections.Generic;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;

namespace Meso.Volunteer.Services {
    public interface ICalendarService : IDependency {
        bool DateInFuture(SchedulePart part);
        bool DateInRange(SchedulePart part, DateTime start, DateTime end);
        bool DateCollection(ScheduleOccurrence occurrence, DateTime start, DateTime end);
        Dictionary<string, object> GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData);
        object GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData, bool withAttendee = true, int userId = 0);
        IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query);
        IEnumerable<ContentItem> GetProjectionContentItems(int queryId);

        void Notification(ContentItem content, string contentType, JObject obj=null);
        Dictionary<string, object> FormDataToDictionary(NameValueCollection nvc);
    }
}

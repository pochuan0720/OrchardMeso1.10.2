using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Projections.Models;
using System;
using System.Collections.Generic;
using Meso.Volunteer.ViewModels;

namespace Meso.Volunteer.Services {
    public interface ICalendarService : IDependency {
        bool DateInFuture(SchedulePart part);
        bool DateInRange(SchedulePart part, DateTime start, DateTime end);
        Dictionary<string, object> GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData);
        object GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData, bool withAttendee = true);
        IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query);
        IEnumerable<ContentItem> GetProjectionContentItems(int queryId);
    }
}

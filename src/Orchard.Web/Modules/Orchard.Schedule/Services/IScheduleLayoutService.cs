using Orchard.Schedule.Models;
using Orchard.Schedule.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Projections.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Services {
    public interface IScheduleLayoutService : IDependency {
        bool DateInFuture(SchedulePart part);
        bool DateInRange(SchedulePart part, DateTime start, DateTime end);
        Dictionary<string, object> GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData);
        ScheduleApiViewMode GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData);
        IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query);
        IEnumerable<ContentItem> GetProjectionContentItems(int queryId);
    }
}

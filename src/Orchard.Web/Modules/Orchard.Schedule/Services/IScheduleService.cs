using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Services
{
    public interface IScheduleService: IDependency
    {
        IEnumerable<ScheduleOccurrence> GetOccurrencesForDateRange(SchedulePart schedule, DateTime start, DateTime end);

        IEnumerable<ScheduleOccurrence> GetOccurrencesFromDate(IEnumerable<SchedulePart> schedules, DateTime start, int count);
        ScheduleOccurrence GetNextOccurrence(SchedulePart schedule, DateTime start);
        
        void UpdateExcludedDatesForContentItem(ContentItem item, IEnumerable<DateTime> dates);

        void RemoveScheduleItem(int id);
        void RemoveFollowingDatesForScheduleItem(int id, DateTime date);
        void RemoveSingleDateForScheduleItem(int id, DateTime date);

        string ScheduleDescription(SchedulePart schedule, string DateFormat);
    }
}

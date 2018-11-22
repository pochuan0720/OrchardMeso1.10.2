using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;

namespace F2B.Orchard.Schedule.Models
{
    public class SchedulePartRecord: ContentPartRecord
    {
        public SchedulePartRecord()
        {
            StartDate = DateTime.Now.Date;
            //StartTime = Convert.ToInt32(TimeSpan.FromHours(8).TotalMinutes);
            //Duration = Convert.ToInt32(TimeSpan.FromHours(1).TotalMinutes);
            ScheduleType = (short)ScheduleRepeatType.Single;
            RepeatInterval = 1;
            ExcludedDates = new List<ExcludedDateRecord>();
            TimeZone = TimeZoneInfo.Utc.Id;
        }

        public virtual DateTime StartDate { get; set; }    // When to start scheduling
        public virtual DateTime? EndDate { get; set; }  // When to end scheduling for repeat
        public virtual int? Occurrences { get; set; }   // Number of occurrences to schedule

        public virtual short ScheduleType { get; set; }     // integer value from ScheduleType enum
        public virtual short RepeatInterval { get; set; }

        // Daily Schedule -- no special fields needed

        // Weekly Schedule
        public virtual short DaysOfWeek { get; set; }       // bit field from ScheduleDayOfWeek enum

        // Monthly Schedule By Day of Month
        public virtual short DayOfMonth { get; set; }

        // Monthly Schedule By Day of Week
        public virtual short WeekOfMonth { get; set; }      // bit field from ScheduleWeekOfMonth enum

        // Yearly Schedule
        public virtual short Month { get; set; }            // bit field from ScheduleMonth enum

        // Event information
        public virtual bool AllDay { get; set; }            // true if all day event (start time will be ignored)
        public virtual int StartTime { get; set; }          // start time in minutes from midnight
        public virtual int Duration { get; set; }           // duration of event in (AllDay)?days:minutes

        public virtual IList<ExcludedDateRecord> ExcludedDates { get; set; }

        public virtual int OffsetDays { get; set; }             // offset from start dates in days

        public virtual string TimeZone { get; set; }
    }
}
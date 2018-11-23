using Orchard.ContentManagement;
using System;

namespace Orchard.Schedule.Models
{
    public class ScheduleOccurrence: IComparable<ScheduleOccurrence>, IEquatable<ScheduleOccurrence> {
        public ScheduleOccurrence(SchedulePart schedule, DateTime start)
        {
            Schedule = schedule;
            if (schedule != null) {
                Source = schedule.ContentItem;
                AllDay = schedule.AllDay;

                Start = start.Date;

                if (!AllDay) Start += schedule.StartTime;
                End = Start + schedule.Duration;

                if (AllDay) End = End.AddSeconds(-1);
            }
            else {
                Source = null;
                AllDay = true;
                Start = start.Date;
                End = start.Date.AddDays(1).AddMinutes(-1);
            }

        }

        public IContent Source { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool AllDay { get; set; }

        public SchedulePart Schedule { get; set; }

        public int CompareTo(ScheduleOccurrence other) {
            var compareDate = Start.Date.CompareTo(other.Start.Date);
            if (AllDay && other.AllDay) {
                return compareDate != 0 ? compareDate : CompareScheduleParts(Schedule, other.Schedule);
            }

            if (compareDate == 0) {
                if (AllDay) return -1;
                if (other.AllDay) return 1;
            }

            var compare = Start.CompareTo(other.Start);
            if (compare == 0) {
                return (Schedule == null) ? 0 : (Schedule.Id.CompareTo(other.Schedule.Id));
            }

            return compare;
        }

        private static int CompareScheduleParts(IContent left, IContent right) {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            return left.Id.CompareTo(right.Id);
        }

        public ScheduleOccurrence AddDays(int days) {
            return new ScheduleOccurrence(Schedule, Start.AddDays(days));
        }

        public bool Equals(ScheduleOccurrence other) {
            return Start.Equals(other.Start) && End.Equals(other.End) && Schedule == other.Schedule;
        }
    }
}
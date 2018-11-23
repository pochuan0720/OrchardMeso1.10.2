using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Providers
{
    public class DayOfWeekMonthProvider : DateProvider
    {
        private readonly DayOfWeek _dayOfWeek;
        private readonly short _weekOfMonth;
        private readonly int _interval;
        private readonly DateTime _startDate;
        private readonly TimeSpan _duration;

        private readonly bool _fromEnd;

        public DayOfWeekMonthProvider(DayOfWeek dayOfWeek, short weekOfMonth, SchedulePart part): base(part)
        {
            if (part == null) throw new ArgumentNullException("part");

            _dayOfWeek = dayOfWeek;
            _weekOfMonth = weekOfMonth;
            _interval = part.RepeatInterval;
            _startDate = part.StartDate;
            _duration = TimeSpan.FromDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));

            _fromEnd = part.ScheduleType == ScheduleRepeatType.MonthlyByWeekFromEnd;

            if (!_fromEnd) {
                return;
            }
            var firstDate = new DateTime(_startDate.Year, _startDate.Month, 1);
            var lastDate = firstDate.AddMonths(1).AddDays(-1);

            var firstDay = firstDate.DayOfWeek;
            var lastDay = lastDate.DayOfWeek;

            var weeksForDay = 4;
            if (firstDay < lastDay)
            {
                if (_dayOfWeek >= firstDay && _dayOfWeek <= lastDay) weeksForDay = 5;
            }
            else if (firstDay > lastDay)
            {
                if (_dayOfWeek >= firstDay || _dayOfWeek <= lastDay) weeksForDay = 5;
            }

            _weekOfMonth = (short)(_weekOfMonth - weeksForDay - 1);
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            if (_startDate > end) return new List<ScheduleOccurrence>();

            DateTime currentDate;
            var startDateMonth = _startDate.Year * 12 + _startDate.Month;

            if (start < _startDate) currentDate = new DateTime(_startDate.Year, _startDate.Month, 1);
            else currentDate = new DateTime(start.Year, start.Month, 1);
            var currentDateMonth = currentDate.Year * 12 + currentDate.Month;

            var months = (currentDateMonth - startDateMonth) % _interval;
            if (months != 0)
            {
                currentDate = currentDate.AddMonths(-months);
            }

            currentDate = currentDate.AddMonths(-_interval);

            return _fromEnd ? Over(currentDate, start, end, GetNextDateFromEnd, reverse) : Over(currentDate, start, end, GetNextDateFromStart, reverse);
        }

        private IEnumerable<ScheduleOccurrence> Over(DateTime currentDate, DateTime start, DateTime end, Func<DateTime, DateTime> nextDate, bool reverse = false)
        {
            while (currentDate + _duration < start) currentDate = nextDate(currentDate);

            while (currentDate <= end)
            {
                yield return new ScheduleOccurrence(_part, currentDate);
                currentDate = nextDate(currentDate);
            }
        }

        private DateTime GetNextDateFromStart(DateTime current) {
            while (true) {
                var next = new DateTime(current.Year, current.Month, 1).AddMonths(_interval);
                var month = next.Month;

                var dow = (int) next.DayOfWeek;
                var delta = (int) _dayOfWeek - dow;
                if (delta < 0) {
                    delta += 7;
                }
                next = next.AddDays(delta);
                var next2 = next.AddDays(7*(_weekOfMonth - 1));
                if (next2.Month == month) {
                    return next2;
                }
                current = next;
            }
        }

        private DateTime GetNextDateFromEnd(DateTime current) {
            while (true) {
                var next = new DateTime(current.Year, current.Month, 1).AddMonths(_interval);
                next = next.AddMonths(1).AddDays(-1);

                var month = next.Month;

                var dow = (int) next.DayOfWeek;
                var delta = (int) _dayOfWeek - dow;
                if (delta > 0) {
                    delta -= 7;
                }
                next = next.AddDays(delta);
                var next2 = next.AddDays(7*(_weekOfMonth + 1));
                if (next2.Month == month) {
                    return next2;
                }
                current = next;
            }
        }
    }
}
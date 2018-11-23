using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Providers
{
    public class DayOfMonthProvider : DateProvider
    {
        private readonly short _dayOfMonth;
        private readonly short _interval;
        private readonly DateTime _startDate;
        private readonly TimeSpan _duration;
        private readonly bool _fromEnd;

        public DayOfMonthProvider(SchedulePart part): base(part)
        {
            if (part == null) throw new ArgumentNullException("part");

            _dayOfMonth = part.DayOfMonth;
            _interval = part.RepeatInterval;
            _startDate = part.StartDate;
            _duration = TimeSpan.FromDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));

            _fromEnd = part.ScheduleType == ScheduleRepeatType.MonthlyByDayFromEnd;

            if (_fromEnd)
            {
                // figure out how many days from end of month
                short daysInMonth = (short)DateTime.DaysInMonth(_startDate.Year, _startDate.Month);
                _dayOfMonth = (short)(_dayOfMonth - daysInMonth - 1);
            }
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            if (_startDate > end) return new List<ScheduleOccurrence>();

            var startDateMonth = _startDate.Year * 12 + _startDate.Month;

            var currentDate = start < _startDate ? new DateTime(_startDate.Year, _startDate.Month, 1) : new DateTime(start.Year, start.Month, 1);
            var currentDateMonth = currentDate.Year * 12 + currentDate.Month;

            var months = (currentDateMonth - startDateMonth) % _interval;
            if (months != 0)
            {
                currentDate = currentDate.AddMonths(-months);
            }

            currentDate = currentDate.AddMonths(-_interval);

            return _fromEnd ? Over(currentDate, start, end, GetNextDateFromEnd, reverse) : Over(currentDate, start, end, GetNextDateFromStart, reverse);
        }

        private IEnumerable<ScheduleOccurrence> Over(DateTime currentDate, DateTime start, DateTime end,  Func<DateTime, DateTime> nextDate, bool reverse = false)
        {
            while (currentDate + _duration < start) currentDate = nextDate(currentDate);

            while (currentDate <= end)
            {
                yield return new ScheduleOccurrence(_part, currentDate);
                currentDate = nextDate(currentDate);
            }
        }

        private DateTime GetNextDateFromStart(DateTime current)
        {
            DateTime next;
            int dayOfMonth = _dayOfMonth;
            if (current.Day < dayOfMonth)
            {
                int currentMonth;
                do
                {
                    currentMonth = current.Month;
                    current = current.AddDays(dayOfMonth - current.Day);
                    if (currentMonth != current.Month)
                    {
                        current = new DateTime(current.Year, current.Month, 1);
                    }
                } while (currentMonth != current.Month);
                next = current;
            }
            else
            {
                var first = new DateTime(current.Year, current.Month, 1);
                var interval = _interval;
                do
                {
                    first = first.AddMonths(interval);
                    try
                    {
                        next = new DateTime(first.Year, first.Month, dayOfMonth);
                        break;
                    }
                    catch (ArgumentOutOfRangeException) { }
                } while (true);
            }

            return next;
        }

        private DateTime GetNextDateFromEnd(DateTime current)
        {
            DateTime next;
            var targetDay = new DateTime(current.Year, current.Month, 1).AddMonths(1).AddDays(_dayOfMonth);

            if (current < targetDay) return targetDay;
            else
            {
                var first = new DateTime(current.Year, current.Month, 1).AddMonths(1);
                var interval = _interval;
                do
                {
                    first = first.AddMonths(interval);
                    var month = first.AddDays(-1).Month;
                    next = first.AddDays(_dayOfMonth);
                    if (next.Month == month) break;
                } while (true);
            }

            return next;
        }
    }
}
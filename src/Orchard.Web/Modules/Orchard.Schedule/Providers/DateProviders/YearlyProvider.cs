using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Providers
{
    public class YearlyProvider : DateProvider
    {
        private readonly int _month;
        private readonly int _day;
        private readonly int _startYear;
        private readonly int _yearInterval;
        private readonly TimeSpan _duration;

        public YearlyProvider(SchedulePart part): base(part)
        {
            _startYear = part.StartDate.Year;
            _month = part.StartDate.Month;
            _day = part.StartDate.Day;
            _duration = TimeSpan.FromDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));

            _yearInterval = part.RepeatInterval;
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            var year = start.Year;
            if (year < _startYear)
                year = _startYear;
            else
            {
                var yearsOff = (year - _startYear) % _yearInterval;
                if (yearsOff != 0)
                {
                    year += _yearInterval - yearsOff;
                }
            }

            var current = GetDateForYear(year);
            if (current + _duration < start)
            {
                year += _yearInterval;
                current = GetDateForYear(year);
            }
            if (current > end) yield break;

            while (true)
            {
                yield return new ScheduleOccurrence(_part, current);
                year += _yearInterval;
                current = GetDateForYear(year);
                if (current > end) break;
            }
        }

        private DateTime GetDateForYear(int year)
        {
            var day = _day;

            if (_month == 2 && _day == 29 && !DateTime.IsLeapYear(year))
            {
                day = 28;
            }

            return new DateTime(year, _month, day);
        }
    }
}
using F2B.Orchard.Schedule.Models;
using System;
using System.Collections.Generic;

namespace F2B.Orchard.Schedule.Providers
{
    public class WeeklyProvider: DateProvider
    {
        private readonly DayOfWeek _dayOfWeek;
        private readonly int _interval;
        private readonly DateTime _startDate;
        private readonly TimeSpan _duration;

        public WeeklyProvider(DayOfWeek dayOfWeek, SchedulePart part): base(part)
        {
            _dayOfWeek = dayOfWeek;
            _interval = part.RepeatInterval;
            _startDate = part.StartDate;
            _duration = TimeSpan.FromDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));

            // Make sure the start date is on the day of week given.
            if (_startDate.DayOfWeek == _dayOfWeek) {
                return;
            }
            var days = _dayOfWeek - _startDate.DayOfWeek;
            if (days < 0) days += 7;
            _startDate = _startDate.AddDays(days);
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            var interval = 7 * _interval;

            var currentDate = start.Date;
            if (currentDate < _startDate) currentDate = _startDate;

            var days = (int)currentDate.DayOfWeek - (int)_dayOfWeek;

            if (days < 0) days += 7;
            currentDate = currentDate.AddDays(-days);

            var weeksOff = ((currentDate - _startDate).Days / 7) % _interval;
            if (weeksOff != 0) currentDate = currentDate.AddDays(-7 * weeksOff);

            if (currentDate + _duration < start) currentDate = currentDate.AddDays(interval);

            while (currentDate <= end)
            {
                yield return new ScheduleOccurrence(_part, currentDate);
                currentDate = currentDate.AddDays(interval);
            }
        }
    }
}
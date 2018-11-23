using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.Providers
{
    public class DailyProvider: DateProvider
    {
        private readonly DateTime _startDate;
        private readonly TimeSpan _duration;
        private readonly int _dayInterval;

        public DailyProvider(SchedulePart part) : base(part)
        {
            _startDate = _part.StartDate;
            _duration = TimeSpan.FromDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));
            _dayInterval = _part.RepeatInterval;
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            var current = start.Date;

            if (current < _startDate) current = _startDate;

            var days = (current - _startDate).Days % _dayInterval;
            current = current.AddDays(-days);

            if (current + _duration < start) current = current.AddDays(_dayInterval);

            while (current <= end)
            {
                yield return new ScheduleOccurrence(_part, current);
                current = current.AddDays(_dayInterval);
            }
        }
    }
}
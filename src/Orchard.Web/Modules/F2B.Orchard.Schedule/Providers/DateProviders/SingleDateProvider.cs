using F2B.Orchard.Schedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Providers
{
    public class SingleDateProvider : DateProvider
    {
        private readonly DateTime _scheduleStart;
        private readonly DateTime _scheduleEnd;

        internal SingleDateProvider(DateTime date)
        {
            _scheduleStart = _scheduleEnd = date;
        }

        public SingleDateProvider(SchedulePart part): base(part)
        {
            _scheduleStart = part.StartDate;
            _scheduleEnd = part.StartDate.AddDays(part.DaysIncluded()).Subtract(TimeSpan.FromMinutes(1));
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            start = start.Date;
            end = end.Date;

            if (_scheduleEnd >= start && _scheduleStart <= end)
            {
                yield return new ScheduleOccurrence(_part, _scheduleStart);
            }
        }
    }
}
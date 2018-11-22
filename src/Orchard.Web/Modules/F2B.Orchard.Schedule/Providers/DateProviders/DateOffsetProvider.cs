using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using F2B.Orchard.Schedule.Models;

namespace F2B.Orchard.Schedule.Providers
{
    public class DateOffsetProvider: DateProvider
    {
        private readonly int _offsetDays;
        private readonly DateProvider _wrapped;

        public DateOffsetProvider(DateProvider wrapped, int offsetDays): base(wrapped)
        {
            _wrapped = wrapped;
            _offsetDays = offsetDays;
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            if (_offsetDays == 0) return _wrapped.Over(start, end, reverse); // shouldn't happen, but slightly more performant if it does

            return _wrapped.Over(start.AddDays(-_offsetDays), end.AddDays(-_offsetDays), reverse).Select(s => s.AddDays(_offsetDays));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
{
    public class OccurrenceProvider : DateProvider
    {
        private readonly DateTime _startDate;
        private readonly int _occurrences;
        private readonly DateProvider _wrapped;

        public OccurrenceProvider(DateProvider wrapped, DateTime startDate, int occurrences): base(wrapped)
        {
            _wrapped = wrapped;
            _startDate = startDate;
            _occurrences = occurrences;
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            var results = _wrapped.Over(_startDate, end).Take(_occurrences).Where(s => s.Start >= start);
            if (reverse) results = results.Reverse();
            return results;
        }
    }
}
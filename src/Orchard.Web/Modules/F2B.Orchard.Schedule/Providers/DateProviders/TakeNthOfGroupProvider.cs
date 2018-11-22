using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using F2B.Orchard.Schedule.Models;

namespace F2B.Orchard.Schedule.Providers.DateProviders
{
    public class TakeNthOfGroupProvider: DateProvider
    {
        private readonly GroupedDateProvider _wrapped;
        private readonly int _offset;

        public TakeNthOfGroupProvider(GroupedDateProvider wrapped, int offset) {
            _wrapped = wrapped;
            if (_offset == 0) throw new ArgumentException("Offset must be a non-zero value", "offset");
            _offset = offset;
        }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false) {
            return _offset < 0
                ? _wrapped.Over(start, end).SelectMany(g => g.Reverse().Skip(-_offset - 1).Take(1))
                : _wrapped.Over(start, end).SelectMany(g => g.Skip(_offset - 1).Take(1));
        }
    }
}
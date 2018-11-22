using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using F2B.Orchard.Schedule.Models;

namespace F2B.Orchard.Schedule.Providers.DateProviders
{
    public class GroupedByMonthProvider
    {
        private readonly DateProvider _wrapped;
        public GroupedByMonthProvider(DateProvider wrapped) {
            _wrapped = wrapped;
        }

        public IEnumerable<IEnumerable<ScheduleOccurrence>> Over(DateTime start, DateTime end, bool reverse = false) {
            var startDate = new DateTime(start.Year, start.Month, 1);
            while (startDate <= end) {
                var endDate = startDate.AddMonths(1).AddDays(-1);
                yield return _wrapped.Over(start < startDate ? start : startDate, endDate > end ? end : endDate, reverse);
                startDate = startDate.AddMonths(1);
            }
        }
    }
}
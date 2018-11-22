using F2B.Orchard.Schedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Providers
{
    public abstract class DateProvider {
        protected SchedulePart _part;

        protected DateProvider(SchedulePart part) {
            if (part == null) throw new ArgumentNullException("part");

            _part = part;
        }

        protected DateProvider(DateProvider wrapped) {
            _part = wrapped._part;
        }

        // This constructor should only be used for providers that don't get a schedule part or wrap another provider.
        protected DateProvider() {
            _part = null;
        }

        public abstract IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false);
    }
}
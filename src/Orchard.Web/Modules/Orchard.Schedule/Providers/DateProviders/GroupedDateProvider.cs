using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Providers
{
    public abstract class GroupedDateProvider {
        protected DateProvider _wrapped;

        protected GroupedDateProvider(DateProvider wrapped) {
            _wrapped = wrapped;
        }

        public abstract IEnumerable<IEnumerable<ScheduleOccurrence>> Over(DateTime start, DateTime end, bool reverse = false);
    }
}
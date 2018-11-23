using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
{
    public abstract class FilterProvider : DateProvider
    {
        private readonly DateProvider _wrapped;

        public FilterProvider(DateProvider wrapped): base(wrapped)
        {
            _wrapped = wrapped;
        }

        protected abstract bool IsIncluded(DateTime date);

        protected virtual DateTime MinDate { get { return DateTime.MinValue; } }
        protected virtual DateTime MaxDate { get { return DateTime.MaxValue; } }

        //public override IEnumerable<DateTime> From(DateTime start)
        //{
        //    if (start < MinDate) start = MinDate;
        //    return _wrapped.From(start).Until(MaxDate).Where(IsIncluded);
        //}

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            start = start.Date;
            if (start < MinDate) start = MinDate;

            end = end.Date;
            if (end > MaxDate) end = MaxDate;

            return _wrapped.Over(start, end).Where(s => IsIncluded(s.Start));
        }
    }
}
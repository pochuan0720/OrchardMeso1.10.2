using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Providers
{
    public class DateRangeFilterProvider : FilterProvider
    {
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;

        public DateRangeFilterProvider(DateProvider wrapped, DateTime startDate) : this(wrapped, startDate, null) { }

        public DateRangeFilterProvider(DateProvider wrapped, DateTime startDate, DateTime? endDate)
            : base(wrapped)
        {
            if (endDate.HasValue && endDate.Value < startDate) throw new ArgumentException("endDate must not be before startDate");
            _startDate = startDate.Date;
            _endDate = endDate.HasValue ? endDate.Value.Date : DateTime.MaxValue;
        }

        protected override DateTime MinDate { get { return _startDate; } }
        protected override DateTime MaxDate { get { return _endDate; } }

        protected override bool IsIncluded(DateTime date)
        {
            return (date.Date >= _startDate && date.Date <= _endDate);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
{
    public class DifferenceProvider: DateProvider
    {
        private readonly DateProvider _include;
        private readonly DateProvider _exclude;

        public DifferenceProvider(DateProvider include, DateProvider exclude) : base(include)
        {
            _include = include;
            _exclude = exclude;
        }

        //public override IEnumerable<DateTime> From(DateTime start)
        //{
        //    return _include.From(start).DifferenceOrdered(_exclude.From(start));
        //}

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            return _include.Over(start, end).DifferenceOrdered(_exclude.Over(start, end), Comparer<ScheduleOccurrence>.Create((a,b)=>a.Start.Date.CompareTo(b.Start.Date)));
        }
    }
}
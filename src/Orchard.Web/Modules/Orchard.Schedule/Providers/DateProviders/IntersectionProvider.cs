using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
{
    public class IntersectionProvider : CollectionProvider
    {
        //public override IEnumerable<DateTime> From(DateTime start)
        //{
        //    return Providers
        //        .MergeOrdered(p => p.From(start))
        //        .Distinct();
        //}

        public IntersectionProvider() {}
        public IntersectionProvider(ICollection<DateProvider> providers) : base(providers) {}

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            return Providers.IntersectOrdered(p => p.Over(start, end, reverse)).Distinct();
        }
    }
}
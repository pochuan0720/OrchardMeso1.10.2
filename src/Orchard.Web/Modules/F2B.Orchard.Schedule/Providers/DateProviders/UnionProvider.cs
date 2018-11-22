using System;
using System.Collections.Generic;
using System.Linq;
using F2B.Orchard.Schedule.Models;

namespace F2B.Orchard.Schedule.Providers
{
    public class UnionProvider : CollectionProvider
    {
        //public override IEnumerable<DateTime> From(DateTime start)
        //{
        //    return Providers
        //        .MergeOrdered(p => p.From(start))
        //        .Distinct();
        //}
        public UnionProvider() {}

        public UnionProvider(ICollection<DateProvider> providers) : base(providers) {}

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            return Providers.MergeOrdered(p => p.Over(start, end, reverse)).Distinct();
        }
    }
}
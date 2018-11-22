using System;
using System.Collections.Generic;
using F2B.Orchard.Schedule.Models;

namespace F2B.Orchard.Schedule.Providers
{
    public class NullProvider: DateProvider
    {
        private static NullProvider _instance;
        public static NullProvider Instance
        {
            get { return _instance ?? (_instance = new NullProvider()); }
        }

        protected NullProvider() { }

        public override IEnumerable<ScheduleOccurrence> Over(DateTime start, DateTime end, bool reverse = false)
        {
            yield break;
        }
    }
}
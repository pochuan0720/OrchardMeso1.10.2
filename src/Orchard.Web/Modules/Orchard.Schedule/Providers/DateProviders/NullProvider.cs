﻿using System;
using System.Collections.Generic;
using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
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
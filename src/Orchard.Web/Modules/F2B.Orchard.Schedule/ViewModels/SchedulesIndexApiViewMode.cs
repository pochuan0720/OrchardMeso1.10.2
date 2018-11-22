﻿using F2B.Orchard.Schedule.Models;
using Orchard.Projections.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;

namespace F2B.Orchard.Schedule.ViewModels
{
    public class SchedulesIndexApiViewMode
    {
        public QueryModel Query { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IList<SchedulePart> Schedules { get; set; }
        public Pager Pager { get; set; }
    }
}
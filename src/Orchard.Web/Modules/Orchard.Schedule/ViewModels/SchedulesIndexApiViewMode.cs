using Orchard.Schedule.Models;
using Orchard.Projections.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.ViewModels
{
    public class SchedulesIndexApiViewMode
    {
        public int Id { get; set; }
        public QueryModel Query { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IList<SchedulePart> Schedules { get; set; }
        public Pager Pager { get; set; }
    }
}
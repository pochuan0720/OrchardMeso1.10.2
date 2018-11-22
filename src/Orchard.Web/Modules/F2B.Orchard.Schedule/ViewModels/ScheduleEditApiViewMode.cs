using F2B.Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;

namespace F2B.Orchard.Schedule.ViewModels
{
    public class ScheduleEditApiViewMode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Contact { get; set; }

        public object Data { get; set; }

    }
}
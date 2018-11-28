using Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;

namespace Orchard.Schedule.ViewModels
{
    public class ScheduleApiViewMode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int[] Attendee { get; set; }
        public string[] Container { get; set; }
        public object Data { get; set; }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Meso.Volunteer.ViewModels
{
    public class VolunteerSummaryApiViewModel
    {
        public string UserName { get; set; }
        public string Name { get; set; } 
        public List<object> Attendee { get; set; }
        public Dictionary<string, int> PlaceDays { get; set; }
        public int TotalDays { get; set; }
    }
}
using Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;


namespace Meso.Volunteer.ViewModels
{
    public class VolunteerApiViewMode
    {
        public string ContentType { get; set; }
        public string Place { get; set; }
        public bool? AttendState { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
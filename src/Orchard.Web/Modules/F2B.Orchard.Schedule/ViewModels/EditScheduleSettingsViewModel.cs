using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace F2B.Orchard.Schedule.ViewModels
{
    public class EditScheduleSettingsViewModel
    {
        public string StartTime { get; set; }
        public string Duration { get; set; }
        public string TimeZone { get; set; }

        public string DateFormat { get; set; }

        public SelectListItem[] TimeZones { get; set; }
    }
}
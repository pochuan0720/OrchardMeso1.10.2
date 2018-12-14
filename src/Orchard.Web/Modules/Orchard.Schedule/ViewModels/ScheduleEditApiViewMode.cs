using Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Orchard.Schedule.ViewModels
{
    public class ScheduleEditApiViewMode : ScheduleApiViewMode
    {
        [Required]
        public string ContentType { get; set; }
        public string[] Places { get; set; }
        public bool? IsDaily { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
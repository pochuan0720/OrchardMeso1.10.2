using Newtonsoft.Json.Linq;
using Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Orchard.Schedule.ViewModels
{
    public class ScheduleApiViewMode
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? PublishLater { get; set; }
       // public DateTime? ArchiveLater { get; set; }
        public bool? IsPublished { get; set; }
        public object[] Attendee { get; set; }
        [Required]
        public string[] Container { get; set; }
        public JObject Data { get; set; }

    }
}
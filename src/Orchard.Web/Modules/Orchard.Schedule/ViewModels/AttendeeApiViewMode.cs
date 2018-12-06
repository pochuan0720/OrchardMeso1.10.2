using Orchard.Schedule.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Orchard.Schedule.ViewModels
{
    public class AttendeeApiViewMode
    {
        public int Id { get; set; }
        [Required]
        public string ContainerId { get; set; }
        public string Owner { get; set; }
        public object Data { get; set; }

    }
}
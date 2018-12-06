﻿using Orchard.Schedule.Models;
using Orchard.Projections.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Orchard.Schedule.ViewModels
{
    public class SchedulesIndexApiViewMode
    {
        [Required]
        public string ContentType { get; set; }
        public int Id { get; set; }
        //public QueryModel Query { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IList<SchedulePart> Schedules { get; set; }
        public Pager Pager { get; set; }
    }
}
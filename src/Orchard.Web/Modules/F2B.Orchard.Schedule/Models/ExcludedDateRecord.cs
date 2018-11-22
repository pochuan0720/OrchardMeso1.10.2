using Orchard.Environment.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Models
{
    public class ExcludedDateRecord
    {
        public virtual int Id { get; set; }
        public virtual SchedulePartRecord SchedulePartRecord { get; set; }
        public virtual DateTime Date { get; set; }
    }
}
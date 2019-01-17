using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Meso.TyMetro.ViewModels
{
    public class AccessibilityReservationApiViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int ServiceItem { get; set; }
        public string DepStation { get; set; }
        public string ArrStation { get; set; }
        public string Status { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime StartDateTimeStart { get; set; }
        public DateTime StartDateTimeEnd { get; set; }

    }
}
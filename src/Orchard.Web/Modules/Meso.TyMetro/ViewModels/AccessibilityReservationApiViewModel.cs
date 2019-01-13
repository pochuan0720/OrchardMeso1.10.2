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
        [JsonProperty("Name")]
        public string Title { get; set; }

    }
}
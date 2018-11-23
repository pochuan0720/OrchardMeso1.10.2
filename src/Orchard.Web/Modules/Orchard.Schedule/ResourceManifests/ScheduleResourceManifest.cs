using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.ResourceManifests
{
    [OrchardFeature("Orchard.Schedule")]
    public class ScheduleResourceManifest: IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest.DefineStyle("Orchard.Schedule").SetUrl("schedule.css");
            manifest.DefineScript("MomentJS").SetUrl("moment.min.js");
            //manifest.DefineScript("knockout-jQueryUI").SetUrl("knockout-jquery-ui-widget.js").SetDependencies("jQueryUI", "ko");
            
            manifest.DefineScript("jQuery.DateTimePicker").SetUrl("jquery.datetimepicker.min.js", "jquery.datetimepicker.js").SetDependencies("jQuery");
            manifest.DefineStyle("jQuery.DateTimePicker").SetUrl("jquery.datetimepicker.min.css", "jquery.datetimepicker.css");

            manifest.DefineScript("Orchard.Schedule.Bindings").SetUrl("schedule.bindings.min.js", "schedule.bindings.js").SetDependencies("jQuery", "ko", "jQuery.DateTimePicker");
            manifest.DefineScript("Orchard.Schedule.Extensions").SetUrl("schedule.extensions.min.js", "schedule.extensions.js").SetDependencies("ko");
            manifest.DefineScript("Orchard.Schedule").SetUrl("schedule.min.js", "schedule.js")
                .SetDependencies("jQuery", "ko", "MomentJS", "Orchard.Schedule.Bindings", "Orchard.Schedule.Extensions");


            manifest.DefineScript("Spectrum").SetUrl("spectrum.min.js", "spectrum.js").SetDependencies("jQuery");
            manifest.DefineStyle("Spectrum").SetUrl("spectrum.min.css", "spectrum.css");
        }
    }
}
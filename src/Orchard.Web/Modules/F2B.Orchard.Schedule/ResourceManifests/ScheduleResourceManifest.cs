using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.ResourceManifests
{
    [OrchardFeature("F2B.Orchard.Schedule")]
    public class ScheduleResourceManifest: IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest.DefineStyle("F2B.Orchard.Schedule").SetUrl("schedule.css");
            manifest.DefineScript("MomentJS").SetUrl("moment.min.js");
            //manifest.DefineScript("knockout-jQueryUI").SetUrl("knockout-jquery-ui-widget.js").SetDependencies("jQueryUI", "ko");
            
            manifest.DefineScript("jQuery.DateTimePicker").SetUrl("jquery.datetimepicker.min.js", "jquery.datetimepicker.js").SetDependencies("jQuery");
            manifest.DefineStyle("jQuery.DateTimePicker").SetUrl("jquery.datetimepicker.min.css", "jquery.datetimepicker.css");

            manifest.DefineScript("F2B.Orchard.Schedule.Bindings").SetUrl("schedule.bindings.min.js", "schedule.bindings.js").SetDependencies("jQuery", "ko", "jQuery.DateTimePicker");
            manifest.DefineScript("F2B.Orchard.Schedule.Extensions").SetUrl("schedule.extensions.min.js", "schedule.extensions.js").SetDependencies("ko");
            manifest.DefineScript("F2B.Orchard.Schedule").SetUrl("schedule.min.js", "schedule.js")
                .SetDependencies("jQuery", "ko", "MomentJS", "F2B.Orchard.Schedule.Bindings", "F2B.Orchard.Schedule.Extensions");


            manifest.DefineScript("Spectrum").SetUrl("spectrum.min.js", "spectrum.js").SetDependencies("jQuery");
            manifest.DefineStyle("Spectrum").SetUrl("spectrum.min.css", "spectrum.css");
        }
    }
}
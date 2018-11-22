using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.ResourceManifests
{
    [OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class CalendarLayoutResourceManifest: IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest.DefineScript("FullCalendar").SetUrl("fullcalendar.min.js", "fullcalendar.js").SetDependencies("jQuery", "jQueryUI");
            manifest.DefineStyle("FullCalendar").SetUrl("fullcalendar.css");
            manifest.DefineStyle("FullCalendar.Print").SetUrl("fullcalendar.print.css");

            manifest.DefineScript("qTip").SetUrl("jquery.qtip.min.js", "jquery.qtip.js").SetDependencies("jQuery");
            manifest.DefineStyle("qTip").SetUrl("jquery.qtip.min.css", "jquery.qtip.css");

            manifest.DefineStyle("TagColorPicker").SetUrl("TagColorPicker.css");

            manifest.DefineStyle("UpcomingEvents").SetUrl("UpcomingEvents.css");
        }
    }
}
using Orchard.Schedule.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Handlers
{
    [OrchardFeature("Orchard.Schedule")]
    public class ScheduleSettingsHandler : ContentHandler
    {
        public ScheduleSettingsHandler()
        {
            T = NullLocalizer.Instance;
            Filters.Add(new ActivatingFilter<ScheduleSettingsPart>("Site"));
            //Filters.Add(new TemplateFilterForPart<ScheduleSettingsPart>("ScheduleSettings", "Parts.Schedule.SiteSettings", "schedule"));
        }

        public Localizer T { get; set; }

        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Schedule")));
        }
    }
}
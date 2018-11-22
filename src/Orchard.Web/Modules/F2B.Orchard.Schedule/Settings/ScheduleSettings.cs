using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.MetaData.Builders;
using System.Globalization;
using Orchard.Environment.Extensions;

namespace F2B.Orchard.Schedule.Settings
{
    public class ScheduleSettings
    {
        public ScheduleSettings()
        {
            EventBackgroundColor = 0x5555ff;
            EventBorderColor = 0x5555ff;
            EventForegroundColor = 0xffffff;
        }

        public int EventForegroundColor { get; set; }
        public int EventBackgroundColor { get; set; }
        public int EventBorderColor { get; set; }

        public void Build(ContentTypePartDefinitionBuilder builder)
        {
            builder.WithSetting("ScheduleSettings.EventForegroundColor", EventForegroundColor.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ScheduleSettings.EventBackgroundColor", EventBackgroundColor.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("ScheduleSettings.EventBorderColor", EventBorderColor.ToString(CultureInfo.InvariantCulture));
         
        }
    }
}
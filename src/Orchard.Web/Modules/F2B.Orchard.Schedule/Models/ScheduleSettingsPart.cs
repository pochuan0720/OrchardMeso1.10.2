using Orchard.ContentManagement;
using System;

namespace F2B.Orchard.Schedule.Models
{
    public class ScheduleSettingsPart : ContentPart
    {
        public TimeSpan DefaultStartTime
        {
            get
            {
                var current = Retrieve<string>("DefaultStartTime") ?? "480";
                return TimeSpan.FromMinutes(Convert.ToInt32(current));
            }
            set { Store("DefaultStartTime", ((int)value.TotalMinutes).ToString()); }
        }

        public TimeSpan DefaultDuration
        {
            get
            {
                var current = Retrieve<string>("DefaultDuration") ?? "60";
                return TimeSpan.FromMinutes(Convert.ToInt32(current));
            }
            set { Store("DefaultDuration", ((int)value.TotalMinutes).ToString()); }
        }

        public TimeZoneInfo TimeZone
        {
            get
            {
                var current = Retrieve<string>("TimeZone") ?? TimeZoneInfo.Utc.Id;
                return TimeZoneInfo.FindSystemTimeZoneById(current);
            }
            set
            {
                Store("TimeZone", (value.Id));
            }
        }

        public string DateFormat
        {
            get { return Retrieve<string>("DateFormat") ?? "MDY"; }
            set { Store("DateFormat", value); }
        }
    }
}
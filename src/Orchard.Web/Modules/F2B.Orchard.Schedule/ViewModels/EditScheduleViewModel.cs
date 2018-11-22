using F2B.Orchard.Schedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace F2B.Orchard.Schedule.ViewModels
{
    public class EditScheduleViewModel
    {
        private static Dictionary<ScheduleRepeatType, string> _repeatOptions = new Dictionary<ScheduleRepeatType, string>
        {
            { ScheduleRepeatType.Daily, "Daily" },
            { ScheduleRepeatType.Weekly, "Weekly" },
            { ScheduleRepeatType.MonthlyByDay, "Monthly (by day of the month)" },
            { ScheduleRepeatType.MonthlyByWeek, "Monthly (by day of the week)" },
            { ScheduleRepeatType.Yearly, "Yearly" }
        };

        public int Duration { get; set; }

        public string StartDate { get; set; }
        public string EndDate { get; set; }

        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public bool AllDay { get; set; }
        public bool Repeat { get; set; }

        public bool LastDayOrWeek { get; set; }
        public bool FromEndOfMonth { get; set; }

        //public string RepeatType { get; set; }

        public static SelectList RepeatOptions
        {
            get
            {
                return new SelectList(_repeatOptions, "Key", "Value");
            }
        }

        public static SelectListItem[] TimeZones
        {
            get { return TimeZoneInfo.GetSystemTimeZones().Select(tz => new SelectListItem { Text = tz.DisplayName, Value = tz.Id }).ToArray(); }
        }

        public string TimeZone { get; set; }

        public ScheduleRepeatType RepeatType { get; set; }
        public short RepeatInterval { get; set; }

        public short RepeatDays { get; set; }

        public short RepeatMonths { get; set; }
        public short RepeatWeeks { get; set; }

        public short RepeatDayOfMonth { get; set; }

        public string EndValue { get; set; }
        public string TerminalDate { get; set; }
        public int Occurrences { get; set; }

        public string ExcludedDates { get; set; }

        public int Offset { get; set; }

        public string DateFormat { get; set; }
    }
}
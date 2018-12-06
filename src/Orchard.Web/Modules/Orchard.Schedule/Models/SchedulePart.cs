using Newtonsoft.Json;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Orchard.Schedule.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SchedulePart : ContentPart<SchedulePartRecord>
    {
        [JsonProperty]
        public string Title
        {
            get { return this.As<TitlePart>().Title; }
            set { this.As<TitlePart>().Title = value; }
        }

        /*[JsonProperty("Contact")]
        public IUser User
        {
            get { return this.As<CommonPart>().Owner; }
        }*/

        [Required]
        [JsonProperty]
        public DateTime StartDate
        {
            get { return Record.StartDate; }
            set { Record.StartDate = value; }
        }

        [JsonProperty]
        public DateTime? EndDate
        {
            get { return Record.EndDate; }
            set { Record.EndDate = value; }
        }

        [JsonProperty]
        public bool IsPublished
        {
            get { return ContentItem.VersionRecord != null && ContentItem.VersionRecord.Published; }
        }

        public int? Occurrences
        {
            get { return Record.Occurrences; }
            set { Record.Occurrences = value; }
        }

        public ScheduleRepeatType ScheduleType
        {
            get { return (ScheduleRepeatType)Record.ScheduleType; }
            set { Record.ScheduleType = (short)value; }
        }

        public short RepeatInterval
        {
            get { return Record.RepeatInterval; }
            set { Record.RepeatInterval = value; }
        }

        public ScheduleDayOfWeek DaysOfWeek
        {
            get { return (ScheduleDayOfWeek)Record.DaysOfWeek; }
            set { Record.DaysOfWeek = (short)value; }
        }

        public short DayOfMonth
        {
            get { return Record.DayOfMonth; }
            set { Record.DayOfMonth = value; }
        }

        public ScheduleWeekOfMonth WeekOfMonth
        {
            get { return (ScheduleWeekOfMonth)Record.WeekOfMonth; }
            set { Record.WeekOfMonth = (short)value; }
        }

        public ScheduleMonth Month
        {
            get { return (ScheduleMonth)Record.Month; }
            set { Record.Month = (short)value; }
        }

        public bool AllDay
        {
            get { return Record.AllDay; }
            set { Record.AllDay = value; }
        }

        public TimeSpan StartTime
        {
            get { return TimeSpan.FromMinutes(Record.StartTime); }
            set { Record.StartTime = (int)value.TotalMinutes; }
        }

        // NEED TO LOOK AT THIS...
        public TimeSpan Duration
        {
            get { return TimeSpan.FromMinutes(Record.Duration); }
            set { Record.Duration = (int)value.TotalMinutes; }
        }

        public IEnumerable<DateTime> ExcludedDates
        {
            get { return Record.ExcludedDates.Select(ex => ex.Date); }
        }

        public int Offset
        {
            get { return Record.OffsetDays; }
            set { Record.OffsetDays = value; }
        }

        public TimeZoneInfo TimeZone
        {
            get { return TimeZoneInfo.FindSystemTimeZoneById(Record.TimeZone ?? "UTC"); }
            set { Record.TimeZone = value.Id; }
        }

    }
}
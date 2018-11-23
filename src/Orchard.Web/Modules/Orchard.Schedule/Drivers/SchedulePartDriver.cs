using Orchard.Schedule.Models;
using Orchard.Schedule.Services;
using Orchard.Schedule.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Orchard;

namespace Orchard.Schedule.Drivers
{
    public class SchedulePartDriver : ContentPartDriver<SchedulePart>
    {
        private readonly IScheduleService _scheduleService;
        private readonly IWorkContextAccessor _accessor;
        private const string TemplateName = "Parts/Schedule";

        public SchedulePartDriver(IScheduleService scheduleService, IWorkContextAccessor accessor) {
            _scheduleService = scheduleService;
            _accessor = accessor;
        }

        protected override DriverResult Display(SchedulePart part, string displayType, dynamic shapeHelper)
        {
            return Combined(
                ContentShape("Parts_Schedule", () => shapeHelper.Parts_Schedule(Schedule: part, Recurrence: _scheduleService.ScheduleDescription(part, ParseFormat), DisplayFormat: ParseFormat)),
                ContentShape("Parts_Schedule_Summary", () => shapeHelper.Parts_Schedule_Summary(Schedule: part, Recurrence: _scheduleService.ScheduleDescription(part, ParseFormat), DisplayFormat: ParseFormat)),
                ContentShape("Parts_Schedule_SummaryAdmin", () => shapeHelper.Parts_Schedule_SummaryAdmin(Schedule: part, Recurrence: _scheduleService.ScheduleDescription(part, ParseFormat), DisplayFormat: ParseFormat))
            );
        }

        protected override DriverResult Editor(SchedulePart part, dynamic shapeHelper)
        {
            //if (part.ContentItem.Id == 0)
            //{
            //    // set default start time and duration
            //    var settings = _accessor.GetContext().CurrentSite.As<ScheduleSettingsPart>();
            //    part.StartTime = settings.DefaultStartTime;
            //    part.Duration = settings.DefaultDuration;
            //}

            return ContentShape("Parts_Schedule_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: TemplateName,
                    Model: BuildEditorViewModel(part),
                    Prefix: Prefix));
        }

        protected override DriverResult Editor(SchedulePart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var model = new EditScheduleViewModel();
            updater.TryUpdateModel(model, Prefix, null, null);

            part.AllDay = model.AllDay;

            var startDate = DateTime.ParseExact(model.StartDate, ParseFormat, CultureInfo.InvariantCulture);
            var startTime = TimeSpan.Parse(model.StartTime);

            part.StartDate = startDate;
            part.StartTime = startTime;

            var endDate = DateTime.ParseExact(model.EndDate, ParseFormat, CultureInfo.InvariantCulture);
            var endTime = TimeSpan.Parse(model.EndTime);

            var duration = endDate - startDate;
            if (!model.AllDay)
            {
                duration += endTime - startTime;
            }
            else
            {
                duration += TimeSpan.FromDays(1);
            }

            part.Duration = duration;

            part.RepeatInterval = model.RepeatInterval;
            part.ScheduleType = (model.Repeat) ? model.RepeatType : ScheduleRepeatType.Single;

            if (model.FromEndOfMonth && model.Repeat)
            {
                if (model.RepeatType == ScheduleRepeatType.MonthlyByDay) part.ScheduleType = ScheduleRepeatType.MonthlyByDayFromEnd;
                if (model.RepeatType == ScheduleRepeatType.MonthlyByWeek) part.ScheduleType = ScheduleRepeatType.MonthlyByWeekFromEnd;
            }

            part.DayOfMonth = (short)((model.LastDayOrWeek) ? -1 : startDate.Day);
            part.WeekOfMonth = (model.LastDayOrWeek) ? ScheduleWeekOfMonth.Last : (ScheduleWeekOfMonth)(1 << ((startDate.Day - 1) / 7));

            part.DaysOfWeek = (ScheduleDayOfWeek)model.RepeatDays;
            part.Month = (ScheduleMonth)model.RepeatMonths;

            part.Occurrences = null;
            part.EndDate = null;

            part.Offset = model.Offset;

            switch (model.EndValue)
            {
                case "times": if (model.Occurrences > 0) part.Occurrences = model.Occurrences; break;
                case "date": part.EndDate = DateTime.ParseExact(model.TerminalDate, ParseFormat, CultureInfo.InvariantCulture); break;
            }

            _scheduleService.UpdateExcludedDatesForContentItem(part.ContentItem, (model.ExcludedDates != null) 
                ? model.ExcludedDates.Split(',').Select(d => DateTime.ParseExact(d, ParseFormat, CultureInfo.InvariantCulture)) 
                : null);

            return ContentShape("Parts_Schedule_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: TemplateName,
                    Model: model,
                    Prefix: Prefix));
        }

        private string _dateFormat;
        private string DateFormat {
            get { return _dateFormat ?? (_dateFormat = _accessor.GetContext().CurrentSite.As<ScheduleSettingsPart>().DateFormat); }            
        }

        private string ParseFormat
        {
            get
            {
                switch (DateFormat) {
                case "DMY":
                    return "dd/MM/yyyy";
                case "MDY":
                    return "MM/dd/yyyy";
                case"YMD":
                    return "yyyy/MM/dd";
                default:
                    return "MM/dd/yyyy";
                }
            }
        }

        private EditScheduleViewModel BuildEditorViewModel(SchedulePart part)
        {
            var durationDays = part.Duration.Days;
            if (part.AllDay) durationDays--;

            var startHours = part.StartTime.Hours;
            var startMinutes = part.StartTime.Minutes;

            var endHours = part.Duration.Hours + startHours;
            var endMinutes = part.Duration.Minutes + startMinutes;

            var endDate = part.StartDate.AddDays(durationDays);

            var duration = (int)part.Duration.TotalMinutes;
            if (part.AllDay) duration -= 1440;

            var repeatType = part.ScheduleType;
            var fromEndOfMonth = (repeatType == ScheduleRepeatType.MonthlyByDayFromEnd || repeatType == ScheduleRepeatType.MonthlyByWeekFromEnd);
            if (repeatType == ScheduleRepeatType.MonthlyByWeekFromEnd) repeatType = ScheduleRepeatType.MonthlyByWeek;
            if (repeatType == ScheduleRepeatType.MonthlyByDayFromEnd) repeatType = ScheduleRepeatType.MonthlyByDay;
            
            return new EditScheduleViewModel
            {
                StartDate = part.StartDate.ToString(ParseFormat),
                StartTime = string.Format("{0:d02}:{1:d02}", startHours, startMinutes),
                EndTime = string.Format("{0:d02}:{1:d02}", endHours, endMinutes),
                EndDate = endDate.ToShortDateString(),
                Duration = duration,
                AllDay = part.AllDay,
                Repeat = repeatType != ScheduleRepeatType.Single,
                RepeatType = repeatType,
                RepeatInterval = part.RepeatInterval,
                RepeatDays = (short)part.DaysOfWeek,
                RepeatMonths = (short)part.Month,
                RepeatWeeks = (short)part.WeekOfMonth,
                LastDayOrWeek = (
                    part.ScheduleType == ScheduleRepeatType.MonthlyByDay ? part.DayOfMonth == -1 :
                    part.ScheduleType == ScheduleRepeatType.MonthlyByWeek && part.WeekOfMonth == ScheduleWeekOfMonth.Last),

                EndValue = (part.EndDate.HasValue) ? "date" : (part.Occurrences.HasValue) ? "times" : "never",
                TerminalDate = part.EndDate.HasValue ? part.EndDate.Value.ToString(ParseFormat) : "",
                Occurrences = part.Occurrences ?? -1,

                TimeZone = part.TimeZone.Id,

                ExcludedDates = string.Join(",", part.ExcludedDates.Select(d => d.ToString(ParseFormat))),

                FromEndOfMonth = fromEndOfMonth,
                Offset = part.Offset,

                DateFormat = DateFormat
            };
        }
    }
}

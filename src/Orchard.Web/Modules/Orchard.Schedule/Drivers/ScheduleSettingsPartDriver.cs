using Orchard.Schedule.Models;
using Orchard.Schedule.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace Orchard.Schedule.Drivers
{
    public class ScheduleSettingsPartDriver : ContentPartDriver<ScheduleSettingsPart>
    {
        private const string TemplateName = "Parts/Schedule.SiteSettings";

        public ScheduleSettingsPartDriver()
        {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        protected override string Prefix { get { return "ScheduleSettings"; } }

        protected override DriverResult Editor(ScheduleSettingsPart part, dynamic shapeHelper)
        {
            return Editor(part, null, shapeHelper);
        }

        protected override DriverResult Editor(ScheduleSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            return ContentShape("Parts_Schedule_SiteSettings", () =>
            {
                var model = BuildViewModel(part);

                if (updater != null)
                {
                    updater.TryUpdateModel(model, Prefix, null, null);

                    DateTime start;
                    TimeSpan duration;
                    var culture = new CultureInfo("en-US");

                    if (!DateTime.TryParseExact(model.StartTime, new[] {"h:mm tt", "hh:mm tt"}, culture, DateTimeStyles.None, out start))
                    {
                        updater.AddModelError("StartTime", T("The start time is not in the correct format"));
                    }
                    else
                    {
                        part.DefaultStartTime = start.TimeOfDay;
                    }

                    if (!TimeSpan.TryParseExact(model.Duration, new[] {@"h\:mm", @"hh\:mm"}, culture, TimeSpanStyles.None, out duration))
                    {
                        updater.AddModelError("Duration", T("The duration is not in the correct format"));
                    }
                    else
                    {
                        part.DefaultDuration = duration;
                    }

                    part.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.TimeZone);
                    part.DateFormat = model.DateFormat;
                }
                return shapeHelper.EditorTemplate(TemplateName: TemplateName, Model: model, Prefix: Prefix);
            }).OnGroup("Schedule");
        }

        private static EditScheduleSettingsViewModel BuildViewModel(ScheduleSettingsPart part)
        {
            var viewModel = new EditScheduleSettingsViewModel();

            var startHours = part.DefaultStartTime.Hours;
            var startMinutes = part.DefaultStartTime.Minutes;
            var isPm = startHours > 12;
            startHours = (isPm ? startHours - 12 : startHours);
            startHours = (startHours == 0 ? 12 : startHours);

            viewModel.StartTime = string.Format("{0:d02}:{1:d02} {2}M", startHours, startMinutes, isPm ? "P" : "A");
            viewModel.Duration = part.DefaultDuration.ToString(@"hh\:mm");
            viewModel.TimeZone = part.TimeZone.Id;

            viewModel.TimeZones = TimeZoneInfo.GetSystemTimeZones().Select(tz => new SelectListItem {Text = tz.DisplayName, Value = tz.Id}).ToArray();
            viewModel.DateFormat = part.DateFormat;

            return viewModel;
        }
    }
}
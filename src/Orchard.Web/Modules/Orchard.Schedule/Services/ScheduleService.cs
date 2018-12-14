using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Contents;
using Orchard.Data;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Mvc.Html;
using System.Text;
using Orchard.Schedule.Providers;
using Orchard.Schedule.Providers.ProviderFactories;
using Orchard.Environment.Extensions;

namespace Orchard.Schedule.Services
{
    [OrchardFeature("Orchard.Schedule")]
    public class ScheduleService : IScheduleService
    {
        private readonly IRepository<ExcludedDateRecord> _excludedDateRepository;
        private readonly IOrchardServices _services;

        public Localizer T { get; set; }

        public ScheduleService(IRepository<ExcludedDateRecord> excludedDateRepository, IOrchardServices services)
        {
            _excludedDateRepository = excludedDateRepository;
            _services = services;

            T = NullLocalizer.Instance;
        }

        public IEnumerable<ScheduleOccurrence> GetOccurrencesForDateRange(SchedulePart schedule, DateTime start, DateTime end)
        {
            var builder = new TemplateProviderFactory(schedule);

            return builder.Build().Over(start, end);
        }

        public IEnumerable<ScheduleOccurrence> GetOccurrencesFromDate(IEnumerable<SchedulePart> schedules, DateTime start, int count) {
            var providers = schedules.Select(s => new TemplateProviderFactory(s).Build()).ToList();
            var merged = new UnionProvider(providers);
            return merged.Over(start, DateTime.MaxValue).Take(count);
        }

        public ScheduleOccurrence GetNextOccurrence(SchedulePart schedule, DateTime start)
        {
            return GetOccurrencesForDateRange(schedule, start, DateTime.MaxValue).FirstOrDefault();
        }

        public void UpdateExcludedDatesForContentItem(ContentItem item, IEnumerable<DateTime> dates)
        {
            var record = item.As<SchedulePart>().Record;
            Dictionary<DateTime, bool> lookupNew;

            var oldDates = _excludedDateRepository.Fetch(r => r.SchedulePartRecord == record);
            if (dates != null)
            {
                lookupNew = dates.ToDictionary(d => d, d => false);
            }
            else
            {
                lookupNew = new Dictionary<DateTime, bool>();
            }

            foreach (var dateRecord in oldDates)
            {
                if (lookupNew.ContainsKey(dateRecord.Date))
                {
                    lookupNew[dateRecord.Date] = true;
                }
                else
                {
                    _excludedDateRepository.Delete(dateRecord);
                }
            }

            foreach (var newDate in lookupNew.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
            {
                var newDateRecord = new ExcludedDateRecord { Date = newDate, SchedulePartRecord = record };
                _excludedDateRepository.Create(newDateRecord);
            }
        }

        private void AddExcludedDateToSchedulePart(SchedulePart part, DateTime date)
        {
            var record = part.Record;

            var oldDates = _excludedDateRepository.Fetch(r => r.SchedulePartRecord == record);
            if (!oldDates.Select(od => od.Date).Contains(date))
            {
                var newDateRecord = new ExcludedDateRecord { Date = date, SchedulePartRecord = record };
                _excludedDateRepository.Create(newDateRecord);
            }
        }

        public void RemoveScheduleItem(int id)
        {
            var item = _services.ContentManager.Get(id, VersionOptions.Latest);
            if (item.Has<SchedulePart>() && _services.Authorizer.Authorize(Core.Contents.Permissions.DeleteContent, item))
            {
                _services.ContentManager.Remove(item);
            }
        }

        public void RemoveFollowingDatesForScheduleItem(int id, DateTime date)
        {
            var item = _services.ContentManager.Get(id);
            if (item.Has<SchedulePart>())
            {
                if (_services.Authorizer.Authorize(Core.Contents.Permissions.DeleteContent, item))
                {
                    var schedule = item.As<SchedulePart>();
                    if (!(schedule.EndDate.HasValue) || (schedule.EndDate.HasValue && schedule.EndDate.Value > date))
                    {
                        schedule.EndDate = date.Date.AddDays(-1);
                    }
                }
            }

        }

        public void RemoveSingleDateForScheduleItem(int id, DateTime date)
        {
            var item = _services.ContentManager.Get(id);
            if (item.Has<SchedulePart>())
            {
                if (_services.Authorizer.Authorize(Core.Contents.Permissions.DeleteContent, item))
                {
                    var schedule = item.As<SchedulePart>();
                    AddExcludedDateToSchedulePart(schedule, date);
                }
            }
        }

        private string MakeOrdinal(int number)
        {
            if (number == -1) return "last";
            else if (number < 1) return "unknown";
            else if (number % 10 == 1) return string.Format("{0}st", number);
            else if (number % 10 == 2) return string.Format("{0}nd", number);
            else if (number % 10 == 3) return string.Format("{0}rd", number);
            else return string.Format("{0}th", number);
        }

        public string ScheduleDescription(SchedulePart schedule, string DateFormat)
        {
            var eventStartDate = schedule.StartDate;
            if (!schedule.AllDay)
            {
                eventStartDate += schedule.StartTime;
            }

            var eventEndDate = eventStartDate + schedule.Duration;
            if (schedule.AllDay)
            {
                eventEndDate -= TimeSpan.FromSeconds(1);
            }

            LocalizedString recurrencePeriod = T("unknown");
            LocalizedString recurrenceAdditional = null;
            LocalizedString recurrenceDuration = null;
            LocalizedString recurrenceExclusions = null;

            switch (schedule.ScheduleType)
            {
                case ScheduleRepeatType.Single: recurrencePeriod = T("Single Day"); break;
                case ScheduleRepeatType.Daily:
                    recurrencePeriod = T.Plural("Daily", "Every {0} days", schedule.RepeatInterval);
                    break;
                case ScheduleRepeatType.Weekly:
                    recurrencePeriod = T.Plural("Weekly", "Every {0} weeks", schedule.RepeatInterval);
                    recurrenceAdditional = T(", on " + schedule.DaysOfWeek.ToString());
                    break;
                case ScheduleRepeatType.Yearly:
                    recurrencePeriod = T.Plural("Annually", "Every {0} years", schedule.RepeatInterval);
                    recurrenceAdditional = T(", on " + schedule.StartDate.ToString("MMMM") + ", " + schedule.DayOfMonth.ToString());
                    break;
                case ScheduleRepeatType.MonthlyByDay:
                    recurrencePeriod = T.Plural("Monthly", "Every {0} months", schedule.RepeatInterval);
                    recurrenceAdditional = T(", on the " + MakeOrdinal(schedule.DayOfMonth) + " day");
                    break;
                case ScheduleRepeatType.MonthlyByWeek:
                    recurrencePeriod = T.Plural("Monthly", "Every {0} months", schedule.RepeatInterval);
                    recurrenceAdditional = T(", on the " + schedule.WeekOfMonth.ToString() + " " + schedule.DaysOfWeek.ToString());
                    break;
            }

            if (schedule.Occurrences.HasValue)
            {
                recurrenceDuration = T(string.Format(", for {0} times", schedule.Occurrences.Value));
            }
            else if (schedule.EndDate.HasValue)
            {
                recurrenceDuration = T(string.Format(", until {0}", schedule.EndDate.Value.ToString(DateFormat)));
            }

            if (schedule.ExcludedDates.Any())
            {
                recurrenceExclusions = T(", except " + string.Join(", ", schedule.ExcludedDates.Select(ex => ex.ToString(DateFormat))));
            }

            StringBuilder builder = new StringBuilder();
            builder.Append(recurrencePeriod.Text);
            builder.Append(recurrenceAdditional != null ? recurrenceAdditional.Text : "");
            builder.Append(recurrenceDuration != null ? recurrenceDuration.Text : "");
            builder.Append(recurrenceExclusions != null ? recurrenceExclusions.Text : "");

            return builder.ToString();
        }
    }
}
using Orchard.Schedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Providers.ProviderFactories
{
    public class TemplateProviderFactory: IDateProviderFactory
    {
        private readonly SchedulePart _part;

        public TemplateProviderFactory(SchedulePart part)
        {
            _part = part;
        }

        public DateProvider Build()
        {
            DateProvider provider = NullProvider.Instance;

            switch (_part.ScheduleType)
            {
                case ScheduleRepeatType.Single:
                    provider = new SingleDateProvider(_part);
                    break;
                case ScheduleRepeatType.Daily:
                    provider = new DailyProvider(_part);
                    break;
                case ScheduleRepeatType.Weekly:
                    UnionProvider up = new UnionProvider();
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Sunday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Sunday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Monday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Monday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Tuesday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Tuesday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Wednesday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Wednesday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Thursday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Thursday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Friday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Friday, _part));
                    if ((_part.DaysOfWeek & ScheduleDayOfWeek.Saturday) != 0) up.Add(new WeeklyProvider(DayOfWeek.Saturday, _part));

                    provider = up.Simplified();
                    break;
                case ScheduleRepeatType.MonthlyByDay:
                case ScheduleRepeatType.MonthlyByDayFromEnd:
                    provider = new DayOfMonthProvider(_part);
                    break;
                case ScheduleRepeatType.MonthlyByWeek:
                case ScheduleRepeatType.MonthlyByWeekFromEnd:
                    UnionProvider up2 = new UnionProvider();
                    foreach (var week in GetWeeksOfMonth(_part.WeekOfMonth)) {
                        foreach (var dow in GetDaysOfWeek(_part.DaysOfWeek)) {
                            up2.Add(new DayOfWeekMonthProvider(dow, week, _part));
                        }
                    }

                    provider = up2.Simplified();
                    break;
                case ScheduleRepeatType.Yearly:
                    provider = new YearlyProvider(_part);
                    break;
            }

            if (_part.Occurrences.HasValue)
            {
                provider = new OccurrenceProvider(provider, _part.StartDate, _part.Occurrences.Value);
            }
            else
            {
                provider = new DateRangeFilterProvider(provider, _part.StartDate, _part.EndDate);
            }

            if (_part.ExcludedDates.Any())
            {
                var excludedUnion = new UnionProvider();
                foreach (var excluded in _part.ExcludedDates.Select(ex => new SingleDateProvider(ex.Date)))
                {
                    excludedUnion.Add(excluded);
                }
                
                provider = new DifferenceProvider(provider, excludedUnion.Simplified());
            }

            if (_part.Offset != 0)
            {
                provider = new DateOffsetProvider(provider, _part.Offset);
            }

            return provider;
        }

        private static IEnumerable<DayOfWeek> GetDaysOfWeek(ScheduleDayOfWeek sdow)
        {
            if ((sdow & ScheduleDayOfWeek.Sunday) != 0) yield return DayOfWeek.Sunday;
            if ((sdow & ScheduleDayOfWeek.Monday) != 0) yield return DayOfWeek.Monday;
            if ((sdow & ScheduleDayOfWeek.Tuesday) != 0) yield return DayOfWeek.Tuesday;
            if ((sdow & ScheduleDayOfWeek.Wednesday) != 0) yield return DayOfWeek.Wednesday;
            if ((sdow & ScheduleDayOfWeek.Thursday) != 0) yield return DayOfWeek.Thursday;
            if ((sdow & ScheduleDayOfWeek.Friday) != 0) yield return DayOfWeek.Friday;
            if ((sdow & ScheduleDayOfWeek.Saturday) != 0) yield return DayOfWeek.Saturday;
        }

        private static IEnumerable<short> GetWeeksOfMonth(ScheduleWeekOfMonth swom)
        {
            if ((swom & ScheduleWeekOfMonth.First) != 0) yield return 1;
            if ((swom & ScheduleWeekOfMonth.Second) != 0) yield return 2;
            if ((swom & ScheduleWeekOfMonth.Third) != 0) yield return 3;
            if ((swom & ScheduleWeekOfMonth.Fourth) != 0) yield return 4;
            if ((swom & ScheduleWeekOfMonth.Fifth) != 0) yield return 5;
            if ((swom & ScheduleWeekOfMonth.Last) != 0) yield return -1;
        }
    }
}
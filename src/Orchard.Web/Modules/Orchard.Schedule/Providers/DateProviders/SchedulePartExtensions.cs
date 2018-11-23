using Orchard.Schedule.Models;

namespace Orchard.Schedule.Providers
{
    public static class SchedulePartExtensions
    {
        public static int DaysIncluded(this SchedulePart part)
        {
            if (part.AllDay)
            {
                return part.Duration.Days;
            }
            else {
                var end = part.StartDate + part.StartTime + part.Duration;
                return end.Date == part.StartDate ? 1 : (end.Date - part.StartDate).Days;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Models
{
    public enum ScheduleRepeatType
    {
        Single,
        Daily,
        Weekly,
        MonthlyByDay,
        MonthlyByWeek,
        Yearly,

        MonthlyByDayFromEnd,
        MonthlyByWeekFromEnd,
    }

    [Flags]
    public enum ScheduleDayOfWeek
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64
    }

    [Flags]
    public enum ScheduleWeekOfMonth
    {
        First = 1,
        Second = 2,
        Third = 4,
        Fourth = 8,
        Fifth = 16,
        Last = 32
    }

    [Flags]
    public enum ScheduleMonth
    {
        January = 0x001,
        February = 0x002,
        March = 0x004,
        April = 0x008,
        May = 0x010,
        June = 0x020,
        July = 0x040,
        August = 0x080,
        September = 0x100,
        October = 0x200,
        November = 0x400,
        December = 0x800
    }
}
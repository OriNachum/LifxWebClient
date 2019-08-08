using System;

namespace ProvidersInterface
{
    public class ActionSchedule
    {
        public DayOfWeek Day { get; set; }
        public DateTime Time { get; set; }
        public string ActionName { get; set; }
    }
}
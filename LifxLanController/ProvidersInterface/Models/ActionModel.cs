using System;
using System.Collections.Generic;
using System.Text;

namespace ProvidersInterface.Models
{
    public class ActionModel
    {
        public string Name { get; set; }
        public string FullUrl { get; set; }
        public DateTime Time { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public bool Active { get; set; }
    }
}

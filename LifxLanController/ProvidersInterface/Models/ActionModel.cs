using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ProvidersInterface.Models
{
    public class ActionModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullUrl { get; set; }
        public DateTime Time { get; set; }
        public DateTime? Date { get; set; }
        public IEnumerable<DayOfWeek> DaysOfWeek { get; set; }
        public bool Active { get; set; }
        public bool Repeating { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

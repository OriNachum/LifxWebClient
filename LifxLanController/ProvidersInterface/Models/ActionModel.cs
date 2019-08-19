using Newtonsoft.Json;
using System;

namespace ProvidersInterface.Models
{
    public class ActionModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullUrl { get; set; }
        public DateTime Time { get; set; }
        public DayOfWeek? DayOfWeek { get; set; }
        public bool Active { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

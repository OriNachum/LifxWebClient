using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ActionService.Models
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ActionSchedule
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        //[JsonProperty("Day")]
        //public DayOfWeek? Day { get; set; }

        [JsonProperty("DaysOfWeek")]
        public IEnumerable<DayOfWeek> DaysOfWeek { get; set; }

        [JsonProperty("Time")]
        public DateTime Time { get; set; }
        [JsonProperty("ActionName")]
        public string ActionName { get; set; }
        [JsonProperty("Date")]

        public DateTime? Date { get; internal set; }
        [JsonProperty("Repeating")]

        public bool Repeating { get; internal set; }
        [JsonProperty("Active")]
        public bool Active { get; internal set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
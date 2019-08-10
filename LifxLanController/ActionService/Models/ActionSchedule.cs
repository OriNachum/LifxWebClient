using Newtonsoft.Json;
using System;

namespace ActionService.Models
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ActionSchedule
    {
        [JsonProperty("Day")]
        public DayOfWeek? Day { get; set; }
        [JsonProperty("Time")]
        public DateTime Time { get; set; }
        [JsonProperty("ActionName")]
        public string ActionName { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
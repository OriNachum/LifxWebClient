﻿using Newtonsoft.Json;
using System;

namespace ActionService.Models
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ActionSchedule
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Day")]
        public DayOfWeek? Day { get; set; }
        [JsonProperty("Time")]
        public DateTime Time { get; set; }
        [JsonProperty("ActionName")]
        public string ActionName { get; set; }
        public DateTime? Date { get; internal set; }
        public bool Repeating { get; internal set; }
        public bool Active { get; internal set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
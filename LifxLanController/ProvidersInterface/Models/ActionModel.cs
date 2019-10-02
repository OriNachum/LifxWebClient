using Infrared.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace ProvidersInterface.Models
{
    [JsonObject]
    public class ActionModel
    {
        [JsonProperty("Id")]
        public int Id { get; set; }
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Service")]
        public eService Service { get; set; }
        [JsonProperty("ActionId")]
        public string ActionId { get; set; }
        [JsonProperty("Parameters")]
        public IDictionary<string, string> Parameters { get; set; }
        [JsonProperty("Time")]
        [JsonConverter(typeof(TimeOnlyDateTimeConverter))]
        public DateTime Time { get; set; }
        [JsonConverter(typeof(DateOnlyDateTimeConverter))]
        [JsonProperty("Date")]
        public DateTime? Date { get; set; }
        [JsonProperty("DaysOfWeek")]
        public IEnumerable<DayOfWeek> DaysOfWeek { get; set; }
        [JsonProperty("Active")]
        public bool Active { get; set; }
        [JsonProperty("Repeating")]
        public bool Repeating { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class TimeOnlyDateTimeConverter : IsoDateTimeConverter
    {
        public TimeOnlyDateTimeConverter()
        {
            base.DateTimeFormat = "HH':'mm':'ss.FFFFFFFK";
        }
    }

    public class DateOnlyDateTimeConverter : IsoDateTimeConverter
    {
        public DateOnlyDateTimeConverter()
        {
            base.DateTimeFormat = "yyyy-MM-dd";
        }
    }
}

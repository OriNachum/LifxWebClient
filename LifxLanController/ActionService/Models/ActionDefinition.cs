using Infrared.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ActionService.Models
{
    [JsonObject]
    public class ActionDefinition
    {
        [JsonProperty("Url")]
        public string Url { get; set; }

        public eService Service { get; set; }

        public string ActionId { get; set; }

        [JsonProperty("Parameters")]
        public IDictionary<string, string> Parameters { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

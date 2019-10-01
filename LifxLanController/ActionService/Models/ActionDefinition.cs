using Infrared.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ActionService.Models
{
    [JsonObject]
    public class ActionDefinition
    {
        //[JsonProperty("Url")]
        //public string Url { get; set; }

        [JsonProperty("Service")]
        public eService Service { get; set; }

        [JsonProperty("ActionId")]
        public string ActionId { get; set; }

        //[JsonProperty("Params")]
        //public string Params { get; set; }

        [JsonProperty("Parameters")]
        public IDictionary<string, string> Parameters { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

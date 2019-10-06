using Infrared.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace ProvidersInterface.Models
{
    [JsonObject]
    public class ActionDefinitionModel
    {
        [JsonProperty("Name")]
        public string Name { get; set; }
        [JsonProperty("Service")]
        public string Service { get; set; }
        [JsonProperty("ActionId")]
        public string ActionId { get; set; }
        [JsonProperty("Parameters")]
        public IDictionary<string, string> Parameters { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

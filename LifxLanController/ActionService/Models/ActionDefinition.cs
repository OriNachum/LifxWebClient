using Newtonsoft.Json;
using System;

namespace ActionService.Models
{
    [JsonObject]
    public class ActionDefinition
    {
        [JsonProperty("Url")]
        public string Url { get; set; }
        [JsonProperty("Params")]
        public string Params { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        

        //public override string ToString()
        //{
        //    return $"Url: {Url.ToString()}, Params: {Params}";
        //}
    }
}

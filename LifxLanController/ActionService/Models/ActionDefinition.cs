using System;
using System.Collections.Generic;
using System.Text;

namespace ProvidersInterface
{
    public class ActionDefinition
    {
        public Enum Url { get; set; }
        public string Params { get; set; }

        public override string ToString()
        {
            return $"Url: {Url.ToString()}, Params: {Params}";
        }
    }
}

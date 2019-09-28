using Infrared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared
{
    public interface IServiceUrlProvider
    {
        string GetUrl(eService service, string actionId, IDictionary<string, string> parameters = null);
    }
}

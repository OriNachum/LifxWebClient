using Infrared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared
{
    public interface IServiceUrlProvider
    {
        string GetUrl(eService service, Enum actionId);
    }
}

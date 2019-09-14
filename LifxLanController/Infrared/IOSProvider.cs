using Infrared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared
{
    public interface IOSProvider
    {
        eOSPlatform GetOSPlatform();

        bool IsOSPlatform(eOSPlatform osPlatform);
    }
}

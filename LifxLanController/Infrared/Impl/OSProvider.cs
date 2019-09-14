using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Infrared.Enums;

namespace Infrared.Impl
{
    public class OSProvider : IOSProvider
    {
        public eOSPlatform GetOSPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return eOSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return eOSPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return eOSPlatform.OSX;
            }
            return eOSPlatform.Unknown;
        }

        public bool IsOSPlatform(eOSPlatform osPlatform)
        {
            switch (osPlatform)
            {
                case eOSPlatform.Windows:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                case eOSPlatform.Linux:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                case eOSPlatform.OSX:
                    return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
                case eOSPlatform.Unknown:
                    return false;
                default:
                    throw new Exception($"OSProvider - IsOSPlatform - Unknown platform: { osPlatform.ToString() }");
            }
        }
    }
}

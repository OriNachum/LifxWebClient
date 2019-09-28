using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared.Enums
{
#pragma warning disable IDE1006 // Naming Styles
    public enum eLifxWebApiUrl : short
#pragma warning restore IDE1006 // Naming Styles
    {
        GetBulbs,
        Reset,
        Toggle,
        Refresh,
        RefreshBulb,
        Off,
        On,
        Label,
        SetPower,
        SetTemperature,
        SetBrightness,
        SetColor,
    }
}

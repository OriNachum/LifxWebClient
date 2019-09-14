using Infrared.Impl;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LifxWebApi
{
    public class LifxWebApiServiceLogger : LifxBaseLogger
    {
        protected override string FileName
        {
            get
            {
                return "ApiController.log";
            }
        }
    }
}

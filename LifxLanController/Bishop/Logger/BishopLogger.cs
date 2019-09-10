using Infrared.Impl;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bishop.Logger
{
    public class BishopLogger : LifxBaseLogger
    {
        protected override string FileName
        {
            get
            {
                return $"BishopController.log";
            }
        }
    }
}

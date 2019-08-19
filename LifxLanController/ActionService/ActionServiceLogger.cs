using Infrared.Impl;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActionService
{
    public class ActionServiceLogger : LifxBaseLogger
    {
        protected override string FilePath
        {
            get
            {
                return $"C:\\Logs\\LifxWebApi\\ActionController.log";
            }
        }
    }
}

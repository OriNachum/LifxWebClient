using Infrared.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VueLifxServer
{
    public class VueLifxServerLogger : LifxBaseLogger
    {
        protected override string FilePath => @"C:\Logs\LifxWebApi\VueLifxServer.log";
    }
}

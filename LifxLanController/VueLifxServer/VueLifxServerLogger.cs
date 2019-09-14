using Infrared.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace VueLifxServer
{
    public class VueLifxServerLogger : LifxBaseLogger
    {
        protected override string FileName
        {
            get
            {

                return "VueLifxServer.log";
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LifxCoreController
{
    internal class CommandRequest
    {
        public eLifxCommand Command { get; internal set; }
        public CancellationTokenSource CancelTokenSource { get; internal set; }
    }
}

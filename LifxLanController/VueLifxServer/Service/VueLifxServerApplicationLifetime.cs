using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VueLifxServer.Service
{
    public class VueLifxServerApplicationLifetime : IApplicationLifetime
    {
        CancellationTokenSource CancellationTokenSource;

        public VueLifxServerApplicationLifetime()
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        public CancellationToken ApplicationStarted => CancellationTokenSource.Token;

        public CancellationToken ApplicationStopping => CancellationTokenSource.Token;

        public CancellationToken ApplicationStopped => CancellationTokenSource.Token;

        public void StopApplication()
        {
            
        }
    }
}

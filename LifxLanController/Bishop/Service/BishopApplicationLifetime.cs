using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bishop.Service
{
    public class BishopApplicationLifetime : IApplicationLifetime
    {
        CancellationTokenSource CancellationTokenSource;

        public BishopApplicationLifetime()
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

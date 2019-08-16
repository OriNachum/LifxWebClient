using Bishop.Engine;
using Infrared.Impl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bishop.Service
{
    public class BishopService : IWebHost
    {
        public BishopService(IServiceProvider services = null)
        {
            Services = services;

            if (Services == null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddHttpClient();
                Services = serviceCollection.BuildServiceProvider();
            }
        }

        IBishopEngine BishopEngine;

        private ILogger _logger = null;

        protected ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                        .WriteTo.File($"C:\\Logs\\LifxWebApi\\Bishop.log", shared: true)
                        .CreateLogger();
                }
                return _logger;
            }
        }

        public IFeatureCollection ServerFeatures => throw new NotImplementedException();

        public IServiceProvider Services  { get; private set;}

        public void Dispose()
        {
            this.BishopEngine?.Dispose();
            this.BishopEngine = null;
        }

        public void Start()
        {
            IHttpClientFactory httpClientFactory = Services.GetService<IHttpClientFactory>();

            var serviceUrlProvider = new ServiceUrlProvider(this.Logger);

            this.BishopEngine?.Dispose();
            this.BishopEngine = new BishopEngine(httpClientFactory, this.Logger, serviceUrlProvider);
            this.BishopEngine.Start();
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

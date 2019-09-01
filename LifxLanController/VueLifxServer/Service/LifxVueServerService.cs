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
using VueLifxServerAsService;

namespace VueLifxServer.Service
{
    public class VueLifxServerService : IWebHost
    {
        public VueLifxServerService(IServiceProvider services = null)
        {
            Services = services;

            if (Services == null)
            {
                var serviceCollection = new ServiceCollection();
                Services = serviceCollection.BuildServiceProvider();
            }

            _logger = Services.GetService<ILogger>();
        }

        ILifxVueServer LifxVueServerEngine;

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
            this.LifxVueServerEngine?.Dispose();
            this.LifxVueServerEngine = null;
        }

        public void Start()
        {
            this.LifxVueServerEngine?.Dispose();
            this.LifxVueServerEngine = new LifxVueServer(this.Logger);
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

using System;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;
using Bishop.Engine;
using Bishop.Service;
using Infrared.Impl;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace Bishop
{
    public class Program
    {
        private static ILogger _logger = null;

        protected static ILogger Logger
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

        public static void Main(string[] args)
        {
            Logger.Information("Test Program for bishop started!");

            var serviceCollection = new ServiceCollection();
            var bishopShartup = new BishopStartup();
            IServiceProvider serviceProvider = bishopShartup.ConfigureServices(serviceCollection);

            if (args != null && args.Any() && args[0] == "console")
            {
                IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

                var serviceUrlProvider = new ServiceUrlProvider(Logger);
                //using (var engine = new BishopEngine(httpClientFactory, Logger, serviceUrlProvider))
                using (var engine = serviceProvider.GetService<IWebHost>())
                {
                    engine.Start();
                    Console.WriteLine("Press any key to close service");
                    Console.ReadKey();
                }
            }
            else
            {
                // IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder(args)
                //.UseStartup<BishopStartup>();
                //using (var host = webHostBuilder.Build())
                //{
                //    //BishopService item =null;
                //    //item.RunAsService()
                //    host.RunAsService();
                //}
                //}
                ;
                using (var bishopService = serviceProvider.GetService<IWebHost>())
                {
                    try
                    {
                        bishopService.RunAsService();
                    }
                    catch (Exception ex)
                    {
                        Logger.Information($"bishopService RunAsService encoutnered an exception: {ex}");
                    }
                }
            }

            Logger.Information("Test Program for bishop ended!");
        }
    }
}

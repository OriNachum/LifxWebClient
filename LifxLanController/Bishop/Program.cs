using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;
using Bishop.Engine;
using Bishop.Service;
using Infrared.Enums;
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
        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            var bishopStartup = new BishopStartup();
            IServiceProvider serviceProvider = bishopStartup.ConfigureServices(serviceCollection);

            ILogger logger = serviceProvider.GetService<ILogger>();
            logger.Information("Bishop Main Program initialized");
            using (IWebHost bishopService = serviceProvider.GetService<IWebHost>())
            {
                try
                {
                    var osProvier = new OSProvider();
                    switch (osProvier.GetOSPlatform())
                    {
                        case eOSPlatform.Linux:
                            
                            logger.Information("Bishop Main Service is starting");
                            IWebHostBuilder webHosterBuilder = CreateWebHostBuilder(args);
                            var urlProvider = new ServiceUrlProvider(null);
                            var httpsPort = urlProvider.LinuxHttpsPorts[eService.BishopService];
                            webHosterBuilder.ConfigureKestrel((context, options) =>
                            {
                                options.Listen(IPAddress.Any, httpsPort);
                                // Set properties and call methods on options
                            });

                            webHosterBuilder.Build().Run();
                            logger.Information("Bishop Main Service is stopped");
                            break;

                        case eOSPlatform.Windows:
                            if (args != null && args.Any() && args[0] == "console")
                            {
                                bishopService.Start();
                                Console.WriteLine("Press any key to close service");
                                Console.ReadKey();
                            }
                            else
                            {
                                bishopService.RunAsService();
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.Information($"bishopService RunAsService encoutnered an exception: {ex}");
                }
            }
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
        .UseStartup<BishopStartup>();

    }
}

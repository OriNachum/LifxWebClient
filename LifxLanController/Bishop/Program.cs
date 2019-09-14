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
        public static void Main(string[] args)
        {

            var serviceCollection = new ServiceCollection();
            var bishopShartup = new BishopStartup();
            IServiceProvider serviceProvider = bishopShartup.ConfigureServices(serviceCollection);
            
            var logger = serviceProvider.GetService<ILogger>();
            logger.Information("Bishop Main Program initialized");
            using (var bishopService = serviceProvider.GetService<IWebHost>())
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    logger.Information($"bishopService RunAsService encoutnered an exception: {ex}");
                }
            }
        }
    }
}

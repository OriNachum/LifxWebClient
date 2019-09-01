using VueLifxServer.Service;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace VueLifxServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            var bishopShartup = new VueLifxServerStartup();
            IServiceProvider serviceProvider = bishopShartup.ConfigureServices(serviceCollection);

            var logger = serviceProvider.GetService<ILogger>();
            logger.Information("Bishop Main Program initialized");
            using (var bishopService = serviceProvider.GetService<IWebHost>())
            {
                try
                {
                    bishopService.RunAsService();
                    logger.Information("VueLifxServerAsService Program Main ILifxVueServer was created, running this as service");
                }
                catch (Exception ex)
                {
                    logger.Information($"VueLifxServerAsService Program Main running this as service failed with ex { ex }");
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Infrared.Enums;
using Infrared.Impl;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LifxWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHostBuilder webHosterBuilder = CreateWebHostBuilder(args);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var urlProvider = new ServiceUrlProvider(null);
                var httpsPort = urlProvider.LinuxHttpsPorts[eService.LifxWebApi];
                webHosterBuilder.ConfigureKestrel((context, options) =>
                {
                    options.Listen(IPAddress.Any, httpsPort);
                    // Set properties and call methods on options
                });
            }

            webHosterBuilder.Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}

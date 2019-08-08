using System;
using System.Net.Http;
using System.Threading.Tasks;
using ActionService.Logic;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace Bishop
{
    class Program
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

            IServiceProvider serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var actionProvider = new ActionProvider(httpClientFactory, Logger);

            using (IBishopEngine engine = new BishopEngine(actionProvider, httpClientFactory, Logger))
            {
                engine.Start();

                Console.WriteLine("Press any key to close service");
                Console.ReadKey();
            }

            Logger.Information("Test Program for bishop ended!");
        }
    }
}

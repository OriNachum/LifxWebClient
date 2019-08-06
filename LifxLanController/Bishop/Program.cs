using System;
using System.Threading.Tasks;
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

            using (IBishopEngine engine = new BishopEngine(actionProvider: null, Logger))
            {
                engine.Start();

                Console.WriteLine("Press any key to close service");
                Console.ReadKey();
            }

            Logger.Information("Test Program for bishop ended!");
        }
    }
}

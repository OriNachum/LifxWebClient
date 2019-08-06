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

        static async Task Main(string[] args)
        {
            Logger.Information("Hello world!");

            var engine = new BishopEngine(actionProvider: null, Logger);
            await engine.Start();
        }
    }
}

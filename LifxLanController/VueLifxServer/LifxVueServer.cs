using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace VueLifxServerAsService
{
    public class LifxVueServer : ILifxVueServer
    {
        Process VueProcess = null;

        public LifxVueServer(ILogger logger)
        {
            logger.Information("LifxVueServer Starting vue process");
            VueProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Git\vue-lifx-server\hello-world\run.bat",
                    //Arguments = // $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            try
            {
                VueProcess.Start();
                logger.Information("LifxVueServer vue process started successfully");
            }
            catch (Exception ex)
            {
                logger.Information($"LifxVueServer vue process failed with ex { ex }");
            }
        }

        public void Dispose()
        {
            VueProcess?.Dispose();
        }

    }
}

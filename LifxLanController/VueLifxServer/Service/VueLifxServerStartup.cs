using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using VueLifxServer;
using VueLifxServerAsService;

namespace VueLifxServer.Service
{
    public class VueLifxServerStartup : IStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            //app.UseHsts();

            //app.UseHttpsRedirection();
            //app.UseMvc();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger, VueLifxServerLogger>();
            services.AddSingleton<ILifxVueServer, LifxVueServer>();
            services.AddSingleton<IWebHost, VueLifxServerService>();
            services.AddSingleton<IApplicationLifetime, VueLifxServerApplicationLifetime>();

            return services.BuildServiceProvider();
        }
    }
}

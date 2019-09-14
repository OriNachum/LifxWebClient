using Bishop.Engine;
using Bishop.Logger;
using Infrared;
using Infrared.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bishop.Service
{
    public class BishopStartup : IStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            //app.UseHsts();

            //app.UseHttpsRedirection();
            //app.UseMvc();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<ILogger, BishopLogger>();
            services.AddSingleton<IServiceUrlProvider, ServiceUrlProvider>();
            services.AddSingleton<IBishopEngine, BishopEngine>();
            services.AddSingleton<IWebHost, BishopService>();
            services.AddSingleton<IApplicationLifetime, BishopApplicationLifetime>();

            return services.BuildServiceProvider();
        }
    }
}

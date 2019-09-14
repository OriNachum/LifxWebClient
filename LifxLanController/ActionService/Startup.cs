using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ActionService.Logic;
using Infrared;
using Infrared.Impl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProvidersInterface;
using Serilog;

namespace ActionService
{
    public class Startup : IStartup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //IServiceProvider serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            //IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            //services.AddSingleton<IHttpContextFactory, httpClientFactory>();
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy.Name,
                builder =>
                {
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowAnyOrigin();
                    //builder.WithOrigins("*:8080");
                    // builder.SetIsOriginAllowed(origin => true); // For anyone access.

                    //corsBuilder.WithOrigins("http://localhost:56573"); // for a specific url. Don't add a forward slash on the end!
                    // builder.AllowCredentials(); No AnyOrigin with Credentials
                    // builder.WithOrigins(CorsPolicy.AllowedSources);
                });
            });
            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("SiteCorsPolicy"));
            });

            services.AddHttpClient();
            services.AddSingleton<ILogger, ActionServiceLogger>();
            services.AddSingleton<IServiceUrlProvider, ServiceUrlProvider>();
            services.AddSingleton<IActionProvider, ActionProvider>();
            services.Configure<ActionProvider>(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            return services.BuildServiceProvider();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(CorsPolicy.Name);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors(CorsPolicy.Name);
            app.UseHsts();
        
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}

using Infrared.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Infrared.Impl
{
    public class ServiceUrlProvider : IServiceUrlProvider
    {
        const string Hostname = "ori";

        readonly IDictionary<string, string> Sites = new Dictionary<string, string> {
            { "dev", $"https://{Hostname}:44370" },
            { "devIisDebug", $"https://{Hostname}:5001"},
            { "devIis",  $"https://{Hostname}"},
        };

        string ActiveSite
        {
            get
            {
                return $"{Sites["devIis"]}";
            }
        }

        public ILogger Logger { get; }
        public IDictionary<eService, string> Urls { get; }
        public Dictionary<eService, int> LinuxHttpsPorts { get; }

        // string GetNextActionUrl = "https://ori/ActionService/api/Action";
        // "https://localhost:44306/api/Action/GetNextAction";
        //$"https://ori/ActionService/api/Action/GetNextAction"; 
        // https://ori:444/ActionService/api/Action/GetNextAction


        public ServiceUrlProvider(ILogger logger)
        {
            this.Logger = logger;

            this.Urls = new Dictionary<eService, string>
            {
                { eService.LifxWebApi, $"Lifx/Api" },
                { eService.ActionService, $"api/Action" },
            };

            this.LinuxHttpsPorts = new Dictionary<eService, int>
            {
                { eService.LifxWebApi, 5011 },
                { eService.ActionService, 5021 },
            };
        }

        public string GetUrl(eService service, string actionId)
        {
            if (!this.Urls.ContainsKey(service))
            {
                Logger.Error($"Requested service not supported {service.ToString()}");
                return null;
            }

            string basePath = "";
            var osProvier = new OSProvider();
            switch (osProvier.GetOSPlatform())
            {
                case eOSPlatform.Windows:
                    basePath = $"{ ActiveSite }/{ service.ToString() }";
                    break;
                case eOSPlatform.Linux:
                    basePath = $"{ ActiveSite }:{ this.LinuxHttpsPorts[service] }";
                    break;
            }

            return $"{ basePath }/{this.Urls[service]}/{actionId}";
        }
    }
}

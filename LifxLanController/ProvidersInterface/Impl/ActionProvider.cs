using ProvidersInterface.Enums;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Http;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ProvidersInterface.Impl
{
    public class ActionProvider : IActionProvider
    {
        const string Hostname = "ori";

        readonly IDictionary<string, string> Sites = new Dictionary<string, string> {
            { "dev", $"https://{Hostname}:44370/" },
            { "devIisDebug", $"https://{Hostname}:5001/"},
            { "devIis",  $"https://{Hostname}/LifxWebApi/"},
        };

        string ActiveSite
        {
            get
            {
                return Sites["devIis"];
            }
        }

        IDictionary<string, string> Urls;

        public ActionProvider()
        {
            Urls = new Dictionary<string, string>
            {
                { "getBulbs", $"{ActiveSite}Lifx/Api/GetBulbs" },
                { "reset", $"{ActiveSite}Lifx/Api/Reset" },
                { "toggleBulb", $"{ActiveSite}Lifx/Api/Toggle" },
                { "refreshBulbs", $"{ActiveSite}Lifx/Api/Refresh" },
                { "refreshBulb", $"{ActiveSite}Lifx/Api/RefreshBulb" },
                { "off", $"{ActiveSite}Lifx/Api/Off" },
                { "on", $"{ActiveSite}Lifx/Api/On" },
                { "setBrightness", $"{ActiveSite}Lifx/Api/Brightness" },
                { "setColor", $"{ActiveSite}Lifx/Api/Color" },
                { "setLabel", $"{ActiveSite}Lifx/Api/Label" },
                { "setPower", $"{ActiveSite}Lifx/Api/Power" },
                { "setTemperature", $"{ActiveSite}Lifx/Api/Temperature" },
                { "fadeToState", $"{ActiveSite}Lifx/Api/FadeToState" },
            };
        }

        public Func<Task> GetNextAction()
        {
            return async () =>
            {
                IServiceProvider serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
                IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                using (var client = httpClientFactory.CreateClient())
                {
                    var uri = new Uri(string.Concat(Urls["toggleBulb"], "?label=Television")); // ?date=today add that aas constant value for starters
                    var reseponse = await client.GetAsync(uri);
                }
            };
        }

        public void SetCurrentActionState(eActionState success)
        {
            // throw new NotImplementedException();
        }

        public void QueueState(Action action)
        {
            throw new NotImplementedException();
        }

        public void ScheduleAction(Action action, ActionSchedule actionSchedule)
        {
            throw new NotImplementedException();
        }
    }
}
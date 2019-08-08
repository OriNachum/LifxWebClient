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
using LifxCoreController.Lightbulb;
using Newtonsoft.Json;
using System.Linq;
using Serilog;
using ProvidersInterface;
using ProvidersInterface.Models;

namespace ActionService.Logic
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
        IDictionary<ActionSchedule, bool> ActionsSchedule;
        IDictionary<string, ActionDefinition> ActionsDefinitions;

        IHttpClientFactory HttpClientFactory;
        ILogger Logger;

        public ActionProvider(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            this.Logger = logger;
            this.HttpClientFactory = httpClientFactory;

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

            string actionStartWakeupName = "Wakeup";
            string actionStartFadeInName = "FadeIn";

            var actionDayTime = new DateTime(2000, 1, 1, hour: 7, minute: 30, second: 00);
            var actionWeekendTime = new DateTime(2000, 1, 1, hour: 9, minute: 30, second: 00);

            InitializeActionsDefinitions(actionStartWakeupName, actionStartFadeInName);

            this.ActionsSchedule = new Dictionary<ActionSchedule, bool>();
            InitializeActionsSchedule(actionStartWakeupName, actionDayTime.AddMinutes(-5), actionWeekendTime.AddMinutes(-5));
            InitializeActionsSchedule(actionStartFadeInName, actionDayTime, actionWeekendTime);
        }

        private void InitializeActionsDefinitions(string actionStartWakeupName, string actionStartFadeInName)
        {
            var bulbState = new BulbState
            {
                Brightness = 1,
                Temperature = 3550,
                Power = Lifx.Power.On,
                Saturation = 0,
                Hue = 0,
            };
            var serializedBulbState = JsonConvert.SerializeObject(bulbState);

            this.ActionsDefinitions = new Dictionary<string, ActionDefinition>
            {
                {
                    actionStartWakeupName, new ActionDefinition
                    {
                        Url = "getBulbs",
                    }
                },
                {
                    actionStartFadeInName, new ActionDefinition
                    {
                        Url = "fadeToState",
                        Params = $"?label={"Bedroom"}&serializedState={serializedBulbState}&fadeInDuration={900000}",
                    }
                },
            };
        }

        private void InitializeActionsSchedule(string actionStartFadeInName, DateTime actionDayTime, DateTime actionWeekendTime)
        {
            foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
            {
                var actionSchedule = new ActionSchedule
                {
                    Day = dayOfWeek,
                    Time = actionDayTime,
                    ActionName = actionStartFadeInName,
                };
                if (dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday)
                {
                    actionSchedule.Time = actionWeekendTime;
                }
                this.ActionsSchedule.Add(actionSchedule, true);
            }
        }

        public ActionModel GetNextScheduledAction()
        {
            ActionSchedule actionSchedule = this.ActionsSchedule
                .Where(x => x.Value)
                .Where(x => x.Key.Day == DateTime.Now.DayOfWeek)
                .Where(x => x.Key.Time.TimeOfDay <= DateTime.Now.TimeOfDay)
                .Where(x => x.Key.Time.AddMinutes(30).TimeOfDay > DateTime.Now.TimeOfDay)
                .Select(x => x.Key)
                .FirstOrDefault();
            if (actionSchedule == null || !this.ActionsDefinitions.ContainsKey(actionSchedule.ActionName))
            {
                return null;
            }

            this.Logger.Information("ActionProvider - GetNextAction - Found an action model to perform");
            this.ActionsSchedule[actionSchedule] = false;

            ActionDefinition actionDefinition = this.ActionsDefinitions[actionSchedule.ActionName];

            var actionModel = new ActionModel
            {
                FullUrl = string.Concat(this.Urls[actionDefinition.Url], actionDefinition.Params),
            };
            return actionModel;

            // this.Logger.Information("ActionProvider - GetNextAction - Generating action");
            // Func<Task<string>> nextAction = GenerateActionFromScheduleModel(actionDefinition);

            // return nextAction;
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

        public void ScheduleAction(Action action, ActionScheduleModel actionSchedule)
        {
            throw new NotImplementedException();
        }
    }
}
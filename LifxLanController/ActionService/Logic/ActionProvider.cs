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
using Infrared;
using Infrared.Enums;

namespace ActionService.Logic
{
    public class ActionProvider : IActionProvider
    {
        IDictionary<ActionSchedule, bool> ActionsSchedule;
        IDictionary<string, ActionDefinition> ActionsDefinitions;

        private ILogger Logger { get; }

        private IServiceUrlProvider ServiceUrlProvider { get; }

        public ActionProvider(ILogger logger, IServiceUrlProvider serviceUrlProvider)
        {
            this.Logger = logger;
            this.Logger.Information("ActionProvider started");

            this.ServiceUrlProvider = serviceUrlProvider;

            string actionStartWakeupName = "Wakeup";
            string actionStartFadeInName = "FadeIn";

            var actionDayTime = new DateTime(2000, 1, 1, hour: 7, minute: 30, second: 00);
            var actionWeekendTime = new DateTime(2000, 1, 1, hour: 9, minute: 30, second: 00);

            InitializeActionsDefinitions(actionStartWakeupName, actionStartFadeInName);

            this.ActionsSchedule = new Dictionary<ActionSchedule, bool>();
            InitializeActionsSchedule(actionStartWakeupName, actionDayTime.AddMinutes(-5), actionWeekendTime.AddMinutes(-5));
            InitializeActionsSchedule(actionStartFadeInName, actionDayTime, actionWeekendTime);

            string testAction = "TestAction";
            DayOfWeek? dayOfweek = DayOfWeek.Friday;
            DateTime timeToRun = new DateTime(2000, 1, 1, hour: 19, minute: 20, second: 00);
            var urlCodeToRun = eLifxWebApi.GetBulbs;
            string paramsForUrl = null;
            AddActionToSchedule(testAction, dayOfweek, timeToRun, urlCodeToRun, paramsForUrl);
        }

        private void AddActionToSchedule(string testAction, DayOfWeek? dayOfweek, DateTime timeToRun, Enum urlCodeToRun, string paramsForUrl)
        {
            this.ActionsSchedule.Add(new ActionSchedule
            {
                ActionName = testAction,
                Day = dayOfweek,
                Time = timeToRun,
            }, true);
            this.ActionsDefinitions.Add(testAction, new ActionDefinition
            {
                Url = urlCodeToRun,
                Params = paramsForUrl,
            });
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
                        Url = eLifxWebApi.GetBulbs,
                    }
                },
                {
                    actionStartFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApi.FadeToState,
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
                .Where(x => !x.Key.Day.HasValue || x.Key.Day == DateTime.Now.DayOfWeek)
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
            string url = this.ServiceUrlProvider.GetUrl(eService.LifxWebApi, actionDefinition.Url);

            var actionModel = new ActionModel
            {
                FullUrl = string.Concat(url, actionDefinition.Params),
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
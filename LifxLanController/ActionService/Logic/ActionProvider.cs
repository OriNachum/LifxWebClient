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
using ActionService.Models;

namespace ActionService.Logic
{
    public class ActionProvider : IActionProvider
    {
        private string ActionsScheduleFilePath = @"C:\LifxWebApi\ActionsSchedule.json";
        private string ActionsDefinitionsFilePath = @"C:\LifxWebApi\ActionsDefinitions.json";

        IDictionary<ActionSchedule, bool> ActionsSchedule;
        IDictionary<string, ActionDefinition> ActionsDefinitions;

        private ILogger Logger { get; }

        private IServiceUrlProvider ServiceUrlProvider { get; }

        public ActionProvider(ILogger logger, IServiceUrlProvider serviceUrlProvider)
        {
            this.Logger = logger;
            this.Logger.Information("ActionProvider started");

            this.ServiceUrlProvider = serviceUrlProvider;

            // InitializeArraysHardCoded();

            // SaveActionsSchedule();
            LoadActionsSchedule();

            // SaveActionsDefinitions();
            LoadActionsDefinitions();
        }

        private void LoadActionsDefinitions()
        {
            string fileContent = File.ReadAllText(ActionsDefinitionsFilePath);
            this.ActionsDefinitions = JsonConvert.DeserializeObject<Dictionary<string, ActionDefinition>>(fileContent);
        }

        private void SaveActionsDefinitions()
        {
            string fileContent = JsonConvert.SerializeObject(this.ActionsDefinitions);
            File.WriteAllText(ActionsDefinitionsFilePath, fileContent);
        }

        private void LoadActionsSchedule()
        {
            this.ActionsSchedule = new Dictionary<ActionSchedule, bool>();
            string fileContent = File.ReadAllText(ActionsScheduleFilePath);
            KeyValuePair<ActionSchedule, bool>[] actionsScheduleArray = JsonConvert.DeserializeObject<KeyValuePair<ActionSchedule, bool>[]>(fileContent);
            foreach (var record in actionsScheduleArray)
            {
                this.ActionsSchedule.Add(record.Key, record.Value);
            }
        }

        private void SaveActionsSchedule()
        {
            KeyValuePair<ActionSchedule, bool>[] actionsScheduleArray = this.ActionsSchedule.ToArray();
            string fileContent = JsonConvert.SerializeObject(actionsScheduleArray);
            File.WriteAllText(ActionsScheduleFilePath, fileContent);
        }

        private void AddActionToSchedule(string testAction, DayOfWeek? dayOfweek, DateTime timeToRun, string urlCodeToRun, string paramsForUrl)
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

        private void InitializeActionsDefinitions(string actionStartWakeupName, string actionStartFadeInName, string actionTurnOnBulbName, string actionFadeInName)
        {
            this.ActionsDefinitions = new Dictionary<string, ActionDefinition>
            {
                {
                    actionStartWakeupName, new ActionDefinition
                    {
                        Url = eLifxWebApi.GetBulbs.ToString(),
                    }
                },
                {
                    actionStartFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApi.SetBrightness.ToString(),
                        Params = $"?label={"Bedroom"}&brightness={0.01}&fadeInDuration={0}",
                    }
                },
                {
                    actionTurnOnBulbName, new ActionDefinition
                    {
                        Url = eLifxWebApi.SetPower.ToString(),
                        Params = $"?label={"Bedroom"}&onOffState={"on"}",
                    }
                },
                {
                    actionFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApi.SetBrightness.ToString(),
                        Params = $"?label={"Bedroom"}&brightness={1}&fadeInDuration={900000}",
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
                .Where(x => this.ActionsDefinitions.ContainsKey(x.Key.ActionName))
                .Select(x => x.Key)
                .FirstOrDefault();
            if (actionSchedule == null)
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

        private void InitializeArraysHardCoded()
        {
            string actionStartWakeupName = "Wakeup";
            string actionInitFadeInName = "StartFadeIn";
            string actionTurnOnBulbName = "TuronOnBulb";
            string actionFadeInName = "FadeIn";

            var actionDayTime = new DateTime(2000, 1, 1, hour: 7, minute: 30, second: 00);
            var actionWeekendTime = new DateTime(2000, 1, 1, hour: 9, minute: 30, second: 00);

            InitializeActionsDefinitions(actionStartWakeupName, actionInitFadeInName, actionTurnOnBulbName, actionFadeInName);

            this.ActionsSchedule = new Dictionary<ActionSchedule, bool>();
            InitializeActionsSchedule(actionStartWakeupName, actionDayTime.AddMinutes(-5), actionWeekendTime.AddMinutes(-5));
            InitializeActionsSchedule(actionInitFadeInName, actionDayTime, actionWeekendTime);
            InitializeActionsSchedule(actionTurnOnBulbName, actionDayTime.AddMinutes(1), actionWeekendTime.AddMinutes(1));
            InitializeActionsSchedule(actionFadeInName, actionDayTime.AddMinutes(2), actionWeekendTime.AddMinutes(2));

            /*
                string testAction = "TestAction";
                DayOfWeek? dayOfweek = DayOfWeek.Friday;
                DateTime timeToRun = DateTime.Now.AddMinutes(2);
                var urlCodeToRun = eLifxWebApi.GetBulbs.ToString();
                string paramsForUrl = null;
                AddActionToSchedule(testAction, dayOfweek, timeToRun, urlCodeToRun, paramsForUrl);
                InitializeActionsSchedule(actionStartFadeInName, timeToRun, timeToRun);
                InitializeActionsSchedule(actionFadeInName, timeToRun.AddMinutes(1), timeToRun.AddMinutes(1));
            */
        }
    }
}

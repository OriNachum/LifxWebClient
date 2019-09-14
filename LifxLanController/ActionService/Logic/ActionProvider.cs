using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using Serilog;
using ProvidersInterface;
using ProvidersInterface.Models;
using Infrared;
using Infrared.Enums;
using ActionService.Models;
using System.Runtime.InteropServices;

namespace ActionService.Logic
{
    public class ActionProvider : IActionProvider
    {
        private const string ActionsScheduleFileName = "ActionsSchedule.json";
        private const string ActionsDefinitionsFileName = @"ActionsDefinitions.json";
        private string ActionsScheduleFilePath
        {
            get
            {
                return PrependConfigPath(ActionsScheduleFileName);
            }
        }

        private string PrependConfigPath(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"C:\\LifxWebApi\\{ fileName }";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"/home/pi/lifx/{ fileName }";
            }
            return fileName;
        }

        private string ActionsDefinitionsFilePath 
        {
            get
            {
                return PrependConfigPath(ActionsDefinitionsFileName);
            }
        }

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

            // SaveActionsDefinitions();
            LoadActionsDefinitions();

            // SaveActionsSchedule();
            LoadActionsSchedule();

            ResetActions();
        }

        #region IActionProvider
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

            ResetOlderActions();
            SaveActionsSchedule();

            ActionDefinition actionDefinition = this.ActionsDefinitions[actionSchedule.ActionName];
            string fullUrl = GetFullUrl(actionDefinition);
            var actionModel = new ActionModel
            {
                Name = actionSchedule.ActionName,
                FullUrl = fullUrl,
                Time = actionSchedule.Time,
                DayOfWeek = actionSchedule.Day,
                Active = true,
            };
            return actionModel;
        }
        public ScheduleModel GetFullSchedule()
        {
            var actionModels = new List<ActionModel>();
            foreach (KeyValuePair<string, ActionDefinition> actionDefinition in this.ActionsDefinitions)
            {
                IEnumerable<KeyValuePair<ActionSchedule, bool>> scheduleOfAction = this.ActionsSchedule.Where(x => x.Key.ActionName == actionDefinition.Key);
                foreach (KeyValuePair<ActionSchedule, bool> actionSchedule in scheduleOfAction)
                {
                    var actionModel = new ActionModel
                    {
                        Id = actionSchedule.Key.Id,
                        Name = actionDefinition.Key,
                        FullUrl = GetFullUrl(actionDefinition.Value),
                        Time = actionSchedule.Key.Time,
                        DayOfWeek = actionSchedule.Key.Day,
                        Active = actionSchedule.Value,
                    };
                    actionModels.Add(actionModel);
                }
            }
            var schedule = new ScheduleModel
            {
                Actions = actionModels,
            };
            return schedule;
        }


        public IEnumerable<string> GetActions()
        {
            return this.ActionsDefinitions.Keys;
        }

        public IEnumerable<string> GetSupportedActions()
        {
            return Enum.GetNames(typeof(eLifxWebApiUrl));
        }

        public bool DefineAction(string name, string supportedAction, string urlParameters)
        {
            if (Enum.TryParse(supportedAction, out eLifxWebApiUrl supportedActionCode))
            {
                var actionDefinition = new ActionDefinition
                {
                    Url = supportedAction,
                    Params = urlParameters,
                };

                Logger.Information($"ActionProvider - DefineAction - adding actionDefinition { actionDefinition } to memory");
                try
                {
                    this.ActionsDefinitions.Add(name, actionDefinition);
                }
                catch (Exception ex)
                {
                    Logger.Information($"ActionProvider - DefineAction - adding actionDefinition { actionDefinition } to memory failed. Ex: { ex }");
                    return false;
                }
                Logger.Information($"ActionProvider - DefineAction - added actionDefinition { actionDefinition } to memory");

                SaveActionsDefinitions();

                return true;
            }

            return false;
        }

        public void ScheduleAction(string actionName, DateTime timeToRun, DayOfWeek? dayOfweek)
        {
            Logger.Information($"ActionProvider - ScheduleAction - adding action { actionName } to memory");

            try
            {
                this.ActionsSchedule.Add(new ActionSchedule
                {
                    Id = this.ActionsSchedule.Select(x => x.Key.Id).Max() + 1,
                    ActionName = actionName,
                    Time = timeToRun,
                    Day = dayOfweek,
                }, true); ; ;
            }
            catch (Exception ex)
            {
                Logger.Information($"ActionProvider - ScheduleAction - adding action { actionName } to memory failed. Ex { ex }");
            }

            Logger.Information($"ActionProvider - ScheduleAction - added { actionName } to schedule in memory");

            this.SaveActionsSchedule();
        }

        public bool DeleteScheduledAction(int id)
        {
            Logger.Information($"ActionProvider - DeleteScheduledAction - removing action { id } from schedule in memory");

            ActionSchedule actionSchedule = this.ActionsSchedule.Select(x => x.Key).Where(x => x.Id == id).FirstOrDefault();
            if (actionSchedule == null)
            {
                Logger.Error($"ActionProvider - DeleteScheduledAction - did not find scheduledAction { id } in memory");
                return false;
            }

            this.ActionsSchedule.Remove(actionSchedule);

            Logger.Information($"ActionProvider - DeleteScheduledAction - removed action { id } from schedule in memory");

            SaveActionsSchedule();

            return true;
        }

        public bool ModifyScheduledAction(ActionModel actionModel)
        {
            Logger.Information($"ActionProvider - ModifyScheduledAction - modifying action { actionModel } from schedule in memory");

            ActionSchedule actionSchedule = this.ActionsSchedule.Select(x => x.Key).Where(x => x.Id == actionModel.Id).FirstOrDefault();
            if (actionSchedule == null)
            {
                Logger.Error($"ActionProvider - ModifyScheduledAction - did not find scheduledAction { actionModel } in memory");
                return false;
            }

            actionSchedule.ActionName = actionModel.Name;
            actionSchedule.Day = actionModel.DayOfWeek;
            actionSchedule.Time = actionModel.Time;
            this.ActionsSchedule[actionSchedule] = actionModel.Active;

            Logger.Information($"ActionProvider - ModifyScheduledAction - modified action { actionModel } from schedule in memory");

            SaveActionsSchedule();

            return true;
        }
        #endregion

        private void AddActionToSchedule(string actionName,
                                         DateTime timeToRun,
                                         DayOfWeek? dayOfweek,
                                         string urlCodeToRun,
                                         string paramsForUrl)
        {
            this.ActionsSchedule.Add(new ActionSchedule
            {
                Id = this.ActionsSchedule.Select(x => x.Key.Id).Max() + 1,
                ActionName = actionName,
                Time = timeToRun,
                Day = dayOfweek,
            }, true);
            this.ActionsDefinitions.Add(actionName, new ActionDefinition
            {
                Url = urlCodeToRun,
                Params = paramsForUrl,
            });
        }
        private void LoadActionsDefinitions()
        {
            Logger.Information($"ActionProvider - LoadActionsDefinitions - loading actionDefinitions");

            try
            {
                string fileContent = File.ReadAllText(ActionsDefinitionsFilePath);
                this.ActionsDefinitions = JsonConvert.DeserializeObject<Dictionary<string, ActionDefinition>>(fileContent);
            }
            catch (Exception ex)
            {
                Logger.Error($"ActionProvider - LoadActionsDefinitions - loading actionDefinitions failed. ex: { ex }");
                return;
            }

            Logger.Information($"ActionProvider - LoadActionsDefinitions - loaded actionDefinitions");
        }

        private void SaveActionsDefinitions()
        {
            Logger.Information($"ActionProvider - SaveActionsDefinitions - saving actionDefinitions");

            try
            {
                string fileContent = JsonConvert.SerializeObject(this.ActionsDefinitions);
                File.WriteAllText(ActionsDefinitionsFilePath, fileContent);
            }
            catch (Exception ex)
            {
                Logger.Error($"ActionProvider - SaveActionsDefinitions - saving actionDefinitions failed. ex: { ex }");
                return;
            }

            Logger.Information($"ActionProvider - SaveActionsDefinitions - saved actionDefinitions");
        }

        private void LoadActionsSchedule()
        {
            Logger.Information($"ActionProvider - LoadActionsSchedule - loading actionsSchedule");

            try
            {
                this.ActionsSchedule = new Dictionary<ActionSchedule, bool>();
                string fileContent = File.ReadAllText(ActionsScheduleFilePath);
                KeyValuePair<ActionSchedule, bool>[] actionsScheduleArray = JsonConvert.DeserializeObject<KeyValuePair<ActionSchedule, bool>[]>(fileContent);
                foreach (var record in actionsScheduleArray)
                {
                    record.Key.Id = this.ActionsSchedule.Count() + 1;
                    this.ActionsSchedule.Add(record.Key, record.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ActionProvider - LoadActionsSchedule - loading actionsSchedule failed. ex: { ex }");
                return;
            }

            Logger.Information($"ActionProvider - LoadActionsSchedule - loaded actionsSchedule");
        }

        private void SaveActionsSchedule()
        {
            Logger.Information($"ActionProvider - SaveActionsSchedule - saving actionsSchedule");

            try
            {
                KeyValuePair<ActionSchedule, bool>[] actionsScheduleArray = this.ActionsSchedule.ToArray();
                string fileContent = JsonConvert.SerializeObject(actionsScheduleArray);
                File.WriteAllText(ActionsScheduleFilePath, fileContent);
            }
            catch (Exception ex)
            {
                Logger.Error($"ActionProvider - SaveActionsSchedule - saving actionsSchedule failed. ex: { ex }");
                return;
            }

            Logger.Information($"ActionProvider - SaveActionsSchedule - saved actionsSchedule");
        }

        private void ResetOlderActions()
        {
            var todayAndYesterday = new List<DayOfWeek>
            {
                DateTime.UtcNow.DayOfWeek,
                DateTime.UtcNow.AddDays(-1).DayOfWeek,
            };
            ResetActions(todayAndYesterday);
        }

        private void ResetActions()
        {
            var daysToIgnore = new List<DayOfWeek>();
            ResetActions(daysToIgnore);
        }

        private void ResetActions(IEnumerable<DayOfWeek> daysToIgnore)
        {
            var oneTimeActions = this.ActionsSchedule.Where(x => !x.Value)
                .Where(x => !x.Key.Day.HasValue)
                .Select(x => x.Key).ToList();
            foreach (ActionSchedule actionSchedule in oneTimeActions)
            {
                this.ActionsSchedule.Remove(actionSchedule);
            }

            var repeatableDays = this.ActionsSchedule.Where(x => !x.Value)
                .Where(x => x.Key.Day.HasValue)
                .Where(x => !daysToIgnore.Contains(x.Key.Day.Value))
                .Select(x => x.Key).ToList();
            foreach (ActionSchedule actionSchedule in repeatableDays)
            {
                this.ActionsSchedule[actionSchedule] = true;
            }
        }

        private string GetFullUrl(ActionDefinition actionDefinition)
        {
            string url = this.ServiceUrlProvider.GetUrl(eService.LifxWebApi, actionDefinition.Url);
            string fullUrl = string.Concat(url, actionDefinition.Params);
            return fullUrl;
        }

        #region Initialize datafile
        private void InitializeActionsDefinitions(string actionStartWakeupName, string actionStartFadeInName, string actionTurnOnBulbName, string actionFadeInName)
        {
            this.ActionsDefinitions = new Dictionary<string, ActionDefinition>
            {
                {
                    actionStartWakeupName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.GetBulbs.ToString(),
                    }
                },
                {
                    actionStartFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.SetBrightness.ToString(),
                        Params = $"?label={"Bedroom"}&brightness={0.01}&fadeInDuration={0}",
                    }
                },
                {
                    actionTurnOnBulbName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.SetPower.ToString(),
                        Params = $"?label={"Bedroom"}&onOffState={"on"}",
                    }
                },
                {
                    actionFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.SetBrightness.ToString(),
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
                    Id = this.ActionsSchedule.Select(x => x.Key.Id).Max() + 1,
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
        #endregion
    }
}

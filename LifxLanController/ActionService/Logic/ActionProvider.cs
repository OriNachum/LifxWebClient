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
            switch (this.OSProvider.GetOSPlatform())
            {
                case eOSPlatform.Windows:
                    return $"C:\\LifxWebApi\\{ fileName }";
                case eOSPlatform.Linux:
                    return $"/home/pi/lifx/{ fileName }";
                default:
                    return fileName;
            }
        }

        private string ActionsDefinitionsFilePath 
        {
            get
            {
                return PrependConfigPath(ActionsDefinitionsFileName);
            }
        }

        IList<ActionSchedule> ActionsSchedule;
        IDictionary<string, ActionDefinition> ActionsDefinitions;

        private ILogger Logger { get; }
        public IOSProvider OSProvider { get; }
        private IServiceUrlProvider ServiceUrlProvider { get; }

        public ActionProvider(ILogger logger, IServiceUrlProvider serviceUrlProvider, IOSProvider osProvider)
        {
            this.Logger = logger;
            this.Logger.Information("ActionProvider started");
            this.OSProvider = osProvider;
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
                .Where(x => x.Active)
                .Where(x => !x.Day.HasValue || x.Day == DateTime.Now.DayOfWeek)
                .Where(x => x.Time.TimeOfDay <= DateTime.Now.TimeOfDay)
                .Where(x => x.Time.AddMinutes(30).TimeOfDay > DateTime.Now.TimeOfDay)
                .Where(x => this.ActionsDefinitions.ContainsKey(x.ActionName))
                .FirstOrDefault();
            if (actionSchedule == null)
            {
                return null;
            }

            this.Logger.Information("ActionProvider - GetNextAction - Found an action model to perform");
            actionSchedule.Active = false;

            ResetOlderActions();
            SaveActionsSchedule();

            ActionDefinition actionDefinition = this.ActionsDefinitions[actionSchedule.ActionName];
            string fullUrl = GetFullUrl(actionDefinition);
            var actionModel = new ActionModel
            {
                Name = actionSchedule.ActionName,
                // FullUrl = fullUrl,
                Time = actionSchedule.Time,
                DaysOfWeek = actionSchedule.Day.HasValue ? new[] { actionSchedule.Day.Value } : new DayOfWeek[] { },
                Active = true,
            };
            return actionModel;
        }
        public ScheduleModel GetFullSchedule()
        {
            var actionModels = new List<ActionModel>();
            foreach (KeyValuePair<string, ActionDefinition> actionDefinition in this.ActionsDefinitions)
            {
                IEnumerable<ActionSchedule> scheduleOfAction = this.ActionsSchedule.Where(x => x.ActionName == actionDefinition.Key);
                foreach (ActionSchedule actionSchedule in scheduleOfAction)
                {
                    var actionModel = new ActionModel
                    {
                        Id = actionSchedule.Id,
                        Name = actionDefinition.Key,
                        // FullUrl = GetFullUrl(actionDefinition.Value),
                        Time = actionSchedule.Time,
                        DaysOfWeek = actionSchedule.Day.HasValue ? new[] { actionSchedule.Day.Value } : new DayOfWeek[] { },
                        Date = actionSchedule.Date,
                        Repeating = actionSchedule.Repeating,
                        Active = actionSchedule.Active,
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

        public bool DefineAction(string name, string supportedAction, IDictionary<string, string> urlParameters)
        {
            if (Enum.TryParse(supportedAction, out eLifxWebApiUrl supportedActionCode))
            {
                var actionDefinition = new ActionDefinition
                {
                    Url = supportedAction,
                    Parameters = urlParameters,
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

        public void ScheduleAction(string actionName, DateTime timeToRun, DayOfWeek? dayOfweek, DateTime? specificDate, bool repeating)
        {
            Logger.Information($"ActionProvider - ScheduleAction - adding action { actionName } to memory");

            try
            {
                this.ActionsSchedule.Add(new ActionSchedule
                {
                    Id = this.ActionsSchedule.Select(x => x.Id).Max() + 1,
                    ActionName = actionName,
                    Time = timeToRun,
                    Day = dayOfweek,
                    Date = specificDate,
                    Repeating = repeating,
                    Active = true,
                });
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

            ActionSchedule actionSchedule = this.ActionsSchedule.Where(x => x.Id == id).FirstOrDefault();
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

            ActionSchedule actionSchedule = this.ActionsSchedule.Where(x => x.Id == actionModel.Id).FirstOrDefault();
            if (actionSchedule == null)
            {
                Logger.Error($"ActionProvider - ModifyScheduledAction - did not find scheduledAction { actionModel } in memory");
                return false;
            }

            actionSchedule.ActionName = actionModel.Name;
            if (actionModel.DaysOfWeek.Any())
            {
                actionSchedule.Day = actionModel.DaysOfWeek.First();
            }
            actionSchedule.Time = actionModel.Time;
            actionSchedule.Active = actionModel.Active;

            Logger.Information($"ActionProvider - ModifyScheduledAction - modified action { actionModel } from schedule in memory");

            SaveActionsSchedule();

            return true;
        }
        #endregion

        private void AddActionToSchedule(string actionName,
                                         DateTime timeToRun,
                                         DayOfWeek? dayOfweek,
                                         string urlCodeToRun,
                                         IDictionary<string, string> paramsForUrl)
        {
            this.ActionsSchedule.Add(new ActionSchedule
            {
                Id = this.ActionsSchedule.Select(x => x.Id).Max() + 1,
                ActionName = actionName,
                Time = timeToRun,
                Day = dayOfweek,
                Active = true,
            });
            this.ActionsDefinitions.Add(actionName, new ActionDefinition
            {
                Url = urlCodeToRun,
                Parameters = paramsForUrl,
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
                this.ActionsSchedule = new List<ActionSchedule>();
                string fileContent = File.ReadAllText(ActionsScheduleFilePath);
                KeyValuePair<ActionSchedule, bool>[] actionsScheduleArray = JsonConvert.DeserializeObject<KeyValuePair<ActionSchedule, bool>[]>(fileContent);
                foreach (var record in actionsScheduleArray)
                {
                    record.Key.Id = this.ActionsSchedule.Count() + 1;
                    record.Key.Active = record.Value;
                    this.ActionsSchedule.Add(record.Key);
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
                ActionSchedule[] actionsScheduleArray = this.ActionsSchedule.ToArray();
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
            var oneTimeActions = this.ActionsSchedule.Where(x => !x.Active)
                .Where(x => !x.Day.HasValue)
                .ToList();
            foreach (ActionSchedule actionSchedule in oneTimeActions)
            {
                this.ActionsSchedule.Remove(actionSchedule);
            }

            var repeatableDays = this.ActionsSchedule.Where(x => !x.Active)
                .Where(x => x.Day.HasValue)
                .Where(x => !daysToIgnore.Contains(x.Day.Value))
                .ToList();
            foreach (ActionSchedule actionSchedule in repeatableDays)
            {
                actionSchedule.Active = true;
            }
        }

        private string GetFullUrl(ActionDefinition actionDefinition)
        {
            string url = this.ServiceUrlProvider.GetUrl(eService.LifxWebApi, actionDefinition.Url, actionDefinition.Parameters);
            return url;
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
                        Parameters = new Dictionary<string, string>
                        {
                            { "label" , "Bedroom" },
                            { "brightness", "0.01" },
                            { "fadeInDuration", "0" },
                        },
                    }
                },
                {
                    actionTurnOnBulbName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.SetPower.ToString(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "label" , "Bedroom" },
                            { "onOffState", "on" },
                        },
                    }
                },
                {
                    actionFadeInName, new ActionDefinition
                    {
                        Url = eLifxWebApiUrl.SetBrightness.ToString(),
                        Parameters = new Dictionary<string, string>
                        {
                            { "label" , "Bedroom" },
                            { "brightness", "1" },
                            { "fadeInDuration", "900000" },
                        },
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
                    Id = this.ActionsSchedule.Select(x => x.Id).Max() + 1,
                    Day = dayOfWeek,
                    Time = actionDayTime,
                    ActionName = actionStartFadeInName,
                };
                if (dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday)
                {
                    actionSchedule.Time = actionWeekendTime;
                }
                actionSchedule.Active = true;
                this.ActionsSchedule.Add(actionSchedule);
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

            this.ActionsSchedule = new List<ActionSchedule>();
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

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
using Infrared.Impl;

namespace ActionService.Logic
{
    public class ActionProvider : IActionProvider
    {
        private const string ActionsScheduleFileName = "ActionsSchedule.json";
        private const string ActionsDefinitionsFileName = @"ActionsDefinitions.json";
        private const int GraceTimeForPerformingAction = 30;

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
        public IDictionary<ActionSchedule, DateTime> ActionHistory { get; private set; }

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
                .Where(x => !x.DaysOfWeek.Any() || x.DaysOfWeek.Contains(DateTime.Now.DayOfWeek))
                .Where(x => x.Time.TimeOfDay <= DateTime.Now.TimeOfDay)
                .Where(x => x.Time.AddMinutes(GraceTimeForPerformingAction).TimeOfDay > DateTime.Now.TimeOfDay)
                .Where(x => this.ActionsDefinitions.ContainsKey(x.ActionName))
                .Where(x => (DateTime.Now - this.ActionHistory[x]).TotalMinutes > GraceTimeForPerformingAction)
                .FirstOrDefault();
            if (actionSchedule == null)
            {
                return null;
            }

            this.Logger.Information("ActionProvider - GetNextAction - Found an action model to perform");

            if (!actionSchedule.Repeating)
            {
                actionSchedule.Active = false;
            }

            this.ActionHistory.Add(actionSchedule, DateTime.Now);

            ResetOlderActions();
            SaveActionsSchedule();

            ActionDefinition actionDefinition = this.ActionsDefinitions[actionSchedule.ActionName];
            //string fullUrl = GetFullUrl(actionDefinition);
            var actionModel = new ActionModel
            {
                Id = actionSchedule.Id,
                Name = actionSchedule.ActionName,
                Date = actionSchedule.Date,
                DaysOfWeek = actionSchedule.DaysOfWeek ?? new DayOfWeek[] { },
                Time = actionSchedule.Time,
                Repeating = actionSchedule.Repeating,
                Service = actionDefinition.Service,
                ActionId = actionDefinition.ActionId,
                Parameters = actionDefinition.Parameters,
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
                        DaysOfWeek = actionSchedule.DaysOfWeek ?? new DayOfWeek[] { },
                        Date = actionSchedule.Date,
                        Service = actionDefinition.Value.Service,
                        ActionId = actionDefinition.Value.ActionId,
                        Parameters = actionDefinition.Value.Parameters,
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


        public IEnumerable<ActionDefinitionModel> GetActions()
        {
            return this.ActionsDefinitions.Select(x => new ActionDefinitionModel
            {
                Name = x.Key,
                Service = x.Value.Service.ToString(),
                ActionId = x.Value.ActionId,
                Parameters = x.Value.Parameters,
            });
        }

        public IReadOnlyDictionary<string, IEnumerable<string>> GetSupportedActions()
        {
            var serviceActionsProvider = new ServiceActionsProvider();

            return serviceActionsProvider.GetAllActions();
        }

        public bool DefineAction(string name, string service, string actionId, IDictionary<string, string> parameters)
        {
            // if (Enum.TryParse(supportedAction, out eLifxWebApiUrl supportedActionCode))
            {

                var actionDefinition = new ActionDefinition
                {
                    ActionId = actionId,
                    Parameters = parameters,
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

            // return false;
        }

        public void ScheduleAction(ActionModel actionModel)
        {
            ScheduleAction(actionModel.Name, actionModel.Time, actionModel.DaysOfWeek, actionModel.Date, actionModel.Repeating);
        }

        public void ScheduleAction(string actionName, DateTime timeToRun, IEnumerable<DayOfWeek> daysOfweek, DateTime? specificDate, bool repeating)
        {
            Logger.Information($"ActionProvider - ScheduleAction - adding action { actionName } to memory");

            try
            {
                this.ActionsSchedule.Add(new ActionSchedule
                {
                    Id = this.ActionsSchedule.Select(x => x.Id).Max() + 1,
                    ActionName = actionName,
                    Time = timeToRun,
                    DaysOfWeek = daysOfweek,
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
                actionSchedule.DaysOfWeek = actionModel.DaysOfWeek;
            }
            actionSchedule.Time = actionModel.Time;
            actionSchedule.Active = actionModel.Active;

            Logger.Information($"ActionProvider - ModifyScheduledAction - modified action { actionModel } from schedule in memory");

            SaveActionsSchedule();

            return true;
        }
        #endregion

        private void LoadActionsDefinitions()
        {
            Logger.Information($"ActionProvider - LoadActionsDefinitions - loading actionDefinitions");

            try
            {
                string fileContent = File.ReadAllText(ActionsDefinitionsFilePath);
                this.ActionsDefinitions = JsonConvert.DeserializeObject<Dictionary<string, ActionDefinition>>(fileContent);
                foreach (KeyValuePair<string, ActionDefinition> actionDefinition in this.ActionsDefinitions)
                {
                    if (actionDefinition.Value.Parameters == null)
                    {
                        actionDefinition.Value.Parameters = new Dictionary<string, string>();
                    }
                }
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
                IList<ActionSchedule> actionsScheduleList = JsonConvert.DeserializeObject<IList<ActionSchedule>>(fileContent);
                foreach (var record in actionsScheduleList)
                {
                    record.Id = this.ActionsSchedule.Count() + 1;
                    record.DaysOfWeek = record.DaysOfWeek ?? new DayOfWeek[] { };
                    
                    this.ActionsSchedule.Add(record);
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
            List<KeyValuePair<ActionSchedule, DateTime>> oldActions = this.ActionHistory
                .Where(x => x.Value < DateTime.Now.AddMinutes(40))
                .ToList();
            foreach (var actionTime in oldActions)
            {
                this.ActionHistory.Remove(actionTime);
            }
        }

        private void ResetActions()
        {
            this.ActionHistory = new Dictionary<ActionSchedule, DateTime>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActionService.Logic;
using Infrared;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProvidersInterface;
using ProvidersInterface.Models;
using Serilog;

namespace ActionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActionController : ControllerBase
    {
        private IActionProvider ActionProvider { get; }
        private ILogger Logger { get; }

        public ActionController([FromServices] IActionProvider actionProvider, [FromServices] ILogger logger)
        {
            this.Logger = logger;
            this.Logger.Information("ActionController started");
            this.ActionProvider = actionProvider;
        }

        // api/Action/GetNext
        [HttpGet("GetNext")]
        public ActionResult<string> GetNext()
         {
            Logger.Information("ActionController - GetNextAction - next action requested");
            ActionModel actionDefinition = this.ActionProvider.GetNextScheduledAction();
            var serializedActionDefinition = JsonConvert.SerializeObject(actionDefinition);

            return new ActionResult<string>(serializedActionDefinition);
        }

        // api/Action/GetSchedule
        [HttpGet("GetSchedule")]
        public ActionResult<string> GetSchedule()
        {
            Logger.Information("ActionController - GetSchedule - full schedule requested");
            ScheduleModel schedule = this.ActionProvider.GetFullSchedule();
            var serializedschedule = JsonConvert.SerializeObject(schedule);

            return new ActionResult<string>(serializedschedule);
        }

        // api/Action/GetSchedule
        [HttpGet("GetActions")]
        public ActionResult<string> GetActions()
        {
            Logger.Information("ActionController - GetActions - actions requested");
            IEnumerable<string> definedActionsIds = this.ActionProvider.GetActions();
            var serializedschedule = JsonConvert.SerializeObject(definedActionsIds);

            return new ActionResult<string>(serializedschedule);
        }

        // api/Action/GetSchedule
        [HttpGet("GetSupportedActions")]
        public ActionResult<string> GetSupportedActions()
        {
            Logger.Information("ActionController - GetSupportedActions - supported actions requested");
            IEnumerable<string> definedActionsIds = this.ActionProvider.GetSupportedActions();
            var serializedschedule = JsonConvert.SerializeObject(definedActionsIds);

            return new ActionResult<string>(serializedschedule);
        }

        // api/Action/GetSchedule
        [HttpGet("DefineAction")]
        public ActionResult<string> DefineAction(string name, string supportedAction, string parameters)
        {
            Logger.Information($"ActionController - DefineAction - requested to create action { name } with supportedAction { supportedAction } and parameters { parameters }");
            bool success = this.ActionProvider.DefineAction(name, supportedAction, parameters);
            var serializedResult = success ? "Success" : "Fail";
            Logger.Information($"ActionController - DefineAction - defining action { name } result: { serializedResult }");
            return new ActionResult<string>(serializedResult);
        }

        // api/Action/GetSchedule
        [HttpGet("ScheduleAction")]
        public ActionResult<string> ScheduleAction(string name, string timeToRun, string dayOfWeek)
        {
            Logger.Information($"ActionController - ScheduleAction - action { name } was requested to be scheduled on { timeToRun } for day { dayOfWeek }");

            if (DateTime.TryParse(timeToRun, out DateTime parsedTimeToRun))
            {
                DayOfWeek? parsedDayOfWeek = null;
                if (dayOfWeek == null && Enum.TryParse(dayOfWeek, out DayOfWeek eDayOfWeek))
                {
                    parsedDayOfWeek = eDayOfWeek;
                    Logger.Information($"ActionController - ScheduleAction - day of week for { name } is { parsedDayOfWeek }");
                }

                this.ActionProvider.ScheduleAction(name, parsedTimeToRun, parsedDayOfWeek);

                Logger.Error($"ActionController - ScheduleAction - succeeded scheduling action { name }");

                return new ActionResult<string>("Success");
            }
            
            Logger.Error($"ActionController - ScheduleAction - failed parsing dateTime {timeToRun} for action { name }.");
            return new ActionResult<string>("Fail");
        }

        // api/Action/GetSchedule
        [HttpGet("DeleteScheduledAction")]
        public ActionResult<string> DeleteScheduledAction(int id)
        {
            Logger.Information($"ActionController - DeleteScheduledAction - requested to modify action { id }");
            bool success = this.ActionProvider.DeleteScheduledAction(id);
            //var serializedResult = success ? "Success" : "Fail";
            //Logger.Information($"ActionController - DefineAction - defining action { name } result: { serializedResult }");
            return new ActionResult<string>("");
        }

        // api/Action/GetSchedule
        [HttpGet("ModifyScheduledAction")]
        public ActionResult<string> ModifyScheduledAction(string actionModel)
        {
            Logger.Information($"ActionController - ModifyScheduledAction - requested to modify action { actionModel }");

            ActionModel deserializedModel =JsonConvert.DeserializeObject<ActionModel>(actionModel);
            bool success = this.ActionProvider.ModifyScheduledAction(deserializedModel);
            // var serializedResult = success ? "Success" : "Fail";
            // Logger.Information($"ActionController - DefineAction - defining action { name } result: { serializedResult }");
            return new ActionResult<string>("");
        }
    }
}

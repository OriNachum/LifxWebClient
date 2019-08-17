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
            Logger.Information("ActionController - GetSchedule - full schedule requested");
            IEnumerable<string> definedActionsIds = this.ActionProvider.GetActions();
            var serializedschedule = JsonConvert.SerializeObject(definedActionsIds);

            return new ActionResult<string>(serializedschedule);
        }

        // api/Action/GetSchedule
        [HttpGet("GetSupportedActions")]
        public ActionResult<string> GetSupportedActions()
        {
            Logger.Information("ActionController - GetSchedule - full schedule requested");
            IEnumerable<string> definedActionsIds = this.ActionProvider.GetSupportedActions();
            var serializedschedule = JsonConvert.SerializeObject(definedActionsIds);

            return new ActionResult<string>(serializedschedule);
        }

        // api/Action/GetSchedule
        [HttpGet("CreateAction")]
        public ActionResult<string> CreateAction(string name, string supportedAction, string parameters)
        {
            Logger.Information("ActionController - GetSchedule - full schedule requested");
            bool success = this.ActionProvider.CreateAction(name, supportedAction, parameters);
            var serializedResult = success ? "Success" : "Fail";

            return new ActionResult<string>(serializedResult);
        }

        // api/Action/GetSchedule
        [HttpGet("ScheduleAction")]
        public ActionResult<string> ScheduleAction(string name, string timeToRun, string dayOfWeek)
        {
            Logger.Information("ActionController - GetSchedule - full schedule requested");
            DateTime parsedTimeToRun = JsonConvert.DeserializeObject<DateTime>(timeToRun);
            DayOfWeek? parsedDayOfWeek = null;
            if (dayOfWeek == null && Enum.TryParse(dayOfWeek, out DayOfWeek eDayOfWeek))
            {
                parsedDayOfWeek = eDayOfWeek;
            }
            this.ActionProvider.ScheduleAction(name, parsedTimeToRun, parsedDayOfWeek);

            return new ActionResult<string>("Success");
        }
    }
}

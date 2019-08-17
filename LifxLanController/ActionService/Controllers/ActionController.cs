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

        // api/Action/ScheduleAction
        [HttpGet("ScheduleAction")]
        public ActionResult<string> ScheduleAction()
        {
            Logger.Information("ActionController - ScheduleAction - requested for a new schedule requested");
            ActionModel actionDefinition = this.ActionProvider.GetNextScheduledAction();
            var serializedActionDefinition = JsonConvert.SerializeObject(actionDefinition);

            return new ActionResult<string>(serializedActionDefinition);
        }
    }
}

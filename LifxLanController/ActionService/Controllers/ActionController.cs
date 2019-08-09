using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActionService.Logic;
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
        IActionProvider ActionProvider;

        public ILogger Logger { get; set; }

        public ActionController([FromServices] IActionProvider actionProvider, ILogger logger)
        {
            this.Logger = logger;
            this.Logger.Information("ActionController started");
            this.ActionProvider = actionProvider;
        }

        // api/Action/GetNextAction
        [HttpGet]
        public ActionResult<string> GetNextAction()
        {
            Logger.Information("ActionController - GetNextAction - next action requested");
            ActionModel actionDefinition = this.ActionProvider.GetNextScheduledAction();
            var serializedActionDefinition = JsonConvert.SerializeObject(actionDefinition);

            return new ActionResult<string>(serializedActionDefinition);
        }
    }
}

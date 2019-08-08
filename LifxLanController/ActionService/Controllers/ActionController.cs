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

        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\ActionController.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }

        public ActionController([FromServices] IActionProvider actionProvider)
        {
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

        //[HttpGet]
        //public ActionResult<string> ResolveAction()
        //{
        //    this.ActionProvider()
        //}

        // GET api/values
        //[HttpGet]
        //public ActionResult<IEnumerable<string>> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public ActionResult<string> Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/values
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}

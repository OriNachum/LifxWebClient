using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController;
using LifxCoreController.Lightbulb;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace LifxWebApi.Controllers
{
    [Route("Lifx/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private ILifxApi Lifx;

        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\ApiController.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }

        public ApiController(ILifxApi lifxApi, ILogger logger)
        {
            _logger = logger;
            Lifx = lifxApi;
        }

        // GET api/values11
        [HttpGet("Reset")]
        public ActionResult<eLifxResponse> Reset()
        {
            Logger.Information("ApiController - Reset");
            //lock (lifxLock)
            //{
            //    if (_lifx != null)
            //    {
            //        _lifx.Dispose();
            //    }

            //    _lifx = new LifxApi();
            //}
            // Added automatic refresh
            return eLifxResponse.Success;
        }

        // GET api/values
        [HttpGet("GetBulbs")]
        public async Task<ActionResult<object>> GetBulbsAsync()
        {
            Logger.Information("ApiController - GetBulbs");

            var (response, message) = await Lifx.RefreshBulbsAsync();
            if (response == eLifxResponse.Success)
            {
                IEnumerable<IBulb> ips = Lifx.Bulbs.Values.ToList();
                var serializedIps = JsonConvert.SerializeObject(ips);
                return new { responseType = (int)response, bulbs = serializedIps };
            }
            return new  { responseType = (int)response, responseData = message, bulbs = new object[0] { } };
        }

        [HttpGet("Toggle")]
        public async Task<ActionResult<object>> GetToggleLightAsyncParam(string label)
        {
            Logger.Information($"ApiController - Toggle label: { label }");

            (eLifxResponse response, string message) = await Lifx.ToggleLightAsync(label);

            IEnumerable<IBulb> ips = Lifx.Bulbs.Values.ToList();
            var serializedIps = JsonConvert.SerializeObject(ips);

            return new { responseType = (int)response, message, bulbs = serializedIps };
        }

        [HttpGet("Refresh")]
        public async Task<ActionResult<string>> GetRefreshBulbsAsync()
        {
            Logger.Information("ApiController - Refresh");

            (eLifxResponse response, string message) = await Lifx.RefreshBulbsAsync();

            return response.ToString() + ": " + message;
        }

        [HttpGet("RefreshBulb")]
        public async Task<ActionResult<object>> GetRefreshBulbAsync(string label)
        {
            Logger.Information($"ApiController - RefreshBulb({ label })");

            (eLifxResponse response, string message, string bulb) = await Lifx.RefreshBulbAsync(label);

            return new { responseType = (int)response, responseData = message, bulb };
        }

        [HttpGet("Off")]
        public async Task<ActionResult<object>> GetOffAsync(string label, int? overTime)
        {
            Logger.Information($"ApiController - Off label: { label }; overtime: { overTime }");

            var (response, data, bulb) = await Lifx.OffAsync(label, 0);
            return new { responseType = 0, responseData = data, bulb };
        }

        // GET api/values/5
        [HttpGet("On")]
        public async Task<ActionResult<object>> GetOnAsync(string label, int? overTime)
        {
            Logger.Information($"ApiController - On label: { label }; overtime: { overTime }");

            var (response, messages, bulb) = await Lifx.OnAsync(label, overTime);
            return new { responseType = (int)response, responseData = messages, bulb };
        }

        
        [HttpGet("FadeToState")]
        public async Task<ActionResult<object>> FadeToState(string label, string serializedState, long? fadeInDuration)
        {
            Logger.Information($"ApiController - FadeToState - label: { label }; state: { serializedState }; overtime: { fadeInDuration }");

            IBulbState state = JsonConvert.DeserializeObject<BulbState>(serializedState);
            Logger.Information($"ApiController - deserialized state: { state }");

            var (response, messages, bulb) = await Lifx.SetStateOverTimeAsync(label, state, fadeInDuration);
            return new { responseType = (int)response, responseData = messages, bulb };
        }

        [HttpGet("SetPower")]
        public async Task<ActionResult<string>> GetSetPowerAsync(string label, string onOffState)
        {
            bool valid = !string.IsNullOrEmpty(onOffState);
            valid = valid  && new string[] { "off", "on" }.Contains(onOffState.ToLower());
            if (onOffState != null && valid)
            {
                Power power = onOffState.ToLower().Equals("on") ? Power.On : Power.Off;
                IBulb lightBulb = Lifx.Bulbs.FirstOrDefault(x => x.Value.Label == label).Value;
                await lightBulb.SetPowerAsync(power);
            }
            return "";
        }
    }
}

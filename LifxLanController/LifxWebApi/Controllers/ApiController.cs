using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController;
using LifxCoreController.Api;
using LifxCoreController.Lightbulb;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace LifxWebApi.Controllers
{
    [EnableCors("SiteCorsPolicy")]
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
            Lifx.RefreshBulbsAsync();
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
            var (response, message) = await this.Lifx.RefreshBulbsAsync();
            if (response == eLifxResponse.Success)
            {
                IEnumerable<IBulb> ips = this.Lifx.Bulbs.Values.ToList();
                var serializedIps = JsonConvert.SerializeObject(ips);
                return new { responseType = (int)response, bulbs = serializedIps };
            }
            return new  { responseType = (int)response, responseData = message, bulbs = new object[0] { } };
        }

        [HttpGet("Toggle")]
        public async Task<ActionResult<object>> ToggleLightAsync(string label)
        {
            Logger.Information($"ApiController - Toggle label: { label }");

            (eLifxResponse response, string message) = await this.Lifx.ToggleLightAsync(label);

            IEnumerable<IBulb> ips = Lifx.Bulbs.Values.ToList();
            var serializedIps = JsonConvert.SerializeObject(ips);

            return new { responseType = (int)response, message, bulbs = serializedIps };
        }

        [HttpGet("Refresh")]
        public async Task<ActionResult<string>> RefreshBulbsAsync()
        {
            Logger.Information("ApiController - Refresh");

            (eLifxResponse response, string message) = await this.Lifx.RefreshBulbsAsync();

            return response.ToString() + ": " + message;
        }

        [HttpGet("RefreshBulb")]
        public async Task<ActionResult<object>> RefreshBulbAsync(string label)
        {
            Logger.Information($"ApiController - RefreshBulb({ label })");

            (eLifxResponse response, string message, string bulb) = await this.Lifx.RefreshBulbAsync(label);

            return new { responseType = (int)response, responseData = message, bulb };
        }

        [HttpGet("Off")]
        public async Task<ActionResult<object>> SetOffAsync(string label, int? overtime)
        {
            Logger.Information($"ApiController - SetOffAsync - label: { label }; overtime: { overtime }");

            var (response, data, bulb) = await this.Lifx.OffAsync(label, 0);
            return new { responseType = (int)response, responseData = data, bulb };
        }

        // GET api/values/5
        [HttpGet("On")]
        public async Task<ActionResult<object>> SetOnAsync(string label, int? overtime)
        {
            Logger.Information($"ApiController - SetOnAsync - label: { label }; overtime: { overtime }");

            var (response, messages, bulb) = await this.Lifx.OnAsync(label, overtime);
            return new { responseType = (int)response, responseData = messages, bulb };
        }

        [HttpGet("SetBrightness")]
        public async Task<ActionResult<object>> SetBrightnessAsync(string label, string brightness, int? overtime)
        {
            Logger.Information($"ApiController - SetBrightnessAsync - label: { label }; brightness: { brightness }; overtime: { overtime }");

            if (double.TryParse(brightness, out double parsedBrightness))
            {
                Logger.Information($"ApiController - deserialized brightness: { parsedBrightness }");

                var (response, messages, bulb) = await this.Lifx.SetBrightnessOverTimeAsync(label, parsedBrightness, overtime);
                return new { responseType = (int)response, responseData = messages, bulb };
            }
            return new { responseType = (int)eLifxResponse.BadParameter, responseData = "", bulb = "" };
        }

        [HttpGet("SetTemperature")]
        public async Task<ActionResult<object>> SetTemperatureAsync(string label, string temperature, int? overtime)
        {
            Logger.Information($"ApiController - SetTemperatureAsync - label: { label }; brightness: { temperature }; overtime: { overtime }");

            if (int.TryParse(temperature, out int parsedTemperature))
            {
                Logger.Information($"ApiController - deserialized brightness: { parsedTemperature }");

                var (response, messages, bulb) = await this.Lifx.SetTemperatureOverTimeAsync(label, parsedTemperature, overtime);
                return new { responseType = (int)response, responseData = messages, bulb };
            }
            return new { responseType = (int)eLifxResponse.BadParameter, responseData = "", bulb = "" };
        }

        [HttpGet("SetColor")]
        public async Task<ActionResult<object>> SetColorAsync(string label, string saturation, string hue, int? overtime)
        {
            Logger.Information($"ApiController - SetColorAsync - label: { label }; saturation: { saturation }; hue: { hue }; overtime: { overtime }");

            if (double.TryParse(saturation, out double parsedSaturation))
            {
                Logger.Information($"ApiController - deserialized saturation: { parsedSaturation }");
                if (int.TryParse(hue, out int parsedHue))
                {
                    Logger.Information($"ApiController - deserialized hue: { parsedHue }");

                    var (response, messages, bulb) = await this.Lifx.SetColorOverTimeAsync(label, parsedSaturation, parsedHue, overtime);
                    return new { responseType = (int)response, responseData = messages, bulb };
                }
            }
            return new { responseType = (int)eLifxResponse.BadParameter, responseData = "", bulb = "" };
        }

        [HttpGet("SetPower")]
        public async Task<ActionResult<string>> SetPowerAsync(string label, string onOffState)
        {
            Logger.Information($"ApiController - SetPowerAsync - label: { label }; onOff: {onOffState}");

            bool valid = !string.IsNullOrEmpty(onOffState);
            valid = valid  && new string[] { "off", "on" }.Contains(onOffState.ToLower());
            if (onOffState != null && valid)
            {
                Power power = onOffState.ToLower().Equals("on") ? Power.On : Power.Off;
                IBulb lightBulb = this.Lifx.Bulbs.FirstOrDefault(x => x.Value.Label == label).Value;
                await lightBulb.SetPowerAsync(power);
            }
            return "";
        }
    }
}

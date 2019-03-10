using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController;
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
        static object lifxLock = new object();
        static ILifxApi _lifx;
        static ILifxApi Lifx
        {
            get
            {
                lock(lifxLock)
                {
                    if (_lifx == null)
                    {
                        _lifx = new LifxApi();
                    }

                    return _lifx;
                }
            }
        }

        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\1.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }

        // GET api/values11
        [HttpGet("Reset")]
        public ActionResult<eLifxResponse> Reset()
        {
            Logger.Information("ApiController - Reset");
            lock (lifxLock)
            {
                if (_lifx != null)
                {
                    _lifx.Dispose();
                }

                _lifx = new LifxApi();
            }
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
                IEnumerable<LightBulb> ips = Lifx.Lights.Values.ToList();
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

            IEnumerable<LightBulb> ips = Lifx.Lights.Values.ToList();
            var serializedIps = JsonConvert.SerializeObject(ips);

            return new { responseType = (int)response, message, bulbs = serializedIps };
        }

        // GET api/values/5
        [HttpGet("Refresh")]
        public async Task<ActionResult<string>> GetRefreshBulbsAsync()
        {
            Logger.Information("ApiController - Refresh");

            (eLifxResponse response, string message) = await Lifx.RefreshBulbsAsync();

            return response.ToString() + ": " + message;
        }

        // GET api/values/5
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

        // GET api/values/5
        [HttpGet("SetBrightness")]
        public async Task<ActionResult<string>> GetSetBrightnessAsync(string label, double brightness)
        {
            Logger.Information($"ApiController - On label: { label }; brightness: { brightness }");

            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetBrightnessAsync(new Percentage(brightness));
            return "";
        }

        // GET api/values/5
        [HttpGet("SetColor")]
        public async Task<ActionResult<string>> GetSetColorAsync(string label, int hue, double saturation)
        {
            Logger.Information($"ApiController - On label: { label }; hue: { hue }; saturation: { saturation }");

            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetColorAsync(new Color(new Hue(hue), new Percentage(saturation)));
            return "";
        }


        // GET api/values/5
        [HttpGet("SetLabel")]
        public async Task<ActionResult<string>> GetSetLabelAsync(string label, string newLabel)
        {
            string EncodeUtf8(string originalString)
            {
                byte[] bytes = Encoding.Default.GetBytes(originalString);
                var encodedString = Encoding.UTF8.GetString(bytes);
                return encodedString;
            }

            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetLabelAsync(new Label(EncodeUtf8(newLabel)));
            return "";
        }

        // GET api/values/5
        [HttpGet("SetPower")]
        public async Task<ActionResult<string>> GetSetPowerAsync(string label, string onOffState)
        {
            bool valid = !string.IsNullOrEmpty(onOffState);
            valid = valid  && new string[] { "off", "on" }.Contains(onOffState.ToLower());
            if (onOffState != null && valid)
            {
                Power power = onOffState.ToLower().Equals("on") ? Power.On : Power.Off;
                LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
                await lightBulb.SetPowerAsync(power);
            }
            return "";
        }


        // GET api/values/5
        [HttpGet("SetTemperature")]
        public async Task<ActionResult<string>> GetSetTemperatureAsync(string label, int temperature)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetTemperatureAsync(new Temperature(temperature));
            return "";
        }
    }
}

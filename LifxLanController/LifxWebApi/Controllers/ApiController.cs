using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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

        // GET api/values11
        [HttpGet("Reset")]
        public ActionResult<eLifxResponse> Reset()
        {
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
        public async Task<ActionResult<Response>> GetBulbsAsync()
        {
            var (response, message) = await Lifx.RefreshBulbsAsync();
            if (response == eLifxResponse.Success)
            {
                IEnumerable<LightBulb> ips = Lifx.Lights.Values.ToList();
                var serializedIps = JsonConvert.SerializeObject(ips);
                return new Response { responseType = (int)response, responseData = serializedIps };
            }
            return new Response { responseType = (int)response, responseData = message };
        }

        [HttpGet("Toggle")]
        public async Task<ActionResult<string>> GetToggleLightAsyncParam(string label)
        {
            (eLifxResponse response, string message) = await Lifx.ToggleLightAsync(label);

            return response.ToString() + ": " + message;
        }

        // GET api/values/5
        [HttpGet("Refresh")]
        public async Task<ActionResult<string>> GetRefreshBulbsAsync()
        {
            (eLifxResponse response, string message) = await Lifx.RefreshBulbsAsync();

            return response.ToString() + ": " + message;
        }

        // GET api/values/5
        [HttpGet("Off")]
        public async Task<ActionResult<Response>> GetOffAsync(string label, , int? overTime)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.OffAsync();
            await lightBulb.GetStateAsync();
            return new Response { responseType = 0, responseData = lightBulb.Serialize() };
        }

        // GET api/values/5
        [HttpGet("On")]
        public async Task<ActionResult<Response>> GetOnAsync(string label, int? overTime)
        {
            var (response, messages) = await Lifx.OnAsync(label, overTime);
            return new Response { responseType = (int)response, responseData = messages };
        }

        // GET api/values/5
        [HttpGet("SetBrightness")]
        public async Task<ActionResult<string>> GetSetBrightnessAsync(string label, double brightness)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetBrightnessAsync(new Percentage(brightness));
            return "";
        }

        // GET api/values/5
        [HttpGet("SetColor")]
        public async Task<ActionResult<string>> GetSetColorAsync(string label, int hue, double saturation)
        {
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

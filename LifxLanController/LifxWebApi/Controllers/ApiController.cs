using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public async Task<ActionResult<(eLifxResponse, string)>> GetBulbsAsync()
        {
            var (response, message) = await Lifx.RefreshBulbsAsync();
            if (response == eLifxResponse.Success)
            {
                IEnumerable<string> ips = Lifx.Lights.Select(x => x.ToString()).ToList();
                var serializedIps = JsonConvert.SerializeObject(ips);
                return (response, serializedIps);
            }

            return (response, message);
        }

        // GET api/values/5
        [HttpGet("Toggle/{label}")]
        public async Task<ActionResult<string>> GetToggleLightAsync(string label)
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
        [HttpGet("Off/{label}")]
        public async Task<ActionResult<string>> GetOffAsync(string label)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.OffAsync();
            return "";
        }

        // GET api/values/5
        [HttpGet("On/{label}")]
        public async Task<ActionResult<string>> OnAsync(string label)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.OnAsync();
            return "";
        }

        // GET api/values/5
        [HttpGet("SetBrightness/{label}/{brightness}")]
        public async Task<ActionResult<string>> GetSetBrightnessAsync(string label, double brightness)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetBrightnessAsync(new Percentage(brightness));
            return "";
        }

        // GET api/values/5
        [HttpGet("SetColor/{label}/{Hue}/{Saturation}")]
        public async Task<ActionResult<string>> GetSetColorAsync(string label, int hue, double saturation)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetColorAsync(new Color(new Hue(hue), new Percentage(saturation)));
            return "";
        }


        // GET api/values/5
        [HttpGet("SetLabel/{label}/{newLabel}")]
        public async Task<ActionResult<string>> GetSetLabelAsync(string label, string newLabel)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetLabelAsync(new Label(newLabel));
            return "";
        }


        // GET api/values/5
        [HttpGet("SetPower/{label}/{onOffState}")]
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
        [HttpGet("SetTemperature/{label}/{temperature}")]
        public async Task<ActionResult<string>> GetSetTemperatureAsync(string label, int temperature)
        {
            LightBulb lightBulb = Lifx.Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.SetTemperatureAsync(new Temperature(temperature));
            return "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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

        /*
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}

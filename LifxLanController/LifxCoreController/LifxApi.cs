using Lifx;
using LifxCoreController.Lightbulb;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxCoreController
{
    public class LifxApi : LifxDetector, ILifxApi, IDisposable
    {
        public IEnumerable<IPAddress> LightsAddresses => Lights.Keys;

        private readonly TimeSpan REFRESH_CYCLE_SLEEP_TIME = TimeSpan.FromMinutes(1);
        private DateTime? LastRefreshTime;

        private ITimer timer;

        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\LifxApi.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }

        public LifxApi()
        {
            Task.Run(() => StartAutoRefresh(REFRESH_CYCLE_SLEEP_TIME));
        }

        public async Task<(eLifxResponse response, string data)> RefreshBulbsAsync()
        {
            var cts = new CancellationTokenSource();
            return await RefreshBulbsAsync(cts.Token);
        }

        public async Task<(eLifxResponse response, string data)> RefreshBulbsAsync(CancellationToken token)
        {
            try
            {
                Logger.Information("LifxApi - Refreshing bulbs");
                await this.DetectLightsAsync(token);

                LastRefreshTime = DateTime.Now;
                string serializedLights = JsonConvert.SerializeObject(Lights.Values);
                return (eLifxResponse.Success, serializedLights);
            }
            catch (Exception ex)
            {
                Logger.Information($"LifxApi - RefreshBulbsAsync failed { ex }");

                return (eLifxResponse.ActionFailed, ex.ToString());
            }
        }

        public async Task<eLifxResponse> SetAutoRefreshAsync(TimeSpan cycle, CancellationToken token)
        {
            try
            {
                await this.DetectLightsAsync(token);
            }
            catch (Exception ex)
            {
                return eLifxResponse.ActionFailed;
            }

            StartAutoRefresh(cycle);
            return eLifxResponse.Success;
        }

        public async Task<(eLifxResponse response, string data)> GetLightAsync(IPAddress ip)
        {
            if (IsLightListObsolete())
            {
                var (response, message) = await RefreshBulbsAsync();
                if (!response.Equals(eLifxResponse.Success))
                {
                    return (response, message);
                }
            }

            if (Lights.ContainsKey(ip))
            {
                string light = JsonConvert.SerializeObject(Lights[ip]);
                return (eLifxResponse.Success, light);
            }

            return (eLifxResponse.BulbDoesntExist, "Could not locate light by IP");
        }

        public async Task<(eLifxResponse response, string data)> GetBulbsAsync(bool refresh, CancellationToken token)
        {
            if (IsLightListObsolete() || refresh)
            {
                var (response, message) = await RefreshBulbsAsync();
                if (!response.Equals(eLifxResponse.Success))
                {
                    return (response, "Could not fetch bulbs");
                }
            }
            string bulbs = JsonConvert.SerializeObject(Lights.Values);
            return (eLifxResponse.Success, bulbs);
        }

        private void StartAutoRefresh(TimeSpan updateSleepSpan)
        {
            timer.InitializeCallback(async () => await RefreshBulbsAsync(), REFRESH_CYCLE_SLEEP_TIME);
        }

        public async Task<(eLifxResponse response, string data)> ToggleLightAsync(string label)
        {
            try
            {
                if (IsLightListObsolete() ||
                    !Lights.Values.Where(x => x.Label == label).Any())
                {
                    Thread.Sleep(100);
                    var (response, message) = await RefreshBulbsAsync();
                    Thread.Sleep(100);
                    if (!response.Equals(eLifxResponse.Success))
                    {
                        return (response, message);
                    }
                }

                LightBulb light = Lights.Values.SingleOrDefault(x => x.Label == label);

                if (light == null)
                {
                    return (eLifxResponse.BulbDoesntExist, "Couldn't locate bulb by label");
                }
                if (light.GetState().Power == Power.Off)
                {
                    await light.OnAsync(); // Start developing a new Light, expose State, expose delay on light
                }
                else
                {
                    await light.OffAsync();
                }

                return (eLifxResponse.Success, light.Serialize());
            }
            catch (Exception ex)
            {
                // return log
                return (eLifxResponse.ActionFailed, ex.ToString());
            }
        }

        public async Task<(eLifxResponse response, string data, string bulb)> OffAsync(string label, int? overTime)
        {
            Logger.Information($"LifxApi - OnAsync started light: { label }; overtime? { overTime ?? 0 }");

            LightBulb lightBulb = Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            /*if (overTime.HasValue)
            {
                return await lightBulb.OnOverTimeAsync(overTime.Value);
            }
            else
            {*/
                await lightBulb.OffAsync();
                string bulb = lightBulb.Serialize();
                return (eLifxResponse.Success, "", bulb);
            //}
        }

        public async Task<(eLifxResponse response, string data, string bulb)> OnAsync(string label, int? overTime)
        {
            Logger.Information($"LifxApi - OnAsync started light: { label }; overtime? { overTime ?? 0 }");

            LightBulb lightBulb = Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.OnOverTimeAsync(overTime.HasValue ? overTime.Value : 0);

            string bulb = lightBulb.Serialize();
            return (eLifxResponse.Success, "", bulb);
        }

        public async Task<(eLifxResponse response, string data, string bulb)> SetStateOverTimeAsync(string label, LightBulbState state, long? fadeInDuration)
        {
            Logger.Information($"LifxApi - SetStateOverTimeAsync light: { label }; overtime? { fadeInDuration }");

            LightBulb lightBulb = Lights.FirstOrDefault(x => x.Value.Label == label).Value;
            long fadeInDurationValue = fadeInDuration.HasValue ? fadeInDuration.Value : 0;
            var (response, date, bulb) = await lightBulb.SetStateOverTimeAsync(state, fadeInDurationValue);
            return (response, date, bulb);
        }

        private bool IsLightListObsolete()
        {
            return !LastRefreshTime.HasValue ||
                                DateTime.Now - LastRefreshTime > TimeSpan.FromMinutes(5);
        }

        public override void Dispose()
        {
            timer?.Dispose();
            timer = null;
            base.Dispose();
        }
    }
}

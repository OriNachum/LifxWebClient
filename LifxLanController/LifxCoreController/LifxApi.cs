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
using Infrared;

namespace LifxCoreController
{
    public class LifxApi : LifxDetector, ILifxApi, IDisposable
    {
        public IEnumerable<IPAddress> LightsAddresses => Bulbs.Keys;

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

        public LifxApi(ILogger logger) : base(logger)
        {
            _logger = logger;
            Task.Run(() => StartAutoRefresh(REFRESH_CYCLE_SLEEP_TIME));
        }

        public async Task<(eLifxResponse response, string data, string bulb)> RefreshBulbAsync(string label)
        {
            var cts = new CancellationTokenSource();
            Logger.Information($"LifxApi - RefreshBulbAsync started light: { label }; ");

            IBulb lightBulb = Bulbs?.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.GetStateAsync(cts.Token);

            string bulb = lightBulb.Serialize();
            return (eLifxResponse.Success, "", bulb);
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
                string serializedLights = JsonConvert.SerializeObject(Bulbs.Values);
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

            if (Bulbs.ContainsKey(ip))
            {
                string light = JsonConvert.SerializeObject(Bulbs[ip]);
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
            string bulbs = JsonConvert.SerializeObject(Bulbs.Values);
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
                    !Bulbs.Values.Where(x => x.Label == label).Any())
                {
                    Thread.Sleep(100);
                    var (response, message) = await RefreshBulbsAsync();
                    Thread.Sleep(100);
                    if (!response.Equals(eLifxResponse.Success))
                    {
                        return (response, message);
                    }
                }

                IBulb light = Bulbs.Values.SingleOrDefault(x => x.Label == label);

                if (light == null)
                {
                    return (eLifxResponse.BulbDoesntExist, "Couldn't locate bulb by label");
                }
                if (light.State.Power == Power.Off)
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

            IBulb lightBulb = Bulbs?.FirstOrDefault(x => x.Value.Label == label).Value;

            await lightBulb.OffAsync();
            string bulb = lightBulb.Serialize();
            return (eLifxResponse.Success, "", bulb);
        }
        
        public async Task<(eLifxResponse response, string data, string bulb)> OnAsync(string label, int? overTime)
        {
            Logger.Information($"LifxApi - OnAsync started light: { label }; overtime? { overTime ?? 0 }");

            IBulb lightBulb = Bulbs.FirstOrDefault(x => x.Value.Label == label).Value;
            await lightBulb.OnAsync();

            string bulb = lightBulb.Serialize();
            return (eLifxResponse.Success, "", bulb);
        }

        public async Task<(eLifxResponse response, string data, string bulb)> SetBrightnessOverTimeAsync(string label, double brightness, int? fadeInDuration)
        {
            Logger.Information($"LifxApi - SetStateOverTimeAsync light: { label }; brightness: { brightness }; overtime? { fadeInDuration }");

            try
            {
                IAdvancedBulb bulb = Bulbs?.FirstOrDefault(x => x.Value.Label == label).Value;
                if (bulb == null)
                {
                    Logger.Information($"LifxApi - SetStateOverTimeAsync could not find bulb: { label };");
                    return (eLifxResponse.BulbDoesntExist, $"Could not find bulb by label { label }", "");
                }

                uint fadeInDurationValue = (fadeInDuration.HasValue && fadeInDuration.Value > 0) ? (uint)fadeInDuration.Value : 0;
                double fractionBrightness = brightness / 100;
                if (fractionBrightness > Percentage.MaxValue || fractionBrightness < Percentage.MinValue)
                {
                    return (eLifxResponse.BadParameter, data: $"brightness: {brightness} should be between {Percentage.MinValue} and {Percentage.MaxValue}", "");
                }
                Percentage percentageBrightness = fractionBrightness;
                await bulb.SetBrightnessAsync(percentageBrightness, fadeInDurationValue);
                return (eLifxResponse.Success, data: "", bulb.Serialize());
            }
            catch (Exception ex)
            {
                Logger.Error($"LifxApi - SetStateOverTimeAsync Got an exception when trying to change bulb: { label }; ex: { ex }");
                return (eLifxResponse.ActionFailed, $"Failed on try to get bulb { label }, exception: { ex }", "");
            }
        }

        public async Task<(eLifxResponse response, string data, string bulb)> SetTemperatureOverTimeAsync(string label, int temperature, int? fadeInDuration)
        {
            Logger.Information($"LifxApi - SetStateOverTimeAsync light: { label }; temperature: { temperature }; overtime? { fadeInDuration }");

            try
            {
                IAdvancedBulb bulb = Bulbs?.FirstOrDefault(x => x.Value.Label == label).Value;
                if (bulb == null)
                {
                    Logger.Information($"LifxApi - SetStateOverTimeAsync could not find bulb: { label };");
                    return (eLifxResponse.BulbDoesntExist, $"Could not find bulb by label { label }", "");
                }

                uint fadeInDurationValue = (fadeInDuration.HasValue && fadeInDuration.Value > 0) ? (uint)fadeInDuration.Value : 0;

                Temperature temperatureValue = temperature;
                await bulb.SetTemperatureAsync(temperatureValue, fadeInDurationValue);
                return (eLifxResponse.Success, data: "", bulb.Serialize());
            }
            catch (Exception ex)
            {
                Logger.Error($"LifxApi - SetStateOverTimeAsync Got an exception when trying to change bulb: { label }; ex: { ex }");
                return (eLifxResponse.ActionFailed, $"Failed on try to get bulb { label }, exception: { ex }", "");
            }
        }

        public async Task<(eLifxResponse response, string data, string bulb)> SetColorOverTimeAsync(string label, double saturation, int hue, int? fadeInDuration)
        {
            Logger.Information($"LifxApi - SetStateOverTimeAsync light: { label }; saturation: { saturation }; hue: { hue }; overtime? { fadeInDuration }");

            try
            {
                IAdvancedBulb bulb = Bulbs?.FirstOrDefault(x => x.Value.Label == label).Value;
                if (bulb == null)
                {
                    Logger.Information($"LifxApi - SetStateOverTimeAsync could not find bulb: { label };");
                    return (eLifxResponse.BulbDoesntExist, $"Could not find bulb by label { label }", "");
                }

                uint fadeInDurationValue = (fadeInDuration.HasValue && fadeInDuration.Value > 0) ? (uint)fadeInDuration.Value : 0;

                Color color = new Color(hue, saturation);
                await bulb.SetColorAsync(color, fadeInDurationValue);
                return (eLifxResponse.Success, data: "", bulb.Serialize());
            }
            catch (Exception ex)
            {
                Logger.Error($"LifxApi - SetStateOverTimeAsync Got an exception when trying to change bulb: { label }; ex: { ex }");
                return (eLifxResponse.ActionFailed, $"Failed on try to get bulb { label }, exception: { ex }", "");
            }
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

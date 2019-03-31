using Lifx;
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

namespace LifxCoreController.Lightbulb
{
    public class Bulb : IBulb
    {
        [JsonIgnore]
        public ILight Light { get; }

        public string Label { get; private set; }

        public IBulbState State { get; set; }

        private LightState? _lastVerifiedState;

        [JsonIgnore]
        public LightState? LastVerifiedState
        {
            get
            {
                return _lastVerifiedState;
            }
            private set
            {
                _lastVerifiedState = value.Value;
                if (_lastVerifiedState.HasValue)
                {
                    LightState state = _lastVerifiedState.Value;
                    this.Label = state.Label.Value;
                    this.State = new BulbState(_lastVerifiedState.Value, enforceLimits: true);
                }
            }
        }

        public DateTime StateVerificationTimeUtc { get; private set; }

        [JsonIgnore]
        public IPAddress Address { get; private set; }

        public string IPv4Address
        {
            get
            {
                return string.Join('.', AddressByte);
            }
        }

        [JsonIgnore]
        public byte[] AddressByte
        {
            get
            {
                return this.Light.Address.GetAddressBytes();
            }
            set
            {
                this.Address = new IPAddress(value);
            }
        }

        public Product Product { get; private set; }

        public uint Version { get; private set; }

        public const int BulbsReliefTimeMilliseconds = 200;
        #region Logger
        private ILogger _logger = null;

        protected ILogger Logger
        {
            get
            {
                if (_logger == null && !string.IsNullOrEmpty(Label))
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\{ Label }.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }
        #endregion

        public Bulb(ILight light, LightState state, ILogger logger = null)
        {
            if (logger != null)
            {
                _logger = logger;
            }

            this.Light = light;

            this.Address = light.Address;
            this.Product = light.Product;
            this.Version = light.Version;

            this.LastVerifiedState = state;
            this.StateVerificationTimeUtc = DateTime.UtcNow;

            Logger.Information($"LifxBulb - Registered new light: { this }");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Name: { Label }; ");
            sb.Append($"IP: { IPv4Address }; ");
            sb.Append($"Product: { Product }; ");
            sb.Append($"Version: { Version }; ");
            sb.Append($"State: { State }; ");
            sb.Append($"LastVerifiedStateTime: { StateVerificationTimeUtc.ToShortTimeString() } UTC; ");
            sb.Append($"");
            return sb.ToString();
        }

        #region Serialization
        public string Serialize()
        {
            string serializedLightBulb = JsonConvert.SerializeObject(this);
            return serializedLightBulb;
        }

        public static Bulb Deserialized(string serializedBulb)
        {
            Bulb light = JsonConvert.DeserializeObject<Bulb>(serializedBulb);
            return light;
        }
        #endregion

        #region ILight
        public async Task<LightState> GetStateAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                return await this.GetStateAsync(cts.Token);
            }
        }

        public async Task<LightState> GetStateAsync(CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.GetState, cts))
                {
                    LightState newState = await this.Light.GetStateAsync(cts.Token);

                    // Thread.Sleep(100); ? 
                    this.LastVerifiedState = newState;
                    this.StateVerificationTimeUtc = DateTime.UtcNow;
                    Logger.Information($"LifxBulb - state refreshed: { this.State }");
                    return newState;
                }
            }
        }

        public async Task SetLabelAsync(Label label)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetLabelAsync(label, cts.Token);
            }
        }

        public async Task SetLabelAsync(Label label, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetLabel, cts))
                {
                    await this.Light.SetLabelAsync(label, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                    return;
                }
            }
        }

        public async Task SetPowerAsync(Power power)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetPowerAsync(power, cts.Token);
            }
        }

        public async Task SetPowerAsync(Power power, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetPower, cts))
                {
                    await this.Light.SetPowerAsync(power, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetPowerAsync(Power power, uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetPowerAsync(power, durationInMilliseconds, cts.Token);
            }
        }

        public async Task SetPowerAsync(Power power, uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetPower, cts))
                {
                    await this.Light.SetPowerAsync(power, durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        protected async Task<bool> VerifyBulbState(Func<Bulb, bool> condition)
        {
            if (condition(this))
            {
                await GetStateAsync();
                return condition(this);
            }

            return false;
        }

        public async Task OffAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.OffAsync(cts.Token);
            }
        }

        public async Task OffAsync(CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.Off, cts))
                {
                    Logger.Information($"LifxBulb - { Label } - Turning light off");
                    await this.Light.OffAsync(cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task OffAsync(uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.OffAsync(durationInMilliseconds, cts.Token);
            }
        }

        public async Task OffAsync(uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.Off, cts))
                {
                    Logger.Information($"LifxBulb - { Label } - Turning light off for { durationInMilliseconds / 1000 } seconds");
                    await this.Light.OffAsync(durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task OnAsync()
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.OnAsync(cts.Token);
            }
        }

        public async Task OnAsync(CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.On, cts))
                {
                    Logger.Information($"LifxBulb - { Label } - Turning light on");

                    await this.Light.OnAsync(cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task OnAsync(uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.OnAsync(durationInMilliseconds, cts.Token);
            }
        }

        public async Task OnAsync(uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.On, cts))
                {
                    Logger.Information($"LifxBulb - { Label } - Turning light on for { durationInMilliseconds / 1000 } seconds");
                    await this.Light.OnAsync(durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetBrightnessAsync(Percentage brightness)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetBrightnessAsync(brightness, cts.Token);
            }
        }

        public async Task SetBrightnessAsync(Percentage brightness, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetBrightness, cts))
                {
                    if (this.State.Brightness == brightness.Value)
                    {
                        await this.GetStateAsync();
                        if (this.State.Brightness == brightness.Value)
                        {
                            return;
                        }
                    }

                    Logger.Information($"LifxBulb - SetBrightnessAsync - Setting brightness to { brightness.Value.ToString() }");
                    await this.Light.SetBrightnessAsync(brightness, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    int count = 0;
                    Logger.Information($"LifxBulb - SetBrightnessAsync - request sent - verifying");
                    while (count < 3 && 
                        await this.VerifyBulbState((bulb) => bulb.State.Brightness.Equals(brightness)) is bool stateNotChanged && 
                        stateNotChanged)
                    {
                        Thread.Sleep(BulbsReliefTimeMilliseconds);
                    }
                    Logger.Information($"LifxBulb - SetBrightnessAsync - Brightness is set to { this.State.Brightness }");

                }
            }
        }

        public async Task SetBrightnessAsync(Percentage brightness, uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetBrightnessAsync(brightness, durationInMilliseconds, cts.Token);
            }
        }

        public async Task SetBrightnessAsync(Percentage brightness, uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetBrightness, cts))
                {
                    Logger.Information($"LifxBulb - OnOverTimeAsync - Changing brightness to: { brightness.Value * 100 }%");
                    await this.Light.SetBrightnessAsync(brightness, durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetTemperatureAsync(Temperature temperature)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetTemperatureAsync(temperature, cts.Token);
            }
        }

        public async Task SetTemperatureAsync(Temperature temperature, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetTemperature, cts))
                {
                    Logger.Information($"LifxBulb - OnOverTimeAsync - Changing temperature to: { temperature.Value }");
                    await this.Light.SetTemperatureAsync(temperature, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetTemperatureAsync(Temperature temperature, uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetTemperatureAsync(temperature, durationInMilliseconds, cts.Token);
            }
        }

        public async Task SetTemperatureAsync(Temperature temperature, uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetTemperature, cts))
                {
                    Logger.Information($"LifxBulb - OnOverTimeAsync - Changing temperature to: { temperature.Value }");
                    await this.Light.SetTemperatureAsync(temperature, durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetColorAsync(Color color)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetColorAsync(color, cts.Token);
            }
        }

        public async Task SetColorAsync(Color color, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetColor, cts))
                {
                    Logger.Information($"LifxBulb - OnOverTimeAsync - Changing hue to: { color.Hue.Value }; saturation to: { color.Saturation.Value * 100 }%");
                    await this.Light.SetColorAsync(color, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public async Task SetColorAsync(Color color, uint durationInMilliseconds)
        {
            using (var cts = new CancellationTokenSource())
            {
                await this.SetColorAsync(color, durationInMilliseconds, cts.Token);
            }
        }

        public async Task SetColorAsync(Color color, uint durationInMilliseconds, CancellationToken cancellationToken)
        {
            using (var cts = new CancellationTokenSource())
            {
                cancellationToken.Register(cts.Cancel);
                using (RaiseCommandRequested(eLifxCommand.SetColor, cts))
                {
                    Logger.Information($"LifxBulb - OnOverTimeAsync - Changing hue to: { color.Hue.Value }; saturation to: { color.Saturation.Value * 100 }%");
                    await this.Light.SetColorAsync(color, durationInMilliseconds, cts.Token);
                    Thread.Sleep(BulbsReliefTimeMilliseconds);
                    await this.GetStateAsync(cts.Token);
                }
            }
        }

        public void Dispose()
        {
            if (Light != null)
            {
                this.Light.Dispose();
            }
        }
        #endregion


        public bool IsCurrentState(IBulbState state)
        {
            Logger.Information($"LifxBulb - IsCurrentState - comparing to { state }");

            if (!this.LastVerifiedState.HasValue)
            {
                return false;
            }
            var currentState = new BulbState(this.LastVerifiedState.Value, enforceLimits: false);
            Logger.Information($"LifxBulb - IsCurrentState - current state is { currentState }");
            return currentState.Equals(state);
        }

        #region RaiseEvents
        object lockObject = new object();

        IDictionary<DateTime, CommandRequest> RequestedCommands = new Dictionary<DateTime, CommandRequest>();
        const int MaximumAllowedCommandsInFrame = 5;
        private const int GapBetweenCommandMilliseconds = BulbsReliefTimeMilliseconds;
        TimeSpan CommandRequestMaxLifeTime = TimeSpan.FromSeconds(30);

        private LifxCommandCallback RaiseCommandRequested(eLifxCommand lifxCommand, CancellationTokenSource cancellationTokenSource)
        {
            Logger.Information($"LifxBulb - RaiseCommandRequested - requested { lifxCommand.ToString() }");
            DateTime requestTime = DateTime.UtcNow;
            lock (lockObject)
            {
                CancelAndClearOldRequests(requestTime);

                // Verified queue not full
                if (RequestedCommands.Count() > MaximumAllowedCommandsInFrame)
                {
                    throw new Exception("Maximum commands requested.");
                }


                // Add request to queue
                AddRequestToQueue(lifxCommand, cancellationTokenSource, requestTime);
                Thread.Sleep(GapBetweenCommandMilliseconds);
                // Technically nothing else should be done after this. 
                // The rest should be picked up by a runner.
                // After the runner is done - it will remove the command from queue
            }

            return new LifxCommandCallback(() =>
            {
                lock (lockObject)
                {
                    RequestedCommands.Remove(requestTime);
                }
            });
        }

        private void AddRequestToQueue(eLifxCommand lifxCommand, CancellationTokenSource cancellationTokenSource, DateTime requestTime)
        {
            var commandRequest = new CommandRequest
            {
                Command = lifxCommand,
                CancelTokenSource = cancellationTokenSource,
            };

            RequestedCommands.Add(requestTime, commandRequest);
        }

        private void CancelAndClearOldRequests(DateTime requestTime)
        {
            IEnumerable<(DateTime Key, CancellationTokenSource TokenSource)> oldCommandsCancelletionTokens = RequestedCommands
                .Where(x => requestTime - x.Key > CommandRequestMaxLifeTime)
                .Select(x => (x.Key, x.Value.CancelTokenSource))
                .ToList();

            foreach ((DateTime Key, CancellationTokenSource TokenSource) command in oldCommandsCancelletionTokens)
            {
                command.TokenSource.Cancel();
                RequestedCommands.Remove(command.Key);
            }
        }
        #endregion


    }
}

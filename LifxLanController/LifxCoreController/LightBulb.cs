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

namespace LifxCoreController
{
    public class LightBulb : ILightBulb
    {
        [JsonIgnore]
        public ILight Light { get; }

        public string Label { get; private set; }
        public Power Power { get; private set; }
        public int Temperature { get; private set; }
        public double Brightness { get; private set; }
        public int ColorHue { get; private set; }
        public double ColorSaturation { get; private set; }

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
                    this.Power = state.Power;
                    this.Temperature = state.Temperature.Value;
                    this.Brightness = state.Brightness.Value;
                    this.ColorHue = state.Color.Hue.Value;
                    this.ColorSaturation = state.Color.Saturation.Value;
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

        private TimeSpan IdlePeriod = TimeSpan.FromSeconds(15);
        private TimeSpan WorkingPeriod = TimeSpan.FromMilliseconds(100);
        private ITimer actionRunner;
        private IProducerConsumerCollection<Func<Task>> actionQueue = new ConcurrentQueue<Func<Task>>();
        object actionQueueLock = new object();
        private async Task CheckActionQueueAsync()
        {
            Logger.Information($"LifxBulb - CheckActionQueue - { actionQueue.Count() } items in action queue");
            if (actionQueue.Any() && actionQueue.TryTake(out Func<Task> unqueuedAction))
            {
                await unqueuedAction();
            }
            else
            {
                actionRunner.Reset(IdlePeriod);
            }
        }
        #region Logger
        private ILogger _logger = null;

        private ILogger Logger
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

        public LightBulb(ILight light, LightState state, ILogger logger = null)
        {
            this.Light = light;

            this.Address = light.Address;
            this.Product = light.Product;
            this.Version = light.Version;

            this.LastVerifiedState = state;
            this.StateVerificationTimeUtc = DateTime.UtcNow;

            if (logger != null)
            {
                _logger = logger;
            }

            Logger.Information($"LifxBulb - { Label } - Registered Light");

            actionRunner = new GapBasedTimer(CheckActionQueueAsync, IdlePeriod, Logger);
            actionRunner.InitializeCallback(CheckActionQueueAsync, IdlePeriod);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Name: { Label }; ");
            sb.Append($"IP: { IPv4Address }");
            sb.Append($"Product: { Product }; ");
            sb.Append($"Version: { Version }; ");
            sb.Append($"Power: { Power }; ");
            sb.Append($"Temperature: { Temperature }; ");
            sb.Append($"Brightness: { Brightness }; ");
            sb.Append($"ColorHue: { ColorHue }; ");
            sb.Append($"ColorSaturation: { ColorSaturation }; ");
            sb.Append($"LastVerifiedState: { StateVerificationTimeUtc.ToShortTimeString() }; ");
            sb.Append($"");
            return sb.ToString();
        }

        #region Serialization
        public string Serialize()
        {
            string serializedLightBulb = JsonConvert.SerializeObject(this);
            return serializedLightBulb;
        }

        public static LightBulb Deserialized(string serializedBulb)
        {
            LightBulb light = JsonConvert.DeserializeObject<LightBulb>(serializedBulb);
            return light;
        }
        #endregion


        #region ILightBulb
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
                    this.LastVerifiedState = newState;
                    this.StateVerificationTimeUtc = DateTime.UtcNow;
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
                    this.Label = label.Value;
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
                    this.Power = power;
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
                    this.Power = power;
                }
            }
        }

        private async Task<bool> VerifyBulbState(Func<LightBulb, bool> condition)
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
                    this.Power = Power.Off;
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
                    this.Power = Power.Off;
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
                    this.Power = Power.On;
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
                    this.Power = Power.On;
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
                    if (this.Brightness == brightness.Value)
                    {
                        await this.GetStateAsync();
                        if (this.Brightness == brightness.Value)
                        {
                            return;
                        }
                    }

                    Logger.Information($"LifxBulb - SetBrightnessAsync - Setting brightness to { brightness.Value.ToString() }");
                    await this.Light.SetBrightnessAsync(brightness, cts.Token);
                    await this.GetStateAsync();
                    Logger.Information($"LifxBulb - SetBrightnessAsync - Brightness is set to { this.Brightness }");
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
                    await this.Light.SetBrightnessAsync(brightness, durationInMilliseconds, cts.Token);
                    this.Brightness = brightness.Value;
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
                    await this.Light.SetTemperatureAsync(temperature, cts.Token);
                    this.Temperature = temperature.Value;
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
                    await this.Light.SetTemperatureAsync(temperature, durationInMilliseconds, cts.Token);
                    this.Temperature = temperature.Value;
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
                    await this.Light.SetColorAsync(color, cts.Token);
                    this.ColorHue = color.Hue;
                    this.ColorSaturation = color.Saturation;
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
                    await this.Light.SetColorAsync(color, durationInMilliseconds, cts.Token);
                    this.ColorHue = color.Hue;
                    this.ColorSaturation = color.Saturation;
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

        /// <summary>
        /// Brings the lightbulb to 100% over duration of time
        /// </summary>
        /// <param name="overTime">time in seconds for bulb to reach 100%</param>
        /// <returns></returns>
        public async Task<(eLifxResponse response, string data)> OnOverTimeAsync(int overTime = 0)
        {
            if (overTime == 0)
            {
                await this.OnAsync();
                string serializedBulb = this.Serialize();
                return (eLifxResponse.Success, serializedBulb);
            }
            else if (overTime < 0)
            {
                string msg = $"LifxBulb - OnOverTimeAsync - Can't turn on over { overTime } seconds";
                Logger.Information(msg);
                return (eLifxResponse.NoChange, msg);
            }
            Logger.Information($"LifxBulb - OnOverTimeAsync - Starting On Over Time operation");
            Func<LightBulb, bool> condition = (x) => (x.Power == Power.On && x.Brightness == 1);
            var sameCondition = await VerifyBulbState(condition);
            if (sameCondition)
            {
                string serializedBulb = this.Serialize();
                return ((int)eLifxResponse.Success, serializedBulb);
            }
            double initialBrightness = this.Power == Power.Off ? 0 : this.Brightness;
            if (this.Power == Power.Off)
            {
                await this.SetBrightnessAsync(new Percentage(initialBrightness));
                await this.OnAsync();
            }

            double singleStepProgression = CalculateSingleStep(overTime, initialBrightness);
            // double nextBrightness = Math.Min(1, initialBrightness + singleStepProgression);

            Logger.Information($"LifxBulb - OnOverTimeAsync - Initializing callback first time; step size: { singleStepProgression }");
            Logger.Information($"LifxBulb - OnOverTimeAsync - actionQueueSize = { actionQueue.Count() }");

            Logger.Information($"LifxBulb - OnOverTimeAsync - Enqueuing action to run");
            Func<Task> nextAction = async () => await OnOverTimeCallbackAsync(singleStepProgression);
            actionQueue.TryAdd(nextAction);

            Logger.Information($"LifxBulb - OnOverTimeAsync - Reseting runner to work period");
            actionRunner.Reset(WorkingPeriod);

            return (eLifxResponse.Success, this.Serialize());
        }

        private double CalculateSingleStep(int overTime, double initialBrightness)
        {
            double overTimeInMiliseconds = 1000.0 * overTime;
            double numberOfStepsToComplete = overTimeInMiliseconds / WorkingPeriod.TotalMilliseconds;
            double totalBrightnessGap = (1.0 - initialBrightness);
            double singleStepProgression = totalBrightnessGap / WorkingPeriod.TotalMilliseconds;
            singleStepProgression = Math.Round(singleStepProgression, 4, MidpointRounding.ToEven);
            singleStepProgression = Math.Max(singleStepProgression, 0.01);
            singleStepProgression = Math.Min(singleStepProgression, 1);

            return singleStepProgression;
        }

        private async Task OnOverTimeCallbackAsync(double singleStepProgression)
        {
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Increasing brightness by { singleStepProgression }");

            double nextBrightness = Math.Min(1, this.Brightness + singleStepProgression);
            nextBrightness = Math.Round(nextBrightness, 4, MidpointRounding.ToEven);
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Increasing brightness to { nextBrightness } as part of OverTime operation");
            var nextBrightnessPercentage = new Percentage(nextBrightness);
            await this.SetBrightnessAsync(nextBrightnessPercentage);
            await this.GetStateAsync();

            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - brightness set to { nextBrightnessPercentage } and value is: { this.Brightness }. Should queue more? { this.Brightness < 1.0 }");
            if (this.Brightness < 1.0)
            {
                Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - setting to increase brightness by: { singleStepProgression }");
                Func<Task> nextAction = async () => await OnOverTimeCallbackAsync(singleStepProgression);
                actionQueue.TryAdd(nextAction);
                Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Enqueued new action. Total now: { actionQueue.Count() }");
            }
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - done");
        } 
        #endregion

        #region RaiseEvents
        object lockObject = new object();

        IDictionary<DateTime, CommandRequest> RequestedCommands = new Dictionary<DateTime, CommandRequest>();
        const int MaximumAllowedCommandsInFrame = 5;
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
                Thread.Sleep(100);
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

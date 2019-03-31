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

        private TimeSpan IdlePeriod = TimeSpan.FromSeconds(5);
        private const int BulbsReliefTimeMilliseconds = 200;
        private TimeSpan WorkingPeriod = TimeSpan.FromMilliseconds(100);
        private ITimer actionRunner;
        private IProducerConsumerCollection<Func<Task>> actionQueue = new ConcurrentQueue<Func<Task>>();
        object actionQueueLock = new object();
        private async Task CheckActionQueueAsync()
        {
            Logger.Information($"LifxBulb - CheckActionQueue - { actionQueue.Count() } items in action queue");
            if (actionQueue.Any() && actionQueue.TryTake(out Func<Task> unqueuedAction))
            {
                try
                {
                    await unqueuedAction();
                }
                catch (Exception ex)
                {
                    _logger.Error($"LightBulb - CheckActionQueueAsync - unqueuedAction throw an exception: { ex }");
                    throw;
                }
            }
            else
            {
                await this.GetStateAsync();
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

            actionRunner = new GapBasedTimer(CheckActionQueueAsync, IdlePeriod, Logger);
            actionRunner.InitializeCallback(CheckActionQueueAsync, IdlePeriod);
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

        private async Task<bool> VerifyBulbState(Func<Bulb, bool> condition)
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

        #region OnOverTime
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
            Func<Bulb, bool> condition = (x) => (x.State.Power == Power.On && x.State.Brightness == 1);
            var sameCondition = await VerifyBulbState(condition);
            if (sameCondition)
            {
                string serializedBulb = this.Serialize();
                return ((int)eLifxResponse.Success, serializedBulb);
            }
            double initialBrightness = this.State.Power == Power.Off ? 0 : this.State.Brightness.Value;
            Logger.Information($"LifxBulb - OnOverTimeAsync - bulb power is: { this.State.Power.ToString() }, initial brightness: { initialBrightness }");
            if (this.State.Power == Power.Off)
            {
                await this.SetBrightnessAsync(new Percentage(initialBrightness));
                await this.OnAsync();
            }

            double singleStepProgression = CalculateSingleStep(overTime, initialBrightness, 1.0);
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
        private double CalculateSingleStep(long overTime, double initialBrightness, double endBrightness)
        {
            double overTimeInMiliseconds = 1000.0 * overTime;
            double numberOfStepsToComplete = overTimeInMiliseconds / WorkingPeriod.TotalMilliseconds;
            double totalBrightnessGap = (endBrightness - initialBrightness);
            double singleStepProgression = totalBrightnessGap / WorkingPeriod.TotalMilliseconds;
            singleStepProgression = Math.Round(singleStepProgression, 4, MidpointRounding.ToEven);
            singleStepProgression = Math.Max(singleStepProgression, 0.01);
            singleStepProgression = Math.Min(singleStepProgression, 1);

            return singleStepProgression;
        }
        private async Task OnOverTimeCallbackAsync(double singleStepProgression)
        {
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Increasing brightness by { singleStepProgression }");

            double nextBrightness = Math.Min(1, this.State.Brightness.Value + singleStepProgression);
            nextBrightness = Math.Round(nextBrightness, 4, MidpointRounding.ToEven);
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Increasing brightness to { nextBrightness } as part of OverTime operation");
            var nextBrightnessPercentage = new Percentage(nextBrightness);
            await this.SetBrightnessAsync(nextBrightnessPercentage);
            // await this.GetStateAsync();

            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - brightness set to { nextBrightnessPercentage } and value is: { this.State.Brightness }. Should queue more? { this.State.Brightness < 1.0 }");
            if (this.State.Brightness < 1.0)
            {
                Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - setting to increase brightness by: { singleStepProgression }");
                Func<Task> nextAction = async () => await OnOverTimeCallbackAsync(singleStepProgression);
                actionQueue.TryAdd(nextAction);
                Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - Enqueued new action. Total now: { actionQueue.Count() }");
            }
            Logger.Information($"LifxBulb - OnOverTimeCallbackAsync - done");
        }
        #endregion

        #region StateOverTime
        /// <summary>
        /// Brings the lightbulb to 100% over duration of time
        /// BUG: End condition not met, continues indefenitely.
        /// TODO: Change from 'step' to 'function(currentTime)' which will be generated in SetStateOverTimeAsync
        /// </summary>
        /// <param name="fadeInDuration">time in miliseconds for bulb to reach state</param>
        /// <returns></returns>
        public async Task<(eLifxResponse response, string message, string bulb)> SetStateOverTimeAsync(IBulbState state, long fadeInDuration = 0)
        {
            Logger.Information($"LifxBulb - SetStateOverTimeAsync - fadeInDuration: { fadeInDuration }; state: { state }");

            if (fadeInDuration == 0)
            {
                await this.SetStateAsync(state);
                string serializedBulb = this.Serialize();
                return (eLifxResponse.Success, "Immediate success", serializedBulb);
            }
            else if (fadeInDuration < 0)
            {
                string msg = $"LifxBulb - SetStateOverTimeAsync - Can't turn on over { fadeInDuration } seconds";
                Logger.Information(msg);
                string serializedBulb = this.Serialize();
                return (eLifxResponse.NoChange, msg, serializedBulb);
            }

            Logger.Information($"LifxBulb - SetStateOverTimeAsync - Verifying bulb state");
            var sameCondition = await VerifyBulbState(LightBulb => LightBulb.IsCurrentState(state));
            if (sameCondition)
            {
                Logger.Information($"LifxBulb - SetStateOverTimeAsync - state already applied");
                string serializedBulb = this.Serialize();
                return ((int)eLifxResponse.Success, "No change", serializedBulb);
            }
            IBulbState initialState = new BulbState(LastVerifiedState.Value, enforceLimits: true);
            Logger.Information($"LifxBulb - SetStateOverTimeAsync - initial state is: { initialState }");
            if (this.State.Power == Power.Off)
            {
                await this.SetBrightnessAsync(new Percentage(0));
                await this.OnAsync();
            }

            int stateStepAccuracy = 10000;
            IBulbState singleStepStateProgression = CalculateSingleStateStep(fadeInDuration / stateStepAccuracy, initialState, state);

            Logger.Information($"LifxBulb - SetStateOverTimeAsync - Step is: { singleStepStateProgression }");
            IBulbState startState = initialState.Copy();

            DateTime setStateStartTime = DateTime.UtcNow;
            Func<IBulbState> GetNextState = () =>
            {
                Logger.Information($"LifxBulb - GetNextState - Calculating next state, base step is { singleStepStateProgression }");

                double durationSinceStart = (DateTime.UtcNow - setStateStartTime).TotalMilliseconds;
                Logger.Information($"LifxBulb - GetNextState - time passed since start: { durationSinceStart }ms");

                double factor = durationSinceStart / stateStepAccuracy;
                Logger.Information($"LifxBulb - GetNextState - factor to multiply: { factor }ms");

                IBulbState stateDifference = BulbState.MultiplyState(singleStepStateProgression, factor, enforceLimits: false);
                Logger.Information($"LifxBulb - GetNextState - add : { stateDifference } to start state { startState }");

                IBulbState newStateToSet = BulbState.AddStates(startState, stateDifference, enforceLimits: true);
                Logger.Information($"LifxBulb - GetNextState - new state should be: { newStateToSet }");

                return newStateToSet;
            };

            DateTime endTime = setStateStartTime.AddMilliseconds(fadeInDuration);
            Logger.Information($"LifxBulb - OnOverTimeAsync - Enqueuing action to run");
            Func<Task> nextAction = async () => await SetStateOverTimeCallbackAsync(GetNextState, state, endTime);
            actionQueue.TryAdd(nextAction);

            Logger.Information($"LifxBulb - OnOverTimeAsync - Reseting runner to work period");
            actionRunner.Reset(WorkingPeriod);

            return (eLifxResponse.Success, "Success", this.Serialize());
        }

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

        private IBulbState CalculateSingleStateStep(long overtime, IBulbState initialState, IBulbState endState)
        {
            Logger.Information($"LifxBulb - CalculateSingleStateStep - initial state is { initialState }");
            Logger.Information($"LifxBulb - CalculateSingleStateStep - end state is: { endState }");
            Logger.Information($"LifxBulb - CalculateSingleStateStep - overtime: { overtime }");

            //double numberOfMilliseconds = Math.Max(1, overtime / WorkingPeriod.TotalMilliseconds);
            //Logger.Information($"LifxBulb - IsCurrentState - fadeInTime: { numberOfMilliseconds }");

            IBulbState singleStepState = BulbState.SubtractState(initialState, endState, enforceLimits: false);
            Logger.Information($"LifxBulb - CalculateSingleStateStep - difference state is { singleStepState }");

            singleStepState = BulbState.DivideStep(singleStepState, overtime, enforceLimits: false);
            Logger.Information($"LifxBulb - CalculateSingleStateStep - divided state is { singleStepState }");

            return singleStepState;
        }

        private async Task<LightState> SetStateAsync(IBulbState state)
        {
            await this.GetStateAsync();
            LightState lastVerifiedState = this.LastVerifiedState.Value;
            if (state.Power.HasValue && this.State.Power != state.Power)
            {
                await this.SetPowerAsync(state.Power.Value);
            }

            if (state.Brightness.HasValue && this.State.Brightness != state.Brightness)
            {
                await this.SetBrightnessAsync(state.Brightness.Value);
            }

            if ((state.Hue.HasValue && this.State.Hue != state.Hue) ||
                (state.Saturation.HasValue && this.State.Saturation != state.Saturation))
            {
                var color = new Color(state.Hue.Value, state.Saturation.Value);
                await this.SetColorAsync(color);
            }

            if (state.Temperature.HasValue && this.State.Temperature != state.Temperature)
            {
                await this.SetTemperatureAsync(state.Temperature.Value);
            }
            lastVerifiedState = this.LastVerifiedState ?? this.LastVerifiedState.Value;

            return lastVerifiedState;
        }

        private async Task SetStateOverTimeCallbackAsync(Func<IBulbState> getNextState, IBulbState finalState, DateTime endTime)
        {
            IBulbState nextState = getNextState();
            bool finalStep = DateTime.UtcNow >= endTime;
            if (DateTime.UtcNow >= endTime)
            {
                nextState = finalState;
            }

            Logger.Information($"LifxBulb - SetStateOverTimeCallbackAsync - changing state to { nextState } as part of OverTime operation");
            int numberOfTries = finalStep ? 3 : 1; 
            while (numberOfTries > 0)
            {
                try
                {
                    LightState newState = await this.SetStateAsync(nextState);
                    var newBulbState = new BulbState(newState, enforceLimits: false);
                    Logger.Information($"LifxBulb - SetStateOverTimeCallbackAsync - new state is: { newBulbState }");
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    Logger.Warning($"LifxBulb - SetStateOverTimeCallbackAsync - attempt { numberOfTries } to set state: { nextState } failed");
                    numberOfTries -= 1;
                    Thread.Sleep(1000);
                }
            }

            if (!finalStep)
            {
                //Logger.Information($"LifxBulb - SetStateOverTimeCallbackAsync - setting to increase brightness by: { singleStepProgressionState }");
                Func<Task> nextAction = async () => await SetStateOverTimeCallbackAsync(getNextState, finalState, endTime);
                actionQueue.TryAdd(nextAction);
                Logger.Information($"LifxBulb - SetStateOverTimeCallbackAsync - Enqueued new action. Total now: { actionQueue.Count() }");
            }
            Logger.Information($"LifxBulb - SetStateOverTimeCallbackAsync - done");
        }
        #endregion        
        #endregion

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

using Lifx;
using Newtonsoft.Json;
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
    public class LightBulb : ILight
    {
        [JsonIgnore]
        public ILight Light { get; }

        public string Label { get; private set; }
        public Power Power { get; private set; }
        public int Temperature { get; private set; }
        public double Brightness { get; private set; }
        public int ColorHue { get; private set; }
        public double ColorSaturation { get; private set; }

        public LightState? LastVerifiedState
        {
            get;
            private set;
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

        public LightBulb(ILight light, LightState state)
        {
            this.Light = light;

            this.Address = light.Address;
            this.Product = light.Product;
            this.Version = light.Version;

            this.Label = state.Label.Value;
            this.Power = state.Power;
            this.Temperature = state.Temperature.Value;
            this.Brightness = state.Brightness.Value;
            this.ColorHue = state.Color.Hue.Value;
            this.ColorSaturation = state.Color.Saturation.Value;
            this.LastVerifiedState = state;
            this.StateVerificationTimeUtc = DateTime.UtcNow;
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
        
        /// <summary>
        /// Brings the lightbulb to 100% over duration of time
        /// </summary>
        /// <param name="overTime">time in seconds for bulb to reach 100%</param>
        /// <returns></returns>
        public async Task<(eLifxResponse response, string data)> OnOverTimeAsync(int overTime)
        {
            Func<LightBulb, bool> condition = (x) => (x.Power == Power.On && x.Brightness == 1);
            var sameCondition = await VerifyBulbState(condition);
            if (sameCondition)
            {
                string serializedBulb = this.Serialize();
                return ((int)eLifxResponse.Success, serializedBulb);
            }
            double initialBrightness = this.Brightness;
            double nextBrightness = initialBrightness + SINGLE_PERCENT;
            await this.SetBrightnessAsync(new Percentage(nextBrightness));
            
            // Export this part to runner, use 'overTime' to calculate progression
            while (this.Brightness != 1) 
            {
                nextBrightness += SINGLE_PERCENT;
                var nextBrightnessPercentage = new Percentage(nextBrightness);
                Thread.Sleep(1000);
                await this.SetBrightnessAsync(nextBrightnessPercentage);
            }
            // Add runner to keep going on "a pace"
            // Add a runner to run in loops and send messages to the bulb.
            // This will just load it with commands, and the runner will run them.
            // Queue will be SortedDictionary by run time.

            // await lightBulb.GetStateAsync();
            return (eLifxResponse.Success, this.Serialize());
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
                using (RaiseCommandRequested(eLifxCommand.Off, cts))
                {
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
                    await this.Light.SetBrightnessAsync(brightness, cts.Token);
                    this.Brightness = brightness.Value;
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

        #region RaiseEvents
        object lockObject = new object();

        IDictionary<DateTime, CommandRequest> RequestedCommands = new Dictionary<DateTime, CommandRequest>();
        const int MaximumAllowedCommandsInFrame = 5;
        TimeSpan CommandRequestMaxLifeTime = TimeSpan.FromSeconds(30);
        private const double SINGLE_PERCENT = 0.01;

        private LifxCommandCallback RaiseCommandRequested(eLifxCommand lifxCommand, CancellationTokenSource cancellationTokenSource)
        {
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

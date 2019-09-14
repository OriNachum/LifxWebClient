using Lifx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LifxCoreController.Lightbulb
{
    public class BulbState : IBulbState
    {
        public BulbState()
        {
            EnforceLimits = false;
        }

        public BulbState(bool enforceLimits)
        {
            EnforceLimits = enforceLimits;
        }

        public BulbState(LightState state, bool enforceLimits) : this(enforceLimits)
        {
            Power = state.Power;
            Brightness = state.Brightness;
            Hue = state.Color.Hue;
            Saturation = state.Color.Saturation;
            Temperature = state.Temperature;

        }

        public Power? Power { get; set; }

        #region Bulb Properties
        private double? _brightness;
        public double? Brightness
        {
            get
            {
                return _brightness;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _brightness = FilterBetweenLimits(Percentage.MinValue, value.Value, Percentage.MaxValue);
                }
                else
                {
                    _brightness = value;
                }
            }
        }

        // Min: 2500; Max: 9000
        private int? _temperature;
        public int? Temperature
        {
            get
            {
                return _temperature;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _temperature = (int)FilterBetweenLimits(Lifx.Temperature.MinValue, value.Value, Lifx.Temperature.MaxValue);
                }
                else
                {
                    _temperature = value;
                }
            }
        }

        private int? _hue;
        public int? Hue
        {
            get
            {
                return _hue;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _hue = (int)FilterBetweenLimits(Lifx.Hue.MinValue, value.Value, Lifx.Hue.MaxValue);
                }
                else
                {
                    _hue = value;
                }
            }
        }

        private double? _saturation;
        public double? Saturation
        {
            get
            {
                return _saturation;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _saturation = FilterBetweenLimits(Percentage.MinValue, value.Value, Percentage.MaxValue);

                }
                else
                {
                    _saturation = value;
                }
            }
        }

        [JsonIgnore]
        public bool EnforceLimits { get; }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj is BulbState state)
            {
                if (this.Power.HasValue && state.Power.HasValue && 
                    this.Power.Value != state.Power.Value) return false;

                if (this.Brightness.HasValue && state.Brightness.HasValue && 
                    this.Brightness.Value != state.Brightness.Value) return false;

                if (this.Hue.HasValue && state.Hue.HasValue && 
                    this.Hue.Value != state.Hue.Value) return false;

                if (this.Saturation.HasValue && state.Saturation.HasValue && 
                    this.Saturation.Value != state.Saturation.Value) return false;

                if (this.Temperature.HasValue && state.Temperature.HasValue &&
                    this.Temperature.Value != state.Temperature.Value) return false;

                return true;
            }

            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"Power: { this.Power.ToString() }; ")
              .Append($"Brightness: { this.Brightness * 100 }%; ")
              .Append($"Color Hue: { this.Hue }; ")
              .Append($"Color Saturation: { this.Saturation * 100 }%; ")
              .Append($"Temperature: { this.Temperature }");

            return sb.ToString();
        }

        private double? FilterBetweenLimits(double minValue, double value, double maxValue)
        {
            if (value < minValue) return minValue;
            if (value > maxValue) return maxValue;
            return value;
        }

        public static BulbState AddStates(IBulbState baseState, IBulbState addedState, bool enforceLimits)
        {
            var combinedState = new BulbState(enforceLimits);

            if (addedState.Brightness.HasValue)
            {
                combinedState.Brightness = baseState.Brightness + addedState.Brightness;
                combinedState.Power = combinedState.Brightness == Percentage.MinValue ? Lifx.Power.Off : Lifx.Power.On;
            }
            if (addedState.Hue.HasValue)
            {
                combinedState.Hue = baseState.Hue + addedState.Hue;
            }
            if (addedState.Saturation.HasValue)
            {
                combinedState.Saturation = baseState.Saturation + addedState.Saturation;
            }
            if (addedState.Temperature.HasValue)
            {
                combinedState.Temperature = baseState.Temperature + addedState.Temperature;
            }

            return combinedState;
        }

        public static BulbState DivideStep(IBulbState state, double dividor, bool enforceLimits)
        {
            int accuracy = 2;
            var resultState = new BulbState(enforceLimits);
            if (state.Brightness.HasValue)
            {
                resultState.Brightness = Math.Round(state.Brightness.Value / dividor, accuracy);
                if (resultState.Brightness == 0 && state.Brightness.Value > 0) resultState.Brightness = 0.01;
            }
            if (state.Hue.HasValue)
            {
                resultState.Hue = (int)Math.Ceiling(state.Hue.Value / dividor);
            }
            if (state.Saturation.HasValue)
            {
                resultState.Saturation = Math.Round(state.Saturation.Value / dividor, accuracy);
                if (resultState.Saturation == 0 && state.Saturation.Value > 0) resultState.Saturation = 0.01;
            }
            if (state.Temperature.HasValue)
            {
                resultState.Temperature = (int)Math.Ceiling(state.Temperature.Value / dividor);
            }
            return resultState;
        }

        public static BulbState SubtractState(IBulbState initialState, IBulbState endState, bool enforceLimits)
        {
            var resultState = new BulbState(enforceLimits);

            if (endState.Brightness.HasValue)
            {
                resultState.Brightness = endState.Brightness.Value - initialState.Brightness.Value;
            }
            if (endState.Hue.HasValue)
            {
                resultState.Hue = endState.Hue.Value - initialState.Hue.Value;
            }
            if (endState.Saturation.HasValue)
            {
                resultState.Saturation = endState.Saturation.Value - initialState.Saturation.Value;
            }
            if (endState.Temperature.HasValue)
            {
                resultState.Temperature = endState.Temperature.Value - initialState.Temperature.Value;
            }

            return resultState;
        }

        public IBulbState Copy(bool? enforceLimits = null)
        {
            return new BulbState(enforceLimits ?? EnforceLimits)
            {
                Power = this.Power,
                Brightness = this.Brightness,
                Hue = this.Hue,
                Saturation = this.Saturation,
                Temperature = this.Temperature,
            };
        }

        public static BulbState MultiplyState(IBulbState state, double factor, bool enforceLimits)
        {
            int accuracy = 2;
            var resultState = new BulbState(enforceLimits);
            if (state.Brightness.HasValue)
            {
                resultState.Brightness = Math.Round(state.Brightness.Value * factor, accuracy);
                if (resultState.Brightness == 0 && state.Brightness.Value > 0) resultState.Brightness = 0.01;
            }
            if (state.Hue.HasValue)
            {
                resultState.Hue = (int)Math.Ceiling(state.Hue.Value * factor);
            }
            if (state.Saturation.HasValue)
            {
                resultState.Saturation = Math.Round(state.Saturation.Value * factor, accuracy);
                if (resultState.Saturation == 0 && state.Saturation.Value > 0) resultState.Saturation = 0.01;
            }
            if (state.Temperature.HasValue)
            {
                resultState.Temperature = (int)Math.Ceiling(state.Temperature.Value * factor);
            }
            return resultState;
        }

    }
}

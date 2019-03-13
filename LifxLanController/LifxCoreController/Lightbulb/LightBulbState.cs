using Lifx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LifxCoreController.Lightbulb
{
    public class LightBulbState : ILightBulbState
    {
        public LightBulbState()
        {
            EnforceLimits = false;
        }

        public LightBulbState(bool enforceLimits)
        {
            EnforceLimits = enforceLimits;
        }

        public LightBulbState(LightState state, bool enforceLimits) : this(enforceLimits)
        {
            Power = state.Power;
            Brightness = state.Brightness;
            ColorHue = state.Color.Hue;
            ColorSaturation = state.Color.Saturation;
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

        private int? _colorHue;
        public int? ColorHue
        {
            get
            {
                return _colorHue;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _colorHue = (int)FilterBetweenLimits(Hue.MinValue, value.Value, Hue.MaxValue);
                }
                else
                {
                    _colorHue = value;
                }
            }
        }

        public double? _colorSaturation;
        public double? ColorSaturation
        {
            get
            {
                return _colorSaturation;
            }
            set
            {
                if (value.HasValue && EnforceLimits)
                {
                    _colorSaturation = FilterBetweenLimits(Percentage.MinValue, value.Value, Percentage.MaxValue);

                }
                else
                {
                    _colorSaturation = value;
                }
            }
        }

        [JsonIgnore]
        public bool EnforceLimits { get; }
        #endregion

        public override bool Equals(object obj)
        {
            if (obj is LightBulbState state)
            {
                if (this.Power.HasValue && state.Power.HasValue && 
                    this.Power.Value != state.Power.Value) return false;

                if (this.Brightness.HasValue && state.Brightness.HasValue && 
                    this.Brightness.Value != state.Brightness.Value) return false;

                if (this.ColorHue.HasValue && state.ColorHue.HasValue && 
                    this.ColorHue.Value != state.ColorHue.Value) return false;

                if (this.ColorSaturation.HasValue && state.ColorSaturation.HasValue && 
                    this.ColorSaturation.Value != state.ColorSaturation.Value) return false;

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
              .Append($"Color Hue: { this.ColorHue }; ")
              .Append($"Color Saturation: { this.ColorSaturation * 100 }%; ")
              .Append($"Temperature: { this.Temperature }");

            return sb.ToString();
        }

        private double? FilterBetweenLimits(double minValue, double value, double maxValue)
        {
            if (value < minValue) return minValue;
            if (value > maxValue) return maxValue;
            return value;
        }

        public static LightBulbState AddStates(LightBulbState baseState, LightBulbState addedState, bool enforceLimits)
        {
            var combinedState = new LightBulbState(enforceLimits);

            if (addedState.Brightness.HasValue)
            {
                combinedState.Brightness = baseState.Brightness + addedState.Brightness;
                combinedState.Power = combinedState.Brightness == Percentage.MinValue ? Lifx.Power.Off : Lifx.Power.On;
            }
            if (addedState.ColorHue.HasValue)
            {
                combinedState.ColorHue = baseState.ColorHue + addedState.ColorHue;
            }
            if (addedState.ColorSaturation.HasValue)
            {
                combinedState.ColorSaturation = baseState.ColorSaturation + addedState.ColorSaturation;
            }
            if (addedState.Temperature.HasValue)
            {
                combinedState.Temperature = baseState.Temperature + addedState.Temperature;
            }

            return combinedState;
        }

        public static LightBulbState DivideStep(LightBulbState state, double dividor, bool enforceLimits)
        {
            int accuracy = 2;
            var resultState = new LightBulbState(enforceLimits);
            if (state.Brightness.HasValue)
            {
                resultState.Brightness = Math.Round(state.Brightness.Value / dividor, accuracy);
                if (resultState.Brightness == 0 && state.Brightness.Value > 0) resultState.Brightness = 0.01;
            }
            if (state.ColorHue.HasValue)
            {
                resultState.ColorHue = (int)Math.Ceiling(state.ColorHue.Value / dividor);
            }
            if (state.ColorSaturation.HasValue)
            {
                resultState.ColorSaturation = Math.Round(state.ColorSaturation.Value / dividor, accuracy);
                if (resultState.ColorSaturation == 0 && state.ColorSaturation.Value > 0) resultState.ColorSaturation = 0.01;
            }
            if (state.Temperature.HasValue)
            {
                resultState.Temperature = (int)Math.Ceiling(state.Temperature.Value / dividor);
            }
            return resultState;
        }

        public static LightBulbState SubtractState(LightBulbState initialState, LightBulbState endState, bool enforceLimits)
        {
            var resultState = new LightBulbState(enforceLimits);

            if (endState.Brightness.HasValue)
            {
                resultState.Brightness = endState.Brightness.Value - initialState.Brightness.Value;
            }
            if (endState.ColorHue.HasValue)
            {
                resultState.ColorHue = endState.ColorHue.Value - initialState.ColorHue.Value;
            }
            if (endState.ColorSaturation.HasValue)
            {
                resultState.ColorSaturation = endState.ColorSaturation.Value - initialState.ColorSaturation.Value;
            }
            if (endState.Temperature.HasValue)
            {
                resultState.Temperature = endState.Temperature.Value - initialState.Temperature.Value;
            }

            return resultState;
        }

        public LightBulbState Copy(bool? enforceLimits = null)
        {
            return new LightBulbState(enforceLimits ?? EnforceLimits)
            {
                Power = this.Power,
                Brightness = this.Brightness,
                ColorHue = this.ColorHue,
                ColorSaturation = this.ColorSaturation,
                Temperature = this.Temperature,
            };
        }

        public static LightBulbState MultiplyState(LightBulbState state, double factor, bool enforceLimits)
        {
            int accuracy = 2;
            var resultState = new LightBulbState(enforceLimits);
            if (state.Brightness.HasValue)
            {
                resultState.Brightness = Math.Round(state.Brightness.Value * factor, accuracy);
                if (resultState.Brightness == 0 && state.Brightness.Value > 0) resultState.Brightness = 0.01;
            }
            if (state.ColorHue.HasValue)
            {
                resultState.ColorHue = (int)Math.Ceiling(state.ColorHue.Value * factor);
            }
            if (state.ColorSaturation.HasValue)
            {
                resultState.ColorSaturation = Math.Round(state.ColorSaturation.Value * factor, accuracy);
                if (resultState.ColorSaturation == 0 && state.ColorSaturation.Value > 0) resultState.ColorSaturation = 0.01;
            }
            if (state.Temperature.HasValue)
            {
                resultState.Temperature = (int)Math.Ceiling(state.Temperature.Value * factor);
            }
            return resultState;
        }

    }
}

using Lifx;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
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
    public class AdvancedBulb : Bulb, IAdvancedBulb
    {

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
                    Logger.Error($"LightBulb - CheckActionQueueAsync - unqueuedAction throw an exception: { ex }");
                    throw;
                }
            }
            else
            {
                await this.GetStateAsync();
                actionRunner.Reset(IdlePeriod);
            }
        }

        private ITimer actionRunner;
        private TimeSpan IdlePeriod = TimeSpan.FromSeconds(5);
        private TimeSpan WorkingPeriod = TimeSpan.FromMilliseconds(100);


        public AdvancedBulb(ILight light, LightState state, ILogger logger = null) : base (light, state, logger)
        {
            actionRunner = new GapBasedTimer(CheckActionQueueAsync, IdlePeriod, Logger);
            actionRunner.InitializeCallback(CheckActionQueueAsync, IdlePeriod);
        }

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
    }
}

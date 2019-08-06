using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrared.Impl
{
    /// <summary>
    /// A timer that instead of running tasks by an interval, keeps a certain gap between tasks. 
    ///  (From end of execution to the start of next)
    /// </summary>
    public class GapBasedTimer : ITimer
    {
        private readonly TimeSpan NEVER = TimeSpan.FromMilliseconds(-1);
        private Timer timer = null;
        TimeSpan timerSleepSpan;
        Func<Task> timerCallbackAsync;
        private ILogger _logger;

        public GapBasedTimer(Func<Task> callback = null, TimeSpan? sleepSpan = null, ILogger logger = null)
        {
            _logger = logger;
            ResetTimerProperties(callback, sleepSpan);
        }

        private void ResetTimerProperties(Func<Task> callback, TimeSpan? sleepSpan)
        {
            _logger?.Information("GapBasedTimer - ResetTimerProperties - started");
            if (callback != null)
            {
                this.timerCallbackAsync = callback;
            }
            if (sleepSpan.HasValue)
            {
                timerSleepSpan = sleepSpan.Value;
            }
        }

        public void InitializeCallback(Func<Task> callback, TimeSpan sleepSpan)
        {
            _logger?.Information("GapBasedTimer - InitializeCallback - calling action");

            ResetTimerProperties(callback, sleepSpan);

            async Task timerCallBackWrapperAsync(object state)
            {
                ScheduleTimerToRun(NEVER);
                _logger?.Information("GapBasedTimer - timerCallBackWrapper - calling action");
                try
                {
                    await timerCallbackAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error($"GapBasedTimer - InitializeCallback - timerCallbackAsync tried to raise callback and throw an exception: { ex }");
                    throw;
                }
                ScheduleTimerToRun(timerSleepSpan);
            }

            _logger?.Information("GapBasedTimer - InitializeCallback - Running first time");

            // Set Timer
            timer?.Dispose();
            _logger?.Information("GapBasedTimer - InitializeCallback - wrapping action with timer");
            timer = new Timer(async (state) => await timerCallBackWrapperAsync(state));

            _logger?.Information("GapBasedTimer - InitializeCallbackAsync - Scheduling timer for next run");
            ScheduleTimerToRun(timerSleepSpan);
        }

        public void Pause()
        {
            _logger?.Information("GapBasedTimer - Pause");
            ScheduleTimerToRun(NEVER);
        }

        public void Reset(TimeSpan? sleepSpan = null)
        {
            _logger?.Information("GapBasedTimer - Reset");

            if (sleepSpan.HasValue)
            {
                timerSleepSpan = sleepSpan.Value;
            }

            ScheduleTimerToRun(timerSleepSpan);
        }
        
        void ScheduleTimerToRun(TimeSpan timerSleepSpanForNextRun)
        {
            _logger?.Information($"GapBasedTimer - ScheduleTimerToRun time for next run: {timerSleepSpanForNextRun}");
            timer.Change(timerSleepSpanForNextRun, NEVER);
        }

        #region Dispose
        public void Dispose()
        {
            timer?.Dispose();
            timer = null;
        }
        #endregion
    }
}

using Infrared;
using Infrared.Impl;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ProvidersInterface.Impl;
using ProvidersInterface;
using ProvidersInterface.Enums;

namespace Bishop
{
    public class BishopEngine : IDisposable
    {
        ITimer timer;
        private readonly TimeSpan SleepTime = TimeSpan.FromSeconds(1);
        ILogger _logger;
        IActionProvider _actionProvider;


        public BishopEngine(IActionProvider actionProvider, ILogger logger)
        {
            _logger = logger;
            _actionProvider = actionProvider;
        }

        internal void Start()
        {
            Func<Task> GenerateNextCycleAction = () => new Task(NextCycleAction);
            timer = new GapBasedTimer(GenerateNextCycleAction, SleepTime, _logger);

            // Loop queries database (Refresh) and asks for state
        }


        private void NextCycleAction()
        {
            IActionProvider actionProvider = _actionProvider ?? new ActionProvider();
            (eNextActionResult result, Action action) = actionProvider.GetNextAction();
            if (result == eNextActionResult.NoNextAction)
            {
                return;
            }

            try
            {
                action();
                actionProvider.SetCurrentActionState(eActionState.Success);
            }
            catch (Exception ex)
            {
                actionProvider.SetCurrentActionState(eActionState.Fail);
            }
        }

        #region IDisposable
        public void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
            }
        } 
        #endregion

    }
}

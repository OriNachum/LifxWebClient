﻿using Infrared;
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
    public class BishopEngine : IBishopEngine
    {
        ITimer timer;
        private readonly TimeSpan SleepTime = TimeSpan.FromSeconds(5);
        ILogger _logger;
        IActionProvider _actionProvider;


        public BishopEngine(IActionProvider actionProvider, ILogger logger)
        {
            _logger = logger;
            _actionProvider = actionProvider ?? new ActionProvider();
        }

        public void Start()
        {
            Func<Task> GenerateNextCycleAction = NextCycleAction;
            timer = new GapBasedTimer(GenerateNextCycleAction, SleepTime, _logger);
            timer.InitializeCallback(GenerateNextCycleAction, SleepTime);
            // wait NextCycleAction();
            // Loop queries database (Refresh) and asks for state
        }


        private async Task NextCycleAction()
        {
            IActionProvider actionProvider = _actionProvider;
            Func<Task> action = actionProvider.GetNextAction();
            if (action == null)
            {
                return;
            }

            try
            {
                await action();
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

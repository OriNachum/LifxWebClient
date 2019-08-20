using Infrared;
using Infrared.Impl;
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
            Logger.Information($"LifxBulb - CheckActionQueueAsync - { actionQueue.Count() } items in action queue");
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
                LightState? newState = await this.GetStateAsync();
                if (!newState.HasValue)
                {
                    await this.ResetBulbAsync();
                }
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
    }
}

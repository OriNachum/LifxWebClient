using System;
using System.Threading.Tasks;

namespace LifxCoreController
{
    interface ITimer : IDisposable
    {

        /// <summary>
        /// Initializes timer's callback action and sleepSpan.
        /// Upon initialization, callback is ran once
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="sleepSpan"></param>
        void InitializeCallback(Func<Task> callback, TimeSpan sleepSpan);

        /// <summary>
        /// Resets timer to stop running until another reset occurs.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resets the timer's period cycle
        /// If no sleepSpan given - the old one is kept, and current cycle delayed.
        /// </summary>
        /// <param name="sleepSpan"></param>
        void Reset(TimeSpan? sleepSpan = null);
    }
}
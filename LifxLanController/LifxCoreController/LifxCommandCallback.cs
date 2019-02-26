using System;

namespace LifxCoreController
{
    internal class LifxCommandCallback : IDisposable
    {
        private Action Callback;

        public LifxCommandCallback(Action callback)
        {
            this.Callback = callback;
        }

        public void Dispose()
        {
            Callback();
        }
    }
}
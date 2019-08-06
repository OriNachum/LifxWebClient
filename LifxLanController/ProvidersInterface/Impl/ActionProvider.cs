using ProvidersInterface.Enums;
using System;

namespace ProvidersInterface.Impl
{
    public class ActionProvider : IActionProvider
    {
        public (eNextActionResult result, Action action) GetNextAction()
        {
            throw new NotImplementedException();
        }

        public void SetCurrentActionState(eActionState success)
        {
            throw new NotImplementedException();
        }

        public void QueueState(Action action)
        {
            throw new NotImplementedException();
        }

        public void ScheduleAction(Action action, ActionSchedule actionSchedule)
        {
            throw new NotImplementedException();
        }
    }
}
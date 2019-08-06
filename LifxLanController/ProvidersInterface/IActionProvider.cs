using ProvidersInterface.Enums;
using System;

namespace ProvidersInterface
{
    public interface IActionProvider
    {
        (eNextActionResult result, Action action) GetNextAction();

        void SetCurrentActionState(eActionState success);

        void QueueState(Action action);

        void ScheduleAction(Action action, ActionSchedule actionSchedule);
    }
}
using ProvidersInterface.Enums;
using System;
using System.Threading.Tasks;

namespace ProvidersInterface
{
    public interface IActionProvider
    {
        Func<Task> GetNextAction();

        void SetCurrentActionState(eActionState success);

        void QueueState(Action action);

        void ScheduleAction(Action action, ActionSchedule actionSchedule);
    }
}
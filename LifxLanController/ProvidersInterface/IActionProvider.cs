using ProvidersInterface.Enums;
using ProvidersInterface.Models;
using System;
using System.Threading.Tasks;

namespace ProvidersInterface
{
    public interface IActionProvider
    {
        ActionModel GetNextScheduledAction();
        ScheduleModel GetFullSchedule();

        void SetCurrentActionState(eActionState success);

        void QueueState(Action action);

        void ScheduleAction(Action action, ActionScheduleModel actionSchedule);
    }
}
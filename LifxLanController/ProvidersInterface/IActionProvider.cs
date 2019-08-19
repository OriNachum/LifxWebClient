using ProvidersInterface.Enums;
using ProvidersInterface.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProvidersInterface
{
    public interface IActionProvider
    {
        ActionModel GetNextScheduledAction();
        ScheduleModel GetFullSchedule();
        IEnumerable<string> GetActions();
        IEnumerable<string> GetSupportedActions();
        bool DefineAction(string name, string supportedAction, string parameters);

        void ScheduleAction(string actionName, DateTime timeToRun, DayOfWeek? dayOfweek);
    }
}
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
        IEnumerable<ActionDefinitionModel> GetActions();
        IReadOnlyDictionary<string, IEnumerable<string>> GetSupportedActions();
        bool DefineAction(string name, string service, string actionId, IDictionary<string, string> parameters);

        void ScheduleAction(ActionModel actionModel);
        bool DeleteScheduledAction(int id);
        bool ModifyScheduledAction(ActionModel deserializedModel);
    }
}
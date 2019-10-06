using Infrared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrared.Impl
{
    public class ServiceActionsProvider
    {
        private readonly IReadOnlyDictionary<string, IEnumerable<string>> serviceToActions;
        public ServiceActionsProvider()
        {
            serviceToActions = new Dictionary<string, IEnumerable<string>>
            {
                { eService.LifxWebApi.ToString(), Enum.GetNames(typeof(eLifxWebApiUrl)) },
            };
        }

        public IReadOnlyDictionary<string, IEnumerable<string>> GetAllActions()
        {
            return serviceToActions;
        }
    }
}

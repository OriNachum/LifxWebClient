using Lifx;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LifxLanController
{
    public interface ILifxApi : ILifxDetector, IDisposable
    {
        IEnumerable<IPAddress> LightsAddresses { get; }
        /// <summary>
        /// Gets a list of ips representing each bulb
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        Task<IEnumerable<ILight>> GetBulbsAsync(bool refresh, CancellationToken token);

        Task<eLifxResponse> RefreshBulbsAsync();

        Task<eLifxResponse> RefreshBulbsAsync(CancellationToken token);

        Task<eLifxResponse> SetAutoRefreshAsync(TimeSpan cycle, CancellationToken token);

        void DisableAutoRefresh(CancellationToken token);

        (eLifxResponse response, ILight) GetLightAsync(IPAddress ip);
    }
}
using Lifx;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LifxCoreController
{
    public interface ILifxApi : ILifxDetector, IDisposable
    {
        IEnumerable<IPAddress> LightsAddresses { get; }
        /// <summary>
        /// Gets a list of ips representing each bulb
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        Task<IEnumerable<LightBulb>> GetBulbsAsync(bool refresh, CancellationToken token);

        Task<(eLifxResponse, string)> RefreshBulbsAsync();

        Task<(eLifxResponse, string)> RefreshBulbsAsync(CancellationToken token);

        Task<eLifxResponse> SetAutoRefreshAsync(TimeSpan cycle, CancellationToken token);

        void DisableAutoRefresh(CancellationToken token);

        Task<(eLifxResponse response, string)> GetLightAsync(IPAddress ip);
        Task<(eLifxResponse, string)> ToggleLightAsync(string label);
    }
}
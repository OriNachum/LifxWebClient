using Lifx;
using LifxCoreController.Lightbulb;
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
        Task<(eLifxResponse response, string data)> GetBulbsAsync(bool refresh, CancellationToken token);

        Task<(eLifxResponse response, string data)> RefreshBulbsAsync();

        Task<(eLifxResponse response, string data, string bulb)> RefreshBulbAsync(string label);

        Task<(eLifxResponse response, string data)> RefreshBulbsAsync(CancellationToken token);

        Task<eLifxResponse> SetAutoRefreshAsync(TimeSpan cycle, CancellationToken token);

        Task<(eLifxResponse response, string data)> GetLightAsync(IPAddress ip);
        Task<(eLifxResponse response, string data)> ToggleLightAsync(string label);
        Task<(eLifxResponse response, string data, string bulb)> OnAsync(string label, int? fadeDuration);
        Task<(eLifxResponse response, string data, string bulb)> OffAsync(string label, int? fadeDuration);
        Task<(eLifxResponse response, string data, string bulb)> SetStateOverTimeAsync(string label, IBulbState state, long? fadeInDuration);
        Task<(eLifxResponse response, string data, string bulb)> SetBrightnessOverTimeAsync(string label, double brightness, int? fadeInDuration);
    }
}
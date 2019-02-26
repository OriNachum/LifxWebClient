using Lifx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxLanController
{
    public class LifxApi : LifxDetector, ILifxApi
    {
        public IEnumerable<IPAddress> LightsAddresses => Lights.Keys;

        public LifxApi() { }

        public async Task<eLifxResponse> RefreshBulbsAsync()
        {
            var cts = new CancellationTokenSource();
            return await RefreshBulbsAsync(cts.Token);
        }

        public async Task<eLifxResponse> RefreshBulbsAsync(CancellationToken token)
        {
            try
            {
                await this.DetectLights(token);
                return eLifxResponse.Success;
            }
            catch (Exception ex)
            {
                return eLifxResponse.RefreshFailed;
            }
        }

        public async Task<eLifxResponse> SetAutoRefreshAsync(TimeSpan cycle, CancellationToken token)
        {
            try
            {
                await this.DetectLights(token);
            }
            catch (Exception ex)
            {
                return eLifxResponse.RefreshFailed;
            }

            StartAutoRefresh(cycle);
            return eLifxResponse.Success;
        }

        public void DisableAutoRefresh(CancellationToken token)
        {
            StopAutoRefresh();
        }

        public (eLifxResponse response, ILight) GetLightAsync(IPAddress ip)
        {
            if (Lights.ContainsKey(ip))
            {
                return (eLifxResponse.Success, Lights[ip]);
            }

            return (eLifxResponse.BulbDoesntExist, null);
        }

        public async Task<IEnumerable<ILight>> GetBulbsAsync(bool refresh, CancellationToken token)
        {
            if (refresh)
            {
                await RefreshBulbsAsync(token);
            }

            return Lights.Values;
        }


        private void StartAutoRefresh(object cycle)
        {
            throw new NotImplementedException();
        }

        private void StopAutoRefresh()
        {
            throw new NotImplementedException();
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lifx;
using LifxLanController;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace LifxLanControllerTest
{
    [TestClass]
    public class LifxApiTest
    {
        [TestMethod]
        public void CreateServerTest()
        {
            var server = new LifxApi();
            Assert.IsNotNull(server);
        }

        [TestMethod]
        public async Task DetectNearbyDevices_Success()
        {
            // Assign
            var knownIp = new IPAddress(new byte[] { 192, 168, 1, 11 });
            using (var server = new LifxDetector())
            {
                // Act
                IEnumerable<IPAddress> allIpsQuery = await server.GetAllIpsInNetworkAsync();
                IEnumerable<IPAddress> allIps = allIpsQuery.ToList();
                // Assert
                Assert.IsTrue(allIps.Count() > 0);
                Assert.IsTrue(allIps.Contains(knownIp));
            }
        }

        [TestMethod]
        public async Task LightBulbFadeIn_Success()
        {
            // Assign
            var cts = new CancellationTokenSource();
            var knownIp = new IPAddress(new byte[] { 192, 168, 1, 11 });
            using (var server = new LifxDetector())
            {
                // Act
                await server.DetectLights(cts.Token);
                foreach (ILight light in server.Lights.Values)
                {
                    uint second = 1000;
                    uint minute = second * 60;
                    uint minutes5 = minute * 5;
                    await light.OnAsync(minutes5);
                }

                // Assert
                Assert.IsTrue(server.Lights.Count > 0);
            }
        }
    }
}

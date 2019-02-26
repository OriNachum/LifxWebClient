
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LifxLanController;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace LifxLanControllerTest
{
    [TestClass]
    public class LifxDetectorTest
    {
        [TestMethod]
        public void CreateServerTest()
        {
            var server = new LifxDetector();
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
        public async Task DetectLights_Success()
        {
            // Assign
            using (var server = new LifxDetector())
            {
                var cancellationTokenSource = new CancellationTokenSource();
                // Act
                await server.DetectLights(cancellationTokenSource.Token);

                // Assert
                Assert.IsTrue(server.Lights.Count() > 0);
            }
        }
    }
}

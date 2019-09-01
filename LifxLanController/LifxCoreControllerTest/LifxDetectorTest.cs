
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LifxCoreController;
using NSubstitute;
using Serilog;
using Xunit;

namespace LifxCoreControllerTest
{
    public class LifxDetectorTest
    {
        ILogger _logger;

        public LifxDetectorTest()
        {
            _logger = Substitute.For<ILogger>();
        }


        [Fact]
        public void CreateServerTest()
        {
            var server = new LifxDetector(_logger);
            Assert.NotNull(server);
        }

        [Fact]
        public async Task DetectNearbyDevices_Success()
        {
            // Assign
            var knownIp = new IPAddress(new byte[] { 192, 168, 1, 11 });
            using (var server = new LifxDetector(_logger))
            {
                // Act
                IEnumerable<IPAddress> allIpsQuery = await server.GetAllIpsInNetworkAsync();
                IEnumerable<IPAddress> allIps = allIpsQuery.ToList();
                // Assert
                Assert.True(allIps.Count() > 0);
                Assert.Contains(knownIp, allIps);
            }
        }

        [Fact]
        public async Task DetectLights_Success()
        {
            // Assign
            using (var server = new LifxDetector(_logger))
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                // Act
                await server.DetectLightsAsync(cancellationTokenSource.Token);

                // Assert
                Assert.True(server.Bulbs.Count() > 0);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController;
using LifxCoreController.Lightbulb;
using NSubstitute;
using Serilog;
using Xunit;

namespace LifxCoreControllerTest
{
    public class LifxApiTest
    {
        ILogger _logger;

        public LifxApiTest()
        {
            _logger =  Substitute.For<ILogger>();
        }


        [Fact]
        public void CreateServerTest()
        {
            var server = new LifxApi(_logger);
            Assert.NotNull(server);
        }

        [Fact]
        public async Task DetectNearbyDevices_Success()
        {
            // Assign
            var knownIp = new IPAddress(new byte[] { 10, 0, 0, 3 });
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
        public async Task LightBulbFadeIn_Success()
        {
            // Assign
            var cts = new CancellationTokenSource();
            var knownIp = new IPAddress(new byte[] { 10, 0, 0, 4 });
            using (var server = new LifxDetector(_logger))
            {
                // Act
                await server.DetectLightsAsync(cts.Token);
                IAdvancedBulb light = server.Bulbs.Values
                    .Where(x => x.Label == "Television")
                    .FirstOrDefault();
                Assert.NotNull(light);
                // foreach (ILight light in server.Bulbs.Values)
                {
                    uint second = 1000;
                    uint minute = second * 60;
                    uint minutes = minute * 10;

                    Power newPower = (light.State.Power.Value == Power.On) ? Power.Off : Power.On;
                    Percentage newBrightness = 0.01;

                    //await light.SetPowerAsync(newPower, second);
                    await light.SetBrightnessAsync(newBrightness, second);
                    //if (light.State.Power.Value == Power.Off)
                    //{
                    //    await light.OnAsync(minutes);
                    //}
                    //else
                    //{
                    //    await light.OffAsync(minutes);
                    //}
                }

                // Assert
                Assert.True(server.Bulbs.Count > 0);
            }
        }
    }
}

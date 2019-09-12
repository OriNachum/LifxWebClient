
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lifx;
using LifxCoreController.Api;
using LifxCoreController.Detector;
using LifxCoreController.Lightbulb;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;
using Xunit;

namespace LifxCoreControllerTest
{
    public class LifxApiTest
    {
        private IHttpContextAccessor _httpContextAccessor;
        private HttpContext _defaultHttpContext;
        private ConnectionInfo _connectionInfo;
        private IOptions<LifxDetectorConfiguration> _optionsLifxDetector;
        ILogger _logger;

        public LifxApiTest()
        {
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _defaultHttpContext = Substitute.For<HttpContext>();
            _connectionInfo = Substitute.For<ConnectionInfo>();
            _httpContextAccessor.HttpContext.Returns(_defaultHttpContext);
            _defaultHttpContext.Connection.Returns(_connectionInfo);
            _optionsLifxDetector = Substitute.For<IOptions<LifxDetectorConfiguration>>();
            _logger =  Substitute.For<ILogger>();
        }


        [Fact]
        public void CreateServerTest()
        {
            var server = new LifxApi(_optionsLifxDetector, _httpContextAccessor, _logger);
            Assert.NotNull(server);
        }

        [Fact]
        public async Task DetectNearbyDevices_Success()
        {
            // Assign
            var knownIp = new IPAddress(new byte[] { 10, 0, 0, 7 });
            _connectionInfo.RemoteIpAddress.Returns(knownIp);

            using (var server = new LifxDetector(_optionsLifxDetector, _httpContextAccessor, _logger))
            {
                // Act
                IEnumerable<IPAddress> allIpsQuery = await server.GetAllIpsInNetworkAsync();
                IEnumerable<IPAddress> allIps = allIpsQuery.ToList();
                // Assert
                Assert.True(allIps.Count() > 0);
                Assert.Contains(knownIp, allIps);
            }
        }

        //[Fact]
        //public async Task LightBulbFadeIn_Success()
        //{
        //    // Assign
        //    using (var cts = new CancellationTokenSource())
        //    {
        //        var knownIp = new IPAddress(new byte[] { 10, 0, 0, 4 });
        //        _connectionInfo.RemoteIpAddress.Returns(knownIp);

        //        using (var server = new LifxDetector(_httpContextAccessor, _logger))
        //        {
        //            // Act
        //            await server.DetectLightsAsync(cts.Token);
        //            IBulb light = server.Bulbs.Values
        //                .Where(x => x.Label == "Television")
        //                .FirstOrDefault();
        //            Assert.NotNull(light);
        //            // foreach (ILight light in server.Bulbs.Values)
        //            {
        //                uint second = 1000;
        //                uint minute = second * 60;
        //                uint minutes = minute * 10;

        //                Power newPower = (light.State.Power.Value == Power.On) ? Power.Off : Power.On;
        //                Percentage newBrightness = 0.01;

        //                //await light.SetPowerAsync(newPower, second);
        //                await light.SetBrightnessAsync(newBrightness, second);
        //                //if (light.State.Power.Value == Power.Off)
        //                //{
        //                //    await light.OnAsync(minutes);
        //                //}
        //                //else
        //                //{
        //                //    await light.OffAsync(minutes);
        //                //}
        //            }

        //            // Assert
        //            Assert.True(server.Bulbs.Count > 0);
        //        }
        //    }
        //}
    }
}

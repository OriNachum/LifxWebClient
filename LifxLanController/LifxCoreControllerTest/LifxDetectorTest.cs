
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LifxCoreController.Detector;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;
using Xunit;

namespace LifxCoreControllerTest
{
    public class LifxDetectorTest
    {
        ILogger _logger;
        private IOptions<LifxDetectorConfiguration> _optionsLifxDetector;
        private IHttpContextAccessor _httpContextAccessor;
        private HttpContext _defaultHttpContext;
        private ConnectionInfo _connectionInfo;

        public LifxDetectorTest()
        {
            _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            _defaultHttpContext = Substitute.For<HttpContext>();
            _connectionInfo = Substitute.For<ConnectionInfo>();
            _httpContextAccessor.HttpContext.Returns(_defaultHttpContext);
            _defaultHttpContext.Connection.Returns(_connectionInfo);
            _logger = Substitute.For<ILogger>();
            _optionsLifxDetector = Substitute.For<IOptions<LifxDetectorConfiguration>>();
        }


        [Fact]
        public void CreateServerTest()
        {
            var server = new LifxDetector(_optionsLifxDetector, _httpContextAccessor, _logger);
            Assert.NotNull(server);
        }

        [Fact]
        public async Task DetectNearbyDevices_Success()
        {
            // Assign
            var knownIp = new IPAddress(new byte[] { 192, 168, 1, 11 });
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

        [Fact]
        public async Task DetectLights_Success()
        {
            // Assign
            using (var server = new LifxDetector(_optionsLifxDetector, _httpContextAccessor, _logger))
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

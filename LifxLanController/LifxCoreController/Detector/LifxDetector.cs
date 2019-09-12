using Lifx;
using LifxCoreController.Detector;
using LifxCoreController.Lightbulb;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxCoreController.Detector
{
    public class LifxDetector : ILifxDetector
    {
        object lightsLock = new object();

        public IOptions<LifxDetectorConfiguration> LifxDetectorConfiguration { get; }

        private IHttpContextAccessor HttpContextAccessor;
        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\LifxDetector.log", shared: true)
                    .CreateLogger();
                }
                return _logger;
            }
        }

        private IDictionary<IPAddress, IAdvancedBulb> _bulbs { get; set; }

        public IDictionary<IPAddress, IAdvancedBulb> Bulbs
        {
            get
            {
                return _bulbs;
            }
        }

        public LifxDetector(IOptions<LifxDetectorConfiguration> lifxDetectorConfiguration,
            IHttpContextAccessor httpContextAccessor,
            ILogger logger)
        {
            this.LifxDetectorConfiguration = lifxDetectorConfiguration;
            this.HttpContextAccessor = httpContextAccessor;
            _logger = logger;
            _bulbs = new ConcurrentDictionary<IPAddress, IAdvancedBulb>();
        }

        object DetectionStartedLock = new object();
        bool DetectionStarted = false;

        public async Task DetectLightsAsync(CancellationToken cancellationToken)
        {
            bool anotherDetectectionStarted = CheckAndWaitIfAnotherDetectionStarted();
            if (anotherDetectectionStarted)
            {
                return;
            }

            try
            {
                Logger.Information("LifxDetector - Starting to detect lights");

                var lightFactory = new LightFactory();
                // var candidateIpByteArray = new byte[] { 10, 0, 0, 2 };
                // var candidateIpByteArray = new byte[] { 192, 168, 1, 11 };
                // var candidateIpAddress = new IPAddress(candidateIpByteArray);
                ICollection<Task> detectionTries = new List<Task>();
                IEnumerable<IPAddress> allIpsInNetwork = await GetAllIpsInNetworkAsync();
                lock (lightsLock)
                {
                    IEnumerable<IPAddress> deadIps = _bulbs.Keys.Where(x => !allIpsInNetwork.Contains(x));
                    foreach (IPAddress ipAddress in deadIps)
                    {
                        RemoveLightBulb(ipAddress);
                    }
                }
                var scanTasks = new List<Task>();
                await ScanExistingIps(lightFactory, allIpsInNetwork, scanTasks, cancellationToken);
                await GetAllCollectedLightsStates(cancellationToken);
            }
            finally
            {
                lock (DetectionStartedLock)
                {
                    DetectionStarted = false;
                }
            }
        }

        private async Task GetAllCollectedLightsStates(CancellationToken cancellationToken)
        {
            var getStateTasks = new List<Task<(string label, LightState? state)>>();
            foreach (Bulb light in Bulbs.Values)
            {
                getStateTasks.Add(GetLightStateAsync(light));
            }
            var labelStates = await Task.WhenAll(getStateTasks);
            IEnumerable<string> labelsOfFailedStates = labelStates.Where(x => x.state == null)
                                                                  .Select(x => x.label)
                                                                  .ToList();
            IEnumerable<IPAddress> ipsOfFailedBulbs = _bulbs.Where(x => labelsOfFailedStates.Contains(x.Value.Label))
                                                            .Select(x => x.Key)
                                                            .ToList();
            foreach (IPAddress ip in ipsOfFailedBulbs)
            {
                Logger.Information($"LifxDetector - GetAllCollectedLightsStates - sadly, { ip } is no longer with us.");
                _bulbs.Remove(ip);
            }
        }

        private static async Task<(string label, LightState? state)> GetLightStateAsync(Bulb light)
        {
            return (light.Label, await light.GetStateAsync());
        }

        private bool CheckAndWaitIfAnotherDetectionStarted()
        {
            bool detectionAlreadyStarted = false;
            lock (DetectionStartedLock)
            {
                detectionAlreadyStarted = DetectionStarted;
                if (!DetectionStarted)
                {
                    DetectionStarted = true;
                }
            }
            if (detectionAlreadyStarted)
            {
                while (detectionAlreadyStarted)
                {
                    Thread.Sleep(1000);
                    lock (DetectionStartedLock)
                    {
                        detectionAlreadyStarted = DetectionStarted;
                    }
                }
                return true;
            }

            return false;
        }

        private async Task ScanExistingIps(LightFactory lightFactory, IEnumerable<IPAddress> allIpsInNetwork, List<Task> scanTasks, CancellationToken cancellationToken)
        {
            foreach (IPAddress candidateIpAddress in allIpsInNetwork)
            {
                var knownIP = false;
                ILight light = null;
                lock (lightsLock)
                {
                    knownIP = _bulbs.ContainsKey(candidateIpAddress);
                }
                if (!knownIP)
                {
                    scanTasks.Add(DetectLightAsync(lightFactory, candidateIpAddress, cancellationToken));
                }
            }
            await Task.WhenAll(scanTasks);
        }

        private async Task DetectLightAsync(LightFactory lightFactory, IPAddress candidateIpAddress, CancellationToken cancellationToken)
        {
            bool lightKnown = false;
            lock (lightsLock)
            {
                lightKnown = _bulbs.ContainsKey(candidateIpAddress);
            }
            if (lightKnown)
            {
                return;
            }

            ILight light = await CreateLightMuffleExceptionAsync(lightFactory, candidateIpAddress, cancellationToken);
            if (light != null)
            {
                LightState? state = null;
                try
                {
                    state = await light.GetStateAsync();
                }
                catch (Exception ex)
                {
                    light.Dispose();

                    string serializedLights = JsonConvert.SerializeObject(_bulbs.Values.Select(x => x.Serialize()));
                    var sb = new StringBuilder().AppendLine();
                    sb.Append($"|Failed to add Light. Already in dictionary: { _bulbs.ContainsKey(candidateIpAddress) }");
                    sb.Append($"|CandidateIP: { candidateIpAddress } ");
                    sb.Append($"|All Ips: {_bulbs.Keys.Select(x => string.Join(".", x.GetAddressBytes())) } ");
                    sb.Append($"|All Lights: { serializedLights } { Environment.NewLine } ");

                    string errorMessage = sb.ToString();
                    _logger.Warning(errorMessage + $" ex: { ex }");
                    throw new Exception(errorMessage, ex);
                }

                lock (lightsLock)
                {
                    if (state.HasValue && !_bulbs.ContainsKey(candidateIpAddress))
                    {
                        var bulbLogger = new BulbLogger(state.Value.Label.Value);

                        var lightBulb = new AdvancedBulb(light, state.Value, bulbLogger);
                        _bulbs.Add(candidateIpAddress, lightBulb);
                        _logger.Information($"Added light { state.Value.Label.Value }, with IP: { string.Join('.', candidateIpAddress.GetAddressBytes()) }");
                    }
                    else
                    {
                        _logger.Information($"Already existing light: label: { state.Value.Label.Value }, with IP: { string.Join('.', candidateIpAddress.GetAddressBytes()) }");
                    }
                }
            }
        }

        private async Task<ILight> CreateLightMuffleExceptionAsync(LightFactory lightFactory, IPAddress candidateIpAddress, CancellationToken cancellationToken)
        {
            ILight light = null;
            using (var cts = new CancellationTokenSource())
            {
                try
                {

                    cancellationToken.Register(cts.Cancel);
                    light = await lightFactory.CreateLightAsync(candidateIpAddress, cancellationToken);
                    _logger.
                        Information($"LifxDetector CreateLightMuffleExceptionAsync created bulb with IP: { string.Join('.', candidateIpAddress.GetAddressBytes()) }");
                }
                catch (OperationCanceledException ex)
                {
                    _logger.Warning($"LifxDetector CreateLightMuffleExceptionAsync Failed to create bulb with IP: { string.Join('.', candidateIpAddress.GetAddressBytes()) }.");
                    light?.Dispose();
                    light = null;
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    _logger.Warning($"LifxDetector CreateLightMuffleExceptionAsync Failed to create bulb with IP: { string.Join('.',candidateIpAddress.GetAddressBytes()) }. Ex: { ex } ");
                    light?.Dispose();
                    light = null;
                    cts.Cancel();
                }
            }
            return light;
        }

        private void RemoveLightBulb(IPAddress lightIp)
        {
            lock (lightsLock)
            {
                IBulb savedLight = null;
                if (_bulbs.ContainsKey(lightIp))
                {
                    savedLight = _bulbs[lightIp];
                    _bulbs.Remove(lightIp);
                }
                savedLight?.Dispose();
            }
        }

        public async Task<IEnumerable<IPAddress>> GetAllIpsInNetworkAsync()
        {
            int port = 56700;
            var neighbourIps = new List<IPAddress>();
            // new byte[4] { 10, 0, 0, 1 }; //{ 192, 168, 1, 1 };
            byte[] ipBase = HttpContextAccessor.HttpContext.Connection.RemoteIpAddress.GetAddressBytes();
            Logger.Information($"LifxDetector - GetAllIpsInNetworkAsync - fetching IP from config");
            if (!string.IsNullOrEmpty(this.LifxDetectorConfiguration?.Value.IP))
            {
                Logger.Information($"LifxDetector - GetAllIpsInNetworkAsync - fetching IP from config { this.LifxDetectorConfiguration?.Value.IP }");

                ipBase = this.LifxDetectorConfiguration?.Value.IP.Split('.')
                    .Select(x => byte.Parse(x))
                    .ToArray();
            }
            if (ipBase.First() == 127)
            {
                Logger.Information($"LifxDetector - GetAllIpsInNetworkAsync - Looking for addresses in localhost, canceled");
                return new List<IPAddress>();
            }
            Logger.Information($"LifxDetector - GetAllIpsInNetworkAsync - Starts detecting lights for address family {HttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString()}");
            /*using (var pinger = new TcpClient())
            {
                pinger.SendTimeout = 100;
                var ip = new IPAddress(ipBase);
                await pinger.ConnectAsync(ip, port);
                if (!pinger.Connected)
                {
                    // host is active
                    return neighbourIps;
                }
            }*/
            var lastSygments = new List<byte>();
            for (byte i = 2; i < 255; i++)
            {
                lastSygments.Add(i);
            }
            var ipAddresses = new List<IPAddress>();
            await Task.WhenAll(lastSygments.Select(async lastSygment => {
                ipBase[3] = lastSygment;
                var ipAddress = new IPAddress(ipBase);
                using (var pinger = new Ping())
                {
                    // pinger.SendTimeout = 100;
                    try
                    {
                        PingReply reply = await pinger.SendPingAsync(ipAddress);
                        if (reply.Status == IPStatus.Success)
                        {
                            ipAddresses.Add(ipAddress);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }));
            return ipAddresses;
        }

        /// <summary>
        /// Does not support UWP yet..
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<IPAddress>> GetAllIpsInNetworkAsync_WithPing()
        {
            var neighbourIps = new List<IPAddress>();

            //using (var pinger = new Ping())
            //{
            //    PingReply rep = await pinger.SendPingAsync("10.0.0.1");
            //    if (rep.Status == IPStatus.Success)
            //    {
            //        //host is active
            //    }
            //}

            string ipBase = "10.0.0."; //"192.168.1.";
            string dhcpServerIp = ipBase + "1";
            
            PingReply basePingReply = null;

            using (var p = new Ping())
            {                    
                basePingReply = await p.SendPingAsync(dhcpServerIp, 100);
            }

            if (basePingReply == null || basePingReply.Status != IPStatus.Success)
            {
                return neighbourIps;
            }
            

            var ipEndNumbers = new List<string>();
            for (int i = 2; i < 255; i++)
            {
                ipEndNumbers.Add(i.ToString());
            }

            PingReply[] allPingReplies = await Task.WhenAll(ipEndNumbers.Select(i => // 192.168.1.1 dhcp server
            {
                string ip = ipBase + i.ToString();

                using (var p = new Ping())
                {
                    return p.SendPingAsync(ip, 100);
                }
            }).ToArray());

            foreach (PingReply pingReply in allPingReplies)
            {
                if (pingReply.Status == IPStatus.Success)
                {
                    neighbourIps.Add(pingReply.Address);
                }
            }

            return neighbourIps;
        }

        public virtual void Dispose()
        {
            if (_bulbs.Any())
            {
                foreach (IBulb bulb in _bulbs.Values)
                {
                    bulb.Dispose();
                }
                _bulbs.Clear();
            }
        }
    }
}

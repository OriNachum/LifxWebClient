using Lifx;
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

namespace LifxCoreController
{
    public class LifxDetector : ILifxDetector
    {
        object lightsLock = new object();

        ILogger _logger = null;

        ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new LoggerConfiguration()
                    .WriteTo.File($"C:\\Logs\\LifxWebApi\\1.log")
                    .CreateLogger();
                }
                return _logger;
            }
        }

        private IDictionary<IPAddress, LightBulb> _lights { get; set; }

        public IDictionary<IPAddress, LightBulb> Lights
        {
            get
            {
                return _lights;
            }
        }

        public LifxDetector()
        {
            _lights = new ConcurrentDictionary<IPAddress, LightBulb>();
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
                    IEnumerable<IPAddress> deadIps = _lights.Keys.Where(x => !allIpsInNetwork.Contains(x));
                    foreach (IPAddress ipAddress in deadIps)
                    {
                        RemoveLightBulb(ipAddress);
                    }
                }
                var scanTasks = new List<Task>();
                await ScanExistingIps(lightFactory, allIpsInNetwork, scanTasks, cancellationToken);
                await GetAllLightsStates(cancellationToken);
            }
            finally
            {
                lock (DetectionStartedLock)
                {
                    DetectionStarted = false;
                }
            }
        }

        private async Task GetAllLightsStates(CancellationToken cancellationToken)
        {
            var getStateTasks = new List<Task<LightState>>();
            foreach (LightBulb light in Lights.Values)
            {
                getStateTasks.Add(light.GetStateAsync());
            }
            await Task.WhenAll(getStateTasks);
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
                    knownIP = _lights.ContainsKey(candidateIpAddress);
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
                lightKnown = _lights.ContainsKey(candidateIpAddress);
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

                    string serializedLights = JsonConvert.SerializeObject(_lights.Values.Select(x => x.Serialize()));
                    var sb = new StringBuilder().AppendLine();
                    sb.Append($"|Failed to add Light. Already in dictionary: { _lights.ContainsKey(candidateIpAddress) }");
                    sb.Append($"|CandidateIP: { candidateIpAddress } ");
                    sb.Append($"|All Ips: {_lights.Keys.Select(x => string.Join(".", x.GetAddressBytes())) } ");
                    sb.Append($"|All Lights: { serializedLights } { Environment.NewLine } ");
                    throw new Exception(sb.ToString(), ex);
                }

                lock (lightsLock)
                {
                    if (state.HasValue && !_lights.ContainsKey(candidateIpAddress))
                    {
                        var lightBulb = new LightBulb(light, state.Value);
                        _lights.Add(candidateIpAddress, lightBulb);
                        // Log
                    }
                    else
                    {
                        // Log
                    }
                }
            }
            else
            {
                // Add log, already exists
            }

        }

        private static async Task<ILight> CreateLightMuffleExceptionAsync(LightFactory lightFactory, IPAddress candidateIpAddress, CancellationToken cancellationToken)
        {
            ILight light = null;
            using (var cts = new CancellationTokenSource())
            {
                try
                {

                    cancellationToken.Register(cts.Cancel);
                    light = await lightFactory.CreateLightAsync(candidateIpAddress, cancellationToken);

                }
                catch (Exception ex)
                {
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
                ILight savedLight = null;
                if (_lights.ContainsKey(lightIp))
                {
                    savedLight = _lights[lightIp];
                    _lights.Remove(lightIp);
                }
                savedLight?.Dispose();
            }
        }

        public async Task<IEnumerable<IPAddress>> GetAllIpsInNetworkAsync()
        {
            int port = 56700;
            var neighbourIps = new List<IPAddress>();
            byte[] ipBase = new byte[4] { 192, 168, 1, 1 };

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

            var pinger = new Ping();
            PingReply rep = await pinger.SendPingAsync("192.168.1.1");
            if (rep.Status == IPStatus.Success)
            {
                //host is active
            }

            string ipBase = "192.168.1.";
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
            if (_lights.Any())
            {
                foreach (ILight light in _lights.Values.Select(x => x.Light))
                {
                    light.Dispose();
                }
                _lights.Clear();
            }
        }
    }
}

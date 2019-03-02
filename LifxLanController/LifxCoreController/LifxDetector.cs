using Lifx;
using Newtonsoft.Json;
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

        public async Task DetectLightsAsync(CancellationToken cancellationToken)
        {
            // TODO: 
            // Add lock. 
            // Add last update. 
            // If last update is small enough, just return. 
            // Lights are now updated.
            var lightFactory = new LightFactory();
            // var candidateIpByteArray = new byte[] { 10, 0, 0, 2 };
            // var candidateIpByteArray = new byte[] { 192, 168, 1, 11 };
            // var candidateIpAddress = new IPAddress(candidateIpByteArray);
            ICollection<Task> detectionTries = new List<Task>();
            IEnumerable<IPAddress> allIpsInNetwork = await GetAllIpsInNetworkAsync();
            IEnumerable<IPAddress> deadIps = _lights.Keys.Where(x => !allIpsInNetwork.Contains(x));

            foreach (IPAddress ipAddress in deadIps)
            {
                _lights[ipAddress].Light.Dispose();
                _lights.Remove(ipAddress);
            }

            await Task.WhenAll(allIpsInNetwork.Select(async candidateIpAddress => 
            //foreach (IPAddress candidateIpAddress in allIpsInNetwork)
            {
                if (_lights.ContainsKey(candidateIpAddress))
                {
                   await new Task(async () => await _lights[candidateIpAddress].GetStateAsync());
                }
                else
                {
                    await DetectLightAsync(lightFactory, candidateIpAddress, cancellationToken);
                }
            }));
        }

        object lockObject = new object();
        private async Task DetectLightAsync(LightFactory lightFactory, IPAddress candidateIpAddress, CancellationToken cancellationToken)
        {
            if (!_lights.ContainsKey(candidateIpAddress))
            {

                ILight light = null;
                try
                {
                    light = await lightFactory.CreateLightAsync(candidateIpAddress, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    // Add log
                }
                catch (SocketException ex)
                {
                    // It hints the IP doesn't exist, or not a bulb
                }

                if (light != null)
                {
                    try
                    {
                        LightState state = await light.GetStateAsync();
                        lock (lockObject)
                        {
                            if (!_lights.ContainsKey(candidateIpAddress))
                            {
                                var lightBulb = new LightBulb(light, state);
                                _lights.Add(candidateIpAddress, lightBulb);
                                // Log
                            }
                            else
                            {
                                // Log
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (light != null)
                        {
                            if (_lights.ContainsKey(candidateIpAddress))
                            {
                                _lights.Remove(candidateIpAddress);
                            }
                            light.Dispose();
                        }
                        string serializedLights = JsonConvert.SerializeObject(_lights.Values.Select(x => x.Serialize()));
                        var sb = new StringBuilder().AppendLine();
                        sb.Append($"|Failed to add Light. Already in dictionary: { _lights.ContainsKey(candidateIpAddress) }");
                        sb.Append($"|CandidateIP: { candidateIpAddress } ");
                        sb.Append($"|All Ips: {_lights.Keys.Select(x => string.Join(".", x.GetAddressBytes())) } ");
                        sb.Append($"|All Lights: { serializedLights } { Environment.NewLine } ");
                        throw new Exception(sb.ToString(), ex);
                    }
                }
                else
                {
                    // Add log, already exists
                }
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

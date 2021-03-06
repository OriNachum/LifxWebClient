﻿using Lifx;
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

namespace LifxLanController
{
    public class LifxDetector : ILifxDetector
    {
        private IDictionary<IPAddress, ILight> _lights { get; set; }

        public IDictionary<IPAddress, ILight> Lights
        {
            get
            {
                return _lights;
            }
        }

        public LifxDetector()
        {
            _lights = new ConcurrentDictionary<IPAddress, ILight>();
        }

        public async Task DetectLights(CancellationToken cancellationToken)
        {
            var lightFactory = new LightFactory();
            // var candidateIpByteArray = new byte[] { 10, 0, 0, 2 };
            // var candidateIpByteArray = new byte[] { 192, 168, 1, 11 };
            // var candidateIpAddress = new IPAddress(candidateIpByteArray);
            ICollection<Task> detectionTries = new List<Task>();
            IEnumerable<IPAddress> allIpsInNetwork = await GetAllIpsInNetworkAsync();
            foreach (IPAddress ipAddress in _lights.Keys
                                                   .Where(x => !allIpsInNetwork.Contains(x))
                                                   .ToList())
            {
                _lights.Remove(ipAddress);
            }

            await Task.WhenAll(allIpsInNetwork.Select(candidateIpAddress =>
            {
                return DetectLight(lightFactory, candidateIpAddress, cancellationToken);
            }).ToArray());
        }

        private async Task DetectLight(LightFactory lightFactory, IPAddress candidateIpAddress, CancellationToken cancellationToken)
        {
            if (!_lights.ContainsKey(candidateIpAddress))
            {
                try
                {
                    ILight light = await lightFactory.CreateLightAsync(candidateIpAddress, cancellationToken);
                    if (light != null)
                    {
                        _lights.Add(candidateIpAddress, light);
                        // Add log
                    }
                    else
                    {
                        // Add log
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // Add log
                }
                catch (SocketException ex)
                {
                    // It hints the IP doesn't exist, or not a bulb
                }
            }
        }

        public async Task<IEnumerable<IPAddress>> GetAllIpsInNetworkAsync()
        {
            int port = 80;
            var neighbourIps = new List<IPAddress>();
            byte[] ipBase = new byte[4] { 192, 168, 1, 1 };

            using (var pinger = new TcpClient())
            {
                pinger.SendTimeout = 100;
                await pinger.ConnectAsync(new IPAddress(ipBase), port);
                if (!pinger.Connected)
                {
                    // host is active
                    return neighbourIps;
                }
            }

            var ipAddresses = new List<IPAddress>();
            for (byte i = 2; i < 255; i++)
            {
                ipBase[3] = i;
                var ipAddress = new IPAddress(ipBase);
                ipAddresses.Add(ipAddress);
            }
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

        public void Dispose()
        {
            if (_lights != null && _lights.Any())
            {
                foreach (ILight light in _lights.Values)
                {
                    light.Dispose();
                }
                _lights.Clear();
            }
        }
    }
}

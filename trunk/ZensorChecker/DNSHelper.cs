/*
 * The ZensorCheker  checks the locally present DNS Server against the
 * list of Censored Domains to find out if your ISP is censoring you.
 * 
 *  Copyright (c) 2008-2010 Thomas Bruderer <apophis@apophis.ch>
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.IO;
using System.Net;

namespace apophis.ZensorChecker
{
    /// <summary>
    /// Description of FindDNSServer.
    /// </summary>
    public class DNSHelper
    {
        private static readonly IPAddress Opendns1 = IPAddress.Parse("208.67.222.222");
        
        public static IPAddress OpenDNS1 {
            get {
                return Opendns1;
            }
        }

        private static readonly IPAddress Opendns2 = IPAddress.Parse("208.67.220.220");

        public static IPAddress OpenDNS2 {
            get {
                return Opendns2;
            }
        }
        
        public static IEnumerable<IPAddress> GetLocalDNS() {
            switch(Environment.OSVersion.Platform) {
                case PlatformID.Unix:
                    return GetLocalDNSUnix();
                default:
                    return GetLocalDnSdotNet2();
            }
        }
        
        private static IEnumerable<IPAddress> GetLocalDnSdotNet2() {
            return NetworkInterface.GetAllNetworkInterfaces().Where(adapter => (adapter.OperationalStatus == OperationalStatus.Up) && (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)).Select(adapter => adapter.GetIPProperties()).SelectMany(properties => properties.DnsAddresses).ToList();
        }
        
        private static IEnumerable<IPAddress> GetLocalDNSUnix() {
            var dnsservers = new List<IPAddress>();
            string line;
            TextReader resolve = File.OpenText("/etc/resolv.conf");
            while((line = resolve.ReadLine()) != null) {
                if(line.StartsWith("nameserver")) {
                    dnsservers.Add(IPAddress.Parse(line.Substring(11)));
                }
            }
            return dnsservers;
        }

        
        
        public static string ReverseDNS(string ip) {
            var ipEntry = Dns.GetHostEntry(ip);
            return ipEntry.HostName;
        }
        
    }
}

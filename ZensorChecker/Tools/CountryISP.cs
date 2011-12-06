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

using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace apophis.ZensorChecker.Tools
{
    /// <summary>
    /// Description of CountryISP.
    /// </summary>
    public class CountryISP
    {
        private readonly IPAddress externalIP;
        
        public IPAddress ExternalIP {
            get { return externalIP; }
        }
        
        private readonly string country;
        
        public string Country {
            get { return country; }
        }
        
        private readonly string isp;
        
        public string Isp {
            get { return isp; }
        }
        
        private readonly string region;
        
        public string Region {
            get { return region; }
        }
        
        private readonly string city;
        
        public string City {
            get { return city; }
        }
        
        private readonly string timezone;
        
        public string Timezone {
            get { return timezone; }
        }
        
        private readonly string networkspeed;
        
        public string Networkspeed {
            get { return networkspeed; }
        }
        
        private readonly Regex strongMatch = new Regex(@"(?<=<strong>)[^<]*(?=</strong>)");
        public CountryISP()
        {
            var web = new WebClient();
            
            // Webbrowser
            web.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            var reader = new StreamReader(web.OpenRead("http://www.ip2location.com/ib2/"));
            string line;
            while((line = reader.ReadLine())!= null) {
                var mc = strongMatch.Matches(line);

                if (mc.Count <= 6) continue;

                externalIP = IPAddress.Parse(mc[0].Value);
                isp = mc[1].Value;
                country = mc[2].Value;
                region = mc[3].Value;
                city = mc[4].Value;
                timezone = mc[5].Value;
                networkspeed = mc[6].Value;
            }
        }
    }
}

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

using System.Net;

namespace apophis.ZensorChecker
{
    /// <summary>
    /// Description of UrlInfo.
    /// </summary>
    public class UrlInfo
    {
        private readonly string url;
        
        public string URL {
            get { 
                return url; 
            }
        }

        private bool queryAgain;
        
        public bool QueryAgain {
            get {
                return queryAgain;
            }
        }

        public IPAddress IPFromLocalDns { get; private set; }

        public UrlInfo(string url)
        {
            this.url = url;
            this.queryAgain = true;
        }
        
        public void SetIP(IPAddress ip) {
            this.IPFromLocalDns = ip;
            this.queryAgain = false;
        }
    }
}

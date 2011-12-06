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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using apophis.ZensorChecker.dnsclient;

namespace apophis.ZensorChecker
{

    /// <summary>
    /// Description of Spider.
    /// </summary>
    public class StopSpider
    {
        readonly List<SpiderInfo> spiderlist = new List<SpiderInfo>();

        // for fast existence lookup only!
        readonly Dictionary<string, bool> spidercheck = new Dictionary<string, bool>();

        private readonly IPAddress providerDNS;

        private readonly IPAddress censorRedirect;
        private readonly string provider;
        private readonly string country;
        private readonly string reporter;

        public StopSpider(IPAddress providerDNS, IPAddress censorRedirect, string provider, string country, string reporter)
        {
            // initiate spiderlist

            this.providerDNS = providerDNS;
            this.censorRedirect = censorRedirect;
            this.provider = provider;
            this.country = country;
            this.reporter = reporter;

            // add an initial list in randomized order, this will also prevent adding already known urls!
            var rlist = new SortedList<int, string>();
            var shortlist = Assembly.GetExecutingAssembly().GetManifestResourceStream("shortlist.txt");
            if (shortlist != null)
            {
                TextReader inputurls = new StreamReader(shortlist);
                string url;
                var r = new Random();

                while ((url = inputurls.ReadLine()) != null)
                {
                    rlist.Add(r.Next(), url);
                }
            }


            foreach (var uri in rlist.Values)
            {
                spiderlist.Add(new SpiderInfo(uri, 0));
                spidercheck.Add(uri, true);
            }

        }

        public void PublishNewUrls()
        {

        }

        private bool crawl = true;
        private int pooled;
        private int running;
        private SpiderInfo lastfinshed;

        public void CrawlSpiderList()
        {
            int index = 0; lastfinshed = spiderlist[0];
            while (crawl)
            {

                if (index >= spiderlist.Count || pooled >= 100)
                {
                    Thread.Sleep(500);
                    Console.WriteLine("status: i" + (index - 1) + "|" + lastfinshed.URL + "|" + lastfinshed.Depth + "|c" + spiderlist.Count + "|" + running + "/" + pooled);
                    continue;
                }

                pooled++;

                ThreadPool.QueueUserWorkItem(FindNewUrls, spiderlist[index]);

                index++;

            }
        }

        private readonly Regex hrefMatch = new Regex("(?<=href=\"http://)[^\"]*(?=\")");

        private void FindNewUrls(object spiderInfo)
        {
            running++;
            // We cannot use WebClient or similar, since we cannot rely on the DNS resolution!
            var client = new TcpClient();
            var ipAddress = GetRealIPFromUri(((SpiderInfo)spiderInfo).URL);
            //check for censorship

            CheckIfCensored((SpiderInfo)spiderInfo);

            if (ipAddress == null)
            {
                // Invalid Response
                pooled--; running--;
                return;
            }

            try
            {
                client.Connect(ipAddress, 80);
            }
            catch (Exception)
            {
                pooled--; running--;
                return;
            }

            //Send Request
            TextWriter tw = new StreamWriter(client.GetStream());
            tw.WriteLine("GET / HTTP/1.1");
            tw.WriteLine("Host: " + ((SpiderInfo)spiderInfo).URL);
            tw.WriteLine("User-Agent: Mozilla/5.0 (compatible; zensorchecker/" + GetType().Assembly.GetName().Version + ";  http://zensorchecker.origo.ethz.ch/)");
            tw.WriteLine();
            tw.Flush();


            TextReader document = new StreamReader(client.GetStream());
            try
            {
                string line;
                while ((line = document.ReadLine()) != null)
                {
                    var mc = hrefMatch.Matches(line);

                    foreach (var url in from Match m in mc
                                        select m.Value + "/"
                                            into href
                                            select href.Substring(0, href.IndexOf('/'))
                                                into url
                                                where !spidercheck.ContainsKey(url)
                                                select url)
                    {
                        spiderlist.Add(new SpiderInfo(url, ((SpiderInfo)spiderInfo).Depth + 1));
                        spidercheck.Add(url, true);
                    }
                }
            }
            catch (Exception)
            {
                ((SpiderInfo)spiderInfo).ReadError = true;
            }
            lastfinshed = (SpiderInfo)spiderInfo;
            pooled--; running--;
        }

        void CheckIfCensored(SpiderInfo spiderInfo)
        {
            if (spiderInfo.Depth == 0)
            {
                // no test needed
                return;
            }
            try
            {
                var request = new Request();
                request.AddQuestion(new Question(spiderInfo.URL, DnsType.ANAME, DnsClass.IN));
                var response = Resolver.Lookup(request, providerDNS);
                if (((ANameRecord)response.Answers[0].Record).IPAddress.ToString() == censorRedirect.ToString())
                {
                    switch (PostNewFoundUrl(spiderInfo.URL))
                    {
                        case ReturnState.OK:
                            break;
                        case ReturnState.Failed:
                            break;
                        case ReturnState.NotNew:
                            break;
                    }


                    Console.WriteLine("> " + spiderInfo.URL + " (" + spiderInfo.Depth + ") [NEW Censored]");
                    spiderInfo.Censored = true;

                    TextWriter tw = new StreamWriter("spider.txt", true);
                    tw.WriteLine(spiderInfo.URL);
                    tw.Close();

                }
            }
            catch (Exception)
            { }
        }


        private static IPAddress GetRealIPFromUri(string uri)
        {
            try
            {
                var request = new Request();
                request.AddQuestion(new Question(uri, DnsType.ANAME, DnsClass.IN));
                var response = Resolver.Lookup(request, DNSHelper.OpenDNS1);

                if (response.Answers[0].Record is ANameRecord)
                {
                    return ((ANameRecord)response.Answers[0].Record).IPAddress;
                }

                // CNAME redirect (infinite loop?)
                if (response.Answers[0].Record is NSRecord)
                {
                    return GetRealIPFromUri(((NSRecord)response.Answers[0].Record).DomainName);
                }
            }
            catch (ArgumentException)
            {
                //Invalid Domain name ignored
            }
            catch (NoResponseException)
            {
                //happens
            }
            catch (OverflowException)
            {
                //BUG in DNSResolver, should update to another one!
            }
            return null;
        }

        public enum ReturnState
        {
            OK, NotNew, Failed
        }

        public ReturnState PostNewFoundUrl(string url)
        {
            var web = new WebClient();

            web.QueryString.Add("url", url); // new URL
            web.QueryString.Add("rip", censorRedirect.ToString()); // Redirected to
            web.QueryString.Add("cnt", country); // Country
            web.QueryString.Add("isp", provider); // ISP
            web.QueryString.Add("rep", reporter); // Reporter

            string s = web.DownloadString("http://apophis.ch/zensorchecker.php");
            return s.EndsWith("[OK]")
                        ? ReturnState.OK
                        : (s.EndsWith("[NOTNEW]")
                            ? ReturnState.NotNew
                            : ReturnState.Failed);
        }
    }
}

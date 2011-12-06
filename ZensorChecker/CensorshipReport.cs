/*
 * Erstellt mit SharpDevelop.
 * Benutzer: apophis
 * Datum: 03.05.2009
 * Zeit: 19:47
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using apophis.ZensorChecker.dnsclient;
using System.Threading;

namespace apophis.ZensorChecker
{
    /// <summary>
    /// Description of CensorshipReport.
    /// </summary>
    public class CensorshipReport
    {
        private readonly string provider;

        public string Provider
        {
            get
            {
                return provider;
            }
        }

        private readonly string country;

        public string Country
        {
            get
            {
                return country;
            }
        }

        public DateTime Date { get; private set; }

        public bool IsCensoring { get; private set; }

        public IPAddress CensorRedirect { get; private set; }

        private IEnumerable<IPAddress> dnsServers;

        public IEnumerable<IPAddress> DnsServers
        {
            get
            {
                return dnsServers;
            }
        }

        private readonly List<string> cenosredUrls = new List<string>();

        public List<string> CenosredUrls
        {
            get
            {
                return cenosredUrls;
            }
        }

        private readonly List<string> urlsToTest = new List<string>();

        public List<string> UrlsToTest
        {
            get
            {
                return urlsToTest;
            }
        }

        public string Reporter { get; private set; }

        private bool reportReady;

        public CensorshipReport(string provider, string country, string reporter)
        {
            CensorRedirect = null;
            // Get the list from the resources

            var r = new Random();
            var rlist = new SortedList<int, string>();

            //foreach(string res in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
            //    Console.WriteLine("Resource: " + res);
            //}

            var baselist = Assembly.GetExecutingAssembly().GetManifestResourceStream("baselist.txt");
            if (baselist != null)
            {
                TextReader inputurls = new StreamReader(baselist);
                string url;
                while ((url = inputurls.ReadLine()) != null)
                {
                    rlist.Add(r.Next(), url);
                }
            }

            // the list in randomized Order
            urlsToTest.AddRange(rlist.Values);

            // Local DNS Server
            dnsServers = DNSHelper.GetLocalDNS();
            foreach (var dnsserver in dnsServers)
            {
                if ((dnsserver.ToString() == DNSHelper.OpenDNS1.ToString()) || (dnsserver.ToString() == DNSHelper.OpenDNS2.ToString()))
                {
                    Console.Write("Warning: one of your DNS Servers is an OpenDNS Server, which is not censored. Check might give invalid results.");
                    Thread.Sleep(5000);

                }
                if ((dnsserver.ToString().StartsWith("10.")) || (dnsserver.ToString().StartsWith("192.168.")))
                {
                    Console.Write("Warning: Your DNS seems to be a local adress, the gateway probably relies the request to your ISPs DNS, however its not transparent which DNS Server we actually use.");
                    Thread.Sleep(5000);
                }
            }

            //Date
            Date = DateTime.Now;

            //Account Information
            this.provider = provider;
            this.country = country;
            Reporter = reporter;

        }

        /// <summary>
        /// You already know the IP adress of the censoring Server
        /// </summary>
        /// <param name="ip"></param>
        public void CensorServerHint(IPAddress ip)
        {
            CensorRedirect = ip;
            IsCensoring = true;
        }

        /// <summary>
        /// you want to use another DNS Server than the one in your local settings.
        /// </summary>
        /// <param name="ips"></param>
        public void DnsServerHint(IEnumerable<IPAddress> ips)
        {
            dnsServers = ips;
        }

        public void RunCheck()
        {
            if (reportReady)
            {
                return;
            }
            if (CensorRedirect == null)
            {
                reportReady = true;
                IsCensoring = false;
                return;
            }
            Console.WriteLine("We test now which adresses get censored. This will take very long.");
            Console.WriteLine();

            var providerDNS = dnsServers.FirstOrDefault();
            var i = 0;
            foreach (var url in urlsToTest)
            {
                i++;
                try
                {
                    var request = new Request();
                    request.AddQuestion(new Question(url, DnsType.ANAME, DnsClass.IN));
                    Response response = Resolver.Lookup(request, providerDNS);
                    if (((ANameRecord)response.Answers[0].Record).IPAddress.ToString() == CensorRedirect.ToString())
                    {
                        Console.WriteLine("> " + i + "/" + urlsToTest.Count + " : " + url + " [Censored]");
                        //Console.Write("x");
                        Monitor.Enter(cenosredUrls);
                        cenosredUrls.Add(url);
                        Monitor.Exit(cenosredUrls);
                    }
                    else
                    {
                        Console.WriteLine("> " + i + "/" + urlsToTest.Count + " : " + url + " [Open]");
                        //Console.Write("o");
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("> " + i + "/" + urlsToTest.Count + " : " + url + " [Skipped]");
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            reportReady = true;
        }


        public void GetCensoringIP()
        {
            // If we already have a valid IP or a Report is ready, there is nothing to do anymore
            if ((reportReady) || (CensorRedirect != null))
            {
                return;
            }

            Console.WriteLine("We try to find if you get censored, and to which IP you get redirected! This will take very long! If you know the redirct, use --censorhint to skip this test.");
            Console.WriteLine();

            var censoringIPs = new Dictionary<string, int>();
            foreach (string url in urlsToTest)
            {
                try
                {

                    var request = new Request();
                    request.AddQuestion(new Question(url, DnsType.ANAME, DnsClass.IN));

                    Response openDNSResp1 = Resolver.Lookup(request, DNSHelper.OpenDNS1);
                    Response openDNSResp2 = Resolver.Lookup(request, DNSHelper.OpenDNS1);
                    
                    var providerResp = dnsServers.Select(ip => Resolver.Lookup(request, ip)).ToList();

                    if (openDNSResp1.Answers.Length == 1)
                    {
                        // both ODNS Servers agree on a single server
                        if (openDNSResp1.Answers.Length == openDNSResp2.Answers.Length)
                        {
                            // both ODNS Server agree on a single IP
                            if ((((ANameRecord)openDNSResp1.Answers[0].Record).IPAddress.ToString()) ==
                               (((ANameRecord)openDNSResp2.Answers[0].Record).IPAddress.ToString()))
                            {
                                // this single IP agrees with your ISPs DNS
                                if ((((ANameRecord)openDNSResp1.Answers[0].Record).IPAddress.ToString()) ==
                                   (((ANameRecord)providerResp[0].Answers[0].Record).IPAddress.ToString()))
                                {
                                    Console.Write("o");
                                    // this single IP doesnt agree with your ISP, this is potentially a censored IP
                                }
                                else
                                {
                                    Console.Write("x");
                                    if (censoringIPs.ContainsKey(((ANameRecord)providerResp[0].Answers[0].Record).IPAddress.ToString()))
                                    {
                                        censoringIPs[((ANameRecord)providerResp[0].Answers[0].Record).IPAddress.ToString()]++;
                                    }
                                    else
                                    {
                                        censoringIPs.Add(((ANameRecord)providerResp[0].Answers[0].Record).IPAddress.ToString(), 1);
                                    }
                                }

                            }
                            else
                            {
                                Console.Write("-");
                            }
                        }
                    }
                    else
                    {
                        // we ignore round robin entries for the search for the censorhip redirect IP
                        // because its easier
                        Console.Write("-");
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            Console.WriteLine();
            Console.WriteLine();
            var newmax = 0;

            RemoveFalsePositives(censoringIPs);

#if DEBUG
            TextWriter falsepositives = new StreamWriter("falsepositives" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + ".txt", false);
#endif

            foreach (var kvp in censoringIPs)
            {
                Console.WriteLine(kvp.Key + " has #" + kvp.Value);
#if DEBUG
                falsepositives.WriteLine(kvp.Key + " has #" + kvp.Value);
#endif
                if ((kvp.Value > 10) && (kvp.Value > newmax))
                {
                    newmax = kvp.Value;
                    CensorRedirect = IPAddress.Parse(kvp.Key);
                    IsCensoring = true;
                }
            }

#if DEBUG
            falsepositives.Close();
#endif

            Console.WriteLine();
            cenosredUrls.Sort();
        }

        private static void RemoveFalsePositives(Dictionary<string, int> censoringIPs)
        {
            var falsepositives = Assembly.GetExecutingAssembly().GetManifestResourceStream("falsepositivs.txt");
            if (falsepositives != null)
            {
                TextReader inputips = new StreamReader(falsepositives);
                var falsepos = new List<string>();
                string ip;

                while ((ip = inputips.ReadLine()) != null)
                {
                    falsepos.Add(ip);
                }

                foreach (var cip in censoringIPs.Keys.Where(falsepos.Contains).ToList())
                {
                    censoringIPs.Remove(cip);
                }
            }
        }

        public void PrintReport(TextWriter sw)
        {
            if (!reportReady)
            {
                return;
            }

            sw.WriteLine();
            sw.WriteLine();
            sw.WriteLine("Automatic Censorship Report");
            sw.WriteLine("---------------------------");
            sw.WriteLine("ISP       : " + provider);
            sw.WriteLine("Country   : " + country);
            sw.WriteLine("Date      : " + Date.ToShortDateString());
            sw.WriteLine("Reporter  : " + Reporter);
            int ipidx = 0;
            foreach (IPAddress dnsip in dnsServers)
            {
                ipidx++;
                // Reverse DNS
                //Console.WriteLine("DNS#" + ipidx + "     : "+ dnsip + " (" + DNSHelper.ReverseDNS(dnsip.ToString()) + ")");
                sw.WriteLine("DNS#" + ipidx + "     : " + dnsip);
            }
            sw.WriteLine("Censored  : " + ((IsCensoring) ? "Yes" : "No"));
            if (IsCensoring)
            {
                sw.WriteLine("Censor IP : " + CensorRedirect);
                sw.WriteLine("---------------------------");

                cenosredUrls.Sort();
                foreach (string url in cenosredUrls)
                {
                    sw.WriteLine("Censoring : " + url);
                }

                sw.WriteLine("---------------------------");
                sw.WriteLine("We found " + cenosredUrls.Count + " Domains which are banned on your ISP. ");
                sw.WriteLine("Please Post your result on apophis.ch.");
            }

            sw.WriteLine("-----------------");
            sw.WriteLine("apophis.ch - 2009");
        }
    }
}

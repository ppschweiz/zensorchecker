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
using System.Reflection;
#if GUI
using System.Windows.Forms;
using apophis.ZensorChecker.Tools;

#endif

namespace apophis.ZensorChecker
{


    class Program
    {
        public static void Main(string[] args)
        {

            var argsParsed = new Arguments(args);

            var country = "none";
            var provider = "ISP";
            var reporter = "Anonymous";

            if ((argsParsed["h"] != null) || (argsParsed["-help"] != null))
            {
                Help.PrintGeneralHelp();
                return;
            }

            if (argsParsed["-version"] != null)
            {
                Help.printVersionInfo();
                return;
            }

            if ((argsParsed["l"] != null) || (argsParsed["-list"] != null))
            {
                var baseList = Assembly.GetExecutingAssembly().GetManifestResourceStream("baselist.txt");
                if (baseList != null)
                {
                    TextReader urls = new StreamReader(baseList);
                    string url;
                    while ((url = urls.ReadLine()) != null)
                    {
                        Console.WriteLine(url);
                    }
                }
                return;
            }

            if ((argsParsed["n"] == null) && (argsParsed["-noauto"] == null))
            {
                var autodetect = new CountryISP();
                country = autodetect.Country;
                provider = autodetect.Isp;
            }

            if ((argsParsed["c"] != null) || (argsParsed["-country"] != null))
            {
                if (argsParsed["c"] != null)
                {
                    country = (string)argsParsed["c"][0];
                }
                else
                {
                    country = (string)argsParsed["-country"][0];
                }
            }

            if ((argsParsed["p"] != null) || (argsParsed["-provider"] != null))
            {
                if (argsParsed["p"] != null)
                {
                    provider = (string)argsParsed["p"][0];
                }
                else
                {
                    provider = (string)argsParsed["-provider"][0];
                }
            }

            if ((argsParsed["r"] != null) || (argsParsed["-reporter"] != null))
            {
                if (argsParsed["r"] != null)
                {
                    reporter = (string)argsParsed["r"][0];
                }
                else
                {
                    reporter = (string)argsParsed["-reporter"][0];
                }
            }

            var cr = new CensorshipReport(provider, country, reporter);

            if (argsParsed["-censorhint"] != null)
            {
                cr.CensorServerHint(IPAddress.Parse((string)argsParsed["-censorhint"][0]));
            }

            if ((argsParsed["d"] != null) || (argsParsed["-dnshint"] != null))
            {
                var dnshint = new List<IPAddress>();
                if (argsParsed["d"] != null)
                {
                    dnshint.AddRange(from string ip in argsParsed["d"] select IPAddress.Parse(ip));
                }
                else
                {
                    dnshint.AddRange(from string ip in argsParsed["-dnshint"] select IPAddress.Parse(ip));
                }
                cr.DnsServerHint(dnshint);
            }

#if GUI
            bool gui = true;
            if (argsParsed["c"] != null || argsParsed["-console"] != null)
            {
                gui = false;
            }

            if (gui)
            {
                Application.Run(new MainForm());
            }
#endif

#if DEBUG
            var spider = new StopSpider(IPAddress.Parse("192.168.1.1"), IPAddress.Parse("212.142.48.154"), provider, country, reporter);
            spider.CrawlSpiderList();
            return;
#endif

            cr.GetCensoringIP(); // returns without test if a hint was given

            Console.Clear();
            cr.RunCheck();

            Console.Clear();

            cr.PrintReport(Console.Out);
            TextWriter tw = new StreamWriter("report-" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + ".txt", false);
            cr.PrintReport(tw);
            tw.Close();

            return;
        }


    }
}
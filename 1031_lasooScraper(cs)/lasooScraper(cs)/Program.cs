using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace lasooScraper_cs_
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Count(); i++)
                {
                    System.Console.WriteLine(args[i]);
                }

                Scraper scraper = new Scraper();
                string root_dir = scraper.createRootDirectory(args[5], args[3]);
                // bool flag = scraper.lasooScrape("Drakes QLD", root_dir, "http://www.lasoo.com.au/retailer/drakes-supermarkets.html", "4053", "DrakesQLD_YYYY_MM_DD");
                bool flag = scraper.lasooScrape(args[0], root_dir, args[1], args[4], args[2]);

                if (flag == true)
                {
                    System.Console.WriteLine("The result is True");
                    return 1;
                }
                else
                {
                    System.Console.WriteLine("The result is False");
                    return 0;
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + " " + e.StackTrace);
                //return 0;
            }
        }
    }
}

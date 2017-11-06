﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Web.UI;




namespace woolScraper_cs_
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

                bool flag = scraper.woolScrap(args[0], root_dir, args[4], args[2]);

                if (flag == true)
                {
                    System.Console.WriteLine("The result is True");
                    return 1;
                }
                else
                {
                    System.Console.WriteLine("The result it False");
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

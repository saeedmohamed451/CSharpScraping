using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salefinder_cs_
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

                bool falg = scraper.saleScrap(args[0], root_dir, args[1], args[4], args[2]);

                if (falg == true)
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

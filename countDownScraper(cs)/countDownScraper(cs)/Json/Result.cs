using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace countDownScraper_cs_.Json
{
    public class Result
    {
        public long id { get; set; }
        public ResultConfig config { get; set; }
        public List<ResultSpread> spreads { get; set; }
    }
}

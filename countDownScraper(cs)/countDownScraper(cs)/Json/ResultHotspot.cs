using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace countDownScraper_cs_.Json
{
    public class ResultHotspot
    {
        public long id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public ResultPosition position { get; set; }
        public List<ResultProduct> products { get; set; }
    }
}

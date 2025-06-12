using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class StockInfo
    {
        public string stock_code { get; set; }
        public string stock_name { get; set; }
    }

    public class StrategyData
    {
        public int id { get; set; }
        public string strategy_name { get; set; }
        public string stockpicking_date { get; set; }
        public List<StockInfo> stock_info { get; set; }
    }

    public class RootObject
    {
        public int err_code { get; set; }
        public string err_msg { get; set; }
        public List<StrategyData> data { get; set; }
    }
}

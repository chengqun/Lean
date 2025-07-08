using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class OrderDto
    {
        /// <summary>
        /// 股票代码
        /// </summary>
        public string StockCode { get; set; }

        /// <summary>
        /// 下单价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 下单数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 买卖类型（Buy/Sell）
        /// </summary>
        public string OrderType { get; set; }
    }
}

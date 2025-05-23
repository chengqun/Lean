using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces
{
    public interface ISignalGenerator
    {
        /// <summary>
        /// 生成交易信号
        /// </summary>
        // 移除 async 修饰符，因为接口方法不能有方法体
        public IEnumerable<TradingSignal> GenerateSignals(Slice data);
    }
    public class TradingSignal
    {
        public Symbol Symbol { get; set; }
        public OrderDirection Direction { get; set; } // Buy/Sell
        public String OperationReson { get; set; }
        public decimal SuggestedPrice { get; set; }
        public DateTime SignalTime { get; set; }
    }
}
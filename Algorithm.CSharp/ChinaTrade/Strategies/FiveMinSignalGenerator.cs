using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using QLNet;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies
{
    public class FiveMinSignalGenerator : ISignalGenerator
    {
        private readonly Dictionary<Symbol, FiveMinAnalysis> _macdAnalysis;
        public FiveMinSignalGenerator(Dictionary<Symbol, FiveMinAnalysis> macdAnalysis)
        {
            _macdAnalysis = macdAnalysis;
        }
        public  DateTime ParseShanghaiTime(string dateString) => 
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.ParseExact(dateString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"));

        public IEnumerable<TradingSignal> GenerateSignals(Slice data)
        {
            var signals = new List<TradingSignal>();
            if (data == null) return signals;
            // var saveitems = new List<RealDataItem>();
            foreach (var symbol in _macdAnalysis.Keys)
            {
                if (!data.ContainsKey(symbol)) continue;
                var currentData = data[symbol];
                if (currentData == null) continue;
                try
                {
                    var time = ParseShanghaiTime(currentData.Date);
                    var closePrice = currentData.Close;

                    var macdAnalysis = _macdAnalysis[symbol];
                    if (macdAnalysis != null && macdAnalysis.Macd.IsReady && macdAnalysis.CloseIdentity.IsReady)
                    {
                        System.Console.WriteLine($"时间: {time} {macdAnalysis.Name},{macdAnalysis.Industry} , 收盘价: {closePrice}, MACD: {macdAnalysis.Macd.Current.Value}, 收盘价: {macdAnalysis.CloseIdentity.Current.Value}, " +
                            $"{(macdAnalysis.IsGoldenCross ? "金叉" : "false")},  {(macdAnalysis.IsDeathCross ? "死叉" : "false")}, " +
                            $"{(macdAnalysis.IsBullishDivergence ? "底背离" : "false")}, {(macdAnalysis.IsBearishDivergence ? "顶背离" : "false")}, " +
                            $"K线收益率: {macdAnalysis.KLineReturn}, 20日收益率分位数: {macdAnalysis.TwentyDayReturnQuantile}" +
                            $"日K线收益率: {macdAnalysis.DayKLineReturn}" +
                            $"指数收益率: {macdAnalysis.BenchmarkKLineReturn}" +
                            $"今日开盘涨幅:{macdAnalysis.DayNextOpenReturn}" + 
                            $"ROC: {macdAnalysis.Roc.Current.Value}" 
                            );
                        // 保存 RealDataItem 到数据库 ，自增ID不进行赋值
                        var item = new RealDataItem
                        {
                            Date = time.ToString("yyyy-MM-dd HH:mm:ss"), // 将 DateTime 转换为字符串并使用正确的 forma
                            // 存储股票名称
                            Name = macdAnalysis.Name,
                            // 存储股票所属行业
                            Industry = macdAnalysis.Industry,
                            // 存储20日收益率分位数
                            TwentyDayReturnQuantile = macdAnalysis.TwentyDayReturnQuantile,
                            // 存储日K线收益率
                            DayKLineReturn = macdAnalysis.DayKLineReturn,
                            // 存储指数收益率
                            BenchmarkKLineReturn = macdAnalysis.BenchmarkKLineReturn,
                            // 存储今日开盘涨幅
                            DayNextOpenReturn = macdAnalysis.DayNextOpenReturn
                        };
                        // 将数据项添加到队列中
                        GlobalRealDataItemList.Items.Add(item);

                        
                        var score = 0.78m;
                        var OperationReson = "";
                        // 这里模拟调用模型
                        if (macdAnalysis.DayNextOpenReturn > 0.05m)
                        {
                            score = 0.92m;
                            OperationReson += "高开买入";
                        }

                        var direction = score > 0.9m ? OrderDirection.Buy :
                                        score < 0.2m ? OrderDirection.Sell :
                                        OrderDirection.Hold;

                        signals.Add(new TradingSignal
                        {
                            Symbol = symbol,
                            Direction = direction,
                            //操作名称
                            OperationReson = OperationReson,
                            SuggestedPrice = currentData.Close,
                            SignalTime = time
                        });
                    }
                    else
                    {
                        System.Console.WriteLine($"时间: {time}, 收盘价: {closePrice}, MACD指标或收盘价指标数据尚未准备好");
                    }
                }
                catch (NullReferenceException ex)
                {
                    System.Console.WriteLine($"OnData方法中发生空引用异常: {ex.Message}");
                }
            }

            return signals;
        }
    }
}

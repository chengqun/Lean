using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using QLNet;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies
{
    public class FiveSignalGenerator : ISignalGenerator
    {
        private readonly Dictionary<Symbol, FiveAnalysis> _macdAnalysis;
        public FiveSignalGenerator(Dictionary<Symbol, FiveAnalysis> macdAnalysis)
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

                    var analysis = _macdAnalysis[symbol];
                    if (analysis != null )
                    {
                        System.Console.WriteLine($"|时间: {time}" +
                                                $"|名称:{analysis.Name}" +
                                                $"|行业{analysis.Industry}" +
                                                // $"|日K线收益率: {analysis.DayKLineReturn:F4}" +
                                                // $"|指数收益率: {analysis.BenchmarkKLineReturn:F4}" +
                                                // // Day的X特征
                                                // $"|今日开盘涨幅:{analysis.DayNextOpenReturn:F4}" +
                                                // Y
                                                $"|未来收益率 {analysis.MinuteNextDayReturn:F4}" 
                                                // // 分钟的X特征
                                                // $"|分钟K线收益率: {analysis.MinuteKLineReturn:F4}" +
                                                // $"|分钟量比: {analysis.MinuteVolumeRatio:F4}" +
                                                // $"|分钟量比3: {analysis.MinuteVolumeRatio3:F4}" +
                                                // $"|分钟Ema斜率: {analysis.MinuteEmaSlope:F4}" +
                                                // $"|分钟macd背离: {analysis.MinuteMacdDivergence}" +
                                                // $"|分钟RSI: {analysis.MinuteRsi:F4}" +
                                                // $"|分钟突破前30分钟高点: {analysis.MinutePriceBreakout}"

                            );
                        // 保存 RealDataItem 到数据库 ，自增ID不进行赋值
                        var item = new RealDataItem
                        {
                            Date = time.ToString("yyyy-MM-dd HH:mm:ss"), // 将 DateTime 转换为字符串并使用正确的 forma
                            // 存储股票名称
                            Name = analysis.Name,
                            // 存储股票所属行业
                            Industry = analysis.Industry,
                            // X特征

                            // MinuteMacdDivergence = analysis.MinuteMacdDivergence, // 分钟MACD背离
                            // MinuteRsi = Math.Round(analysis.MinuteRsi.Current.Value, 2), // 分钟RSI
                            // MinutePriceBreakout = analysis.MinutePriceBreakout, // 分钟突破前30分钟高点
                            // Y特征
                            Lable = Math.Round(analysis.MinuteNextDayReturn, 4) // 第二天的收益率
                        };
                        // 将数据项添加到队列中
                        GlobalRealDataItemList.Items.Add(item);
                        var score = 0.78m;
                        var OperationReson = "";

                        // // 这里模拟调用模型
                        // if (analysis.MinutePriceBreakout==1 )
                        // {
                        //     score = 0.95m;
                        //     OperationReson += "分钟突破前30分钟高点，";
                        //     // 记录买入时间
                        //     if (analysis.BuyTime == default(DateTime))
                        //     {
                        //         analysis.SetBuyTime(time);
                        //     }
                        // }
                        // // 次日收盘卖出
                        // if (analysis.BuyTime != default(DateTime) &&
                        //     time.Date > analysis.BuyTime.Date &&
                        //     time.Hour == 14 && time.Minute == 55)
                        // {
                        //     score = 0.15m;
                        //     OperationReson += "次日收盘卖出";
                        //     analysis.SetBuyTime(default(DateTime)); // 清除买入时间
                        // }

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
                    _macdAnalysis[symbol] = analysis; // 更新分析数据,主要是买入价
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

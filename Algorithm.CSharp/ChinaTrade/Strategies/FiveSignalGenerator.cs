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
                        System.Console.WriteLine(
                                                // Y标签
                                                $"|未来收益率 {analysis.MinuteNextDayReturn:F4}" +
                                                // 基本信息
                                                $"|时间: {time}" +
                                                $"|名称:{analysis.Name}" +
                                                $"|行业{analysis.Industry}" +
                                                $"|price: {closePrice:F4}" +
                                                // 指数信息
                                                $"|指数收益率: {analysis.BenchmarkKLineReturn:F4}" +
                                                // 日信息
                                                $"|日K线收益率: {analysis.DayKLineReturn:F4}" +
                                                // macd
                                                $"|日MACD: {analysis.DayMacd.Histogram*2:F2}" +
                                                $"|日DIFF: {analysis.DayMacd:F2}" +
                                                $"|日DEA: {analysis.DayMacd.Signal:F2}" +
                                                // 开盘信息
                                                $"|今日开盘涨幅:{analysis.OpenReturn:F4}" +
                                                // 分钟K线信息
                                                $"|分钟K线收益率: {analysis.MinuteKLineReturn:F4}" +
                                                $"|分钟突破前30分钟高点: {analysis.MinutePriceBreakout}" +
                                                // 量比
                                                $"|分钟量比: {analysis.MinuteVolumeRatio:F4}" +
                                                $"|分钟量比3: {analysis.MinuteVolumeRatio3:F4}" +
                                                // Ema斜率
                                                $"|分钟Ema斜率: {analysis.MinuteEmaSlope:F4}" +
                                                // MACD背离
                                                $"|分钟macd背离: {analysis.MinuteMacdDivergence}" +
                                                $"|分钟MACD: {analysis.MinuteMacd.Histogram*2:F2}" +
                                                $"|分钟DIFF: {analysis.MinuteMacd:F2}" +
                                                $"|分钟DEA: {analysis.MinuteMacd.Signal:F2}" +
                                                // RSI
                                                $"|分钟RSI: {analysis.MinuteRsiValue:F4}" 

                            );
                        // 保存 RealDataItem 到数据库 ，自增ID不进行赋值
                        var item = new RealDataItem
                        {

                            // Y特征
                            Lable = Math.Round(analysis.MinuteNextDayReturn, 4), // 第二天的收益率
                            // 基本信息
                            Date = time.ToString("yyyy-MM-dd HH:mm:ss"), // 将 DateTime 转换为字符串并使用正确的 forma
                            Name = analysis.Name,                        // 存储股票名称
                            Industry = analysis.Industry,                // 存储股票所属行业
                            Price = Math.Round(closePrice, 4),          // 当前价格
                            // 指数信息
                            BenchmarkKLineReturn = Math.Round(analysis.BenchmarkKLineReturn, 4), // 基准指数收益率
                            // 日信息
                            DayKLineReturn = Math.Round(analysis.DayKLineReturn, 4), // 日K线收益率
                            // macd
                            DayMacd = Math.Round(analysis.DayMacd.Histogram*2, 2), // 日MACD
                            DayDIFF = Math.Round(analysis.DayMacd, 2) , // 日MACD柱状图
                            DayDEA = Math.Round(analysis.DayMacd.Signal, 2), // 日MACD信号线
                            // 开盘信息
                            OpenReturn = Math.Round(analysis.OpenReturn, 4), // 今日开盘涨幅
                            // 分钟K线信息
                            // 价格
                            MinuteKLineReturn = Math.Round(analysis.MinuteKLineReturn, 4), // 分钟K线收益率
                            MinutePriceBreakout = analysis.MinutePriceBreakout, // 分钟突破前30分钟高点
                            // 量比
                            MinuteVolumeRatio = Math.Round(analysis.MinuteVolumeRatio, 4), // 分钟量比
                            MinuteVolumeRatio3 = Math.Round(analysis.MinuteVolumeRatio3, 4), // 与前3周期平均量比
                            // Ema斜率
                            MinuteEmaSlope = Math.Round(analysis.MinuteEmaSlope, 4), // 分钟Ema斜率
                            // MACD背离
                            MinMACD = Math.Round(analysis.MinuteMacd.Histogram*2, 2) , // 
                            MinDIFF = Math.Round(analysis.MinuteMacd, 2), // 分钟MACD柱状图
                            MinDEA = Math.Round(analysis.MinuteMacd.Signal, 2), // 分钟MACD信号线
                            // RSI
                            MinuteRsi = Math.Round(analysis.MinuteRsiValue, 2), // 分钟RSI


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

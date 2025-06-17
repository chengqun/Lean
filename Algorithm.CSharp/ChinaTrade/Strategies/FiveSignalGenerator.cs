using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.ML;
using QLNet;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.MLnet;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using static QuantConnect.Algorithm.CSharp.ChinaTrade.MLnet.SampleRegression;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies
{
    public class FiveSignalGenerator : ISignalGenerator
    {
        private readonly Dictionary<Symbol, FiveAnalysis> _macdAnalysis;
        private readonly PredictionEngine<ModelInput, ModelOutput> _predictionEngine;
        public FiveSignalGenerator(Dictionary<Symbol, FiveAnalysis> macdAnalysis,PredictionEngine<ModelInput, ModelOutput> predictionEngine)
        {
            _macdAnalysis = macdAnalysis;
            _predictionEngine = predictionEngine;
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
                    if (analysis != null)
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
                                                // 
                                                // 量比
                                                $"|日量比: {analysis.DayVolumeRatio:F4}" +
                                                $"|日量比3: {analysis.DayVolumeRatio3:F4}" +
                                                // macd
                                                $"|日MACD: {analysis.DayMacd.Histogram * 2:F2}" +
                                                $"|日DIFF: {analysis.DayMacd:F2}" +
                                                $"|日DEA: {analysis.DayMacd.Signal:F2}" +
                                                $"|日MACD趋势: {analysis.DayMacdTrend:F2}" +
                                                // 开盘信息
                                                $"|今日开盘涨幅:{analysis.OpenReturn:F4}" +
                                                // 分钟K线信息
                                                $"|分钟K线收益率: {analysis.MinuteKLineReturn:F4}" +
                                                $"|分钟K线距离昨日收盘收益: {analysis.MinuteKLineReturnFromPreviousClose:F4}" +
                                                $"|分钟突破前30分钟高点: {analysis.MinutePriceBreakout}" +
                                                $"|分钟突破前30分钟低点: {analysis.MinutePriceBreakoutEma}" +
                                                $"|分钟弱到强: {analysis.MinuteWeakToStrong}" +
                                                // 量比
                                                $"|分钟量比: {analysis.MinuteVolumeRatio:F4}" +
                                                $"|分钟量比3: {analysis.MinuteVolumeRatio3:F4}" +
                                                // Ema斜率
                                                $"|分钟Ema斜率: {analysis.MinuteEmaSlope:F4}" +
                                                // MACD背离
                                                $"|分钟macd背离: {analysis.MinuteMacdDivergence}" +
                                                $"|分钟MACD: {analysis.MinuteMacd.Histogram * 2:F2}" +
                                                $"|分钟DIFF: {analysis.MinuteMacd:F2}" +
                                                $"|分钟DEA: {analysis.MinuteMacd.Signal:F2}" +
                                                // RSI
                                                $"|分钟RSI: {analysis.MinuteRsiValue:F4}"

                            );
                        // Create single instance of sample data from first line of dataset for model input
                        SampleRegression.ModelInput sampleData = new SampleRegression.ModelInput()
                        {
                            PreviousMinuteKLineReturn3 = (float)analysis.PreviousMinuteKLineReturn3,
                            OpenReturn = (float)analysis.OpenReturn,
                            PreviousMinuteKLineReturn2 = (float)analysis.PreviousMinuteKLineReturn2,
                            PreviousMinuteKLineReturn1 = (float)analysis.PreviousMinuteKLineReturn1,
                            MinuteKLineReturn = (float)analysis.MinuteKLineReturn,
                            MinuteKLineReturnFromPreviousClose = (float)analysis.MinuteKLineReturnFromPreviousClose,
                            MinutePriceBreakout =  analysis.MinutePriceBreakout?1f:0f,
                            MinutePriceBreakoutEma =  analysis.MinutePriceBreakoutEma?1f:0f,
                        };
                        var predictionResult = _predictionEngine.Predict(sampleData);
                        // 保存 RealDataItem 到数据库 ，自增ID不进行赋值
                        var item = new RealDataItem
                        {
                            // Y特征
                            Lable = Math.Round(analysis.MinuteNextDayReturn, 4), // 第二天的收益率
                            Lable5 = Math.Round(analysis.MinuteNext5DayReturn, 4), // 第5天的收益率
                            Score = predictionResult.Score,
                            // 基本信息
                            Date = time.ToString("yyyy-MM-dd HH:mm:ss"), // 将 DateTime 转换为字符串并使用正确的 forma
                            Name = analysis.Name,                        // 存储股票名称
                            Industry = analysis.Industry,                // 存储股票所属行业
                            Price = Math.Round(closePrice, 4),          // 当前价格
                            // 指数信息
                            BenchmarkKLineReturn = Math.Round(analysis.BenchmarkKLineReturn, 4), // 基准指数收益率
                            // 日信息
                            DayStrategyName = analysis.DayStrategyName, // 日策略名称
                            DayKLineReturn = Math.Round(analysis.DayKLineReturn, 4), // 日K线收益率
                            DayKLineReturn5 = Math.Round(analysis.DayKLineReturn5, 4), // 日K线收益率5
                            DayVolumeRatio = Math.Round(analysis.DayVolumeRatio, 4), // 日量比
                            DayVolumeRatio3 = Math.Round(analysis.DayVolumeRatio3, 4), // 日量比3
                            // macd
                            DayMacd = Math.Round(analysis.DayMacd.Histogram * 2, 2), // 日MACD
                            // DayATR = Math.Round(analysis.DayATR,2),
                            DayDIFF = Math.Round(analysis.DayMacd, 2), // 日MACD柱状图
                            DayDEA = Math.Round(analysis.DayMacd.Signal, 2), // 日MACD信号线
                            DayMacdTrend = Math.Round(analysis.DayMacdTrend, 2), // 日MACD趋势
                            // 开盘信息
                            OpenReturn = Math.Round(analysis.OpenReturn, 4), // 今日开盘涨幅
                            PreviousOpenReturn1 = Math.Round(analysis.PreviousOpenReturn1, 4), // 昨日开盘涨幅
                            PreviousOpenReturn2 = Math.Round(analysis.PreviousOpenReturn2, 4), // 昨日开盘涨幅
                            // 分钟K线信息
                            // 价格
                            MinuteKLineReturn = Math.Round(analysis.MinuteKLineReturn, 4), // 分钟K线收益率
                            MinuteKLineReturn5day = Math.Round(analysis.MinuteKLineReturn5day, 4), // 分钟K线收益率
                            PreviousMinuteKLineReturn1 = Math.Round(analysis.PreviousMinuteKLineReturn1, 4), // 分钟K线收益率
                            PreviousMinuteKLineReturn2 = Math.Round(analysis.PreviousMinuteKLineReturn2, 4), // 分钟K线收益率
                            PreviousMinuteKLineReturn3 = Math.Round(analysis.PreviousMinuteKLineReturn3, 4), // 分钟K线收益率
                            MinuteKLineReturnFromPreviousClose = Math.Round(analysis.MinuteKLineReturnFromPreviousClose, 4), // 分钟K线距离昨日收盘收益
                            MinutePriceBreakout = analysis.MinutePriceBreakout, // 分钟突破前30分钟高点
                            MinutePriceBreakoutEma = analysis.MinutePriceBreakoutEma, // 分钟突破前30分钟高点
                            MinuteWeakToStrong = analysis.MinuteWeakToStrong, // 分钟弱到强
                            // 量比
                            MinuteVolumeRatio = Math.Round(analysis.MinuteVolumeRatio, 4), // 分钟量比
                            MinuteVolumeRatio3 = Math.Round(analysis.MinuteVolumeRatio3, 4), // 与前3周期平均量比
                            // Ema斜率
                            MinuteEmaSlope = Math.Round(analysis.MinuteEmaSlope, 4), // 分钟Ema斜率
                            // MACD
                            MinuteMacdDivergence = analysis.MinuteMacdDivergence, // 分钟MACD背离
                            MinMACD = Math.Round(analysis.MinuteMacd.Histogram * 2, 2), // 
                            MinDIFF = Math.Round(analysis.MinuteMacd, 2), // 分钟MACD柱状图
                            MinDEA = Math.Round(analysis.MinuteMacd.Signal, 2), // 分钟MACD信号线
                            // RSI
                            MinuteRsi = Math.Round(analysis.MinuteRsiValue, 2), // 分钟RSI
                        };
                        // 将数据项添加到队列中
                        GlobalRealDataItemList.Items.Add(item);
                        // 检查 analysis.TradingSignal 是否为 null
                        if (analysis.TradingSignal != null)
                        {
                            signals.Add(analysis.TradingSignal);
                        }
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

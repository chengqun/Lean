using System.Collections.Generic;
using QuantConnect.Data;
using System.Net.Http;
using System.Linq;
using System;
using System.Globalization;
using QuantConnect.Indicators;
using System.Xml.Linq;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class FiveMinAnalysis
    {
        public string Name { get; private set; }
        public string Industry { get; private set; }

        // 分钟K线相关
        public MovingAverageConvergenceDivergence MinuteMacd { get; }
        public IndicatorBase<IndicatorDataPoint> MinuteClose { get; }
        public decimal MinuteKLineReturn { get; private set; }
        public decimal MinuteTwentyBarReturnQuantile { get; private set; }
        public decimal MinuteNextDayReturn { get; private set; }
        public bool MinuteIsLowerGoldenCross { get; private set; }
        public bool MinuteIsLowerDeathCross { get; private set; }
        public bool MinuteIsGoldenCross { get; private set; }
        public bool MinuteIsDeathCross { get; private set; }
        public bool MinuteIsUpperGoldenCross { get; private set; }
        public bool MinuteIsUpperDeathCross { get; private set; }
        public bool MinuteIsBullishDivergence { get; private set; }
        public bool MinuteIsBearishDivergence { get; private set; }

        // 日K线相关
        public MovingAverageConvergenceDivergence DayMacd { get; }
        public RateOfChange DayRoc { get; }
        public IndicatorBase<IndicatorDataPoint> DayClose { get; }
        public IndicatorBase<IndicatorDataPoint> DayNextOpen { get; }
        public IndicatorBase<IndicatorDataPoint> DayNext2Open { get; }
        public decimal DayKLineReturn { get; private set; }
        public decimal DayNextOpenReturn { get; private set; }
        public decimal DayNext2Close { get; private set; }

        // 指数相关
        public MovingAverageConvergenceDivergence BenchmarkMacd { get; }
        public IndicatorBase<IndicatorDataPoint> BenchmarkClose { get; }
        public decimal BenchmarkKLineReturn { get; private set; }

        public FiveMinAnalysis(
            MovingAverageConvergenceDivergence minuteMacd,
            IndicatorBase<IndicatorDataPoint> minuteClose,
            string name,
            string industry,
            MovingAverageConvergenceDivergence dayMacd,
            IndicatorBase<IndicatorDataPoint> dayClose,
            IndicatorBase<IndicatorDataPoint> dayNextOpen,
            MovingAverageConvergenceDivergence benchmarkMacd,
            IndicatorBase<IndicatorDataPoint> benchmarkClose,
            RateOfChange dayRoc,
            IndicatorBase<IndicatorDataPoint> dayNext2Open
        )
        {
            // 分钟K线
            Name = name;
            Industry = industry;
            MinuteMacd = minuteMacd;
            MinuteClose = minuteClose;
            MinuteMacd.Updated += (sender, updated) => UpdateMinuteStatus();
            MinuteClose.Updated += (sender, updated) => UpdateMinuteStatus();
            UpdateMinuteStatus();

            // 日K线
            DayMacd = dayMacd;
            DayClose = dayClose;
            DayNextOpen = dayNextOpen;
            DayRoc = dayRoc;
            DayNext2Open = dayNext2Open;
            DayNext2Open.Updated += (sender, updated) => UpdateDayStatus();
            DayRoc.Updated += (sender, updated) => UpdateDayStatus();
            DayMacd.Updated += (sender, updated) => UpdateDayStatus();
            DayClose.Updated += (sender, updated) => UpdateDayStatus();
            DayNextOpen.Updated += (sender, updated) => UpdateDayStatus();
            UpdateDayStatus();

            // 指数
            BenchmarkMacd = benchmarkMacd;
            BenchmarkClose = benchmarkClose;
            BenchmarkMacd.Updated += (sender, updated) => UpdateBenchmarkStatus();
            BenchmarkClose.Updated += (sender, updated) => UpdateBenchmarkStatus();
            UpdateBenchmarkStatus();
        }

        private void UpdateBenchmarkStatus()
        {
            try
            {
                var macdValue = BenchmarkMacd.Current?.Value ?? 0;
                var closePrice = BenchmarkClose.Current?.Value ?? 0;
                var previousClosePrice = BenchmarkClose.Samples > 1 ? BenchmarkClose[1]?.Value ?? 0 : 0;
                BenchmarkKLineReturn = previousClosePrice != 0 ? (closePrice - previousClosePrice) / previousClosePrice : 0;
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"FiveMinAnalysis.UpdateBenchmarkStatus 空引用异常: {ex.Message}");
            }
        }

        private void UpdateDayStatus()
        {
            try
            {
                var macdValue = DayMacd.Current?.Value ?? 0;
                var closePrice = DayClose.Current?.Value ?? 0;
                var nextOpenPrice = DayNextOpen.Current?.Value ?? 0;
                var next2OpenPrice = DayNext2Open.Current?.Value ?? 0;
                var previousClosePrice = DayClose.Samples > 1 ? DayClose[1]?.Value ?? 0 : 0;
                DayKLineReturn = previousClosePrice != 0 ? (closePrice - previousClosePrice) / previousClosePrice : 0;
                DayNextOpenReturn = closePrice != 0 ? (nextOpenPrice - closePrice) / closePrice : 0;
                DayNext2Close = next2OpenPrice;
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"FiveMinAnalysis.UpdateDayStatus 空引用异常: {ex.Message}");
            }
        }

        private void UpdateMinuteStatus()
        {
            try
            {
                var macdValue = MinuteMacd.Current?.Value ?? 0;
                var closePrice = MinuteClose.Current?.Value ?? 0;
                var previousClosePrice = MinuteClose.Samples > 1 ? MinuteClose[1]?.Value ?? 0 : 0;
                MinuteKLineReturn = previousClosePrice != 0 ? (closePrice - previousClosePrice) / previousClosePrice : 0;
                MinuteNextDayReturn = closePrice != 0 ? (DayNext2Close / closePrice - 1) : 0;

                // 20bar收益率分位数
                if (MinuteClose.Samples >= 20)
                {
                    var returns = new List<decimal>();
                    for (int i = 0; i < 19; i++)
                    {
                        var current = MinuteClose[i]?.Value ?? 0;
                        var prev = MinuteClose[i + 1]?.Value ?? 0;
                        if (prev != 0)
                        {
                            returns.Add((current - prev) / prev);
                        }
                    }
                    if (returns.Count > 0)
                    {
                        var sortedReturns = returns.OrderBy(x => x).ToList();
                        var denominator = MinuteClose[1]?.Value ?? 0;
                        var currentReturn = denominator != 0 ? MinuteClose[0]?.Value / denominator - 1 : 0;
                        int count = sortedReturns.Count(x => x < currentReturn);
                        int equal = sortedReturns.Count(x => x == currentReturn);
                        MinuteTwentyBarReturnQuantile = (count + 0.5m * equal) / sortedReturns.Count;
                    }
                    else
                    {
                        MinuteTwentyBarReturnQuantile = 0;
                    }
                }
                else
                {
                    MinuteTwentyBarReturnQuantile = 0;
                }

                // 置信度与形态
                const decimal tolerance = 0.0025m;
                decimal fast = MinuteMacd.Fast;
                decimal delta = (MinuteMacd.Current.Value - MinuteMacd.Signal.Current.Value) / (fast != 0 ? fast : 1);
                bool isSignificant = Math.Abs(fast) > 0.0001m;
                decimal prevDelta = MinuteMacd[1] != null && MinuteMacd.Signal[1] != null ? (MinuteMacd[1].Value - MinuteMacd.Signal[1].Value) / (fast != 0 ? fast : 1) : 0;

                MinuteIsUpperGoldenCross = MinuteMacd.Samples > 1 && isSignificant && delta > tolerance && prevDelta <= tolerance;
                MinuteIsUpperDeathCross = MinuteMacd.Samples > 1 && isSignificant && delta < tolerance && prevDelta >= tolerance;
                MinuteIsLowerGoldenCross = MinuteMacd.Samples > 1 && isSignificant && delta > -tolerance && prevDelta <= -tolerance;
                MinuteIsLowerDeathCross = MinuteMacd.Samples > 1 && isSignificant && delta < -tolerance && prevDelta >= -tolerance;
                MinuteIsGoldenCross = MinuteMacd.Samples > 1 &&
                                      MinuteMacd.Current.Value > MinuteMacd.Signal.Current.Value &&
                                      MinuteMacd[1] != null && MinuteMacd.Signal[1] != null &&
                                      MinuteMacd[1].Value <= MinuteMacd.Signal[1].Value;
                MinuteIsDeathCross = MinuteMacd.Samples > 1 &&
                                     MinuteMacd.Current.Value < MinuteMacd.Signal.Current.Value &&
                                     MinuteMacd[1] != null && MinuteMacd.Signal[1] != null &&
                                     MinuteMacd[1].Value >= MinuteMacd.Signal[1].Value;

                // 顶背离
                MinuteIsBearishDivergence = false;
                if (MinuteClose.Samples > 2 && MinuteMacd.Samples > 2)
                {
                    var prevHigh = 0m;
                    if (MinuteClose[1] != null && MinuteClose[2] != null)
                        prevHigh = Math.Max(MinuteClose[1].Value, MinuteClose[2].Value);
                    var prevMacdHigh = 0m;
                    if (MinuteMacd[1] != null && MinuteMacd[2] != null)
                        prevMacdHigh = Math.Max(MinuteMacd[1].Value, MinuteMacd[2].Value);
                    if (closePrice > prevHigh && macdValue < prevMacdHigh)
                        MinuteIsBearishDivergence = true;
                }
                // 底背离
                MinuteIsBullishDivergence = false;
                if (MinuteClose.Samples > 2 && MinuteMacd.Samples > 2)
                {
                    decimal? prevLow = null, prevMacdLow = null;
                    if (MinuteClose[1] != null && MinuteClose[2] != null)
                        prevLow = Math.Min(MinuteClose[1].Value, MinuteClose[2].Value);
                    if (MinuteMacd[1] != null && MinuteMacd[2] != null)
                        prevMacdLow = Math.Min(MinuteMacd[1].Value, MinuteMacd[2].Value);
                    if (MinuteClose[1] != null && MinuteClose[2] != null &&
                        MinuteMacd[1] != null && MinuteMacd[2] != null &&
                        prevLow.HasValue && prevMacdLow.HasValue &&
                        closePrice < prevLow && macdValue > prevMacdLow)
                    {
                        MinuteIsBullishDivergence = true;
                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"FiveMinAnalysis.UpdateMinuteStatus 空引用异常: {ex.Message}");
            }
        }
    }
}

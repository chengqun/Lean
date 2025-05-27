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
        public ExponentialMovingAverage MinuteEma { get; }
        public RelativeStrengthIndex MinuteRsi { get; }
        public IndicatorBase<IndicatorDataPoint> MinuteClose { get; }
        public IndicatorBase<IndicatorDataPoint> MinuteVolume { get; }

        // 设置买入时间
        public DateTime BuyTime { get; private set; }

        //定义函数设置BuyTime和SellTime
        public void SetBuyTime(DateTime time)
        {
            BuyTime = time;
        }


        // 定义Y
        public decimal MinuteNextDayReturn { get; private set; }

        // 定义X
        // 特征1:分钟K线收益率
        public decimal MinuteKLineReturn { get; private set; }
        // 特征2:分钟量比 
        public decimal MinuteVolumeRatio { get; private set; }
        // 特征3: 与前3周期平均量比
        public decimal MinuteVolumeRatio3 { get; private set; }
        // 特征4: 成交量EMA斜率
        public decimal MinuteEmaSlope  {get; private set; }
        // 特征5: 分钟MACD背离
        public decimal MinuteMacdDivergence { get; private set; }

        // 特征6: 价格突破前30分钟高点
        public decimal MinutePriceBreakout { get; private set; }
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
            ExponentialMovingAverage minuteEma,
            RelativeStrengthIndex minuteRsi,
            IndicatorBase<IndicatorDataPoint> minuteClose,
            IndicatorBase<IndicatorDataPoint> minuteVolume,//
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
            MinuteEma = minuteEma;
            MinuteRsi = minuteRsi;
            MinuteClose = minuteClose;
            MinuteVolume = minuteVolume;

            MinuteMacd.Updated += (sender, updated) => UpdateMinuteStatus();
            MinuteEma.Updated += (sender, updated) => UpdateMinuteStatus();
            MinuteRsi.Updated += (sender, updated) => UpdateMinuteStatus();
            MinuteClose.Updated += (sender, updated) => UpdateMinuteStatus();
            MinuteVolume.Updated += (sender, updated) => UpdateMinuteStatus();
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
                var closePrice = MinuteClose.Current?.Value ?? 0;
                var previousClosePrice1 = MinuteClose.Samples > 1 ? MinuteClose[1]?.Value ?? 0 : 0;
                var previousClosePrice2 = MinuteClose.Samples > 2 ? MinuteClose[2]?.Value ?? 0 : 0;
                var previousClosePrice3 = MinuteClose.Samples > 3 ? MinuteClose[3]?.Value ?? 0 : 0;
                var previousClosePrice4 = MinuteClose.Samples > 4 ? MinuteClose[4]?.Value ?? 0 : 0;
                var previousClosePrice5 = MinuteClose.Samples > 5 ? MinuteClose[5]?.Value ?? 0 : 0;
                var previousClosePrice6 = MinuteClose.Samples > 6 ? MinuteClose[6]?.Value ?? 0 : 0;
                // 最大值
                var maxPreviousClosePrice = Math.Max(Math.Max(previousClosePrice1, previousClosePrice2), 
                    Math.Max(Math.Max(previousClosePrice3, previousClosePrice4), 
                    Math.Max(previousClosePrice5, previousClosePrice6)));
                // 量比
                var volume = MinuteVolume.Current?.Value ?? 0;
                var previousVolume1 = MinuteVolume.Samples > 1 ? MinuteVolume[1]?.Value ?? 0 : 0;
                var previousVolume2 = MinuteVolume.Samples > 2 ? MinuteVolume[2]?.Value ?? 0 : 0;
                var previousVolume3 = MinuteVolume.Samples > 3 ? MinuteVolume[3]?.Value ?? 0 : 0;
                // 前三根K线的平均量比
                var averageVolume = (previousVolume1 + previousVolume2 + previousVolume3) / 3;

                // 成交量EMA斜率
                var emaValue = MinuteEma.Current?.Value ?? 0;
                var previousEmaValue = MinuteEma.Samples > 1 ? MinuteEma[1]?.Value ?? 0 : 0;

                // MACD
                var macdValue = MinuteMacd.Current?.Value ?? 0;
                var previousMacdValue = MinuteMacd.Samples > 1 ? MinuteMacd[1]?.Value ?? 0 : 0;

                // rsi斜率
                // var rsiValue = MinuteRsi.Current?.Value ?? 0;
                // var previousRsiValue = MinuteRsi.Samples > 1 ? MinuteRsi[1]?.Value ?? 0 : 0;

                // 定义Y
                MinuteNextDayReturn = closePrice != 0 ? (DayNext2Close / closePrice - 1) : 0;

                // 加工X
                // 分钟K线收益率，信号的这根是涨是跌，涨幅都很关键的。
                MinuteKLineReturn = previousClosePrice1 != 0 ? (closePrice - previousClosePrice1) / previousClosePrice1 : 0;
                // 量比
                MinuteVolumeRatio = previousVolume1 != 0 ? (volume / previousVolume1) : 0;
                MinuteVolumeRatio3 = averageVolume != 0 ? (volume / averageVolume) : 0;
                // 成交量EMA斜率
                MinuteEmaSlope = previousEmaValue != 0 ? (emaValue - previousEmaValue) / previousEmaValue : 0;

                // macd背离，价格新高但macd未同步新高
                MinuteMacdDivergence = 0;
                if (closePrice > previousClosePrice1 && macdValue < previousMacdValue)
                {
                    MinuteMacdDivergence = 1; // 表示价格新高但MACD未同步新高
                }
                else if (closePrice < previousClosePrice1 && macdValue > previousMacdValue)
                {
                    MinuteMacdDivergence = -1; // 表示价格新低但MACD未同步新低
                }
                else
                {
                    MinuteMacdDivergence = 0; // 没有背离
                }
                // 价格突破前30分钟高点，前一根没突破，当前根突破了
                MinutePriceBreakout = 0;
                if (closePrice > maxPreviousClosePrice && previousClosePrice1 <= maxPreviousClosePrice)
                {
                    MinutePriceBreakout = 1; // 表示价格突破前30分钟高点
                }
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine($"FiveMinAnalysis.UpdateMinuteStatus 空引用异常: {ex.Message}");
            }
        }
    }
}

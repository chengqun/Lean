using System;
using System.Linq;
using QuantConnect.Indicators;
using QuantConnect.Orders;


namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models;

public class FiveAnalysis
{
    private QCAlgorithm _algo;
    // 日线
    public IndicatorBase<IndicatorDataPoint> DayNext2Close { get; private set; }  
    public IndicatorBase<IndicatorDataPoint> NextOpen { get; private set; }
    public IndicatorBase<IndicatorDataPoint> DayClose { get; private set; }
    // 分钟线
    public IndicatorBase<IndicatorDataPoint> MinuteClose { get; private set; }
    public IndicatorBase<IndicatorDataPoint> MinuteVolume { get;private set; }
    public MovingAverageConvergenceDivergence MinuteMacd { get; private set; }
    public ExponentialMovingAverage MinuteEma { get; private set;}
    public RelativeStrengthIndex MinuteRsi { get;private set; }
    // 指数
    public IndicatorBase<IndicatorDataPoint> BenchmarkClose { get; private set; }
    public Symbol Symbol { get; set; }
    public string Name { get; set; }
    public string Industry { get; set; }

    // 定义X
    public decimal BenchmarkKLineReturn { get; private set; }
    public decimal DayKLineReturn { get; private set; }
    public decimal OpenReturn { get; set; } // 今日开盘涨幅
    // 分钟线指标
    // 分钟K线收益率
    public decimal MinuteKLineReturn { get; private set; }
    // 价格突破前30分钟高点
    public decimal MinutePriceBreakout { get; private set; }
    // 量比 
    public decimal MinuteVolumeRatio { get; private set; }
    // 与前3周期平均量比
    public decimal MinuteVolumeRatio3 { get; private set; }
    
    // 成交量EMA斜率
    public decimal MinuteEmaSlope { get; private set; }
    //分钟MACD背离
    public decimal MinuteMacdDivergence { get; private set; }


    // 定义Y
    public decimal MinuteNextDayReturn { get; private set; }

    public FiveAnalysis(QCAlgorithm algo, string code, string name, string industry)
    {
        Name = name.ToString();
        Industry = industry.ToString();
        _algo = algo ?? throw new ArgumentNullException(nameof(algo));
        InitializeIndicators(code);

        MinuteClose.Updated += (sender, updated) => UpdateMinuteStatus();
    }

    private void UpdateMinuteStatus()
    {
        try
        {
            // 价格
            var closePrice = MinuteClose.Current?.Value ?? 0;
            var previousClosePrice1 = MinuteClose.Samples > 1 ? MinuteClose[1]?.Value ?? 0 : 0;
            var previousClosePrice2 = MinuteClose.Samples > 2 ? MinuteClose[2]?.Value ?? 0 : 0;
            var previousClosePrice3 = MinuteClose.Samples > 3 ? MinuteClose[3]?.Value ?? 0 : 0;
            var previousClosePrice4 = MinuteClose.Samples > 4 ? MinuteClose[4]?.Value ?? 0 : 0;
            var previousClosePrice5 = MinuteClose.Samples > 5 ? MinuteClose[5]?.Value ?? 0 : 0;
            var previousClosePrice6 = MinuteClose.Samples > 6 ? MinuteClose[6]?.Value ?? 0 : 0;
            // 30分钟最大值
            var maxPreviousClosePrice = Math.Max(Math.Max(previousClosePrice1, previousClosePrice2), 
                Math.Max(Math.Max(previousClosePrice3, previousClosePrice4), 
                Math.Max(previousClosePrice5, previousClosePrice6)));
            // 价格突破前30分钟高点，前一根没突破，当前根突破了
            MinutePriceBreakout = 0;
            if (closePrice > maxPreviousClosePrice && previousClosePrice1 <= maxPreviousClosePrice)
            {
                MinutePriceBreakout = 1; // 表示价格突破前30分钟高点
            }
            MinuteKLineReturn = previousClosePrice1 != 0 ? (closePrice / previousClosePrice1 - 1) : 0;

            // 成交量
            var volume = MinuteVolume.Current?.Value ?? 0;
            var previousVolume1 = MinuteVolume.Samples > 1 ? MinuteVolume[1]?.Value ?? 0 : 0;
            var previousVolume2 = MinuteVolume.Samples > 2 ? MinuteVolume[2]?.Value ?? 0 : 0;
            var previousVolume3 = MinuteVolume.Samples > 3 ? MinuteVolume[3]?.Value ?? 0 : 0;
            // 前三根K线的平均量比
            var averageVolume = (previousVolume1 + previousVolume2 + previousVolume3) / 3;
            // 量比
            MinuteVolumeRatio = previousVolume1 != 0 ? (volume / previousVolume1) : 0;
            MinuteVolumeRatio3 = averageVolume != 0 ? (volume / averageVolume) : 0;

            // 成交量EMA斜率
            var emaValue = MinuteEma.Current?.Value ?? 0;
            var previousEmaValue = MinuteEma.Samples > 1 ? MinuteEma[1]?.Value ?? 0 : 0;
            MinuteEmaSlope = previousEmaValue != 0 ? (emaValue - previousEmaValue) / previousEmaValue : 0;
            // MACD
            var macdValue = MinuteMacd.Current?.Value ?? 0;
            var previousMacdValue = MinuteMacd.Samples > 1 ? MinuteMacd[1]?.Value ?? 0 : 0;
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

            // 获取日线数据
            var nextDayClose = DayNext2Close.Current?.Value ?? 0;
            var nextDayOpen = NextOpen.Current?.Value ?? 0;

            var dayClose = DayClose.Current?.Value ?? 0;
            var previousDayClose1 = DayClose.Samples > 1 ? DayClose[1]?.Value ?? 0 : 0;
            DayKLineReturn = previousDayClose1 != 0 ? (dayClose / previousDayClose1 - 1) : 0;
            // 获取指数
            var benchmarkClose = BenchmarkClose.Current?.Value ?? 0;
            var previousbenchmarkClose1 = BenchmarkClose.Samples > 1 ? BenchmarkClose[1]?.Value ?? 0 : 0;
            BenchmarkKLineReturn = previousbenchmarkClose1 != 0 ? (benchmarkClose / previousbenchmarkClose1 - 1) : 0;

            // 定义X
            OpenReturn = dayClose != 0 ? (nextDayOpen / dayClose - 1) : 0;
            // 定义Y
            MinuteNextDayReturn = closePrice != 0 ? (nextDayClose / closePrice - 1) : 0;
            
        }
        catch (Exception ex)
        {
            _algo.Error($"Error updating minute status for {Symbol}: {ex.Message}");
        }
    }

    private void InitializeIndicators(string code)
    {
        Symbol = _algo.AddData<Api5MinCustomData>(code, Resolution.Minute, TimeZones.Utc).Symbol;
        MinuteClose = _algo.Identity(Symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Close));
        MinuteVolume = _algo.Identity(Symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Volume));

        MinuteMacd = _algo.MACD(Symbol, 12, 26, 9, MovingAverageType.Wilders, Resolution.Minute);
        MinuteEma = _algo.EMA(Symbol, 30, Resolution.Minute);
        MinuteRsi = _algo.RSI(Symbol, 14, MovingAverageType.Wilders, Resolution.Minute);
        // 日线字段
        var daysymbol = _algo.AddData<ApiDayCustomData>(code, Resolution.Daily, TimeZones.Utc).Symbol;
        DayNext2Close = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Next2Close));
        NextOpen = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).NextOpen));
        DayClose = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Close));

        // 指数
        var benchmarkSymbol = _algo.AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
        BenchmarkClose = _algo.Identity(benchmarkSymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Close));
        _algo.SetBenchmark(benchmarkSymbol);
        if (_algo.LiveMode)
        {
            WarmUpIndicators(Symbol, daysymbol, benchmarkSymbol);
        }
    }

    public void WarmUpIndicators(Symbol minSymbol, Symbol daySymbol, Symbol benchmarkSymbol)
    {
        // 计算MACD所需最小数据量(26周期+9信号线)
        var requiredBars = 12600 + 9;
        // 获取指数的历史数据
        var benchdayhistory = _algo.History<ApiDayCustomData>(benchmarkSymbol, requiredBars * 2, Resolution.Daily);
        var benchdayhistoryList = _algo.History<ApiDayCustomData>(benchmarkSymbol, requiredBars * 2, Resolution.Daily)?.ToList();
        if (benchdayhistoryList == null || !benchdayhistoryList.Any())
        {
            return;
        }
        foreach (var bar in benchdayhistoryList.OrderBy(x => x.Time))
        {
            if (bar is ApiDayCustomData customData)
            {
                BenchmarkClose.Update(bar.EndTime, customData.Close);
            }
        }
        // day的指标
        var dayhistoryList = _algo.History<ApiDayCustomData>(daySymbol, requiredBars * 2, Resolution.Daily)?.ToList();
        if (dayhistoryList == null || !dayhistoryList.Any())
        {
            return;
        }
        foreach (var bar in dayhistoryList.OrderBy(x => x.Time))
        {
            if (bar is ApiDayCustomData customData)
            {
                DayNext2Close.Update(bar.EndTime, customData.Next2Close);
                DayClose.Update(bar.EndTime, customData.Close);
                NextOpen.Update(bar.EndTime, customData.NextOpen);
            }
        }
        // 分钟
        var historyList = _algo.History<Api5MinCustomData>(minSymbol, requiredBars * 2, Resolution.Minute)?.ToList();
        if (historyList == null || !historyList.Any())
        {
            return;
        }
        foreach (var bar in historyList.OrderBy(x => x.Time))
        {
            if (bar is Api5MinCustomData customData)
            {
                MinuteClose.Update(bar.EndTime, customData.Close);
                MinuteVolume.Update(bar.EndTime, customData.Volume);
                MinuteMacd.Update(bar.EndTime, customData.Close);
                MinuteEma.Update(bar.EndTime, customData.Close);
                MinuteRsi.Update(bar.EndTime, customData.Close);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Orders;


namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
/// <summary>
/// 传入股票代码，此类返回该时刻的各项指标数据，包含日线和分钟线数据
/// 以及指数数据。用于分析和生成交易信号。
/// 该类包含了日线和分钟线的各项指标数据，
/// 包括价格、成交量、MACD、EMA、RSI等指标。  
/// </summary>
public class FiveAnalysis
{
    private QCAlgorithm _algo;
    // 指数
    public IndicatorBase<IndicatorDataPoint> BenchmarkClose { get; private set; }
    // 日线
    public MovingAverageConvergenceDivergence DayMacd { get; private set; }
    // public AverageTrueRange DayATR { get; private set; }
    public IndicatorBase<IndicatorDataPoint> DayNext2Close { get; private set; }  
    public IndicatorBase<IndicatorDataPoint> DayNext5Close { get; private set; }  
    public IndicatorBase<IndicatorDataPoint> NextOpen { get; private set; }
    public IndicatorBase<IndicatorDataPoint> NextClose { get; private set; }
    public IndicatorBase<IndicatorDataPoint> DayClose { get; private set; }
    public IndicatorBase<IndicatorDataPoint> DayVolume { get; private set; }
    public IndicatorBase<IndicatorDataPoint> DayStrategyName { get; private set; }

    // 分钟线
    public IndicatorBase<IndicatorDataPoint> MinuteClose { get; private set; }
    public IndicatorBase<IndicatorDataPoint> MinuteOpen { get; private set; }
    public IndicatorBase<IndicatorDataPoint> MinuteVolume { get; private set; }
    public MovingAverageConvergenceDivergence MinuteMacd { get; private set; }
    public ExponentialMovingAverage MinuteEma3 { get; private set;}
    public ExponentialMovingAverage MinuteEma10 { get; private set;}
    public ExponentialMovingAverage MinuteEma20 { get; private set;}
    public ExponentialMovingAverage MinuteEma60 { get; private set;}
    public RelativeStrengthIndex MinuteRsi { get;private set; }


    public Symbol Symbol { get; set; }
    public string Name { get; set; }
    public string Industry { get; set; }
    // 定义X
    public decimal BenchmarkKLineReturn { get; private set; }
    public decimal DayKLineReturn { get; private set; }
    public decimal DayKLineReturn5 { get; private set; }
    // 量比 
    public decimal DayVolumeRatio { get; private set; }
    // 与前3周期平均量比
    public decimal DayVolumeRatio3 { get; private set; }
    public decimal OpenReturn { get; set; } // 今日开盘涨幅

    public decimal PreviousOpenReturn1 {get;set;}
    public decimal PreviousOpenReturn2 {get;set;}

    // 日线MACD指标趋势
    public decimal DayMacdTrend { get; private set; } // 日MACD柱状图
    // 分钟线指标
    // 分钟K线收益率
    public decimal MinuteKLineReturn { get; private set; }
    public decimal MinuteKLineReturn5day {get;private set;}
    public decimal PreviousMinuteKLineReturn1 {get;private set;}
    public decimal PreviousMinuteKLineReturn2 {get;private set;}
    public decimal PreviousMinuteKLineReturn3 {get;private set;}
    // 距离昨日收盘收益
    public decimal MinuteKLineReturnFromPreviousClose { get; private set; }
    // 价格突破前30分钟高点
    public bool MinutePriceBreakout { get; private set; }
    public bool MinutePriceBreakoutEma {get;private set;}
    // 
    public bool MinuteWeakToStrong { get; private set; }
    // 量比 
    public decimal MinuteVolumeRatio { get; private set; }
    // 与前3周期平均量比
    public decimal MinuteVolumeRatio3 { get; private set; }
    
    // 成交量EMA斜率
    public decimal MinuteEmaSlope { get; private set; }
    //分钟MACD背离
    public decimal MinuteMacdDivergence { get; private set; }
    // 分钟RSI值
    public decimal MinuteRsiValue { get; private set; }

    // 定义一个分数，假设是模型分生成的
    public decimal Score { get; private set; }
    // 定义Y
    public decimal MinuteDayReturn {get; private set;}  // 当日收盘浮盈
    public decimal MinuteNextDayReturn { get; private set; }  // 次日收盘浮盈
    public decimal MinuteNext5DayReturn {get; private set; }  // 5日收盘浮盈
    public List<TradingSignal> TradeHistory { get; } = new List<TradingSignal>();
    public TradingSignal LastTrade => TradeHistory.Count > 0 ? TradeHistory.Last() : null;
    public TradingSignal LastBuy => TradeHistory.LastOrDefault(r => r.Direction == OrderDirection.Buy);
    public TradingSignal LastSell => TradeHistory.LastOrDefault(r => r.Direction == OrderDirection.Sell);

    public TradingSignal TradingSignal { get; set; }
    public FiveAnalysis(QCAlgorithm algo, string code, string name, string industry)
    {
        Name = name.ToString();
        Industry = industry.ToString();
        _algo = algo ?? throw new ArgumentNullException(nameof(algo));
        InitializeIndicators(code);

        // 订阅更新事件
        BenchmarkClose.Updated += (sender, updated) => UpdateMinuteStatus();
        // 日线指标更新事件
        DayMacd.Updated += (sender, updated) => UpdateMinuteStatus();
        // DayATR.Updated += (sender, updated) => UpdateMinuteStatus();
        DayNext2Close.Updated += (sender, updated) => UpdateMinuteStatus();
        DayNext5Close.Updated += (sender, updated) => UpdateMinuteStatus();
        NextOpen.Updated += (sender, updated) => UpdateMinuteStatus();
        NextClose.Updated += (sender, updated) => UpdateMinuteStatus();
        DayClose.Updated += (sender, updated) => UpdateMinuteStatus();
        DayVolume.Updated += (sender, updated) => UpdateMinuteStatus();
        DayStrategyName.Updated += (sender, updated) => UpdateMinuteStatus();
        // 分钟线指标更新事件
        MinuteClose.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteOpen.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteVolume.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteMacd.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteEma3.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteEma10.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteEma20.Updated += (sender, updated) => UpdateMinuteStatus();
        MinuteEma60.Updated += (sender, updated) => UpdateMinuteStatus();   
        MinuteRsi.Updated += (sender, updated) => UpdateMinuteStatus();
    }

    private void UpdateMinuteStatus()
    {
        try
        {
            // 获取日线数据
            var next2DayClose = DayNext2Close.Current?.Value ?? 0;
            var nextDayClose5 = DayNext5Close.Current?.Value ?? 0;
            var nextDayOpen = NextOpen.Current?.Value ?? 0;
            var nextDayClose = NextClose.Current?.Value ?? 0;

            var previousNextDayOpen1= NextOpen.Samples > 1 ? NextOpen[1]?.Value ?? 0 : 0;
            var previousNextDayOpen2 = NextOpen.Samples > 2? NextOpen[2]?.Value?? 0 : 0;

            var dayClose = DayClose.Current?.Value ?? 0;
            var previousDayClose1 = DayClose.Samples > 1 ? DayClose[1]?.Value ?? 0 : 0;
            var previousDayClose2 = DayClose.Samples > 2? DayClose[2]?.Value?? 0 : 0;
            var previousDayClose3 = DayClose.Samples > 3? DayClose[3]?.Value?? 0 : 0;
            var previousDayClose4 = DayClose.Samples > 4? DayClose[4]?.Value?? 0 : 0;
            var previousDayClose5 = DayClose.Samples > 5? DayClose[5]?.Value?? 0 : 0;


            //除了昨天近5日涨幅
            DayKLineReturn5 = previousDayClose5!= 0? (previousDayClose1 / previousDayClose5 - 1) : 0;

            // 前一天的收盘价
            DayKLineReturn = previousDayClose1 != 0 ? (dayClose / previousDayClose1 - 1) : 0;
            // 获取日线成交量
            var dayVolume = DayVolume.Current?.Value ?? 0;
            var previousDayVolume1 = DayVolume.Samples > 1 ? DayVolume[1]?.Value ?? 0 : 0;
            var previousDayVolume2 = DayVolume.Samples > 2 ? DayVolume[2]?.Value ?? 0 : 0;
            var previousDayVolume3 = DayVolume.Samples > 3 ? DayVolume[3]?.Value ?? 0 : 0;
            var previousDayVolume4 = DayVolume.Samples > 4? DayVolume[4]?.Value?? 0 : 0;
            var previousDayVolume5 = DayVolume.Samples > 5? DayVolume[5]?.Value?? 0 : 0;
            var previousDayVolume6 = DayVolume.Samples > 6? DayVolume[6]?.Value?? 0 : 0;
            // 前三根K线的平均量比
            var averageDayVolume = (previousDayVolume1 + previousDayVolume2 + previousDayVolume3) / 3;
            // 量比
            DayVolumeRatio = previousDayVolume1 != 0 ? (dayVolume / previousDayVolume1) : 0;
            DayVolumeRatio3 = averageDayVolume != 0 ? (dayVolume / averageDayVolume) : 0;
            
            // var DayAtrValue = DayAtr.Current?.Value ?? 0;
            // 日线MACD指标趋势
            DayMacdTrend = 0;

            var daymacd = 2 * DayMacd.Histogram.Current?.Value ?? 0;
            var dayDIFF = DayMacd.Current?.Value ?? 0; // 日MACD柱状图
            var dayDEA = DayMacd.Signal.Current?.Value ?? 0; // 日MACD信号线
            // 计算前一天的DIFF和DEA的差值
            var previousDayMacd = DayMacd.Samples > 1? 2 * DayMacd.Histogram[1]?.Value ?? 0:0;
            var previousDayDIFF = DayMacd.Samples > 1? DayMacd[1]?.Value?? 0 : 0;
            var previousDayDEA = DayMacd.Samples > 1? DayMacd.Signal[1]?.Value?? 0 : 0;

            //试盘-洗盘-拉升
            if (dayDIFF > dayDEA && daymacd > 0.2m)
            {
                DayMacdTrend = 1; // 上升趋势
            }
            else if (dayDIFF < dayDEA && daymacd < -0.2m)
            {
                DayMacdTrend = -1; // 下降趋势
            }
            else
            {
                DayMacdTrend = 0; // 震荡趋势
            }


            // 获取指数
            var benchmarkClose = BenchmarkClose.Current?.Value ?? 0;
            var previousbenchmarkClose1 = BenchmarkClose.Samples > 1 ? BenchmarkClose[1]?.Value ?? 0 : 0;
            BenchmarkKLineReturn = previousbenchmarkClose1 != 0 ? (benchmarkClose / previousbenchmarkClose1 - 1) : 0;

            // 定义X
            OpenReturn = dayClose != 0 ? (nextDayOpen / dayClose - 1) : 0;
            // 昨日开盘涨幅
            var previousOpenReturn1 = previousDayClose1 != 0 ? (previousNextDayOpen1/ previousDayClose1   - 1) : 0;
            // 昨日收盘价涨幅
            var previousOpenReturn2 = previousDayClose2 != 0 ? (previousNextDayOpen2/previousDayClose2   - 1) : 0;

            PreviousOpenReturn1 = previousOpenReturn1;
            PreviousOpenReturn2 = previousOpenReturn2;

            // 分钟线指标
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
            MinutePriceBreakout = closePrice > maxPreviousClosePrice && previousClosePrice1 <= maxPreviousClosePrice;

            MinuteKLineReturn = previousClosePrice1 != 0 ? (closePrice / previousClosePrice1 - 1) : 0;
            MinuteKLineReturn5day = previousDayClose5 != 0 ? (closePrice / previousDayClose5 - 1) : 0;
            var previousMinuteKLineReturn1 = previousClosePrice1!= 0? (previousClosePrice1 / previousClosePrice2 - 1) : 0;
            var previousMinuteKLineReturn2 = previousClosePrice2!= 0? (previousClosePrice2 / previousClosePrice3 - 1) : 0;
            var previousMinuteKLineReturn3 = previousClosePrice3!= 0? (previousClosePrice3 / previousClosePrice4 - 1) : 0;
            PreviousMinuteKLineReturn1 = previousMinuteKLineReturn1;
            PreviousMinuteKLineReturn2 = previousMinuteKLineReturn2;
            PreviousMinuteKLineReturn3 = previousMinuteKLineReturn3;
            // 但是，这三根成交量相比，前一日的均是放量。
            var isReversal = previousMinuteKLineReturn2 < -0.02m && previousMinuteKLineReturn1 > 0 && MinuteKLineReturn > 0.02m;
            // 价格突破3均线
            var ema3 = MinuteEma3.Current?.Value?? 0;
            var ema10 = MinuteEma10.Current?.Value?? 0;
            var ema20 = MinuteEma20.Current?.Value?? 0;
            var ema60 = MinuteEma60.Current?.Value?? 0;
            // 定义一个变量突破3均线，前一根没突破，当前根突破了
            // 重命名变量为 priceBreakoutEma，更具描述性，表示价格突破多条EMA均线
            var openPrice = MinuteOpen.Current?.Value?? 0;
            MinutePriceBreakoutEma = closePrice > ema3 && closePrice > ema10 && closePrice > ema20 && closePrice > ema60
                && openPrice < ema3 && openPrice < ema10 && openPrice < ema20
            ;
            // 距离昨日收盘收益
            MinuteKLineReturnFromPreviousClose = dayClose != 0 ? (closePrice / dayClose - 1) : 0;

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
            var emaValue = MinuteEma3.Current?.Value ?? 0;
            var previousEmaValue = MinuteEma3.Samples > 1 ? MinuteEma3[1]?.Value ?? 0 : 0;
            MinuteEmaSlope = previousEmaValue != 0 ? (emaValue - previousEmaValue) / previousEmaValue : 0;

            // RSI
            var rsiValue = MinuteRsi.Current?.Value ?? 0;
            var previousRsiValue1 = MinuteRsi.Samples > 1 ? MinuteRsi[1]?.Value ?? 0 : 0;
            var previousRsiValue2 = MinuteRsi.Samples > 2? MinuteRsi[2]?.Value?? 0 : 0;



            // RSI背离，价格新高但RSI未同步新高
            MinuteRsiValue = rsiValue;
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
            // 初始化，不操作

            // 9点35下跌，9点40底部盘整，9点45弱转强，定义一个变量名字叫弱转强
            MinuteWeakToStrong = 
              MinutePriceBreakoutEma ==true 
              && MinutePriceBreakout==true// 9点45 弱转强
            ;

            TradingSignal = new TradingSignal()
            {
                Symbol = Symbol,
                Direction = OrderDirection.Hold,
                Weight = 0.1m,
                //操作名称
                OperationReson = "",
                SuggestedPrice = MinuteClose,
                SignalTime = ConvertToChinaTime(MinuteClose.Current?.EndTime ?? DateTime.MinValue),
            };
            // 判断要买
            if (
                //935 
                //ConvertToChinaTime(MinuteClose.Current?.EndTime ?? DateTime.MinValue) == _algo.Time.Date.AddHours(9).AddMinutes(35)
                //&& 
                MinutePriceBreakout == true
                && DayStrategyName.Current?.Value == 1 // 长上影试盘战法
            )
            {
                TradingSignal.Direction = OrderDirection.Buy;
            }
            
            // 判断要卖
            // 只记录买卖的历史。
            if (TradingSignal.Direction != OrderDirection.Hold)
            {
                TradeHistory.Add(TradingSignal); //保存历史买入，卖出
            }
            // 定义Y
            MinuteDayReturn = closePrice != 0 ? (nextDayClose / closePrice - 1) : 0;
            MinuteNextDayReturn = closePrice != 0 ? (next2DayClose / closePrice - 1) : 0;
            MinuteNext5DayReturn = closePrice != 0 ? (nextDayClose5 / closePrice - 1) : 0;
        }
        catch (Exception ex)
        {
            _algo.Error($"Error updating minute status for {Symbol}: {ex.Message}");
        }
    }

    // 增加一个函数，UTC时间转换为中国时间
    private DateTime ConvertToChinaTime(DateTime utcTime)
    {
        var chinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, chinaTimeZone);
    }
    private void InitializeIndicators(string code)
    {
        Symbol = _algo.AddData<Api5MinCustomData>(code, Resolution.Minute, TimeZones.Utc).Symbol;
        MinuteClose = _algo.Identity(Symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Close));
        MinuteOpen = _algo.Identity(Symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Open));
        MinuteVolume = _algo.Identity(Symbol, Resolution.Minute, (Func<dynamic, decimal>)(x => ((Api5MinCustomData)x).Volume));

        MinuteMacd = _algo.MACD(Symbol, 6, 13, 4, MovingAverageType.Exponential);
        MinuteEma3 = _algo.EMA(Symbol, 3);
        MinuteEma10 = _algo.EMA(Symbol, 10);
        MinuteEma20 = _algo.EMA(Symbol, 20);
        MinuteEma60 = _algo.EMA(Symbol, 60);
        MinuteRsi = _algo.RSI(Symbol, 6, MovingAverageType.Exponential);

        // 日线字段
        var daysymbol = _algo.AddData<ApiDayCustomData>(code, Resolution.Daily, TimeZones.Utc).Symbol;
        DayMacd = _algo.MACD(daysymbol, 12, 26, 9, MovingAverageType.Exponential);
        // 由于需要将 ApiDayCustomData 转换为 IBaseDataBar，我们手动提供所需的价格数据
        // DayATR = _algo.ATR(daysymbol, 14, MovingAverageType.Simple);
        DayNext2Close = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Next2Close));
        DayNext5Close = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Next5Close));
        NextOpen = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).NextOpen));
        NextClose = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).NextClose));
        DayClose = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Close));
        DayVolume = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Volume));
        DayStrategyName = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ConvertStringToDecimal(((ApiDayCustomData)x).StrategyName)));

        // 指数
        var benchmarkSymbol = _algo.AddData<ApiDayCustomData>("sh.000001", Resolution.Daily, TimeZones.Utc).Symbol;
        BenchmarkClose = _algo.Identity(benchmarkSymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Close));
        _algo.SetBenchmark(benchmarkSymbol);
        if (_algo.LiveMode)
        {
            WarmUpIndicators(Symbol, daysymbol, benchmarkSymbol);
        }
    }
    // 新增一个函数，将字符串转换为 decimal
    private decimal ConvertStringToDecimal(string input)
    {
        // 根据不同的字符串值返回不同的数字
        // 例如，"长上影试盘战法" 返回 1，"潜龙出水，温和放量" 返回 2，以此类推
        switch (input)
        {
            case "长上影试盘战法":
                return 1;
            case "潜龙出水，温和放量":
                return 2;
            case "上升通道修整":
                return 3;
            case "BOLL突破，均线共振":
                return 4;
            case "回踩支撑，趋势向上":
                return 5;
            default:
                if (decimal.TryParse(input, out decimal result))
                {
                    return result;
                }
                return 0; // 转换失败或未匹配到特定字符串时返回 0
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
        _algo.Debug($"{benchmarkSymbol} 指数预热完成，状态：{BenchmarkClose.IsReady}");
        // day的指标
        var dayhistoryList = _algo.History<ApiDayCustomData>(daySymbol, requiredBars * 2, Resolution.Daily)?.ToList();
        if (dayhistoryList == null || !dayhistoryList.Any())
        {
            return;
        }

        foreach (var bar in dayhistoryList.OrderBy(x => x.Time))
        {
            DayMacd.Update(bar.EndTime, bar.Close);
            if (bar is ApiDayCustomData customData)
            {
                // DayATR.Update(customData);
                DayVolume.Update(bar.EndTime, customData.Volume);
                DayNext2Close.Update(bar.EndTime, customData.Next2Close);
                DayNext5Close.Update(bar.EndTime, customData.Next5Close);
                DayClose.Update(bar.EndTime, customData.Close);
                NextOpen.Update(bar.EndTime, customData.NextOpen);
                NextClose.Update(bar.EndTime, customData.NextClose);
                DayStrategyName.Update(bar.EndTime, ConvertStringToDecimal(customData.StrategyName));
            }
        }
        _algo.Debug($"{daySymbol} 日指标预热完成，MACD状态：{DayMacd.IsReady}");
        // 分钟
        var historyList = _algo.History<Api5MinCustomData>(minSymbol, requiredBars * 2, Resolution.Minute)?.ToList();
        if (historyList == null || !historyList.Any())
        {
            return;
        }
        foreach (var bar in historyList.OrderBy(x => x.Time))
        {
            MinuteMacd.Update(bar.EndTime, bar.Close);
            MinuteEma3.Update(bar.EndTime, bar.Close);
            MinuteEma10.Update(bar.EndTime, bar.Close);
            MinuteEma20.Update(bar.EndTime, bar.Close);
            MinuteEma60.Update(bar.EndTime, bar.Close);
            MinuteRsi.Update(bar.EndTime, bar.Close);
            if (bar is Api5MinCustomData customData)
            {
                MinuteClose.Update(bar.EndTime, customData.Close);
                MinuteOpen.Update(bar.EndTime, customData.Open);
                MinuteVolume.Update(bar.EndTime, customData.Volume);
            }
        }
        _algo.Debug($"{minSymbol} 分钟指标预热完成，MACD状态：{MinuteMacd.IsReady}, EMA状态：{MinuteEma3.IsReady}, RSI状态：{MinuteRsi.IsReady}");
    }
}

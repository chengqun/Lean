using System;
using System.Linq;
using QuantConnect.Indicators;
using QuantConnect.Orders;


namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models;

public class FiveAnalysis
{
    private QCAlgorithm _algo;
    public IndicatorBase<IndicatorDataPoint> DayNext2Close { get; private set; }  
    public IndicatorBase<IndicatorDataPoint> MinuteClose { get; private set; }
    public Symbol Symbol { get; set; }
    public string Name { get; set; }
    public string Industry { get; set; }

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
            var closePrice = MinuteClose.Current?.Value ?? 0;
            var nextDayClose = DayNext2Close.Current?.Value ?? 0;
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

        // 日线字段
        var daysymbol = _algo.AddData<ApiDayCustomData>(code, Resolution.Daily, TimeZones.Utc).Symbol;
        DayNext2Close = _algo.Identity(daysymbol, Resolution.Daily, (Func<dynamic, decimal>)(x => ((ApiDayCustomData)x).Next2Close));

        if (_algo.LiveMode)
        {
            WarmUpIndicators();
        }
    }

    public void WarmUpIndicators()
    {
        // 计算MACD所需最小数据量(26周期+9信号线)
        var requiredBars = 12600 + 9;
        var history = _algo.History<Api5MinCustomData>(Symbol, requiredBars * 2, Resolution.Minute);
        if (history == null || !history.Any())
        {
            return;
        }
        foreach (var bar in history.OrderBy(x => x.Time))
        {
            if (bar is Api5MinCustomData customData)
            {
                MinuteClose.Update(bar.EndTime, customData.Close);
            }
        }
    }
}
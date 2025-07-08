using Microsoft.AspNetCore.SignalR.Client;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static QuantConnect.Messages;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Orders
{
    public class OrderExecutor : IOrderExecutor
    {
        private readonly QCAlgorithm _algo;
        private HubConnection _connection;
        Dictionary<Symbol, TradingSignal> Buyrecod = new Dictionary<Symbol, TradingSignal>();
        public OrderExecutor(QCAlgorithm algorithm)
        {
            _algo = algorithm;
            // 初始化SignalR连接
            _connection = new HubConnectionBuilder()
                .WithUrl("http://43.142.139.247/orderHub") // 替换为实际Hub地址
                .Build();
            try
            {
                _connection.StartAsync().Wait();
                _algo.Debug("SignalR连接成功");
            }
            catch (Exception ex)
            {
                _algo.Debug($"SignalR连接失败: {ex.Message}");
                _connection = null;
            }
        }

        public async Task ExecuteSignals(IEnumerable<TradingSignal> signals, IEnumerable<RiskSignal> risks)
        {
            // 检查 signals 参数是否为 null，若为 null 则直接返回
            if (signals == null)
            {
                return;
            }
            // 检查 risks 参数是否为 null，若为 null 则直接返回
            if (risks == null)
            {
                return;
            }
            // 合并 signals 和 risks 中的 Symbol 和 Direction 信息，当 Symbol 相同时，以 Risks 中的信号为准
            var combinedSignals = new Dictionary<Symbol, TradingSignal>();
            // 先处理 TradingSignal 信号
            foreach (var signal in signals)
            {
                if (!combinedSignals.ContainsKey(signal.Symbol))
                {
                    combinedSignals[signal.Symbol] = signal;
                }
            }
            // 处理 RiskSignal 信号，若 Symbol 已存在则覆盖
            foreach (var risk in risks)
            {
                if (combinedSignals.ContainsKey(risk.Symbol))
                {
                    var existingSignal = combinedSignals[risk.Symbol];
                    // 如果存在相同的 Symbol，以 RiskSignal 中的 Direction 为准
                    existingSignal.Direction = risk.Direction;
                    existingSignal.OperationReson = risk.Action.ToString();
                    combinedSignals[risk.Symbol] = existingSignal;
                    continue;
                }
            }
            // 处理合并后的信号
            foreach (var (symbol, signal) in combinedSignals)
            {
                var holding = _algo.Portfolio[symbol];
                // 构建日志头
                var logHeader = BuildLogHeader(signal, holding);

                switch (signal.Direction)
                {
                    case OrderDirection.Buy:
                        // 存储买入信号
                        Buyrecod[symbol] = signal;
                        HandleBuy(signal, holding, logHeader);
                        break;
                    case OrderDirection.Sell:
                        // 卖出和买入在同一天,则退出
                        if (signal.SignalTime.Date == Buyrecod[symbol].SignalTime.Date)
                        {
                            break;
                        }
                        // 如果是止盈，则必须是收盘14:55卖出
                        if (signal.OperationReson == "TakeProfit")
                        {
                            if (signal.SignalTime.Hour == 14 && signal.SignalTime.Minute == 55)
                            {
                                HandleSell(signal, holding, logHeader);
                                Buyrecod.Remove(symbol); // 只有真正卖出才清理
                            }
                            else
                            {
                                _algo.Debug($"{logHeader} ⚠ 止盈信号必须在14:55执行");
                            }
                        }
                        else
                        {
                            HandleSell(signal, holding, logHeader);
                            Buyrecod.Remove(symbol); // 只有真正卖出才清理
                        }
                        break;
                    case OrderDirection.Hold:
                        //HandleHold(logHeader);
                        break;
                }
            }
            await Task.CompletedTask;
        }
        private string BuildLogHeader(TradingSignal signal, Securities.SecurityHolding holding)
        {

            return $"{signal.SignalTime} {signal.Symbol} day:--" 
                +$"｜{signal.OperationReson} "
                +$"｜实时价格:{holding.Price} " 
                // $"权重:{signal.Weight:P2} " +
                // $"持仓:{holding.Quantity}股"
                ;
        }
        // ...existing code...
        protected virtual void HandleBuy(TradingSignal signal, Securities.SecurityHolding holding, string logHeader)
        {
            // 不允许加仓：仅在当前无持仓时才买入，否则忽略
            var targetWeight = signal.Weight > 0 ? signal.Weight : 0.1m; // 默认0.1
            var targetPortfolioValue = _algo.Portfolio.TotalPortfolioValue * targetWeight;
            var price = holding.Price > 0 ? holding.Price : _algo.Securities[signal.Symbol].Price;
            if (price <= 0) price = 1; // 防止除零
            var targetQuantity = (int)(targetPortfolioValue / price / 100) * 100; // A股100股一手

            if (holding.Quantity == 0 && targetQuantity > 0)
            {
                _algo.MarketOrder(signal.Symbol, targetQuantity);
                _algo.Debug($"{logHeader} ▶ 开仓{targetQuantity}股");
                if (_algo.LiveMode)
                {
                    var order = new OrderDto
                    {
                        StockCode = signal.Symbol.Value,
                        Price = price,
                        Quantity = targetQuantity,
                        OrderType = signal.Direction.ToString()
                    };
                    _connection.InvokeAsync("PlaceOrder", order).Wait();
                }
            }
            else
            {
                _algo.Debug($"{logHeader} ⚠ 已有持仓，忽略买入");
            }
        }
        // ...existing code...

        protected virtual void HandleSell(TradingSignal signal, Securities.SecurityHolding holding, string logHeader)
        {
            if (holding.Invested)
            {
                _algo.Liquidate(signal.Symbol);
                _algo.Debug($"{logHeader} ▶ 全部平仓");
            }
            else
            {
                _algo.Debug($"{logHeader} ⚠ 无持仓，忽略卖出");
            }
        }
        protected virtual void HandleHold(string logHeader)
        {
            _algo.Debug($"{logHeader} ▶ 保持现状");
        }
    }
}

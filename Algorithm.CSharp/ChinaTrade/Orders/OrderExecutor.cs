using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Client;
using QuantConnect.Algorithm;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Orders
{
    public class OrderExecutor : IOrderExecutor
    {
        private readonly QCAlgorithm _algo;
        private HubConnection _connection;
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
                        HandleBuy(signal, holding, logHeader);
                        break;
                    case OrderDirection.Sell:
                        HandleSell(signal, holding, logHeader);
                        break;
                    // case OrderDirection.Hold:
                    //     HandleHold(logHeader);
                    //     break;
                }
            }
            await Task.CompletedTask;
        }
        private string BuildLogHeader(TradingSignal signal, SecurityHolding holding)
        {

            return $"{signal.SignalTime} {signal.Symbol} day:--" 
                +$"｜{signal.OperationReson} "
                +$"｜实时价格:{holding.Price} " 
                // $"权重:{signal.Weight:P2} " +
                // $"持仓:{holding.Quantity}股"
                ;
        }
        protected virtual void HandleBuy(TradingSignal signal, SecurityHolding holding, string logHeader)
        {
            if (!holding.Invested)
            {
                var orgquantity = _algo.CalculateOrderQuantity(signal.Symbol, 0.01m);
                var quantity = (orgquantity % 100 +1) * 100;
                _algo.MarketOrder(signal.Symbol, quantity);
                _algo.Debug($"{logHeader} ▶ 买入{quantity}股");
                if(_algo.LiveMode)
                {
                    _connection.InvokeAsync("SendOrder", signal.Direction.ToString(),signal.Symbol, holding.Price.ToString(), quantity.ToString());
                }
            }
            else
            {
                _algo.Debug($"{logHeader} ⚠ 已持仓，忽略买入");
            }
        }

        protected virtual void HandleSell(TradingSignal signal, SecurityHolding holding, string logHeader)
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
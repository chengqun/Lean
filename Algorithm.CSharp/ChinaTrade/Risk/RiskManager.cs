using System.Collections.Generic;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Api;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Risk
{
    public class RiskManager : IRiskManager
    {
        private readonly QCAlgorithm _algorithm;
        private const decimal StopLossPercent = 0.02m;
        private const decimal TakeProfitPercent = 0.1m;

        public RiskManager(QCAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }
        public IEnumerable<RiskSignal> CheckRisks(QuantConnect.Securities.SecurityPortfolioManager portfolio)
        {
            var risks = new List<RiskSignal>();
            // 遍历投资组合中的持仓
            foreach (var holding in portfolio.Values)
            {
                var symbol = holding.Symbol;
                var currentPrice = portfolio.Securities[symbol].Price;
                // 检查买入成本价是否为零，避免除零错误
                var profitRatio = holding.AveragePrice != 0
                    ? (currentPrice - holding.AveragePrice) / holding.AveragePrice
                    : 0;

                // 检查止损和止盈条件
                if (profitRatio <= -StopLossPercent)
                {
                    risks.Add(new RiskSignal
                    {
                        Symbol = symbol,
                        Direction = OrderDirection.Sell,
                        Action = RiskAction.StopLoss,
                        TriggerPrice = currentPrice
                    });
                }
                else if (profitRatio >= TakeProfitPercent)
                {
                    risks.Add(new RiskSignal
                    {
                        Symbol = symbol,
                        Direction = OrderDirection.Sell,
                        Action = RiskAction.TakeProfit,
                        TriggerPrice = currentPrice
                    });
                }
            }
            
            return risks;
        }
    }
}

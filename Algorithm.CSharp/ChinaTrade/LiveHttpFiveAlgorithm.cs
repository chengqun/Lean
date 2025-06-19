/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Microsoft.ML;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Orders;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Risk;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies;
using QuantConnect.Api;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;
using static QuantConnect.Algorithm.CSharp.ChinaTrade.MLnet.SampleRegression;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade;

public class LiveHttpFiveAlgorithm : QCAlgorithm
{
    private ISignalGenerator _signalGenerator;
    private IRiskManager _riskManager;
    private IOrderExecutor _orderExecutor;
    private Dictionary<Symbol, FiveAnalysis> _macdAnalysis = new Dictionary<Symbol, FiveAnalysis>();
    
    private PredictionEngine<ModelInput, ModelOutput> _predictionEngine;
    public override void Initialize()
    {
        SetStartDate(2024, 1, 1);
        SetEndDate(2025, 12, 31);
        // 设置基准货币为人民币
        // SetAccountCurrency("CNY");
        // // 初始化CNY现金账户（假设初始金额为10万）
        // SetCash("CNY", 10000000);
        // 初始化金额
        SetCash(10000000);
        SetTimeZone(TimeZones.Utc);
        // 设置手续费模型
        SetBrokerageModel(new AStockBrokerageModel());
        // 初始化数据
        InitializeData();

        // 1. 初始化ML环境
        var mlContext = new MLContext();
        // 2. 加载模型
        var modelPath = Path.Combine(Globals.DataFolder, "AAshares", "SampleRegression.mlnet");
        ITransformer mlModel;
        using (var stream = new FileStream(modelPath, FileMode.Open))
        {
            mlModel = mlContext.Model.Load(stream, out _);
        }
        // 3. 创建预测引擎
        _predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);

        // 初始化模块
        _signalGenerator = new FiveSignalGenerator(_macdAnalysis,_predictionEngine);
        _riskManager = new RiskManager(this);
        _orderExecutor = new OrderExecutor(this);


    }

    private void InitializeData()
    {
        using (var client = new HttpClient())
        {
            // 获取当前时间并减去一天，然后格式化为 "yyyy-MM-dd" 字符串
            var url = $"http://ai.10jqka.com.cn/transfer/index/index?app=19";
            var response = client.GetStringAsync(url).Result;
            // 示例代码片段，放在InitializeData方法中合适位置：
            var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(response);
            var items = new List<LiveStock>();
            foreach (var strategy in jsonData.data)
            {
                var strategyName = strategy.strategy_name;
                // 将 20250618 格式的日期转换为 2025-06-18 格式
                var dateString = strategy.stockpicking_date;
                var formattedDate = $"{dateString.Substring(0,4)}-{dateString.Substring(4,2)}-{dateString.Substring(6, 2)}";
                var date = formattedDate;
                foreach (var stock in strategy.stock_info)
                {
                    var stockCode = GetMarketPrefix(stock.stock_code);
                    var stockName = stock.stock_name;
                    // 这里可以根据需要处理数据
                    var analysis = new FiveAnalysis(this, stockCode, stockName, "");
                    _macdAnalysis.Add(analysis.Symbol, analysis);
                }
            }
        }
    }

    //添加 OnEndOfAlgorithm
    public override void OnEndOfAlgorithm()
    {
        // 这里可以使用 SQLiteDataStorage 类来进行批量写入
        var DatabasePath = Path.Combine(Globals.DataFolder, "AAshares", "QuantConnectData.db3");
        var db = new SQLiteDataStorage<RealDataItem>(DatabasePath);
        db.SaveItemsAsync(GlobalRealDataItemList.Items).Wait();
    }
    public override async void OnData(Slice data)
    {
        // 生成交易信号
        var signals = _signalGenerator.GenerateSignals(data);
        // 检查风险
        var risks = _riskManager.CheckRisks(Portfolio);
        // // 执行订单
        _orderExecutor.ExecuteSignals(signals, risks).Wait();
    }
    // 判断股票代码前缀，返回市场标识
    private string GetMarketPrefix(string stockCode)
    {
        if (stockCode.StartsWith("6"))
        {
            return "SH."+stockCode;
        }
        else if (stockCode.StartsWith("0") || stockCode.StartsWith("3"))
        {
            return "SZ."+stockCode;
        }
        else
        {
            return string.Empty;
        }
    }
}

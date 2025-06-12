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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.ML;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Interfaces;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Models;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Orders;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Risk;
using QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation;
using QuantConnect.Algorithm.CSharp.ChinaTrade.Strategies;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using static QuantConnect.Algorithm.CSharp.ChinaTrade.MLnet.SampleRegression;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade;

public class LiveFiveAlgorithm : QCAlgorithm
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
            var url = $"http://43.142.139.247/api/dayapi/date/{DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")}";
            var response = client.GetStringAsync(url).Result;
            var jsonData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(response);

            var cl = jsonData.Where(x => x.StrategyName.ToString() == "长上影试盘战法").ToList();
            var singlePartItems = jsonData.Where(x => x.Name.ToString() == "派林生物").ToList();
            foreach (var item in singlePartItems)
            {
                var code = item.Code.ToString();
                var name = item.Name.ToString();
                var industry = item.Industry.ToString();
                var analysis = new FiveAnalysis(this, code, name, industry);
                _macdAnalysis.Add(analysis.Symbol, analysis);
            }
        }
    }

    //添加 OnEndOfAlgorithm
    public override void OnEndOfAlgorithm()
    {
        // 这里可以使用 SQLiteDataStorage 类来进行批量写入
        var db = new SQLiteDataStorage<RealDataItem>();
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
}

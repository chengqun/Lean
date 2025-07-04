using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using QuantConnect;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.Models
{
    public class ApiDayCustomData : BaseData
    {
        [JsonProperty("Date")]
        public string Date;

        [JsonProperty("Open")]
        public decimal Open;

        [JsonProperty("Close")]
        public decimal Close;

        [JsonProperty("High")]
        public decimal High;

        [JsonProperty("Low")]
        public decimal Low;

        [JsonProperty("Volume")] 
        public decimal Volume;

        [JsonProperty("Amount")]
        public decimal Amount;

        [JsonProperty("StrategyName")]
        public string StrategyName;

        [JsonProperty("NextOpen")]
        public decimal NextOpen;

        [JsonProperty("NextClose")]
        public decimal NextClose { get; set; }
        [JsonProperty("Next2Open")]
        public decimal Next2Open { get; set; }

        [JsonProperty("Next2Close")]
        public decimal Next2Close { get; set; }  

        [JsonProperty("Next5Close")]
        public decimal Next5Close { get; set; }  
        public override DateTime EndTime { get; set; }
        public override SubscriptionDataSource GetSource(
            SubscriptionDataConfig config,
            DateTime date,
            bool isLiveMode)
        {
                        // 添加毫秒级时间戳参数
            var timestamp = DateTime.UtcNow.Ticks;
            if (isLiveMode)
            {
                
                return new SubscriptionDataSource(
                    $"{Globals.Api}/api/dayapi/{config.Symbol.Value}?_t={timestamp}",
                    SubscriptionTransportMedium.Rest);
            }
            // 返回的是一个csv，正常历史数据只用请求一次呀。
            return new SubscriptionDataSource(
                    $"{Globals.Api}/api/dayapi/csv/{config.Symbol.Value}?_t={timestamp}",
                SubscriptionTransportMedium.RemoteFile);
        }

        public override BaseData Reader(
            SubscriptionDataConfig config,
            string line,
            DateTime date,
            bool isLiveMode)
        {
            if (isLiveMode)
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<ApiDayCustomData>(line);
                    var a = DateTime.ParseExact(data.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(config.ExchangeTimeZone.Id);
                    data.EndTime = TimeZoneInfo.ConvertTimeToUtc(a, timeZoneInfo);
                    data.Symbol = config.Symbol;
                    data.Value = data.Close;
                    return data;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            try
            {
                var csv = line.Split(',');
                var data = new ApiDayCustomData
                {
                    Date = csv[0],
                    Open = Math.Round(Convert.ToDecimal(csv[1]), 2),
                    Close = Math.Round(Convert.ToDecimal(csv[2]), 2),
                    High = Math.Round(Convert.ToDecimal(csv[3]), 2),
                    Low = Math.Round(Convert.ToDecimal(csv[4]), 2),
                    Volume = Math.Round(Convert.ToDecimal(csv[5]), 2),
                    Amount = Math.Round(Convert.ToDecimal(csv[6]), 2),
                    StrategyName = csv[7],
                    NextOpen = string.IsNullOrEmpty(csv[8]) ? 0 : Math.Round(Convert.ToDecimal(csv[8]), 2),
                    NextClose = string.IsNullOrEmpty(csv[9]) ? 0 : Math.Round(Convert.ToDecimal(csv[9]), 2),
                    Next2Open = string.IsNullOrEmpty(csv[10]) ? 0 : Math.Round(Convert.ToDecimal(csv[10]), 2),
                    Next2Close = string.IsNullOrEmpty(csv[11]) ? 0 : Math.Round(Convert.ToDecimal(csv[11]), 2),
                    Next5Close = string.IsNullOrEmpty(csv[12]) ? 0 : Math.Round(Convert.ToDecimal(csv[12]), 2)
                };
                var a = DateTime.ParseExact(data.Date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(config.ExchangeTimeZone.Id);
                data.EndTime = TimeZoneInfo.ConvertTimeToUtc(a, timeZoneInfo);
                data.Symbol = config.Symbol;
                data.Value = data.Close;
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

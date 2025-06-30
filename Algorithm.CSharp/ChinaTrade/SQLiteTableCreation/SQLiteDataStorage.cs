using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace QuantConnect.Algorithm.CSharp.ChinaTrade.SQLiteTableCreation
{
    // 定义常量类，用于存储数据库相关的常量
    public static class Constants
    {
        public const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;
    }

    // 定义一个泛型类，用于处理 SQLite 数据库的存储操作
    public class SQLiteDataStorage<T> where T : new()
    {
        private readonly SQLiteAsyncConnection database;

        public SQLiteDataStorage(string DatabasePath)
        {
            database = new SQLiteAsyncConnection(DatabasePath, Constants.Flags);
            _ = InitializeDatabaseAsync();
        }

        // 初始化数据库，创建对应的表
        private async Task InitializeDatabaseAsync()
        {
            await database.CreateTableAsync<T>();
        }

        // 获取表中所有的项
        public async Task<List<T>> GetItemsAsync()
        {
            return await database.Table<T>().ToListAsync();
        }

        // 根据 ID 获取表中的一项
        // 使用反射获取 Id 属性
        public async Task<T> GetItemAsync(int id)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                throw new InvalidOperationException("Type T must have an 'Id' property.");

            return await database.Table<T>().Where(i => (int)prop.GetValue(i) == id).FirstOrDefaultAsync();
        }

        // 保存一个项到数据库中，如果存在则更新，不存在则插入
        public async Task<int> SaveItemAsync(T item)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                throw new InvalidOperationException("Type T must have an 'Id' property.");

            int id = (int)prop.GetValue(item);
            if (id != 0)
            {
                return await database.UpdateAsync(item);
            }
            else
            {
                return await database.InsertAsync(item);
            }
        }
        // 批量保存项到数据库中（只插入，不更新）
        public async Task<int> SaveItemsAsync(IEnumerable<T> items)
        {
            var prop = typeof(T).GetProperty("Id");
            if (prop == null)
                throw new InvalidOperationException("Type T must have an 'Id' property.");

            var toInsert = new List<T>();

            foreach (var item in items)
            {
                int id = (int)prop.GetValue(item);
                if (id == 0)
                    toInsert.Add(item);
            }

            int affectedRows = 0;
            if (toInsert.Count > 0)
            {
                affectedRows = await database.InsertAllAsync(toInsert);
            }

            return affectedRows;
        }
        // 从数据库中删除一个项
        public async Task<int> DeleteItemAsync(T item)
        {
            return await database.DeleteAsync(item);
        }
    }
    
    [Table("BacktestData")]
    public class BacktestStock
    {
                // 添加自增主键
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }
    }
    [Table("StrategyData")]
    public class StrategyStock
    {
                // 添加自增主键
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Date { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string StrategyName { get; set; }
    }
    [Table("LiveData")]
    public class LiveStock
    {
                // 添加自增主键
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Date { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string StrategyName { get; set; }
    }

    [Table("fivemin_kline_features")]
    public class RealDataItem
    {
        // 添加自增主键
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        // Y 特征
        public decimal Lable1 { get; set; } // 这是Y值，表示当日浮盈
        public decimal Lable2 { get; set; } // 这是Y值，表示第二天的收益率
        public decimal Lable5 { get; set; } // 这是Y值，表示第5天的收益率
        public float Score { get; set; }
        // 基本信息
        public string Date { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }

        public decimal Price { get; set; } // 当前价格，这是X值，表示当前的价格
        // 指数信息
        public decimal BenchmarkKLineReturn { get; set; } // 指数收益率，这是X值，表示指数的收益率
        // 日线X 特征
        public decimal DayStrategyName { get; set; } // 日策略名称，这是X值，表示日策略的名称
        public decimal DayKLineReturn { get; set; } // 日K线收益率，这是X值，表示日K线的收益率
        public decimal DayKLineReturn5 { get; set; } // 日K线收益率，这是X值，表示日K线的收益率
        public decimal DayVolumeRatio { get; set; } // 日量比，这是X值，表示日的量比
        public decimal DayVolumeRatio3 { get; set; } // 与前3周期平均量比，这是X值，表示与前3周期的平均量比
        public decimal DayMacd { get; set; } // 
        // public decimal DayATR { get; set; }
        public decimal DayDIFF { get; set; } // 日MACD柱状图，这是X值，表示日MACD柱状图的值
        public decimal DayDEA { get; set; } // 日MACD信号线，这是X值，表示日MACD信号线的值
        public decimal DayMacdTrend { get; set; } // 日MACD趋势，这是X值，表示日MACD的趋势
        // 开盘信息
        public decimal OpenReturn { get; set; } // 今日开盘涨幅，这是X值，表示今日开盘涨幅
        public decimal PreviousOpenReturn1 { get; set; } // 昨日开盘涨幅，这是X值，表示昨日开盘涨幅
        public decimal PreviousOpenReturn2 { get; set; } // 昨日开盘涨幅，这是X值，表示昨日开盘涨幅
        // 分钟X 特征
        // 价格
        public decimal MinuteKLineReturn { get; set; } // 分钟K线收益率，这是X值，表示分钟K线的收益率
        public decimal MinuteKLineReturn5day {get;set;} 
        public decimal PreviousMinuteKLineReturn1 { get; set; } // 分钟K线收益率，这是X值，表示分钟K线的收益率
        public decimal PreviousMinuteKLineReturn2 { get; set; } // 分钟K线收益率，这是X值，表示分钟K线的收益率
        public decimal PreviousMinuteKLineReturn3 { get; set; } // 分钟K线收益率，这是X值，表示分钟K线的收益率
        public decimal MinuteKLineReturnFromPreviousClose { get; set; } // 分钟K线距离昨日收盘收益，这是X值，表示分钟K线距离昨日收盘的收益率
        public bool MinutePriceBreakout { get; set; } // 分钟突破前30分钟高点，这是X值，表示分钟价格是否突破前30分钟的高点
        public bool MinutePriceBreakoutEma { get; set; } // 分钟突破前30分钟低点，这是X值，表示分钟价格是否突破前30分钟的低点

        public bool MinuteWeakToStrong { get; set; } // 分钟弱到强，这是X值，表示分钟价格是否从弱到强
        // 量比
        public decimal MinuteVolumeRatio { get; set; } // 分钟量比，这是X值，表示分钟的量比
        public decimal MinuteVolumeRatio3 { get; set; } // 与前3周期平均量比，这是X值，表示与前3周期的平均量比
        //EMA
        public decimal MinuteEmaSlope { get; set; } // 分钟Ema斜率，这是X值，表示分钟Ema的斜率
                                                    //MACD
        public decimal MinuteMacdDivergence { get; set; } // 分钟MACD背离，这是X值，表示分钟MACD是否出现背离
        public decimal MinMACD { get; set; } // 分钟MACD，这是X值，表示分钟MACD的值
        public decimal MinDIFF { get; set; } // 分钟MACD柱状图，这是X值，表示分钟MACD柱状图的值
        public decimal MinDEA { get; set; } // 分钟MACD信号线，这是X值，表示分钟MACD信号线的值

        // RSI
        public decimal MinuteRsi { get; set; } // 分钟RSI，这是X值，表示分钟RSI的值
    }

    public static class GlobalRealDataItemList
    {
        public static readonly List<RealDataItem> Items = new List<RealDataItem>();
        
    }
}

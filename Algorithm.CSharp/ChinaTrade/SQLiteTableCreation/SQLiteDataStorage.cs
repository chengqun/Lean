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
        public const string DatabaseFilename = "QuantConnectData.db3";

        public const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;

        public static string DatabasePath => Path.Combine(Globals.DataFolder, "AAshares", DatabaseFilename);
    }

    // 定义一个泛型类，用于处理 SQLite 数据库的存储操作
    public class SQLiteDataStorage<T> where T : new()
    {
        private readonly SQLiteAsyncConnection database;

        public SQLiteDataStorage()
        {
            database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
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


    [Table("RealDataItem")]
    public class RealDataItem
    {
        // 添加自增主键
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Date { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }
        // 将 decimal 类型改为 float 类型以匹配数据库中的 float 类型
        public decimal TwentyDayReturnQuantile { get; set; }
        public decimal DayKLineReturn { get; set; }
        public decimal BenchmarkKLineReturn { get; set; }
        public decimal DayNextOpenReturn { get; set; } // 今日开盘涨幅，这是X值，表示今日开盘涨幅

        public decimal NextDayReturn { get; set; } // 这是Y值，表示第二天的收益率
    }

    public static class GlobalRealDataItemList
    {
        public static readonly List<RealDataItem> Items = new List<RealDataItem>();
    }
}

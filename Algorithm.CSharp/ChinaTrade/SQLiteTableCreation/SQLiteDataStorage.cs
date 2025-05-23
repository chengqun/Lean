using System;
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

        public static string DatabasePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DatabaseFilename);
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
        public decimal DayNextOpenReturn { get; set; }
    }
}
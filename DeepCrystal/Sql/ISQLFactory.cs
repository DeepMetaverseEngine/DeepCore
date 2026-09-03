using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.SQL
{
    [Reflectible]
    public interface ISQLFactory
    {
        Task<DataTable> ExecuteReaderAsync(string sql);
        Task<int> ExecuteNonQueryAsync(string sql);
        string EscapeString(string str);
    }

    [Reflectible]
    public class SQLFactory
    {
        public static ISQLFactory CurrentFactory { get; private set; } = new Blank();

        public static void SetFactory(ISQLFactory sqlFactory)
        {
            CurrentFactory = sqlFactory;
        }

        private class Blank : ISQLFactory
        {
            public string EscapeString(string str)
            {
                return string.Empty;
            }
            public Task<int> ExecuteNonQueryAsync(string sql)
            {
                return Task.FromResult(0);
            }
            public Task<DataTable> ExecuteReaderAsync(string sql)
            {
                return Task.FromResult<DataTable>(null);
            }
        }
    }
}

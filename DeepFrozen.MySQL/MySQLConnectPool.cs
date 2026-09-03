using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using DeepCore.Log;
using DeepCore;

namespace DeepFrozen.MySQL
{
    /*
    4.3 Managing a Connection Pool in Connector/NET
    The MySQL Connector/NET supports connection pooling for better performance and scalability with database-intensive 
    applications. This is enabled by default. You can turn it off or adjust its performance characteristics using the 
    connection string options Pooling, Connection Reset, Connection Lifetime, Cache Server Properties, Max Pool Size and 
    Min Pool Size. See Section 4.1, “Creating a Connector/NET Connection String” for further information.

    Connection pooling works by keeping the native connection to the server live when the client disposes of a MySqlConnection. 
    Subsequently, if a new MySqlConnection object is opened, it is created from the connection pool, rather than creating a new 
    native connection. This improves performance.

    Guidelines
    To work as designed, it is best to let the connection pooling system manage all connections. Do not create a globally 
    accessible instance of MySqlConnection and then manually open and close it. This interferes with the way the pooling works 
    and can lead to unpredictable results or even exceptions.

    One approach that simplifies things is to avoid creating a MySqlConnection object manually. Instead, use the overloaded 
    methods that take a connection string as an argument. With this approach, Connector/NET automatically creates, opens, 
    closes and destructs connections, using the connection pooling system for best performance.

    Typed Datasets and the MembershipProvider and RoleProvider classes use this approach. Most classes that have methods 
    that take a MySqlConnection as an argument, also have methods that take a connection string as an argument. This includes 
    MySqlDataAdapter.

    Instead of creating MySqlCommand objects manually, you can use the static methods of the MySqlHelper class. These methods 
    take a connection string as an argument and they fully support connection pooling.

    Resource Usage
    Connector/NET runs a background job every three minutes and removes connections from pool that have been idle (unused) 
    for more than three minutes. The pool cleanup frees resources on both client and server side. This is because on the client 
    side every connection uses a socket, and on the server side every connection uses a socket and a thread.

    Multiple endpoints.  Starting with Connector/NET 8.0.19, a connection string can include multiple endpoints (server:port) 
    with connection pooling enabled. At runtime, Connector/NET selects one of the addresses from the pool randomly (or by 
    priority when provided) and attempts to connect to it. If the connection attempt is unsuccessful, Connector/NET selects 
    another address until the set of addresses is exhausted. Unsuccessful endpoints are retried every two minutes. Successful 
    connections are managed by the connection pooling mechanism.
    */

    public class MySQLConnectPool : IDisposable
    {
        private readonly Logger log = new LazyLogger(nameof(MySQLConnectPool));
        //private readonly SemaphoreSlim semaphore;
        private readonly string ConnectionStr;//连接字符串
        public string DBName { get; private set; }
        /// <summary>
        /// Pooling=true;ConnectionTimeout=60;MaxPoolSize=200;MinPoolSize=10;
        /// </summary>
        /// <param name="connectionStr"></param>
        public MySQLConnectPool(string connectionStr = "server=localhost;User ID=root;Password=123456;database=test;")
        {
            //this.semaphore = new SemaphoreSlim(maxPoolSize, maxPoolSize);
            //数据库连接字符串
            this.ConnectionStr = connectionStr;
            TestDB(connectionStr);
            using (var conn = Open())
            {
                Test(conn.Connection);
            }
        }
        private bool TestDB(string connectionStr)
        {
            {
                var format = new PropertiesFormat()
                {
                    NextLine = ";",
                    Separator = "="
                };
                var prop = Properties.ParseText(connectionStr, format);
                if (prop.TryRemove("database", out var dbName))
                {
                    this.DBName = dbName;
                    try
                    {
                        var connString = prop.ToString(format, "");
                        using (MySqlConnection conn = new MySqlConnection(connString))
                        {
                            conn.Open();
                            try
                            {
                                using (var cmd = new MySqlCommand($"SHOW DATABASES", conn))
                                {
                                    using (var reader = cmd.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            var db = reader.GetString(0);
                                            if (db == dbName)
                                            {
                                                log.Info($"Database {dbName} already exists, no need to create it again.");
                                                return true;
                                            }
                                        }
                                    }
                                }
                                using (var cmd = new MySqlCommand($"create database {dbName}", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                Console.WriteLine($"Database {dbName} created successfully");
                            }
                            catch (Exception err)
                            {
                                log.Warn(err.Message);
                            }
                            conn.Close();
                        }
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            return false;
        }
        public void Dispose()
        {
            //semaphore.Dispose();
        }
        //创建连接
        protected virtual MySqlConnection CreateConnection()
        {
            MySqlConnection conn = new MySqlConnection(ConnectionStr);
            return conn;
        }
        public AutoRelease Open()
        {
            // semaphore.Wait();
            var conn = CreateConnection();
            conn.Open();
            return new AutoRelease(this, conn);
        }
        public async Task<AutoRelease> OpenAsync()
        {
            //await semaphore.WaitAsync(); 
            var conn = CreateConnection();
            await conn.OpenAsync();
            return new AutoRelease(this, conn);
        }
        private void Release(MySqlConnection conn)
        {
            try
            {
                conn.Dispose();
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
            // semaphore.Release();
        }
        internal bool Test(MySqlConnection con)
        {
            //主要用于不同用户
            bool result = true;
            if (con != null)
            {
                string sql = "select 1";//随便执行对数据库操作
                MySqlCommand cmd = new MySqlCommand(sql, con);
                var rst = cmd.ExecuteScalar();
                rst.ToString();
            }
            return result;
        }

        public struct AutoRelease : IDisposable
        {
            public readonly MySQLConnectPool Pool;
            public readonly MySqlConnection Connection;
            internal AutoRelease(MySQLConnectPool p, MySqlConnection c)
            {
                this.Pool = p;
                this.Connection = c;
            }
            public void Dispose()
            {
                Pool.Release(Connection);
            }
            public static implicit operator MySqlConnection(in AutoRelease value)
            {
                return value.Connection;
            }
        }

        public TResult RunConnection<TResult>(Func<MySqlConnection, TResult> action)
        {
            try
            {
                using (var auto = Open())
                {
                    return action(auto);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public void RunConnection(Action<MySqlConnection> action)
        {
            try
            {
                using (var auto = Open())
                {
                    action(auto);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public async Task<TResult> RunConnectionAsync<TResult>(Func<MySqlConnection, Task<TResult>> action)
        {
            try
            {
                using (var auto = await OpenAsync())
                {
                    return await action(auto);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public async Task RunConnectionAsync(Func<MySqlConnection, Task> action)
        {
            try
            {
                using (var auto = await OpenAsync())
                {
                    await action(auto);
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public TResult RunTransaction<TResult>(Func<MySqlTransaction, TResult> action)
        {
            try
            {
                using (var auto = Open())
                {
                    var conn = auto.Connection;
                    var result = default(TResult);
                    using (var ts = conn.BeginTransaction())
                    {
                        try
                        {
                            result = action(ts);
                            ts.Commit();
                        }
                        catch
                        {
                            ts.Rollback();
                            throw;
                        }
                    }
                    return result;
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public void RunTransaction(Action<MySqlTransaction> action)
        {
            try
            {
                using (var auto = Open())
                {
                    var conn = auto.Connection;
                    using (var ts = conn.BeginTransaction())
                    {
                        try
                        {
                            action(ts);
                            ts.Commit();
                        }
                        catch
                        {
                            ts.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }


        public async Task<TResult> RunTransactionAsync<TResult>(Func<MySqlTransaction, Task<TResult>> action)
        {
            try
            {
                using (var auto = await OpenAsync())
                {
                    var conn = auto.Connection;
                    var result = default(TResult);
                    using (var ts = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            result = await action(ts);
                            ts.Commit();
                        }
                        catch
                        {
                            ts.Rollback();
                            throw;
                        }
                    }
                    return result;
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }
        public async Task RunTransactionAsync(Func<MySqlTransaction, Task> action)
        {
            try
            {
                using (var auto = await OpenAsync())
                {
                    var conn = auto.Connection;
                    using (var ts = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            await action(ts);
                            ts.Commit();
                        }
                        catch
                        {
                            ts.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
                throw;
            }
        }


    }
    /*
    public class ConnectionPool
    {
        private static ConnectionPool cpool = null;//池管理对象
        private static Object objlock = typeof(ConnectionPool);//池管理对象实例
        private int size = 1;//池中连接数
        private int useCount = 0;//已经使用的连接数
        private ArrayList pool = null;//连接保存的集合
        private String ConnectionStr = "";//连接字符串

        public ConnectionPool()
        {
            //数据库连接字符串
            ConnectionStr = "server=localhost;User ID=root;Password=123456;database=test;";
            //创建可用连接的集合
            pool = new ArrayList();
        }

        #region 创建获取连接池对象
        public static ConnectionPool getPool()
        {
            lock (objlock)
            {
                if (cpool == null)
                {
                    cpool = new ConnectionPool();
                }
                return cpool;
            }
        }
        #endregion

        #region 获取池中的连接
        public MySqlConnection getConnection()
        {
            lock (pool)
            {
                MySqlConnection tmp = null;
                //可用连接数量大于0
                if (pool.Count > 0)
                {
                    //取第一个可用连接
                    tmp = (MySqlConnection)pool[0];
                    //在可用连接中移除此链接
                    pool.RemoveAt(0);
                    //不成功
                    if (!isUserful(tmp))
                    {
                        //可用的连接数据已去掉一个
                        useCount--;
                        tmp = getConnection();
                    }
                }
                else
                {
                    //可使用的连接小于连接数量
                    if (useCount <= size)
                    {
                        try
                        {
                            //创建连接
                            tmp = CreateConnection(tmp);
                        }
                        catch (Exception e)
                        {
                        }
                    }
                }
                //连接为null
                if (tmp == null)
                {
                    //达到最大连接数递归调用获取连接否则创建新连接
                    if (useCount <= size)
                    {
                        tmp = getConnection();
                    }
                    else
                    {
                        tmp = CreateConnection(tmp);
                    }
                }
                return tmp;
            }
        }
        #endregion

        #region 创建连接
        private MySqlConnection CreateConnection(MySqlConnection tmp)
        {
            //创建连接
            MySqlConnection conn = new MySqlConnection(ConnectionStr);
            conn.Open();
            //可用的连接数加上一个
            useCount++;
            tmp = conn;
            return tmp;
        }
        #endregion

        #region 关闭连接,加连接回到池中
        public void closeConnection(MySqlConnection con)
        {
            lock (pool)
            {
                if (con != null)
                {
                    //将连接添加在连接池中
                    pool.Add(con);
                }
            }
        }
        #endregion

        #region 目的保证所创连接成功,测试池中连接
        private bool isUserful(MySqlConnection con)
        {
            //主要用于不同用户
            bool result = true;
            if (con != null)
            {
                string sql = "select 1";//随便执行对数据库操作
                MySqlCommand cmd = new MySqlCommand(sql, con);
                try
                {
                    cmd.ExecuteScalar().ToString();
                }
                catch
                {
                    result = false;
                }

            }
            return result;
        }
        #endregion
    }
    */

    public static class MySQLConnectionHelper
    {
        private static Logger log = new LazyLogger(nameof(MySqlConnection));

        public static TResult BeginTransaction<TResult>(this MySqlConnection conn, Func<MySqlTransaction, TResult> action)
        {
            var result = default(TResult);
            using (var ts = conn.BeginTransaction())
            {
                try
                {
                    result = action(ts);
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
            return result;
        }
        public static void BeginTransaction(this MySqlConnection conn, Action<MySqlTransaction> action)
        {
            using (var ts = conn.BeginTransaction())
            {
                try
                {
                    action(ts);
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
        }
        public static TResult BeginTransactionCommand<TResult>(this MySqlConnection conn, Func<MySqlCommand, TResult> action)
        {
            var result = default(TResult);
            using (var ts = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = ts;
                        result = action(cmd);
                    }
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
            return result;
        }
        public static void BeginTransactionCommand(this MySqlConnection conn, Action<MySqlCommand> action)
        {
            using (var ts = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = ts;
                        action(cmd);
                    }
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }

            }
        }


        public static async Task<TResult> BeginTransactionAsync<TResult>(this MySqlConnection conn, Func<MySqlTransaction, Task<TResult>> action)
        {
            var result = default(TResult);
            using (var ts = await conn.BeginTransactionAsync())
            {
                try
                {
                    result = await action(ts);
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
            return result;
        }
        public static async Task BeginTransactionAsync(this MySqlConnection conn, Func<MySqlTransaction, Task> action)
        {
            using (var ts = await conn.BeginTransactionAsync())
            {
                try
                {
                    await action(ts);
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
        }
        public static async Task<TResult> BeginTransactionCommandAsync<TResult>(this MySqlConnection conn, Func<MySqlCommand, Task<TResult>> action)
        {
            var result = default(TResult);
            using (var ts = await conn.BeginTransactionAsync())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = ts;
                        result = await action(cmd);
                    }
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }
            }
            return result;
        }
        public static async Task BeginTransactionCommandAsync(this MySqlConnection conn, Func<MySqlCommand, Task> action)
        {
            using (var ts = await conn.BeginTransactionAsync())
            {
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = ts;
                        await action(cmd);
                    }
                    ts.Commit();
                }
                catch (Exception err)
                {
                    log.Error(err);
                    ts.Rollback();
                    throw;
                }

            }
        }

    }

}

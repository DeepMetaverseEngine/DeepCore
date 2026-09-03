using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using DeepCrystal.SQL;
using MySql.Data.MySqlClient;

namespace DeepFrozen.MySQL
{
    public class MySQLFactory : ISQLFactory, IDisposable
    {
        private string mConnStr;
        private MySqlConnection mConn;
        private ActionBlock<Action> mActionBlock;
        private readonly System.Threading.Timer mTimer;

        //--------------------------------------------------
        private int fixedInterval = 30 * 1000;

        public MySQLFactory(string str)
        {
            mConnStr = str;
            mConn = new MySqlConnection(mConnStr);
            mActionBlock = new ActionBlock<Action>(new Action<Action>(Run));
            mTimer = new System.Threading.Timer(OnTick, this, 0, fixedInterval);
        }

        private void OnTick(object state)
        {
            PingAsync().ConfigureAwait(false);
        }

            internal void Run(Action action)
        {
            action.Invoke();
        }

        private Task<TResult> RunAsync<TResult>(Func<TResult> action)
        {
            var tcs = new TaskCompletionSource<TResult>();
            try
            {
                if (mActionBlock.Post(run) == false)
                {
                    tcs.TrySetCanceled();
                }
            }
            catch (Exception err)
            {
                tcs.TrySetException(err);
            }
            void run()
            {
                try
                {
                    var ret = action();
                    tcs.TrySetResult(ret);
                }
                catch (Exception err)
                {
                    tcs.TrySetException(err);
                }
            }
            return tcs.Task;
        }

        private Task<TResult> RunAsync<T, TResult>(Func<T, TResult> action, T arg)
        {
            return RunAsync(() => action(arg));
        }

        private void CheckConnection()
        {
            if (mConn.State == ConnectionState.Broken || mConn.State == ConnectionState.Closed)
            {
                mConn.Open();
            }
        }

        public Task<DataTable> ExecuteReaderAsync(string sql)
        {
            return RunAsync(ExecuteReader, sql);
        }

        private DataTable ExecuteReader(string sql)
        {
            CheckConnection();
            using (var da = new MySqlDataAdapter())
            {
                using (da.SelectCommand = mConn.CreateCommand())
                {
                    da.SelectCommand.CommandText = sql;
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds.Tables[0];
                }
            }
        }


        public Task<int> ExecuteNonQueryAsync(string sql)
        {
            return RunAsync(ExecuteNonQuery, sql);
        }

        public string EscapeString(string str)
        {
            if (str != null)
            {
                return MySqlHelper.EscapeString(str);
            }
            return str;
        }

        private int ExecuteNonQuery(string sql)
        {
            CheckConnection();
            var cmd = new MySqlCommand(sql, mConn);
            return cmd.ExecuteNonQuery();
        }

        private Task<object> PingAsync()
        {
            return RunAsync(Ping);
        }

        private object Ping()
        {
            CheckConnection();
            mConn.Ping();
            //Console.WriteLine("[Mysql]Ping");
            return null;
        }

        public void Dispose()
        {
            try { mTimer.Dispose(); } catch { }
            mConn?.Dispose();
            mConn = null;
        }
    }
}

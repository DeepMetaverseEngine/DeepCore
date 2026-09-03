using DeepCore;
using DeepCore.Log;
using DeepCore.SQL;
using DeepFrozen.MySQL;
using MySql.Data.MySqlClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH.DeputyCenter
{
    public class DeputyResult
    {
        public long UniqueNonce;
        public string FullCallData;
    }
    //--------------------------------------------------------------------------------------
    /// <summary>
    /// 记录所有服务器签名交易
    /// </summary>
    [SQLTable]
    public class DeputyNonce
    {
        [SQLField(PrimaryKey = true, AutoIncrement = true, NotNull = true)]
        public long UniqueNonce;

        [SQLField()]
        public string CallData;
        [SQLField(Length = 42)]
        public string Signer;

        [SQLField()]
        public long DependUniqueNonce;
        [SQLField()]
        public long Before;
        [SQLField()]
        public BigInteger Value;

        [SQLField()]
        public bool OnlyDesignatedSender;
        [SQLField(Length = 42)]
        public string DesignatedSender;

        //---------------------------------------------------
        // after pay
        [SQLField(Length = 128)]
        public string TransactionGUID;
        [SQLField()]
        public DateTime TransactionTime;
        [SQLField(Length = 128)]
        public string TransactionHash;

    }
    public class DeputyService : Disposable
    {
        private readonly Logger log = LoggerFactory.GetLogger(typeof(DeputyService));
        public MySQLConnectPool MySQL { get; }
        public Web3 Web3 { get; }
        public Deputy Deputy { get; }
        public SQLTableInfo<DeputyNonce, BigInteger> TDeputyNonce { get; }

        public DeputyService(MySQLConnectPool mysql, Deputy deputy, string deputyTableName = "deputy")
        {
            MySQL = mysql;
            Web3 = deputy.Web3;
            Deputy = deputy;
            TDeputyNonce = new SQLTableInfo<DeputyNonce, BigInteger>(deputyTableName);
            using (var auto = MySQL.Open())
            {
                var conn = auto.Connection;
                conn.InitSQLTable(TDeputyNonce);
            }
        }
        protected override void Disposing()
        {

        }
        /// <summary>
        /// 服务端签名交易
        /// </summary>
        public async Task<DeputyResult> SetDeputyAsync(
            CallInput callData,
            Account signer,
            BigInteger dependUniqueNonce,
            BigInteger before,
            BigInteger value,
            bool onlyDesignatedSender,
            string designatedSender)
        {
            using (var auto = await MySQL.OpenAsync())
            {
                var conn = auto.Connection;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $"INSERT INTO {TDeputyNonce.TableName} " +
                        $"(" +
                        $"`{nameof(DeputyNonce.CallData)}`," +
                        $"`{nameof(DeputyNonce.Signer)}`," +
                        $"`{nameof(DeputyNonce.DependUniqueNonce)}`," +
                        $"`{nameof(DeputyNonce.Before)}`," +
                        $"`{nameof(DeputyNonce.Value)}`," +
                        $"`{nameof(DeputyNonce.OnlyDesignatedSender)}`," +
                        $"`{nameof(DeputyNonce.DesignatedSender)}`" +
                        $") VALUES " +
                        $"(@1, @2, @3, @4, @5, @6, @7)";
                    cmd.Parameters.AddWithValue("@1", callData.Data);
                    cmd.Parameters.AddWithValue("@2", signer.Address);
                    cmd.Parameters.AddWithValue("@3", dependUniqueNonce);
                    cmd.Parameters.AddWithValue("@4", before);
                    cmd.Parameters.AddWithValue("@5", value);
                    cmd.Parameters.AddWithValue("@6", onlyDesignatedSender);
                    cmd.Parameters.AddWithValue("@7", designatedSender);
                    cmd.Prepare();
                    //log.Info(cmd.CommandText);
                    var rst = await cmd.ExecuteNonQueryAsync();
                    if (rst == 1)
                    {
                        var uniqueNonce = cmd.LastInsertedId;
                        var fullCallData = Deputy.SetDeputy(
                            callData,
                            signer,
                            new BigInteger(uniqueNonce),
                            dependUniqueNonce,
                            before,
                            value,
                            onlyDesignatedSender,
                            designatedSender);
                        return new DeputyResult()
                        {
                            UniqueNonce = uniqueNonce,
                            FullCallData = fullCallData
                        };
                    }
                }
            }
            return null;
        }

    }
}

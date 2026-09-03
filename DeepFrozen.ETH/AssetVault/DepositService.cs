using DeepCore;
using DeepCore.Log;
using DeepCore.SQL;
using DeepCrystal;
using DeepFrozen.MySQL;
using DeepFrozen.MySQL.Service;
using MySql.Data.MySqlClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DeepFrozen.ETH.AssetVault
{
    /// <summary>
    /// 链上资产提取到链下
    /// </summary> 
    [SQLTable]
    public class DepositTransfer : TransferRecord
    {
        public override object PrimaryKey { get => TransactionHash; }
        [SQLField(Length = 128, PrimaryKey = true)]
        public string TransactionHash;
        [SQLField(Length = 42)]
        public string From;
        [SQLField(Length = 42)]
        public string To;
        [SQLField()]
        public string Data;
        [SQLField()]
        public string ReceiptJson;
        public TransactionReceipt Receipt
        {
            get => JSON.Deserialize<TransactionReceipt>(ReceiptJson);
            set => ReceiptJson = JSON.Serialize(value);
        }
    }

    public class DepositService : TransferService<DepositTransfer, string>
    {
        public Web3 Web3 { get; }
        public DepositService(MySQLConnectPool mysql, Web3 web3, string table_name, int concurrentCount = 3)
            : base(mysql, table_name, concurrentCount)
        {
            Web3 = web3;
        }
        protected override async Task<bool> TryConsumeAsync(DepositTransfer record)
        {
            try
            {
                var receipt = await Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(record.TransactionHash);
                if (receipt != null && receipt.Status.Value == 1)
                {
                    //record.JsonObject = DeserializeJsonObject(record);
                    record.Receipt = receipt;
                    if (receipt.Succeeded(true))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            return false;
        }

        //         public Task<int> PushTransactionAsync(string transactionHash, string fromAddress, string toAddress, object jsonData)
        //         {
        //             return Productor.PushProductAsync(new DepositRecord()
        //             {
        //                 ApprovedRemain = 1,
        //                 TransactionHash = transactionHash,
        //                 From = fromAddress,
        //                 To = toAddress,
        //                 JsonObject = jsonData,
        //             });
        //         }
    }


}


using DeepCore;
using DeepCore.Log;
using DeepCore.SQL;
using DeepFrozen.MySQL;
using DeepFrozen.MySQL.Service;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH.AssetVault
{
    [SQLTable]
    public class WithdrawTransfer : TransferRecord
    {
        public override object PrimaryKey { get => WithdrawNonce; }
        [SQLField(PrimaryKey = true, AutoIncrement = true, NotNull = true)]
        public long WithdrawNonce;
        [SQLField(Length = 128)]
        public string WithdrawUUID;

        [SQLField(Length = 42)]
        public string From;
        [SQLField(Length = 42)]
        public string To;
        [SQLField()]
        public string FullCallData;
    }

    // 
    //     public class WithdrawRequestService : TransferService<WithdrawRecord, long>
    //     {
    //         public WithdrawRequestService(MySQLConnectPool mysql, string table_name)
    //             : base(mysql, table_name)
    //         {
    //         }
    //     }

    /// <summary>
    /// 链下资产提取到链上
    /// </summary>
    public class WithdrawService : TransferService<WithdrawTransfer, long>
    {
        public Web3 Web3 { get; }

        public WithdrawService(MySQLConnectPool mysql, Web3 web3, string table_name, int concurrentCount = 3)
            : base(mysql, table_name, concurrentCount)
        {
            Web3 = web3;
        }
        protected override Task<bool> TryConsumeAsync(WithdrawTransfer record)
        {
            //             var receipt = Web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(record.TransactionHash).WaitForResult(1000);
            //             if (receipt != null && receipt.Status.Value == 1)
            //             {
            //                 record.Receipt = receipt;
            //                 record.ReceiptJson = JsonConvert.SerializeObject(receipt);
            //                 return Task.FromResult(true);
            //             }
            return Task.FromResult(true);
        }
    }

}

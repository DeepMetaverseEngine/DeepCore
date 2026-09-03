using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepFrozen.ETH;
using Nethereum.ABI.Decoders;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH
{
    public static class EthHelper
    {
        //----------------------------------------------------------------------------------------------------------------------------------------------
        public static Logger log = new LazyLogger("eth");

        public static string GetAssemblyABI(this Assembly asm, string name)
        {
            dynamic json = JsonConvert.DeserializeObject(Resource.LoadTextFromAssembly(asm, name));
            var abi = json.abi.ToString() as string;
            return abi;
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------
        #region Convert

        public const string ZeroAddress = "0x0000000000000000000000000000000000000000";
        public const string ZeroBytes = "0x0000000000000000000000000000000000000000000000000000000000000000";
        public static bool IsZeroAddress(this string addr)
        {
            if (string.IsNullOrEmpty(addr)) return true;
            if (addr.StartsWith(ZeroAddress, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return ZeroAddress.Length != addr.Length;
        }
        public static string ToName(this HexBigInteger bytes)
        {
            return new StringBytes32Decoder().Decode(bytes.ToHexByteArray());
        }
        public static string ToName(this byte[] bytes)
        {
            return new StringBytes32Decoder().Decode(bytes);
        }
        public static string ToName(this BigInteger bytes)
        {
            return new StringBytes32Decoder().Decode(bytes.ToByteArray());
        }
        //var KGOLD = "0x4b474f4c44000000000000000000000000000000000000000000000000000000".HexToBigInteger(false);
        //             0x4b474f4c44000000000000000000000000000000000000000000000000000000
        public static string ToBytes32Name(this BigInteger bytes32)
        {
            //var KGOLD = "0x4b474f4c44000000000000000000000000000000000000000000000000000000".HexToBigInteger(false);
            //var KBD = "0x4b474f4c44000000000000000000000000000000000000000000000000000000".HexToBigInteger(false);
            var hex = bytes32.ToHex(false);
            if (hex.StringStartWithIgnoreCase("0x")) hex = hex.Substring(2);
            var sb = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                var _byte = hex.Substring(i, 2);
                var _char = int.Parse(_byte, System.Globalization.NumberStyles.HexNumber);
                if (_char == 0) break;
                sb.Append((char)_char);
            }
            return sb.ToString();
        }
        public static BigInteger ToBytes32(this string name)
        {
            //var KGOLD = "0x4b474f4c44000000000000000000000000000000000000000000000000000000".HexToBigInteger(false);
            //var KBD = "0x4b474f4c44000000000000000000000000000000000000000000000000000000".HexToBigInteger(false);
            var sb = new StringBuilder();
            sb.Append("0x");
            foreach (var c in name.ToCharArray())
            {
                sb.Append(((int)c).ToString("x"));
            }
            var hex = CUtils.FillPlaceHolder(sb.ToString(), 64 + 2, '0');
            return hex.HexToBigInteger(false);
        }

        public static string FormatJson(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }

        public static BigDecimal ConvertUnit(BigDecimal value, UnitConversion.EthUnit src = UnitConversion.EthUnit.Gwei, UnitConversion.EthUnit dst = UnitConversion.EthUnit.Ether)
        {
            return UnitConversion.Convert.FromWeiToBigDecimal(UnitConversion.Convert.ToWei(value, src), dst);
        }
        public static BigDecimal GasEth(HexBigInteger gas)
        {
            var gaseth = UnitConversion.Convert.FromWeiToBigDecimal(gas.Value * DefaultGasPrice, UnitConversion.EthUnit.Ether);
            return gaseth;
        }
        public static BigInteger DefaultGasPrice = UnitConversion.Convert.ToWei(5, UnitConversion.EthUnit.Gwei);

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------------
        #region CallAndTransaction

        public static async Task<string> CallAsync(this Contract contract, string functionName, params object[] functionInput)
        {
            var call = contract.GetFunction(functionName);
            var callInput = call.CreateCallInput(functionInput);
            var result = await call.CallAsync(callInput);
            return result;
        }
        public static async Task<T> CallAsync<T>(this Contract contract, string functionName, params object[] functionInput)
        {
            var call = contract.GetFunction(functionName);
            var result = await call.CallAsync<T>(functionInput);
            return result;
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------
        public static TransactionInput CreateTransactionInput(this Contract contract, string functionName, string from, params object[] functionInput)
        {
            var call = contract.GetFunction(functionName);
            return call.CreateTransactionInput(from, functionInput);
        }

        public static async Task<TransactionReceipt> SendTransactionAndWaitForReceiptAsync(this Web3 web3, TransactionInput ttt)
        {
            var gas = await web3.TransactionManager.EstimateGasAsync(ttt);//474800
            ttt.Gas = gas;
            var hash = await web3.TransactionManager.SendTransactionAsync(ttt);
            var receipt = await web3.WaitTransactionReceiptAsync(hash);
            return receipt;
        }
        public static async Task<string> SendTransactionAsync(this Web3 web3, TransactionInput ttt)
        {
            var gas = await web3.TransactionManager.EstimateGasAsync(ttt);//474800
            ttt.Gas = gas;
            var hash = await web3.TransactionManager.SendTransactionAsync(ttt);
            return hash;
        }
        public static async Task<string> SendTransactionAsync(this Contract c, string from, string functionName, params object[] functionInput)
        {
            var tx = c.CreateTransactionInput(functionName, from, functionInput);
            var gas = await c.Eth.TransactionManager.EstimateGasAsync(tx);
            tx.Gas = gas;
            var transactionHash = await c.Eth.TransactionManager.SendTransactionAsync(tx);
            return transactionHash;
        }
        public static async Task<TransactionReceipt> SendTransactionAndWaitReceiptAsync(this Contract c, string from, string functionName, params object[] functionInput)
        {
            var tx = c.CreateTransactionInput(functionName, from, functionInput);
            var gas = await c.Eth.TransactionManager.EstimateGasAsync(tx);
            tx.Gas = gas;
            var transactionHash = await c.Eth.TransactionManager.SendTransactionAsync(tx);
            var receipt = await c.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            while (receipt == null)
            {
                System.Threading.Thread.Sleep(1000);
                receipt = await c.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            }
            return receipt;
        }

        public static async Task<TransactionReceipt> SendTransactionAndWaitReceiptAsync(this Contract c, Account signer, string functionName, params object[] functionInput) {

            var web3 = new Nethereum.Web3.Web3(signer, c.Eth.Client);
            var tx = c.CreateTransactionInput(functionName, signer.Address, functionInput);
            var gas = await web3.Eth.TransactionManager.EstimateGasAsync(tx);
            tx.Gas = gas;
            var hash = await web3.TransactionManager.SendTransactionAsync(tx);
            var receipt = await web3.WaitTransactionReceiptAsync(hash);
            return receipt;
        }
        public static async Task<TransactionReceipt> SendTransactionAndWaitReceiptAsync(this Contract c, Account signer, TransactionInput tx)
        {
            var web3 = new Nethereum.Web3.Web3(signer, c.Eth.Client);
            var gas = await web3.Eth.TransactionManager.EstimateGasAsync(tx);
            tx.Gas = gas;
            var hash = await web3.TransactionManager.SendTransactionAsync(tx);
            var receipt = await web3.WaitTransactionReceiptAsync(hash);
            return receipt;
        }


        //----------------------------------------------------------------------------------------------------------------------------------------------
        public static Task<TransactionReceipt> GetTransactionReceiptAsync(this Web3 web3, string hash)
        {
            return web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
        }

        public static async Task<TransactionReceipt> WaitTransactionReceiptAsync(this Web3 web3, string hash)
        {
            var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
            while (receipt == null)
            {
                System.Threading.Thread.Sleep(1000);
                receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);
            }
            return receipt;
        }
        public static async Task<List<EventLog<T>>> WaitTransactionReceiptEventsAsync<T>(this Web3 web3, string hash) where T : IEventDTO, new()
        {
            var receipt = await web3.WaitTransactionReceiptAsync(hash);
            var logs = receipt.Logs.ConvertToFilterLog().DecodeAllEvents<T>();
            return logs;
        }
        public static async Task<EventLog<T>> WaitTransactionReceiptEventAsync<T>(this Web3 web3, string hash) where T : IEventDTO, new()
        {
            var receipt = await web3.WaitTransactionReceiptAsync(hash);
            var logs = receipt.Logs.ConvertToFilterLog().DecodeAllEvents<T>();
            return logs.Count > 0 ? logs[0] : null;
        }

        public static List<EventLog<T>> GetEventLogs<T>(this TransactionReceipt receipt) where T : IEventDTO, new()
        {
            var logs = receipt.Logs.ConvertToFilterLog().DecodeAllEvents<T>();
            return logs;
        }
        public static EventLog<T> GetEventLog<T>(this TransactionReceipt receipt) where T : IEventDTO, new()
        {
            var logs = receipt.Logs.ConvertToFilterLog().DecodeAllEvents<T>();
            return logs.Count > 0 ? logs[0] : null;
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------------------------


    }

}


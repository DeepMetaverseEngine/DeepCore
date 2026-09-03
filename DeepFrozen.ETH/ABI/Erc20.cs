using Nethereum.ABI.Decoders;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.StandardTokenEIP20;
using Nethereum.Util;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH.ABI
{
    public static class Erc20
    {
        public static string ABI { get; } = typeof(Erc721).Assembly.GetAssemblyABI("Erc20.abi.json");

        public static StandardTokenService CreateService(this Web3 web3, string address)
        {
            return new StandardTokenService(web3, address);
        }

        public static async Task<decimal> balanceOf(this Web3 web3, string owner, UnitConversion.EthUnit unit = UnitConversion.EthUnit.Ether)
        {
            var balance = await web3.Eth.GetBalance.SendRequestAsync(owner);
            var value = UnitConversion.Convert.FromWei(balance, unit);
            return value;
        }


        public static async Task<HexBigInteger> name(this Contract erc20)
        {
            var call = erc20.GetFunction("name");
            var callInput = call.CreateCallInput();
            var result = await call.CallAsync(callInput);
            return new HexBigInteger(result);
        }
        public static async Task<HexBigInteger> symbol(this Contract erc20)
        {
            var call = erc20.GetFunction("symbol");
            var callInput = call.CreateCallInput();
            var result = await call.CallAsync(callInput);
            return new HexBigInteger(result);
        }

        public static async Task<HexBigInteger> decimals(this Contract erc20)
        {
            var call = erc20.GetFunction("decimals");
            var callInput = call.CreateCallInput();
            var result = await call.CallAsync(callInput);
            return new HexBigInteger(result);
        }
        public static async Task<HexBigInteger> totalSupply(this Contract erc20)
        {
            var call = erc20.GetFunction("totalSupply");
            var callInput = call.CreateCallInput();
            var result = await call.CallAsync(callInput);
            return new HexBigInteger(result);
        }
        public static async Task<HexBigInteger> allowance(this Contract erc20, string _owner, string _spender)
        {
            var call = erc20.GetFunction("allowance");
            var callInput = call.CreateCallInput(_owner, _spender);
            var result = await call.CallAsync(callInput);
            return new HexBigInteger(result);
        }
        public static async Task<decimal> balanceOf(this Contract erc20, string owner, UnitConversion.EthUnit unit = UnitConversion.EthUnit.Ether)
        {
            var call = erc20.GetFunction("balanceOf");
            var callInput = call.CreateCallInput(owner);
            var balance = await call.CallAsync(callInput);
            return UnitConversion.Convert.FromWei(new HexBigInteger(balance), unit);
        }


        public static TransactionInput tx_approve(this Contract erc20, string _sender, string _operator, BigInteger _approved)
        {
            var tx = erc20.GetFunction("approve");
            var td = tx.CreateTransactionInput(_sender, new object[] { _operator, _approved });
            return td;
        }
        public static TransactionInput tx_transfer(this Contract erc20, string _sender, string _to, BigInteger _amount)
        {
            var tx = erc20.GetFunction("transfer");
            var td = tx.CreateTransactionInput(_sender, new object[] { _to, _amount });
            return td;
        }
        public static TransactionInput tx_transferFrom(this Contract erc20, string _sender, string _from, string _to, BigInteger _amount)
        {
            var tx = erc20.GetFunction("transferFrom");
            var td = tx.CreateTransactionInput(_sender, new object[] { _from, _to, _amount });
            return td;
        }

    }
}

using DeepCore;
using Nethereum.ABI.Decoders;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.StandardNonFungibleTokenERC721;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH.ABI
{
    public static class Erc721
    {
        public static string Erc721ABI { get; } = typeof(Erc721).Assembly.GetAssemblyABI("Erc721.abi.json");

        public static ERC721Service CreateService(this Web3 web3, string address)
        {
            return new ERC721Service(web3, address);
        }


        public static async Task<HexBigInteger> tokenOfOwnerByIndex(this Contract erc721, string owner, int index)
        {
            var tx_tokenID = erc721.GetFunction("tokenOfOwnerByIndex");
            var td_tokenID = tx_tokenID.CreateCallInput(owner, index);
            var rt_tokenID = await tx_tokenID.CallAsync(td_tokenID);
            var tokenID = new HexBigInteger(rt_tokenID);
            return tokenID;
        }
        public static async Task<bool> isApprovedForAll(this Contract erc721, string _owner, string _operator)
        {
            //function isApprovedForAll(address _owner, address _operator) external view returns(bool);
            var call = erc721.GetFunction("isApprovedForAll");
            var callInput = call.CreateCallInput(_owner, _operator);
            var rst = await call.CallAsync(callInput);
            var _rst = new HexBigInteger(rst);
            return _rst.Value != 0;
        }


        public static TransactionInput tx_safeTransferFrom(this Contract erc721, string _sender, string _from, string to, BigInteger tokenID)
        {
            var tx_transfer = erc721.GetFunction("safeTransferFrom");
            var td_transfer = tx_transfer.CreateTransactionInput(_sender, new object[] { _from, to, tokenID });
            return td_transfer;
        }
        public static TransactionInput tx_setApprovalForAll(this Contract erc721, string _sender, string _operator, bool _approved)
        {
            var tx = erc721.GetFunction("setApprovalForAll");
            var td = tx.CreateTransactionInput(_sender, new object[] { _operator, _approved });
            return td;
        }
    }

    public static class Erc721Metadata
    {
        public static async Task<string> tokenURI(this Contract erc721, BigInteger tokenID)
        {
            var call = erc721.GetFunction("tokenURI");
            var callInput = call.CreateCallInput(tokenID);
            var rst = await call.CallAsync(callInput);
            return new StringBytes32Decoder().Decode(CUtils.HexToBin(rst));
            //return new ABI.StringType().Decode<string>(rst);
        }

    }
}

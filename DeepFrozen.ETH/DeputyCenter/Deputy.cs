using DeepCore;
using DeepCore.IO;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeepFrozen.ETH.DeputyCenter
{
    public class Deputy
    {
        public const string ZeroAddress = "0x0000000000000000000000000000000000000000";

        public Web3 Web3 { get; }
        public Account Signer { get; }

        public string URI { get; private set; }

        public Deputy(string uri, Account account)
        {
            URI = uri;
            Signer = account;
            Web3 = new Web3(account, uri);
            Web3.TransactionManager.UseLegacyAsDefault = true;
        }

        public string SetDeputy(CallInput pt, Account signer, BigInteger uniqueNonce)
        {
            return SetDeputy(pt, signer, uniqueNonce, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, false, ZeroAddress);
        }
        public string SetDeputy(
            CallInput pt,
            Account signer,
            BigInteger uniqueNonce,
            BigInteger dependUniqueNonce,
            BigInteger before,
            BigInteger value,
            bool onlyDesignatedSender = false,
            string designatedSender = ZeroAddress)
        {
            Debug.Assert(pt != null && pt.Data != null && pt.To != null && signer.ChainId != null);
            var enabledCallData = pt.Data;
            var to = pt.To;
            var chainId = signer.ChainId.Value;

            var abiEncode = new ABIEncode();
            var input = new DeputyParamsInput()
            {
                calldata = enabledCallData.HexToByteArray(),
                real_calldata_length = enabledCallData.Length / 2 - 1,//the enabledCallData is start with 0x
                to = to,
                chainId = chainId,
                beforeTimeStamp = before,
                uniqueNonce = uniqueNonce,
                dependUniqueNonce = dependUniqueNonce,
                value = value,
                onlyDesignatedSender = onlyDesignatedSender,
                designatedSender = designatedSender,
                signer = signer.Address,
            };
            var toSign = abiEncode.GetABIParamsEncodedPacked(input);
            //var toSignHex = toSign.ToHex(true);
            //Console.WriteLine($"toSign is \n{toSignHex}");

            //let digest = ethers.utils.keccak256(toSign)
            var digest = Nethereum.Util.Sha3Keccack.Current.CalculateHash(toSign);
            //var digestHex = digest.ToHex(true);
            //Console.WriteLine($"digest is \n{digestHex}");

            //let sig = await signer.signMessage(ethers.utils.arrayify(digest))
            //var sig = await Web3.Eth.Sign.SendRequestAsync(signer.Address, digestHex);
            var sig = signMessage(signer, digest);

            //Console.WriteLine($"sig is \n{sig}");

            //return toSign + sig.substring(2)
            return toSign.ToHex(true) + sig.Substring(2);
        }


        private static string signMessage(Account signer, byte[] digest)
        {
            var privateKey = signer.PrivateKey;
            var signer1 = new EthereumMessageSigner();
            //                 var signature1 = signer1.EncodeUTF8AndSign(digestHex, new EthECKey(privateKey));
            //                 var signature2 = signer1.HashAndSign(digestHex, privateKey);
            var signature3 = signer1.Sign(digest, privateKey);
            return signature3;
            //             var privateKey = signer.PrivateKey;
            //             var signer1 = new EthereumMessageSigner();
            //             var signature1 = signer1.EncodeUTF8AndSign(msg1, new EthECKey(privateKey));
            //             //var addressRec1 = signer1.EncodeUTF8AndEcRecover(msg1, signature1);
            //             var signer2 = new EthereumMessageSigner();
            //             var signature2 = signer2.HashAndSign(msg1, privateKey);
            //return signature1;
        }
        /**
        export async function setDeputy(
            pt: PopulatedTransaction,
            signer: SignerWithAddress,
            uniqueNonce: BigNumber,
            dependUniqueNonce: BigNumber = ethers.constants.Zero,
            before: BigNumber = ethers.constants.Zero,
            value: BigNumber = ethers.constants.Zero,
            onlyDesignatedSender: boolean = false,
            designatedSender: string = ethers.constants.AddressZero,
        ): Promise<string> {
            let enabledCallData = pt.data!
            let to = pt.to!
            let chainId = (await ethers.provider.getNetwork()).chainId

            let toSign = abiEncoderPacked(
                [
                    "bytes",//calldata
                    "uint256",//real calldata length
                    "address",//to
                    "uint256",//chainId
                    "uint256",//beforeTimeStamp
                    "uint256",//uniqueNonce
                    "uint256",//dependUniqueNonce
                    "uint256",//value
                    "bool",//onlyDesignatedSender
                    "address",//designatedSender
                    "address",//signer
                ],
                [
                    enabledCallData,
                    enabledCallData.length / 2 - 1,//the enabledCallData is start with 0x
                    to,
                    chainId,
                    before,
                    uniqueNonce,
                    dependUniqueNonce,
                    value,
                    onlyDesignatedSender,
                    designatedSender,
                    signer.address,
                ],
            )

            let digest = ethers.utils.keccak256(toSign)

            let sig = await signer.signMessage(ethers.utils.arrayify(digest))
            return toSign + sig.substring(2)
        }
        */

        //-----------------------------------------------------------------------------------------------------------------------------------

    }
    public class DeputyParamsInput
    {
        [Parameter("bytes", 1)] public byte[] calldata { get; set; }
        [Parameter("uint256", 2)] public BigInteger real_calldata_length { get; set; }
        [Parameter("address", 3)] public string to { get; set; }
        [Parameter("uint256", 4)] public BigInteger chainId { get; set; }
        [Parameter("uint256", 5)] public BigInteger beforeTimeStamp { get; set; }
        [Parameter("uint256", 6)] public BigInteger uniqueNonce { get; set; }
        [Parameter("uint256", 7)] public BigInteger dependUniqueNonce { get; set; }
        [Parameter("uint256", 8)] public BigInteger barrierNonce { get; set; }
        [Parameter("uint256", 9)] public BigInteger value { get; set; }
        [Parameter("bool", 10)] public bool onlyDesignatedSender { get; set; }
        [Parameter("address", 11)] public string designatedSender { get; set; }
        [Parameter("address", 12)] public string signer { get; set; }
    }

    //     public static class Helper
    //     {
    // //         public static async Task<TransactionReceipt> WaitTransactionReceiptAsync(this Web3 web3, string tx)
    // //         {
    // //             var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(tx);
    // //             while (receipt == null)
    // //             {
    // //                 System.Threading.Thread.Sleep(1000);
    // //                 receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(tx);
    // //             }
    // //             return receipt;
    // //         }
    //     }

    /*
    [FunctionOutput]
    public class ForgeKCardRecord
    {
        [Parameter("address", "requester", 1)] public string requester { get; set; }
        [Parameter("uint256", "amount", 2)] public BigInteger amount { get; set; }
        [Parameter("uint256", "forgedAmount", 2)] public BigInteger forgedAmount { get; set; }
    }


    [Function("forgeKCardRequest", "bool")]
    public class ApproveFunctionBase : FunctionMessage
    {
        [Parameter("address", "_spender", 1)]
        public virtual string Spender
        {
            get;
            set;
        }

        [Parameter("uint256", "_value", 2)]
        public virtual BigInteger Value
        {
            get;
            set;
        }
    }
    */
}

using Nethereum.ABI.FunctionEncoding.Attributes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DeepFrozen.ETH.Workspace
{
    public class NameServiceType
    {
        public const string SINGLE_REGISTRY_UNKNOWN = "";
        public const string S_Miner = "Miner";
        public const string S_Manager = "Manager";
        public const string S_DeputyCenter = "DeputyCenter";
        public const string S_VRFCenter = "VRFCenter";
        public const string S_AssetVault = "AssetVault";

        public const string MULTIPLE_REGISTRY_UNKNOWN = "";
        public const string M_Server = "Server";

        
    }


    public class MintSudoParam
    {
        [Parameter("bytes32", 1)] public BigInteger model { get; set; }
        [Parameter("address", 2)] public string who { get; set; }
        [Parameter("uint256", 3)] public BigInteger amount { get; set; }
    }


    [Event("DepositErc721")]
    public class DepositErc721EventDTO : IEventDTO
    {
        [Parameter("address", "tokenAddress", 1, true)] public string tokenAddress { get; set; }
        [Parameter("address", "from", 2, false)] public string from { get; set; }
        [Parameter("address", "owner", 3, true)] public string owner { get; set; }
        [Parameter("uint256", "tokenId", 4, false)] public BigInteger tokenId { get; set; }
    }

}

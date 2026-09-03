using DeepCore.Reflection;
using Nethereum.Contracts;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepFrozen.ETH.Workspace
{
    [Expandable]
    public class DeployedContractInfo
    {
        public string Name;
        public string ABI;
        public string Bytecode;
        public string Address;
        public string TransactionHash;
        public string DeployedTimeUTC;
    }

    [Expandable]
    public class DeployedUpgradeableContractInfo
    {
        public string PrefixName;
        [Expandable]
        public DeployedContractInfo Storage;
        [Expandable]
        public DeployedContractInfo Interface;
        [Expandable]
        public DeployedContractInfo[] Logics;
        public DeployedUpgradeableContractInfo(string prefixName)
        {
            PrefixName = prefixName;
        }
        public override string ToString()
        {
            return PrefixName;
        }
        public Contract GetContract(Web3 web3)
        {
            return web3.Eth.GetContract(Interface.ABI, Storage.Address);
        }
    }

    public class DeployedProject
    {
        public string NameServiceAddress;
        [Expandable]
        public DeployedUpgradeableContractInfo NameService;
        [Expandable]
        public DeployedUpgradeableContractInfo DeputyCenter;
        [Expandable]
        public DeployedUpgradeableContractInfo[] Services;
    }
}

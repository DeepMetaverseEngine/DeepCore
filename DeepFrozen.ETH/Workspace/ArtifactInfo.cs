using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nethereum.Contracts;
using Nethereum.Web3;
using DeepCore;
using DeepCore.Reflection;

namespace DeepFrozen.ETH.Workspace
{

    public class ContractArtifact
    {
        [Desc]
        public string ContractName;
        [Desc]
        public FileInfo JsonFile;
        [Desc(Editable = false)]
        public string JsonText;
        [Desc(Editable = false)]
        public JObject Json;
        [Desc]
        public string ABI;
        [Desc]
        public string Bytecode;
        [Desc]
        public FileInfo SourcePath;
        [Desc]
        public string AbsolutePath;
        public override string ToString()
        {
            return ContractName;
        }
        public DeployedContractInfo ToDeployed()
        {
            return new DeployedContractInfo()
            {
                ABI = this.ABI,
                Bytecode = this.Bytecode,
                Name = this.ContractName,
            };
        }
    }
    public class UpgradeableContractArtifact
    {
        public readonly string PrefixName;
        public ContractArtifact Storage;
        public ContractArtifact Interface;
        public ContractArtifact[] Logics;
        public UpgradeableContractArtifact(string prefixName)
        {
            PrefixName = prefixName;
        }
        public override string ToString()
        {
            return PrefixName;
        }
        public DeployedUpgradeableContractInfo ToDeployed()
        {
            return new DeployedUpgradeableContractInfo(PrefixName)
            {
                Interface = this.Interface.ToDeployed(),
                Storage = this.Storage.ToDeployed(),
                Logics = this.Logics.Convert1D((i, t) => t.ToDeployed()),
            };
        }
    }

    public class DeployProjectConfig
    {
        public string NameServiceContractAddress;
        public bool NameServiceNew = false;
        public UpgradeableContractArtifact NameService;
        public UpgradeableContractArtifact DeputyCenter;
        public UpgradeableContractArtifact[] Services;
    }





}

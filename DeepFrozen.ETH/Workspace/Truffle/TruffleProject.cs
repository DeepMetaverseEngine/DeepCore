using DeepCore.IO;
using DeepCore.Log;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nethereum.Web3.Accounts;
using DeepCore;
using Org.BouncyCastle.Ocsp;
using Nethereum.Web3;
using Org.BouncyCastle.Math.EC.Multiplier;
using System.Threading;
using Nethereum.Contracts;
using System.Collections;

namespace DeepFrozen.ETH.Workspace.Truffle
{
    public class TruffleProject : BaseProject
    {
        public static bool TryFindProject(string root, string projectName, out DirectoryInfo workspace)
        {
            if (CFiles.TryFindParentFile(root, $@"{projectName}\truffle-config.js", out var _truffle))
            {
                workspace = new FileInfo(_truffle).Directory;
                return true;
            }
            workspace = null;
            return false;
        }
        public static bool TryFindProject(string root, out DirectoryInfo workspace)
        {
            if (CFiles.TryFindParentFile(root, $@"truffle-config.js", out var _truffle))
            {
                workspace = new FileInfo(_truffle).Directory;
                return true;
            }
            workspace = null;
            return false;
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        public TruffleProject(DirectoryInfo workingDirectory) : base(workingDirectory)
        {
            RefreshArtifacts();
        }



        public Task<int> TruffleCompileAsync()
        {
            return RunCommandAsync("truffle compile");
        }
        public Task<int> TruffleMigrateAsync()
        {
            return RunCommandAsync("truffle migrate");
        }
        public Task<int> TruffleInitAsync()
        {
            return RunCommandAsync("truffle init");
        }
        public Task<int> TruffleInstallAsync()
        {
            return RunCommandAsync("npm install -g truffle");
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Contracts
        override protected  IEnumerable<ContractArtifact> LoadContracts()
        {
            var contracts = new List<ContractArtifact>();
            foreach (var jfile in CFiles.ListAllFiles(Path.Combine(WorkingDirectory.FullName, "build", "contracts"), true))
            {
                if (jfile.Extension.EndsWith("json", StringComparison.OrdinalIgnoreCase))
                {
                    var contract = LoadArtifact(jfile);
                    if (contract != null)
                    {
                        contracts.Add(contract);
                    }
                }
            }
            return contracts;
        }
        protected virtual ContractArtifact LoadArtifact(FileInfo jfile)
        {
            try
            {
                var jtxt = File.ReadAllText(jfile.FullName);
                var json = JsonConvert.DeserializeObject(jtxt) as JObject;
                return new ContractArtifact()
                {
                    JsonFile = jfile,
                    JsonText = jtxt,
                    Json = json,
                    ContractName = json["contractName"].ToString(),
                    ABI = json["abi"].ToString(),
                    Bytecode = json["bytecode"].ToString(),
                    SourcePath = new FileInfo(json["sourcePath"].ToString()),
                    AbsolutePath = json["ast"]["absolutePath"].ToString(),
                };
            }
            catch { return null; }
        }


        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
    }


}

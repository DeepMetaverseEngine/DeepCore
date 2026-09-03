using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepFrozen.ETH;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepFrozen.ETH.Workspace
{
    public abstract class BaseProject : Disposable
    {
        protected readonly FileLogger log;
        public FileLogger Log { get => log; }
        public DirectoryInfo WorkingDirectory { get; }
        public BaseProject(DirectoryInfo workingDirectory)
        {
            this.WorkingDirectory = workingDirectory;
            this.log = new FileLogger(Path.Combine(WorkingDirectory.FullName, "_logs", $"project_{CUtils.FormatTime(DateTime.Now)}.log"));
            this.log.Decoder = (format) => JsonConvert.SerializeObject(format, Formatting.Indented);
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        protected override void Disposing()
        {
            log.Dispose();
        }
        public Task<int> RunCommandAsync(string command)
        {
            return Task.Run(() =>
            {
                log.Info(command);
                var exe = new ProcessStartInfo();
                exe.WorkingDirectory = Path.GetDirectoryName(WorkingDirectory.FullName);
                exe.FileName = "cmd";
                exe.UseShellExecute = false;
                exe.RedirectStandardInput = true;
                exe.RedirectStandardOutput = true;
                exe.RedirectStandardError = true;
                var my = Process.Start(exe);
                my.OutputDataReceived += (sender, e) => { log.Info(e.Data); };
                my.ErrorDataReceived += (sender, e) => { log.Error(e.Data); };
                my.BeginErrorReadLine();
                my.BeginOutputReadLine();
                my.StandardInput.WriteLine($"@cd {WorkingDirectory.FullName}");
                my.StandardInput.WriteLine($"@{command}");
                my.StandardInput.WriteLine($"@exit");
                my.StandardInput.Flush();
                my.WaitForExit();
                return my.ExitCode;
            });
        }
        //-----------------------------------------------------------------------------------------------------------------------------
        #region FileSystem

        public void SaveToFile(string name, string content)
        {
            CFiles.WriteAllText(Path.Combine(WorkingDirectory.FullName, "_nethereum", $"{name}"), content);
        }
        public void SaveToFile(string name, object content)
        {
            var text = JsonConvert.SerializeObject(content, Formatting.Indented);
            SaveToFile(name, text);
        }
        public bool TryLoadFile(string name, out string content)
        {
            var path = Path.Combine(WorkingDirectory.FullName, "_nethereum", $"{name}");
            if (File.Exists(path))
            {
                content = File.ReadAllText(path); return true;
            }
            content = null;
            return false;
        }
        public bool TryLoadFile<T>(string name, out T content)
        {
            if (TryLoadFile(name, out var _content))
            {
                content = JsonConvert.DeserializeObject<T>(_content); return true;
            }
            content = default(T);
            return false;
        }

        public void CopyDependence(string target = null)
        {
            var src = Path.Combine(typeof(BaseProject).AssemblyDirectory().FullName, "template", "contracts", "nameserver");
            var dst = target ?? Path.Combine(WorkingDirectory.FullName, "node_modules", "@nameserver", "contracts", "nameserver");
            CFiles.DirectoryCopy(src, dst, (d) => { log.Info(d.FullName); return true; });
        }
        public void CopyDependenceAndToken(string target = null)
        {
            CopyDependence(target);
            var src = Path.Combine(typeof(BaseProject).AssemblyDirectory().FullName, "template", "contracts", "opentoken");
            var dst = target ?? Path.Combine(WorkingDirectory.FullName, "node_modules", "@nameserver", "contracts", "opentoken");
            CFiles.DirectoryCopy(src, dst, (d) => { log.Info(d.FullName); return true; });
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Web3      
        public Web3 web3 { get; private set; }
        public Account admin { get; private set; }
        public DeployAccounts accounts { get; private set; }
        public void Connect(DeployAccounts _account)
        {
            this.web3 = _account.Web3;
            this.admin = _account.Admin;
            this.accounts = _account;
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Artifact

        private HashMap<string, ContractArtifact> contracts = new HashMap<string, ContractArtifact>();
        private HashMap<string, ContractArtifact> contractFiles = new HashMap<string, ContractArtifact>();

        protected abstract IEnumerable<ContractArtifact> LoadContracts();

        public void RefreshArtifacts()
        {
            contracts.Clear();
            contractFiles.Clear();
            foreach (var contract in LoadContracts())
            {
                contractFiles.Add(contract.JsonFile.FullName, contract);
                contracts.Add(contract.ContractName, contract);
            }
        }
        public ContractArtifact[] GetArtifacts()
        {
            var ret = new List<ContractArtifact>(contracts.Values);
            return ret.ToArray();
        }
        public ContractArtifact GetArtifact(string contractName)
        {
            return contracts.Get(contractName);
        }
        public ContractArtifact GetArtifact(FileInfo contractFile)
        {
            return contracts.Get(contractFile.FullName);
        }
        public ContractArtifact[] GetArtifactsWithPrefix(string prefix)
        {
            var ret = new List<ContractArtifact>();
            foreach (var c in GetArtifacts())
            {
                if (c.ContractName.StartsWith(prefix))
                {
                    ret.Add(c);
                }
            }
            return ret.ToArray();
        }
        public ContractArtifact[] GetArtifactsWithDirectory(DirectoryInfo prefix)
        {
            var ret = new List<ContractArtifact>();
            foreach (var c in GetArtifacts())
            {
                if (c.SourcePath.Directory.FullName == prefix.FullName)
                {
                    ret.Add(c);
                }
            }
            return ret.ToArray();
        }
        public UpgradeableContractArtifact GetUpgradeableContractArtifact(string prefixName)
        {
            var interfaceName = GetArtifact($"{prefixName}Interface");
            if (interfaceName == null) throw new Exception($"Can Not Find Interface '{prefixName}'");
            var storageName = GetArtifact($"{prefixName}Storage");
            if (storageName == null) throw new Exception($"Can Not Find Storage '{prefixName}'");
            var logicsName = GetArtifactsWithPrefix($"{prefixName}Logic");
            if (logicsName.Length == 0) throw new Exception($"Can Not Find Logic '{prefixName}'");
            return new UpgradeableContractArtifact(prefixName)
            {
                Interface = interfaceName,
                Storage = storageName,
                Logics = logicsName,
            };
        }
        public UpgradeableContractArtifact[] GetUpgradeableContractArtifacts(string[] prefixName)
        {
            var list = new UpgradeableContractArtifact[prefixName.Length];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = GetUpgradeableContractArtifact(prefixName[i]);
            }
            return list;
        }
        public bool TryGetUpgradeableContractArtifact(DirectoryInfo prefixName, out UpgradeableContractArtifact artifacts)
        {
            var list = new List<ContractArtifact>(GetArtifactsWithDirectory(prefixName));
            artifacts = null;
            var interfaceName = list.Find(c => c.ContractName.StartsWith($"{prefixName.Name}Interface", StringComparison.OrdinalIgnoreCase));
            if (interfaceName == null) return false;
            var storageName = list.Find(c => c.ContractName.StartsWith($"{prefixName.Name}Storage", StringComparison.OrdinalIgnoreCase));
            if (storageName == null) return false;
            var logicsName = list.FindAll(c => c.ContractName.StartsWith($"{prefixName.Name}Logic", StringComparison.OrdinalIgnoreCase));
            if (logicsName.Count == 0) return false;
            artifacts = new UpgradeableContractArtifact(prefixName.Name)
            {
                Interface = interfaceName,
                Storage = storageName,
                Logics = logicsName.ToArray(),
            };
            return true;
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region DeployedContract

        public async Task<DeployedContractInfo> DeployContractAsync(string contractName, params object[] args)
        {
            var contract = GetArtifact(contractName);
            return await DeployContractAsync(contract, args);
        }
        public async Task<DeployedContractInfo> DeployContractAsync(ContractArtifact contract, params object[] args)
        {
            log.Info($"Deploying '{contract}'");
            var gas = await web3.Eth.DeployContract.EstimateGasAsync(
                contract.ABI,
                contract.Bytecode,
                admin.Address,
                args);
            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(
                contract.ABI,
                contract.Bytecode,
                admin.Address,
                gas,
                args);
            var receipt = await web3.WaitTransactionReceiptAsync(transactionHash);
            var contractAddress = receipt.ContractAddress;
            log.Info($"Deploy Complete '{contract}', ContractAddress is '{contractAddress}', TransactionHash is '{receipt.TransactionHash}'");
            var ret = new DeployedContractInfo()
            {
                Name = contract.ContractName,
                ABI = contract.ABI,
                Bytecode = contract.Bytecode,
                Address = contractAddress,
                TransactionHash = transactionHash,
                DeployedTimeUTC = CUtils.FormatTime(DateTime.UtcNow),
            };
            SaveToFile($"_deployed_{contract}.json", ret);
            return ret;
        }
        public bool TryLoadDeployedContract(string name, out DeployedContractInfo info)
        {
            return TryLoadFile<DeployedContractInfo>($"_deployed_{name}.json", out info);
        }


        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------
        #region Upgradeable

        protected async Task<Contract> GetStorageFromNameServiceAsync(Contract nameServer, UpgradeableContractArtifact artifact)
        {
            if (nameServer == null) return null;
            var proxyAddress = await nameServer.CallAsync("getSingle",
                $"S:{artifact.PrefixName}".ToBytes32());
            if (!proxyAddress.IsZeroAddress())
            {
                var proxy = web3.Eth.GetContract(artifact.Interface.ABI, proxyAddress);
                return proxy;
            }
            return null;
        }
        protected async Task<DeployedContractInfo> GetOrDeployContractStorageAsync(Contract nameServer, UpgradeableContractArtifact artifact, params object[] args)
        {
            var storage = await GetStorageFromNameServiceAsync(nameServer, artifact);
            if (storage != null)
            {
                if (TryLoadDeployedContract(artifact.Storage.ContractName, out var abi) && abi.Address == storage.Address)
                {
                    return abi;
                }
                else
                {
                    return new DeployedContractInfo()
                    {
                        Name = artifact.Storage.ContractName,
                        ABI = artifact.Storage.ABI,
                        Bytecode = artifact.Storage.Bytecode,
                        Address = storage.Address,
                    };
                }
            }
            var deployed = await DeployContractAsync(artifact.Storage, args);
            if (nameServer != null)
            {
                log.Info($"NameService.setSingle '{artifact}' => '{deployed.Address}'");
                var receipt = await nameServer.SendTransactionAndWaitReceiptAsync(accounts.Operator,
                    "setSingle",
                    $"S:{artifact.PrefixName}".ToBytes32(),
                    deployed.Address);
                log.Info($"NameService.setSingle '{artifact}' => '{deployed.Address}' complete, tx is '{receipt.TransactionHash}'");
            }
            return deployed;
        }
        protected async Task<DeployedUpgradeableContractInfo> DeployUpgradeableContract(Contract nameServer, UpgradeableContractArtifact contract, params object[] args)
        {
            if (contract.Storage == null)
            {
                throw new Exception($"deploy storage '{contract}' with no storage");
            }
            if (contract.Logics == null || contract.Logics.Length == 0)
            {
                throw new Exception($"deploy storage '{contract.Storage}' with no logics");
            }
            log.Info($"DeployContractUpgradeable: '{contract}', pending...");
            var storageAddress = await GetOrDeployContractStorageAsync(nameServer, contract, args);
            log.Info($"DeployContractUpgradeable, storage: '{contract.Storage}'");
            var logics = new List<DeployedContractInfo>();
            foreach (var logicName in contract.Logics)
            {
                var logic = await DeployContractAsync(logicName);
                log.Info($"DeployContractUpgradeable, logic: {logicName}");
                logics.Add(logic);
            }
            var proxy = web3.Eth.GetContract(contract.Storage.ABI, storageAddress.Address);
            if (proxy.GetFunction("sysGetDelegateAddresses") != null)
            {
                try
                {
                    var oldDelegates = await proxy.CallAsync<List<string>>("sysGetDelegateAddresses");
                    if (oldDelegates != null && oldDelegates.Count > 0)
                    {
                        var receipt = await proxy.SendTransactionAndWaitReceiptAsync(admin.Address, "sysDelDelegates", oldDelegates);
                        log.Info($"sysDelDelegates: tx is '{receipt.TransactionHash}'");
                    }
                }
                catch { }
                {

                    var newDelegates = logics.ConvertAll(c => c.Address);
                    var receipt = await proxy.SendTransactionAndWaitReceiptAsync(admin.Address, "sysAddDelegates", newDelegates);
                    log.Info($"sysAddDelegates: {newDelegates}, tx is '{receipt.TransactionHash}'");
                }
            }
            var ret = new DeployedUpgradeableContractInfo(contract.PrefixName)
            {
                Storage = storageAddress,
                Logics = logics.ToArray(),
                Interface = new DeployedContractInfo()
                {
                    ABI = contract.Interface.ABI,
                    Bytecode = contract.Interface.Bytecode,
                    Name = contract.Interface.ContractName,
                },
            };
            SaveToFile($"_upgradeable_{contract}.json", ret);
            return ret;
        }
        public bool TryLoadDeployedUpgradeableContract(string name, out DeployedUpgradeableContractInfo info)
        {
            return TryLoadFile<DeployedUpgradeableContractInfo>($"_upgradeable_{name}.json", out info);
        }

        public async Task<DeployedUpgradeableContractInfo> GetOrDeployNameServiceAsync(UpgradeableContractArtifact artifact, string contractAddress)
        {
            if (TryLoadDeployedUpgradeableContract(artifact.PrefixName, out var upgradeable))
            {
                if (!contractAddress.IsZeroAddress() && upgradeable.Storage.Address != contractAddress)
                {
                    upgradeable.Storage.Address = contractAddress;
                }
                var proxy = upgradeable.GetContract(web3);
                var owner = await proxy.CallAsync("getSingle", NameServiceType.M_Server.ToBytes32());
                log.Info($"NameService deployed : address is '{proxy.Address}', server is {owner}");
                return upgradeable;
            }
            if (TryLoadDeployedContract(artifact.Storage.ContractName, out var deployedNameService))
            {
                upgradeable = artifact.ToDeployed();
                upgradeable.Storage.Address = deployedNameService.Address;
                if (!contractAddress.IsZeroAddress() && upgradeable.Storage.Address != contractAddress)
                {
                    upgradeable.Storage.Address = contractAddress;
                }
                var proxy = upgradeable.GetContract(web3);
                var owner = await proxy.CallAsync("getSingle", NameServiceType.M_Server.ToBytes32());
                log.Info($"NameService deployed : address is '{proxy.Address}', server is {owner}");
                return upgradeable;
            }
            if (!contractAddress.IsZeroAddress())
            {
                upgradeable = artifact.ToDeployed();
                upgradeable.Storage.Address = contractAddress;
                var proxy = upgradeable.GetContract(web3);
                var owner = await proxy.CallAsync("getSingle", NameServiceType.M_Server.ToBytes32());
                log.Info($"NameService deployed : address is '{proxy.Address}', server is {owner}");
                return upgradeable;
            }
            else
            {
                return await DeployNewNameServiceAsync(artifact);
            }
        }
        public async Task<DeployedUpgradeableContractInfo> DeployNewNameServiceAsync(UpgradeableContractArtifact artifact)
        {
            var deployed = await DeployUpgradeableContract(null, artifact, accounts.Operator.Address);
            var proxy = deployed.GetContract(web3);
            var owner = await proxy.CallAsync("getSingle", NameServiceType.M_Server.ToBytes32());
            log.Info($"NameService deployed : address is '{proxy.Address}', server is {owner}");
            return deployed;
        }
        //-----------------------------------------------------------------------------------------------------------------------------

        public async Task<DeployedProject> DeployOrUpgradeAllAsync(
            string nameServiceAddress,
            string nameServicePrefix,
            string deputyCenterPrefix,
            params string[] contractsPrefix)
        {
            var nameService = GetUpgradeableContractArtifact(nameServicePrefix);
            var deputyCenter = GetUpgradeableContractArtifact(deputyCenterPrefix);
            var services = GetUpgradeableContractArtifacts(contractsPrefix);
            return await DeployOrUpgradeAllAsync(new DeployProjectConfig()
            {
                NameServiceContractAddress = nameServiceAddress,
                NameService = nameService,
                DeputyCenter = deputyCenter,
                Services = services,
            });
        }
        public async Task<DeployedProject> DeployOrUpgradeAllAsync(DeployProjectConfig deploy)
        {
            if (deploy.NameService == null) throw new Exception($"Can Not Find NameService");
            if (deploy.DeputyCenter == null) throw new Exception($"Can Not Find DeputyCenter");

            var nameService =deploy.NameServiceNew? 
                await DeployNewNameServiceAsync (deploy.NameService) :
                await GetOrDeployNameServiceAsync(deploy.NameService, deploy.NameServiceContractAddress);
            var nameProxy = nameService.GetContract(web3);
            var deputyCenter = await DeployUpgradeableContract(nameProxy, deploy.DeputyCenter,
                    nameProxy.Address,
                    accounts.Operator.Address);
            var services = new List<DeployedUpgradeableContractInfo>();
            foreach (var serviceArtifact in deploy.Services)
            {
                var svc = await DeployUpgradeableContract(nameProxy, serviceArtifact,
                    nameProxy.Address,
                    accounts.Operator.Address);
                services.Add(svc);
            }
            try
            {
                log.Info("setSingleEntries ...");
                {
                    var tx = await nameProxy.SendTransactionAndWaitReceiptAsync(accounts.Operator,
                        "setSingleEntries",
                        new BigInteger[] {
                        NameServiceType.S_DeputyCenter.ToBytes32() ,
                        NameServiceType.S_Miner.ToBytes32() },
                        new string[] {
                        deputyCenter.Storage.Address ,
                        accounts.Miner.Address }
                        );
                    log.Info($"tx {tx.TransactionHash}");
                }
                log.Info("setMultiple ... server ");
                {
                    var tx = await nameProxy.SendTransactionAndWaitReceiptAsync(accounts.Operator,
                        "setMultiple",
                        NameServiceType.M_Server.ToBytes32(),
                        accounts.Server.Address,
                        true);
                    log.Info($"tx {tx.TransactionHash}");
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
            var ret = new DeployedProject()
            {
                NameServiceAddress = nameService.Storage.Address,
                NameService = nameService,
                DeputyCenter = deputyCenter,
                Services = services.ToArray(),
            };
            SaveToFile($"_upgradeable.json", ret);
            log.Info("done");
            return ret;
        }
        public bool TryLoadDeployedAll(out DeployedProject info)
        {
            return TryLoadFile<DeployedProject>($"_upgradeable.json", out info);
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------


    }



}

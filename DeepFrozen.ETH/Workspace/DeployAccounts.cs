using DeepCore.Reflection;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DeepFrozen.ETH.Workspace
{
    public class DeployAccounts
    {
        public Web3 Web3;
        public Account Admin;
        public Account Operator;
        public Account Miner;
        public Account Server;
        public Account[] Accounts { get => new Account[] { Admin, Operator, Miner, Server, }; }
    }

    public class DeployAccountsConfig
    {
        [Desc("ChainID", "Chain")]
        public string ChainID = @"444444444500";
        [Desc("ChainURL", "Chain")]
        public string ChainURL = @"http://localhost:8545/";

        [Desc("Admin PrivateKey", "Account Input")]
        public string Admin;
        [Desc("Operator PrivateKey", "Account Input")]
        public string Operator;
        [Desc("Miner PrivateKey", "Account Input")]
        public string Miner;
        [Desc("Server PrivateKey", "Account Input")]
        public string Server;

        [Desc("Admin Address", "Account Info")] public string AdminAddress { get { try { return new Account(Admin, BigInteger.Parse(ChainID)).Address; } catch (Exception err) { return err.Message; } } }
        [Desc("Admin PublicKey", "Account Info")] public string AdminPublicKey { get { try { return new Account(Admin, BigInteger.Parse(ChainID)).PublicKey; } catch (Exception err) { return err.Message; } } }

        [Desc("Operator Address", "Account Info")] public string OperatorAddress { get { try { return new Account(Operator, BigInteger.Parse(ChainID)).Address; } catch (Exception err) { return err.Message; } } }
        [Desc("Operator PublicKey", "Account Info")] public string OperatorPublicKey { get { try { return new Account(Operator, BigInteger.Parse(ChainID)).PublicKey; } catch (Exception err) { return err.Message; } } }

        [Desc("Miner Address", "Account Info")] public string MinerAddress { get { try { return new Account(Miner, BigInteger.Parse(ChainID)).Address; } catch (Exception err) { return err.Message; } } }
        [Desc("Miner PublicKey", "Account Info")] public string MinerPublicKey { get { try { return new Account(Miner, BigInteger.Parse(ChainID)).PublicKey; } catch (Exception err) { return err.Message; } } }

        [Desc("Server Address", "Account Info")] public string ServerAddress { get { try { return new Account(Server, BigInteger.Parse(ChainID)).Address; } catch (Exception err) { return err.Message; } } }
        [Desc("Server PublicKey", "Account Info")] public string ServerPublicKey { get { try { return new Account(Server, BigInteger.Parse(ChainID)).PublicKey; } catch (Exception err) { return err.Message; } } }

        public DeployAccounts Connect()
        {
            var accounts = new DeployAccounts();
            {
                accounts.Admin    /**/= new Account(this.Admin, BigInteger.Parse(ChainID));
                accounts.Operator /**/= new Account(this.Operator, BigInteger.Parse(ChainID));
                accounts.Miner    /**/= new Account(this.Miner, BigInteger.Parse(ChainID));
                accounts.Server   /**/= new Account(this.Server, BigInteger.Parse(ChainID));
            }
            accounts.Web3 = new Nethereum.Web3.Web3(accounts.Admin, ChainURL ?? @"http://localhost:8545/");
            return accounts;
        }


    }
}

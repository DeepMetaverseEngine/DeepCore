using DeepCore;
using DeepCore.SQL;
using DeepCrystal.ORM;
using DeepFrozen.MySQL;
using Gate.Data;
using Gate.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Mail
{
    public class MailDomain
    {
        public SQLTableInfo<MailData, string> TMailDataTable { get; private set; }
        public string DomainName { get; private set; }
        internal readonly IMappingHash allusers;
        public MailDomain(string domain)
        {
            DomainName = domain;
            TMailDataTable = new SQLTableInfo<MailData, string>(DomainName);
            GateServerManager.MySQL.RunConnection(conn =>
            {
                TMailDataTable.InitSQLTable(conn);
            });
            allusers = GateServerManager.MailBox.MappingAdapter.GetHash(DomainName, null);
        }
        internal async Task RegistAsync(string account)
        {
            await allusers.SetAsync(account, DateTime.UtcNow, When.NotExists);
        }
        public override string ToString()
        {
            return DomainName;
        }
    }
}

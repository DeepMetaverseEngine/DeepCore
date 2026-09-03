using DeepCore;
using DeepCore.SQL;
using DeepCore.Threading;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using DeepFrozen.MySQL;
using Gate.Data;
using Gate.Server;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gate.Server.Mail
{

    public delegate Task IncomingMailAsync(MailBox sender, MailData mail);

    public class MailBox : AsyncDisposable
    {
        protected readonly IMappingAdapter db;
        protected readonly MySQLConnectPool mysql;
        protected readonly ITaskExecutor service;
        protected readonly string mailAccount;
        protected readonly string key;
        protected readonly IChannel domainChannel;
        protected readonly IMappingHash domainUnread;
        protected readonly IMappingHash domainTotal;
        protected readonly IMappingHash ownerCache;
        protected readonly SQLTableInfo<MailData, string> sqlTable;
        protected readonly string mailDomainName;
        protected MailDomain mailDomain;

        public MailDomain Domain { get => mailDomain; }
        public string MailAccount { get => mailAccount; }

        public MailBox(ITaskExecutor service, string mailDomain, string mailAccount)
        {
            this.mailDomainName = mailDomain;
            this.service = service;
            this.mysql = GateServerManager.MySQL;
            this.db = GateServerManager.MailBox.MappingAdapter;
            this.mailAccount = mailAccount;
            this.key = GetDomainAddress(mailAccount);
            this.domainChannel = ORMFactory.Instance.GetChannel(mailDomain, service);
            this.domainUnread = db.GetHash($"{mailDomain}.unread", service);
            this.domainTotal = db.GetHash($"{mailDomain}.total", service);
            this.ownerCache = db.GetHash(key, service);
            this.sqlTable = this.mailDomain.TMailDataTable;
        }
        public async Task StartAsync()
        {
            this.mailDomain = await GateServerManager.MailBox.GetDomainAsync(mailDomainName, mailAccount);
        }
        public override string ToString()
        {
            return key;
        }
        protected override void Disposing()
        {
            domainChannel.Dispose();
        }
        protected override ValueTask DisposingAsync()
        {
            return domainChannel.DisposeAsync();
        }
        //-------------------------------------------------------------------------------------------------

        public virtual string GetDomainAddress(string acc)
        {
            return $"MailBox:{mailDomain}:{acc}";
        }
        //-------------------------------------------------------------------------------------------------
        /// <summary>
        /// 查询未读邮件数量
        /// </summary>
        /// <returns></returns>
        public virtual async Task<long> QueryUnreadAsync()
        {
            var result = await domainUnread.GetAsync(mailAccount);
            var count = ORMFactory.Instance.DecodeObject<int>(result);
            return count;
        }
        /// <summary>
        /// 查询总共邮件数量
        /// </summary>
        /// <returns></returns>
        public virtual async Task<long> QueryTotalAsync()
        {
            var result = await domainTotal.GetAsync(mailAccount);
            var count = ORMFactory.Instance.DecodeObject<int>(result);
            return count;
        }
        /// <summary>
        /// 获取邮件快照
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public virtual async Task<Data.MailSnap> QueryMailSnapAsync(string uuid)
        {
            return await ownerCache.GetAsync<MailSnap>(uuid);
        }
        /// <summary>
        /// 获取邮件快照
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public virtual async Task<Data.MailSnap[]> QueryMailSnapAsync(string[] uuid)
        {
            return await ownerCache.GetAsync<MailSnap>(uuid);
        }
        /// <summary>
        /// 获取所有邮件快照
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Data.MailSnap[]> QueryMailSnapsAsync()
        {
            var snaps = await ownerCache.GetAllAsync<Data.MailSnap>();
            return Array.ConvertAll(snaps, static testc => testc.FieldValue);
        }
        /// <summary>
        /// 删除快照，不直接删除邮件
        /// </summary>
        /// <param name="uuids"></param>
        /// <returns></returns>
        public virtual async Task<int> DeleteMailAsync(params string[] uuids)
        {
            var count = await ownerCache.DeleteAsync(uuids);
            if (count > 0)
            {
                await domainTotal.DecrementAsync(MailAccount, count);
                return (int)count;
            }
            return 0;
        }
        //-------------------------------------------------------------------------------------------------
        public virtual async Task<MailData> QueryMailAsync(string uuid)
        {
            var snap = await ownerCache.GetAsync(uuid);
            if (snap != null)
            {
                return await SQLSelectAsync(uuid);
            }
            return null;
        }
        public virtual async Task<MailData> ReadMailAsync(string uuid, Func<Data.MailData, Task<bool>> func)
        {
            var snap = await QueryMailSnapAsync(uuid);
            if (snap == null) return null;
            if (snap.readed == false)
            {
                // 先处理缓存
                // TODO 可优化为单次事务
                await domainUnread.DecrementAsync(MailAccount);
                snap.readed = true;
                await ownerCache.SetAsync(uuid, snap);
            }
            return await SQLReadMailAsync(uuid, func);
        }
        public virtual async Task<MailData> OpenMailContentAsync(string uuid, Func<MailData, object, Task<bool>> func)
        {
            var snap = await QueryMailSnapAsync(uuid);
            if (snap == null) return null;
            if (snap.opened == false)
            {
                // 先处理缓存
                // TODO 可优化为单次事务
                snap.opened = true;
                await ownerCache.SetAsync(uuid, snap);
                return await SQLOpenMailAsync(uuid, func);
            }
            return null;
        }
        //-------------------------------------------------------------------------------------------------
        public virtual async Task<string> PostMailAsync(MailData mail)
        {
            mail.uuid = Guid.NewGuid().ToString();
            mail.from = mailAccount;
            mail.readed = false;
            mail.opened = false;
            mail.utcTime = DateTime.UtcNow;
            await SQLInsertAsync(mail);
            await domainUnread.IncrementAsync(mail.to);
            await domainTotal.IncrementAsync(mail.to);
            using (var targetCache = db.GetHash(GetDomainAddress(mail.to), service))
            {
                await targetCache.SetAsync(mail.uuid, mail.ToSnap());
            }
            await domainChannel.PublishAsync(mail.to, mail.uuid);
            return mail.uuid;
        }
        public virtual async Task<int> BroadcastMailAsync(MailData mail)
        {
            mail.from = mailAccount;
            mail.readed = false;
            mail.opened = false;
            mail.utcTime = DateTime.UtcNow;
            return await service.Execute(mysql.RunConnectionAsync(async conn =>
            {
                var count = 0;
                var it = Domain.allusers.ScanAsync(mail.to);
                await foreach (var user in it)
                {
                    mail.uuid = Guid.NewGuid().ToString();
                    mail.to = user.FieldName;
                    await SQLInsertAsync(mail);
                    await domainUnread.IncrementAsync(mail.to);
                    await domainTotal.IncrementAsync(mail.to);
                    using (var targetCache = db.GetHash(GetDomainAddress(mail.to), service))
                    {
                        await targetCache.SetAsync(mail.uuid, mail.ToSnap());
                    }
                    await domainChannel.PublishAsync(mail.to, mail.uuid);
                }
                return count;
            }));
        }
        //-------------------------------------------------------------------------------------------------


        public virtual async Task ListenAsync(IncomingMailAsync handler)
        {
            await domainChannel.SubscribeAsync<string>(mailAccount, async (account, uuid) =>
            {
                var mail = await QueryMailAsync(uuid);
                if (mail != null) await handler.Invoke(this, mail);
            });
        }

        //-------------------------------------------------------------------------------------------------
        #region SQL
        public async Task<MailData> SQLSelectAsync(string uuid)
        {
            return await service.Execute(mysql.RunConnectionAsync(conn =>
            {
                return sqlTable.SelectAsync<MailData>(conn, uuid);
            }));
        }

        public async Task SQLInsertAsync(MailData mail)
        {
            await service.Execute(mysql.RunConnectionAsync(conn =>
            {
                return sqlTable.InsertAsync(conn, mail);
            }));
        }

        protected virtual async Task<MailData> SQLReadMailAsync(string uuid, Func<MailData, Task<bool>> func)
        {
            var ret = await service.Execute(mysql.RunConnectionAsync(async conn =>
            {
                var maildata = await sqlTable.SelectForUpdateAsync(conn, async mail =>
                {
                    if (await func(mail))
                    {
                        if (mail.readed == false)
                        {
                            await domainUnread.DecrementAsync(MailAccount);
                        }
                        mail.readed = true;
                        return mail;
                    }
                    return null;
                }, uuid);
                return maildata;
            }));
            return ret;
        }

        protected virtual async Task<MailData> SQLOpenMailAsync(string uuid, Func<MailData, object, Task<bool>> func)
        {
            var ret = await service.Execute(mysql.RunConnectionAsync(async conn =>
            {
                var maildata = await sqlTable.SelectForUpdateAsync(conn, async mail =>
                {
                    if (mail.opened == false)
                    {
                        if (await func(mail, mail.content))
                        {
                            mail.opened = true;
                            //mail.content = null;
                            return mail;
                        }
                    }
                    return null;
                }, uuid);
                return maildata;
            }));
            return ret;
        }

        /// <summary>
        /// 不带缓存的查询所有邮件UUID
        /// </summary>
        /// <returns></returns>
        public virtual Task<string[]> SQLQueryTotalKeysAsync()
        {
            return service.Execute(mysql.RunConnectionAsync(async conn =>
            {
                var entries = await sqlTable.SelectFieldsAsync(conn,
                    fields: new string[] { nameof(MailData.uuid) },
                    where: new Where(nameof(MailData.to), mailAccount));
                return Array.ConvertAll(entries, row => row[0].FieldValue.ToString());
            }));
        }
        /// <summary>
        /// 不带缓存的查询所有邮件
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public virtual Task<MailData[]> SQLQueryMailsAsync(int limit, int offset)
        {
            return service.Execute(mysql.RunConnectionAsync(conn =>
            {
                return sqlTable.SelectRowsAsync<MailData>(conn, limit, offset, where: new Where(nameof(MailData.to), mailAccount));
            }));
        }
        /// <summary>
        /// 巨耗操作
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public virtual async Task SQLForEachMailsAsync(IncomingMailAsync handler, int step = 10)
        {
            var keys = await SQLQueryTotalKeysAsync();
            foreach (var uuid in keys)
            {
                var mail = await QueryMailAsync(uuid);
                if (mail != null) await handler(this, mail);
            }
        }
        #endregion
        //-------------------------------------------------------------------------------------------------
    }
}
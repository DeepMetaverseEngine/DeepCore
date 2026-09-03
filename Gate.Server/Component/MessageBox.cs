using DeepCore;
using DeepCore.ORM;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using Gate.Data;
using System;
using System.Threading.Tasks;

namespace Gate.Server.Component
{
    public delegate Task IncomingMailAsync<T>(string uuid, T mail);
    public delegate Task IncomingMessageAsync(string uuid, object mail);

    public class MessageBox : Disposable
    {
        public IService Service { get => service; }
        protected readonly IService service;
        protected readonly string mailAccount;
        protected readonly string mailDomain;
        protected readonly IChannel domainChannel;
        protected readonly IMappingHash mailBox;
        //protected readonly IMappingHash mailHeader;
        //protected readonly MailBoxMapping mailBox;
        public MessageBox(IService service, string mailDomain, string mailAccount, IMappingAdapter db = null)
        {
            if (db == null) { db = ORMFactory.Instance.DefaultAdapter; }
            this.service = service;
            this.mailDomain = mailDomain;
            this.mailAccount = mailAccount;
            this.domainChannel = ORMFactory.Instance.GetChannel(mailDomain, service);
            //this.domainChannel.Subscribe<string>(mailAccount, HandleIncomingMail);
            //this.mailBox = new MailBoxMapping($"MailBox:{mailDomain}:{mailAccount}", service, db);
            this.mailBox = db.GetHash($"{mailDomain}:{mailAccount}", service);
            this.mailBox = db.GetHash($"{mailDomain}:{mailAccount}:header", service);
        }
        public override string ToString()
        {
            return mailBox.Key;
        }
        protected override void Disposing()
        {
            domainChannel.Dispose();
            mailBox.Dispose();
        }
        //private void HandleIncomingMail(string account, string uuid) { }

        //-------------------------------------------------------------------------------------------------
        public async Task<long> QueryLengthAsync()
        {
            return await mailBox.LengthAsync();
        }
        public async Task<string[]> QueryMessageKeysAsync()
        {
            return await mailBox.KeysAsync();
        }
        public async Task<HashQueryEntry[]> QueryMessageBoxAsync()
        {
            return await mailBox.GetAllAsync();
        }
        public async Task<object> QueryMessageAsync(string uuid)
        {
            return await mailBox.GetAsync<object>(uuid);
        }
        public async Task<HashQueryEntry<T>[]> QueryMessageBoxAsAsync<T>()
        {
            return await mailBox.GetAllAsync<T>();
        }
        public async Task<T> QueryMessageAsAsync<T>(string uuid)
        {
            return await mailBox.GetAsync<T>(uuid);
        }
        public async Task<bool> DeleteMessageAsync(string uuid)
        {
            return await mailBox.DeleteAsync(uuid);
        }
        //-------------------------------------------------------------------------------------------------
        public async Task PostMessageAsync(string to, object mail)
        {
            var uuid = Guid.NewGuid().ToString();
            using (var pushMail = mailBox.Adapter.GetHash($"{mailDomain}:{to}", this.Service))
            { 
                await pushMail.SetAsync(uuid, mail);
            }
            await domainChannel.PublishAsync(to, uuid);
        }
        //-------------------------------------------------------------------------------------------------
        public async Task ForEachMessagesAsync<T>(IncomingMessageAsync handler)
        {
            foreach (var uuid in await this.QueryMessageKeysAsync())
            {
                var mail = await this.QueryMessageAsync(uuid);
                await handler(uuid, mail);
            }
        }
        public async Task ForEachMessagesAsync<T>(IncomingMailAsync<T> handler)
        {
            foreach (var uuid in await this.QueryMessageKeysAsync())
            {
                var mail = await this.QueryMessageAsAsync<T>(uuid);
                await handler(uuid, mail);
            }
        }
       
        public virtual async Task ListenAsync(IncomingMessageAsync handler)
        {
            await domainChannel.SubscribeAsync<string>(mailAccount, async (account, uuid) =>
            {
                var mail = await QueryMessageAsync(uuid);
                await handler.Invoke(uuid, mail);
            });
        }
        public virtual async Task ListenAsync<T>(IncomingMailAsync<T> handler)
        {
            await domainChannel.SubscribeAsync<string>(mailAccount, async (account, uuid) =>
            {
                var mail = await QueryMessageAsync(uuid);
                if (mail is T tmail)
                {
                    await handler.Invoke(uuid, tmail);
                }
            });
        }



        //-------------------------------------------------------------------------------------------------
    }
}


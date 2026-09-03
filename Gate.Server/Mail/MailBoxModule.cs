using DeepCrystal.ORM;
using DeepCrystal.RPC;
using DeepFrozen.MySQL;
using Gate.Data;
using Gate.Data.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gate.Server.Mail
{
    public class MailBoxModule : IServiceModule
    {
        protected readonly MailBox mailBox;
        public MailBox MailBox { get => mailBox; }
        public MailBoxModule(IService service, string mailDomain, string mailAccount) : base(service)
        {
            this.mailBox = CreateMailBox(service, mailDomain, mailAccount);
        }
        protected virtual MailBox CreateMailBox(IService service, string mailDomain, string mailAccount) => new MailBox(service, mailDomain, mailAccount);
        public override async Task OnStartedAsync()
        {
            await mailBox.StartAsync();
            await mailBox.ListenAsync(new IncomingMailAsync(do_IncomintMailAsync));
            await base.OnStartedAsync();
        }
        public override async Task OnStopAsync()
        {
            await mailBox.DisposeAsync();
            await base.OnStopAsync();
        }
        protected override void Disposing()
        {
            event_OnIncomintMailAsync = null;
            event_OnReadMailAsync = null;
            event_OnOpenMailContentAsync = null;
            base.Disposing();
        }
        //------------------------------------------------------------------------------------------------------
        public delegate Task OnIncomintMail(MailBoxModule sender, MailData mail);
        public delegate Task<bool> OnReadMail(MailBoxModule sender, MailData mail);
        public delegate Task<bool> OnOpenMailContent(MailBoxModule sender, MailData mail, object content);
        public event OnIncomintMail OnIncomintMailAsync { add { event_OnIncomintMailAsync += value; } remove { event_OnIncomintMailAsync -= value; } }
        public event OnReadMail OnReadMailAsync { add { event_OnReadMailAsync += value; } remove { event_OnReadMailAsync -= value; } }
        public event OnOpenMailContent OnOpenMailContentAsync { add { event_OnOpenMailContentAsync += value; } remove { event_OnOpenMailContentAsync -= value; } }
        private OnIncomintMail event_OnIncomintMailAsync;
        private OnReadMail event_OnReadMailAsync;
        private OnOpenMailContent event_OnOpenMailContentAsync;
        //------------------------------------------------------------------------------------------------------
        private Task do_IncomintMailAsync(MailBox sender, MailData mail)
        {
            log.Info($"IncomintMailAsync: {mail}");
            return event_OnIncomintMailAsync?.Invoke(this, mail);
        }
        private async Task<bool> do_ReadMailAsync(MailData mail)
        {
            log.Info($"ReadMailAsync: {mail}");
            if (event_OnReadMailAsync != null)
            {
                foreach (OnReadMail e in event_OnReadMailAsync.GetInvocationList())
                {
                    if (await e.Invoke(this, mail))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private async Task<bool> do_OpenMailContentAsync(MailData mail, object content)
        {
            log.Info($"OpenMailContentAsync: {mail}");
            if (event_OnOpenMailContentAsync != null)
            {
                foreach (OnOpenMailContent e in event_OnOpenMailContentAsync.GetInvocationList())
                {
                    if (await e.Invoke(this, mail, content))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        //------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(QueryTotalMailsRequest), typeof(QueryTotalMailsResponse))]
        public virtual async Task<QueryTotalMailsResponse> rpc(QueryTotalMailsRequest req)
        {
            return new QueryTotalMailsResponse()
            {
                total = await mailBox.QueryTotalAsync(),
                unread_total = await mailBox.QueryUnreadAsync(),
            };
        }

        [RpcHandler(typeof(QueryMailRequest), typeof(QueryMailResponse))]
        public virtual async Task<QueryMailResponse> rpc(QueryMailRequest req)
        {
            var mail = await mailBox.QueryMailAsync(req.uuid);
            return new QueryMailResponse() { mail = mail, };
        }

        [RpcHandler(typeof(QueryMailsRequest), typeof(QueryMailsResponse))]
        public virtual async Task<QueryMailsResponse> rpc(QueryMailsRequest req)
        {
            var mails = await mailBox.QueryMailSnapsAsync();
            return new QueryMailsResponse() { mails = mails, };
        }

        [RpcHandler(typeof(DeleteMailRequest), typeof(DeleteMailResponse))]
        public virtual async Task<DeleteMailResponse> rpc(DeleteMailRequest req)
        {
            var count = await mailBox.DeleteMailAsync(req.uuid);
            return new DeleteMailResponse() { count = count, };
        }

        [RpcHandler(typeof(ReadMailRequest), typeof(ReadMailResponse))]
        public virtual async Task<ReadMailResponse> rpc(ReadMailRequest req)
        {
            var mail = await mailBox.ReadMailAsync(req.uuid, do_ReadMailAsync);
            return new ReadMailResponse() { mail = mail, };
        }

        [RpcHandler(typeof(OpenMailContentRequest), typeof(OpenMailContentResponse))]
        public virtual async Task<OpenMailContentResponse> rpc(OpenMailContentRequest req)
        {
            var mail = await mailBox.OpenMailContentAsync(req.uuid, do_OpenMailContentAsync);
            return new OpenMailContentResponse() { mail = mail, };
        }

        [RpcHandler(typeof(SendMailRequest), typeof(SendMailResponse))]
        public virtual async Task<SendMailResponse> rpc(SendMailRequest req)
        {
            var uuid = await mailBox.PostMailAsync(req.mail);
            return new SendMailResponse() { uuid = uuid, };
        }

        //------------------------------------------------------------------------------------------------------
    }
}

using DeepCore.Meta.Channel.Slave;
using DeepCore.PomeloClient;
using DeepCore.Protocol;
using Gate.Client.Channel.World;
using Gate.Data.Protocol;
using System;
using System.Threading.Tasks;

namespace Gate.Client.Modules
{
    public class ChannelModule : GateClientModule<GateClient>, IClientAdapter
    {
        public WorldLayout Layout => layout;
        private WorldLayout layout;
        public ChannelModule(GateClient client) : base(client)
        {
            this.layout = GateClientManager.Instance.Channel.CreateWorldLayout(this);
        }

        //-------------------------------------------------------------------------------------------------------------
        protected override void OnDisposing()
        {
            layout?.Dispose();
        }
        internal protected override void OnEnterGame(ClientEnterGameResponse enter)
        {
        }
        internal protected override void OnGameClientDisconnected(CloseReason reason)
        {
        }
        internal protected override void BeginUpdate(float intervalMS)
        {
            base.BeginUpdate(intervalMS);
        }
        internal protected override void Update(float intervalMS)
        {
            layout?.MainUpdate(intervalMS);
            base.Update(intervalMS);
        }
        //-------------------------------------------------------------------------------------------------------------

        void IClientAdapter.Listen<T>(Action<T> action, bool recursion_base_type)
        {
            Client.game_session.Listen(action, recursion_base_type);
        }
        void IClientAdapter.Send(Notify msg)
        {
            Client.game_session.Notify(msg);
        }
        void IClientAdapter.SendRequest(Request req, Action<Response> callback)
        {
            Client.game_session.Request(req, (err, rsp) =>
                {
                    callback(rsp as Response);
                });
        }
        Task<Response> IClientAdapter.SendRequestAsync(Request req)
        {
            var tcs = new TaskCompletionSource<Response>();
            Client.game_session.Request(req, (err, q) => tcs.SetResult(q as Response));
            return tcs.Task;
        }
        void IClientAdapter.SendRequest<R, Q>(R req, Action<Q> response)
        {
            Client.game_session.Request(req, (err, q) => response(q as Q));
        }
        Task<Q> IClientAdapter.SendRequestAsync<R, Q>(R req)
        {
            var tcs = new TaskCompletionSource<Q>();
            Client.game_session.Request(req, (err, q) => tcs.SetResult(q as Q));
            return tcs.Task;
        }

        //-------------------------------------------------------------------------------------------------------------


        //-------------------------------------------------------------------------------------------------------------
    }
}

using DeepCore.IO;
using DeepCore.Protocol;
using System;
using System.Threading.Tasks;

namespace DeepCore.Meta.Channel.Slave
{
    public interface IClientAdapter
    {
        void Listen<T>(Action<T> action, bool recursion_base_type = true) where T : ISerializable;
        void Send(Notify msg);
        void SendRequest(Request req, Action<Response> callback);
        Task<Response> SendRequestAsync(Request req);
        void SendRequest<R, Q>(R req, Action<Q> response) where R : Request where Q : Response;
        Task<Q> SendRequestAsync<R, Q>(R req) where R : Request where Q : Response;
    }
}

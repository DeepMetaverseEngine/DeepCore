using Cysharp.Threading.Tasks;
using DeepCore.IO;
using DeepCore.NetClient;

namespace DeepCore.PomeloClient
{
    public static class UnityPomeloConnector
    {
        public static UniTask<BinaryMessage> RequestBinaryUniAsync(this INetClient conn, BinaryMessage data)
        {
            var tcs = new UniTaskCompletionSource<BinaryMessage>();
            conn.RequestBinary(data, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            });
            return tcs.Task;
        }
        public static UniTask<ISerializable> RequestUniAsync(this INetClient conn, ISerializable req, object state = null)
        {
            var tcs = new UniTaskCompletionSource<ISerializable>();
            conn.Request(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, state);
            return tcs.Task;
        }
        public static UniTask<RSP> RequestUniAsync<RSP>(this INetClient conn, ISerializable req, object state = null) where RSP : ISerializable
        {
            var tcs = new UniTaskCompletionSource<RSP>();
            conn.Request<RSP>(req, (err, rsp) =>
            {
                if (err != null) { tcs.TrySetException(err); }
                else { tcs.TrySetResult(rsp); }
            }, state);
            return tcs.Task;
        }
    }
}

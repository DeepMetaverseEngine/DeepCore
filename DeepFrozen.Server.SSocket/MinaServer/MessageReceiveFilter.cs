using DeepCore.IO;
using DeepCore.MinaClient;
using DeepCore.Protocol;
using SuperSocket.Facility.Protocol;
using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Protocol;
using System;

namespace DeepFrozen.Server.SSocket.NetServer
{
    public class MessageReceiveFilterFactory : IReceiveFilterFactory<BinaryRequestInfo>
    {
        public MessageReceiveFilterFactory(INetPackageCodec codec)
        {

        }
        public IReceiveFilter<BinaryRequestInfo> CreateFilter(IAppServer appServer, IAppSession appSession, System.Net.IPEndPoint remoteEndPoint)
        {
            return new MessageReceiveFilter(appServer, appSession as SSMniaSession);
        }
    }

    public class MessageReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
    {
        public MessageReceiveFilter(IAppServer appServer, SSMniaSession appSession)
            : base(4)
        {

        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            int pos = offset;
            int bodyLength = LittleEdian.GetS32(header, ref pos);
            return bodyLength;
        }

        protected override BinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {
            byte[] bin = new byte[length];
            Buffer.BlockCopy(bodyBuffer, offset, bin, 0, length);
            return new BinaryRequestInfo("BinCommand", bin);
        }

    }
}

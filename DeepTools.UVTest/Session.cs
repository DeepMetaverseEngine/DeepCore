using DeepCore;
using DeepCore.Pomelo;
using DeepCore.PomeloClient;
using DeepCore.IO;
using DeepCore.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DeepTools.UVTest
{
    public class Session
    {
        const string pem = """
-----BEGIN RSA PRIVATE KEY-----
MIICWQIBAAKBgF+tpKJ2rkVmhL6KBP89Frc5zRA/l+teCPifjt5VutNlwiogM33L
ToeybWXPUUnenVHbpJpEzKdQGxenw2XXDGB17wT6N7L+a03hbm7HOHoHFcKAFlP5
/6NAheYAK+q6z1NvmE3oVhh9mC5g94rRpIv2Fmm22Dp1qOA7wAEPc/gTAgElAoGA
Ur+w+zY12yg7dFuxS3MakKCxWikpcZ1viurcbUM5y5ZHD60lnTo2FH6jzaxh+rKx
kud5mi2qFCma5Aa21J5P46csOZqIVcs+GaOPOVNR3/T7WV8fF9zmRd7PvxSzoIqe
r5f++pQXLIMFRg5xzm/XA8QHflYQ8MeZ5UqQvEdXda0CQQC2OndZ4sBjeV1pTaI8
pSnBNgzbijPIcEhtGyqNaUGfBEhH+gPSWwITec/MkDmu+ezOxCcDu57P0BBHn4jB
IuspAkEAhmlrBadPZ/Ii0wMyQjylUp0WTpAsmiUaCcqGgtL3e4OQH994WsBK8qBF
k7O6cVoCzKmYn5kFPw7e1ekem9Ps2wJBAInnDjYvDiHDoKK3SldoO0YbEKYjZXUW
s1l8V42bwvTgpWbmtsizkt5OWBBtJLvEDSbZoPvn7c2yNdVWIkz31I0CQEinr3it
dwfBJ5SnyCPNwSW8shyg9YPBByfjGEa3OaqMTee3EJ/B41K3f44b9hPB86zmDU9Z
ofiSap0kEIuVJhUCQFv6UNaAJjIe8oBgJo1Q4mk782tpiUi0VkmQEUPBZabESLFP
g2/kBj4M7pQTYbEHs4xjR9jn+3QG/s7NuKHrFLA=
-----END RSA PRIVATE KEY-----
""";

        private TcpClient tcp;
        private RSACryptoServiceProvider rsa;
        public Session(string host, int port)
        {
            var addrs = IPUtil.GetIPAddress(host, port, out var family, out var ips);
            this.rsa = new RSACryptoServiceProvider(1024);
            this.rsa.ImportFromPem(pem);
            this.tcp = new TcpClient(family);
            this.tcp.Connect(addrs, port);
            var send_object = new SendMessage();
            int len = send_object.InitWithSystemMessage(new SystemHandshake()
            {
                local_info = Convert.ToBase64String(GZipCompress.Compress(CUtils.EncodeUTF8(pem)))
            });
            tcp.GetStream().Write(send_object.Buffer, 0, len);
        }

        public string SendPost(string fileName, byte[] bytes)
        {
            return Send($"post {fileName}", CUtils.BinToHex(GZipCompress.Compress(bytes)));
        }
        public string SendPost(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);
            return Send($"post {fileName}", CUtils.BinToHex(GZipCompress.Compress(bytes)));
        }
        public string SendCall(string cmd, string input)
        {
            return Send($"call {cmd}", input);
        }
        public string SendCall(string cmd)
        {
            var args = CUtils.StringSplitWhiteSpace(cmd, 2);
            if (args.Length == 2)
            {
                return Send($"call {args[0]}", args[1]);
            }
            else
            {
                return Send($"call {cmd}", "");
            }
        }
        public string SendCMDCall(string input)
        {
            return Send($"call cmd", $"\r\n{input}\r\nexit");
        }
        public string SendCode(string code)
        {
            return Send($"CSharp", code);
        }

        private void SendPem()
        {
        }
        private string Send(string provider, string input)
        {
            /*
               if (rsa == null)
               {
                   rsa = new RSACryptoServiceProvider(1024);
                   rsa.ImportFromPem(CUtils.DecodeUTF8(GZipCompress.Decompress(Convert.FromBase64String(handshake.local_info))));
                   return;
               }
               else
               {
                   handshake.local_info = CUtils.DecodeUTF8(rsa.Decrypt(GZipCompress.Decompress(Convert.FromBase64String(handshake.local_info)), false));
               }
             */
            {
                var msg = $"{CUtils.ToBase64(provider)} {CUtils.ToBase64(input)}";
                var send_object = new SendMessage();
                int len = send_object.InitWithSystemMessage(new SystemHandshake()
                {
                    local_info = Convert.ToBase64String(GZipCompress.Compress(rsa.Encrypt(CUtils.EncodeUTF8(msg), false)))
                });
                tcp.GetStream().Write(send_object.Buffer, 0, len);
            }
            {
                var recv_object = new RecvMessage();
                IOUtil.ReadToEnd(tcp.GetStream(), recv_object.Buffer, 0, IProtocol.FIXED_HEAD_SIZE);
                recv_object.ReadHead();
                if (recv_object.PkgLength > 0)
                {
                    IOUtil.ReadToEnd(tcp.GetStream(), recv_object.Buffer, IProtocol.FIXED_HEAD_SIZE, recv_object.PkgLength);
                    switch (recv_object.PkgType)
                    {
                        case PackageType.PKG_HANDSHAKE_ACK:
                            if (recv_object.ReadBodySystemMessage() is SystemHandshakeAck ack)
                            {
                                return CUtils.FromBase64(ack.remote_info);
                            }
                            break;
                        case PackageType.PKG_HEARTBEAT:
                            if (recv_object.ReadBodySystemMessage() is SystemHeartbeat beat)
                            {
                            }
                            break;
                        case PackageType.PKG_KICK:
                            if (recv_object.ReadBodySystemMessage() is SystemKick kick)
                            {
                            }
                            break;
                        case PackageType.PKG_MESSAGE:
                            {
                                //recv_object.BeginBody();
                            }
                            break;
                    }
                }
            }
            return null;
        }
        class SendMessage : ISendMessage
        {
            internal SendMessage() : base(null)
            {
                this.BufferLength = SendMessage.FIXED_HEAD_SIZE;
                this.BufferPosition = 0;
            }
        }
        class RecvMessage : IRecvMessage
        {
            internal RecvMessage() : base(null)
            {
                this.BufferLength = RecvMessage.FIXED_HEAD_SIZE;
                this.BufferPosition = 0;
            }
        }
    }
}

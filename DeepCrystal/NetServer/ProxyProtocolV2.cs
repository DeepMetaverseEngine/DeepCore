using DeepCrystal.NetServer;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DeepCore.Net
{

    public class ProxyProtocolV2
    {
        // 12字节固定魔数
        private static readonly byte[] MagicSignature = new byte[]
        {
        0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A
        };
        public static int MagicSignatureLength => MagicSignature.Length;

        public byte Version { get; private set; }
        public byte Command { get; private set; }
        public byte AddressFamily { get; private set; }
        public byte TransportProtocol { get; private set; }
        public int PayloadLength { get; private set; }

        // 解析出的真实客户端/目的端网络信息
        public IPAddress SourceAddress { get; private set; }
        public IPAddress DestinationAddress { get; private set; }
        public ushort SourcePort { get; private set; }
        public ushort DestinationPort { get; private set; }

        /// <summary>
        /// 检查输入的字节流开头是否包含 Proxy Protocol v2 的魔数签名
        /// </summary>
        public static bool IsProxyProtocolV2(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length < MagicSignature.Length) return false;
            if (buffer.Slice(0, MagicSignature.Length).SequenceEqual(MagicSignature))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 检查Buffer是否可能以MagicSignature开头
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool IsStartWithProxyProtocolV2(ReadOnlySpan<byte> buffer)
        {
            for (int i = 0; i < MagicSignature.Length && i < buffer.Length; i++)
            {
                if (buffer[i] != MagicSignature[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 解析二进制数据，并返回该协议头部在 TCP 流中总共占用的字节数（以便后续剥离）
        /// </summary>
        /// <param name="buffer">收到的 TCP 原始数据缓存</param>
        /// <param name="result">输出解析后的结构体</param>
        /// <param name="headerLength">输出协议头总长度（固定16字节 + 地址块 + TLV）</param>
        /// <returns>是否解析成功</returns>
        public static bool TryParse(ReadOnlySpan<byte> buffer, out ProxyProtocolV2 result, out int headerLength)
        {
            result = null;
            headerLength = 0;

            // 1. 基础长度检查（至少需要 16 字节固定头部）
            if (buffer.Length < 16 || !IsProxyProtocolV2(buffer)) return false;

            // 2. 解析第 13 字节：版本与命令
            byte verCmd = buffer[12];
            byte version = (byte)((verCmd >> 4) & 0x0F);
            byte command = (byte)(verCmd & 0x0F);

            if (version != 2) return false; // 必须是 v2 版本

            // 3. 解析第 14 字节：地址族与协议
            byte famProto = buffer[13];
            byte addressFamily = (byte)((famProto >> 4) & 0x0F);
            byte transportProtocol = (byte)(famProto & 0x0F);

            // 4. 解析第 15-16 字节：后续负载长度（网络字节序/大端序）
            ushort payloadLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(14, 2));

            // 确保缓冲区完整包含了声明的负载长度
            if (buffer.Length < 16 + payloadLen) return false;

            headerLength = 16 + payloadLen; // 协议头总长度

            var instance = new ProxyProtocolV2
            {
                Version = version,
                Command = command,
                AddressFamily = addressFamily,
                TransportProtocol = transportProtocol,
                PayloadLength = payloadLen
            };

            // 如果命令是 LOCAL (0x00)，代表是代理的自发心跳流量，不携带地址信息
            if (command == 0x00)
            {
                result = instance;
                return true;
            }

            // 5. 根据地址族解析 IP 和端口
            int currentOffset = 16;
            if (addressFamily == 0x01) // IPv4
            {
                if (payloadLen < 12) return false; // IPv4 至少需要 12 字节

                instance.SourceAddress = new IPAddress(buffer.Slice(currentOffset, 4).ToArray());
                instance.DestinationAddress = new IPAddress(buffer.Slice(currentOffset + 4, 4).ToArray());

                instance.SourcePort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(currentOffset + 8, 2));
                instance.DestinationPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(currentOffset + 10, 2));
            }
            else if (addressFamily == 0x02) // IPv6
            {
                if (payloadLen < 36) return false; // IPv6 至少需要 36 字节

                instance.SourceAddress = new IPAddress(buffer.Slice(currentOffset, 16).ToArray());
                instance.DestinationAddress = new IPAddress(buffer.Slice(currentOffset + 16, 16).ToArray());

                instance.SourcePort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(currentOffset + 32, 2));
                instance.DestinationPort = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(currentOffset + 34, 2));
            }

            // 提示：此处若 payloadLen 还有剩余字节，即为 TLV 附加数据，AWS 会在此放置元数据，可根据需要继续解析

            result = instance;
            return true;
        }



        /// <summary>
        /// 从当前解析出的 Proxy Protocol v2 数据中获取远端客户端的真实 IPEndPoint。
        /// 如果报文属于 LOCAL 命令（无地址信息），则支持安全降级返回底层物理套接字的端点。
        /// </summary>
        /// <param name="fallbackSocket">可选：底层的 Socket。当遇到不携带 IP 的 PROXY LOCAL 流量时，作为备用降级返回。</param>
        /// <returns>真实客户端的 IPEndPoint，如果无法获取则返回 null 或降级端点</returns>
        public EndPoint GetRemoteEndPoint(EndPoint fallbackSocket = null)
        {
            // 1. 如果命令是 PROXY (0x01)，代表成功携带了远端真实客户端的 IP 和端口
            if (Command == 0x01)
            {
                if (SourceAddress != null)
                {
                    return new IPEndPoint(SourceAddress, SourcePort);
                }
            }

            // 2. 如果命令是 LOCAL (0x00)，代表这是代理服务器（如 AWS 负载均衡器）自身的探活/心跳流量，不含客户端地址
            // 此时如果传入了底层的物理 socket，则安全的降级返回代理服务器本身的物理 IP
            if (fallbackSocket != null)
            {
                return fallbackSocket;
            }

            return null;
        }

    }


    // 服务器开启ProxyProtocolV2
    public class ProxyProtocolV2Filter : ISessionDataFilter
    {
        private ProxyProtocolV2 proxyProtocolV2;
        private MemoryStream proxyProtocolBuffer;

        public ArraySegment<byte> Receiving(ISession session, ref EndPoint endpoint, in ArraySegment<byte> buffer)
        {
            if (proxyProtocolV2 == null)
            {
                // 尝试解析ProxyProtocolV2头
                proxyProtocolBuffer = proxyProtocolBuffer ?? new MemoryStream();
                proxyProtocolBuffer.Write(buffer);
                var proxyBuffer = new ArraySegment<byte>(proxyProtocolBuffer.GetBuffer(), 0, (int)proxyProtocolBuffer.Length);
                if (ProxyProtocolV2.IsStartWithProxyProtocolV2(proxyBuffer))
                {
                    if (ProxyProtocolV2.TryParse(proxyBuffer, out proxyProtocolV2, out var headerLength))
                    {
                        try
                        {
                            // TODO endpoint replace
                            endpoint = proxyProtocolV2.GetRemoteEndPoint(endpoint);
                            if (proxyBuffer.Count > headerLength)
                            {
                                // 剩余数据，去解析游戏协议
                                return proxyBuffer.Slice(headerLength);
                            }
                            else
                            {
                                // 解析完了，继续等待数据
                                return ArraySegment<byte>.Empty;
                            }
                        }
                        finally
                        {
                            proxyProtocolBuffer = null;
                        }
                    }
                    else
                    {
                        // 数据不完整，继续等待数据
                        return ArraySegment<byte>.Empty;
                    }
                }
                else
                {
                    try
                    {
                        // 如果首协议不是ProxyProtocolV2头，说明是直连的
                        // 造一个假的头
                        proxyProtocolV2 = new ProxyProtocolV2();
                        // 剩余数据，去解析游戏协议
                        return proxyBuffer;
                    }
                    finally
                    {
                        proxyProtocolBuffer = null;
                    }
                }
            }
            else
            {
                return buffer;
            }
        }
    }

}

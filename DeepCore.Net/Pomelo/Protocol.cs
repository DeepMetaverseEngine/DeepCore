using DeepCore.IO;
using DeepCore.NetClient;
using System;
using System.IO;

namespace DeepCore.Pomelo
{
    public enum PackageType : byte
    {
        PKG_HANDSHAKE = 1,
        PKG_HANDSHAKE_ACK = 2,
        PKG_HEARTBEAT = 3,
        PKG_MESSAGE = 4,
        PKG_KICK = 5
    }
    public enum PackageMask : byte
    {
        Compressed = 1,
        Dummy1 = 2,
        Dummy2 = 4,
        Dummy3 = 8,
    }
    //--------------------------------------------------------------------------------------------------------
    public enum MessageType : byte
    {
        NA = 0,
        MSG_NOTIFY = 1,
        MSG_REQUEST_C2S = 2,
        MSG_RESPONSE_S2C = 3,
        MSG_RPC_REQUEST_S2C = 4,
        MSG_RPC_RESPONSE_C2S = 5,
    }

    //--------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Head:[1][3]
    /// Body:
    /// </summary>
    public abstract class IProtocol : IClientProtocol
    {
        public const int FIXED_HEAD_SIZE = 4;
        //--------------------------------------------------------------------------------------------------------------------

        /// <summary>
        //  Head: 0.5 Byte 4(bit) Package Type
        /// </summary>
        public PackageType PkgType { get; protected set; }
        /// <summary>
        //  Head: 0.5 Byte 4(bit) Package Mask
        /// </summary>
        public PackageMask PkgMask { get; protected set; }
        /// <summary>
        /// Head: 3 Byte Body Length [With no head size]
        /// </summary>
        public int PkgLength { get; protected set; }
        //--------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 1 Byte [可选]
        /// </summary>
        public MessageType MsgType { get; protected set; }
        /// <summary>
        /// 4 Byte [可选]
        /// </summary>
        public uint MsgSendID { get; protected set; }
        /// <summary>
        /// 4 Byte [可选]
        /// </summary>
        public int MsgRoute { get; protected set; }
        //--------------------------------------------------------------------------------------------------------------------
        //         public override string ToString()
        //         {
        //             return string.Format($"{GetType().Name}:PkgType={PkgType} PkgLength={PkgLength} MsgRoute={MsgRoute}");
        //         }
        public override string ToString()
        {
            return $"PkgType={PkgType} PkgLength={PkgLength} MsgRoute={MsgRoute} Body={BodyObject}";
        }
        //--------------------------------------------------------------------------------------------------------------------
        #region Head

        protected void DoEncodeHead(Stream stream)
        {
            stream.WriteByte((byte)(((int)PkgType) | ((int)PkgMask << 4)));
            stream.WriteByte(Convert.ToByte((PkgLength) & 0xFF));
            stream.WriteByte(Convert.ToByte((PkgLength >> 8) & 0xFF));
            stream.WriteByte(Convert.ToByte((PkgLength >> 16) & 0xFF));
        }
        protected void DoDecodeHead(Stream stream)
        {
            int b0 = stream.ReadByte();
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            int b3 = stream.ReadByte();
            this.PkgType = (PackageType)(b0 & 0x0F);
            this.PkgMask = (PackageMask)((b0 >> 4) & 0x0F);
            this.PkgLength = (b3 << 16) + (b2 << 8) + b1;
        }
        public static bool DoDecodeHead(byte[] buffer, int offset, out PackageType type, out PackageMask mask, out int length)
        {
            type = (PackageType)(buffer[offset] & 0x0F);
            mask = (PackageMask)((buffer[offset] >> 4) & 0x0F);
            length = (((int)buffer[offset + 3]) << 16) + (((int)buffer[offset + 2]) << 8) + ((int)buffer[offset + 1]);
            return true;
        }
        public static bool DoDecodeHeadLE(UInt32 u32, out PackageType type, out PackageMask mask, out int length)
        {
            byte b0 = (byte)(u32);
            byte b1 = (byte)(u32 >> 16);
            byte b2 = (byte)(u32 >> 24);
            byte b3 = (byte)(u32 >> 8);
            type = (PackageType)(b0 & 0x0F);
            mask = (PackageMask)((b0 >> 4) & 0x0F);
            length = (((int)b3) << 16) + (((int)b2) << 8) + ((int)b1);
            return true;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------
        #region SystemMessage

        protected void DoEncodeSystemMessage(IOutputStream stream, SystemMessage msg)
        {
            msg.DoEncode(stream);
        }
        protected bool DoDecodeSystemMessage(IInputStream stream, out SystemMessage msg)
        {
            switch (PkgType)
            {
                case PackageType.PKG_HANDSHAKE:
                    msg = new SystemHandshake();
                    msg.DoDecode(stream);
                    return true;
                case PackageType.PKG_HANDSHAKE_ACK:
                    msg = new SystemHandshakeAck();
                    msg.DoDecode(stream);
                    return true;
                case PackageType.PKG_HEARTBEAT:
                    msg = new SystemHeartbeat();
                    msg.DoDecode(stream);
                    return true;
                case PackageType.PKG_KICK:
                    msg = new SystemKick();
                    msg.DoDecode(stream);
                    return true;
            }
            msg = null;
            return false;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------
        #region Body

        public int BodyLength
        {
            get
            {
                var len = PkgLength - 5;
                if (MsgType != MessageType.MSG_NOTIFY)
                {
                    len -= 4;
                }
                return len;
            }
        }
        public int BodyStartPistion
        {
            get
            {
                var pos = 9;
                if (MsgType != MessageType.MSG_NOTIFY)
                {
                    pos += 4;
                }
                return pos;
            }
        }
        public abstract object BodyObject { get; }
        public abstract Type BodyType { get; }
        public abstract ISerializable GetBody();
        //--------------------------------------------------------------------------------------------------------------------
        protected void BeginEncodeBody(IOutputStream stream, ISerializable msg, out TypeCodec codec)
        {
            codec = stream.Factory.GetCodec(msg.GetType());
            if (codec != null)
            {
                this.MsgRoute = codec.MessageID;
                this.BeginEncodeBody(stream);
            }
            else
            {
                throw new Exception("Can not find object codec : Type=" + msg);
            }
        }
        protected void BeginEncodeBody(IOutputStream stream, int route)
        {
            this.MsgRoute = route;
            this.BeginEncodeBody(stream);
        }
        private void BeginEncodeBody(IOutputStream stream)
        {
            if (PkgType == PackageType.PKG_MESSAGE)
            {
                stream.PutU8((byte)this.MsgType);
                if (MsgType != MessageType.MSG_NOTIFY)
                {
                    stream.PutU32(this.MsgSendID);
                }
                stream.PutS32(this.MsgRoute);
            }
            else
            {
                throw new Exception("Only PKG_MESSAGE Can be encode !");
            }
        }
        //--------------------------------------------------------------------------------------------------------------------
        protected void BeginDecodeBody(IInputStream stream)
        {
            if (PkgType == PackageType.PKG_MESSAGE)
            {
                this.MsgType = (MessageType)stream.GetU8();
                if (MsgType != MessageType.MSG_NOTIFY)
                {
                    this.MsgSendID = stream.GetU32();
                }
                this.MsgRoute = stream.GetS32();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------

        // 
        //         protected void DoEncodeBody(IOutputStream stream, ISerializable msg)
        //         {
        //             var codec = stream.Factory.GetCodec(msg.GetType());
        //             if (codec != null)
        //             {
        //                 codec.DoWrite(stream, msg);
        //             }
        //             else
        //             {
        //                 throw new Exception("Can not find object codec : Type=" + msg);
        //             }
        // 
        //         }
        //         protected void DoDecodeBody(IInputStream stream, out ISerializable msg)
        //         {
        //             var codec = stream.Factory.GetCodec(this.MsgRoute);
        //             if (codec != null)
        //             {
        //                 msg = (ISerializable)DeepActivator.CreateInstance(codec.MessageType);
        //                 codec.DoRead(stream, msg);
        //             }
        //             else
        //             {
        //                 throw new Exception("Can not find object codec : ID=" + MsgRoute);
        //             }
        //         }
        // 
        //         protected void DoEncodeBody(IOutputStream stream, ref BinaryMessage msg)
        //         {
        //             stream.PutRawData(msg.Data, 0, msg.Data.Length);
        // 
        //         }
        //         protected void DoDecodeBody(IInputStream stream, out BinaryMessage msg)
        //         {
        //             if (this.BodyLength > 0)
        //             {
        //                 var data = new byte[this.BodyLength];
        //                 stream.GetRawData(data, 0, data.Length);
        //                 msg = new BinaryMessage(MsgRoute, data);
        //             }
        //             else
        //             {
        //                 msg = new BinaryMessage(MsgRoute, null);
        //             }
        //         }

        #endregion
    }


    //--------------------------------------------------------------------------------------------------------
    public abstract class SystemMessage
    {
        public abstract void DoDecode(IInputStream stream);
        public abstract void DoEncode(IOutputStream stream);
    }
    public class SystemKick : SystemMessage
    {
        public string reason;
        public override string ToString()
        {
            return "SystemKick: " + reason;
        }
        public override void DoDecode(IInputStream stream)
        {
            this.reason = stream.GetUTF();
        }
        public override void DoEncode(IOutputStream stream)
        {
            stream.PutUTF(this.reason);
        }
    }
    public class SystemHandshake : SystemMessage
    {
        public ISerializable user;
        public string local_info;
        public override string ToString()
        {
            return "SystemHandshake: " + user;
        }
        public override void DoDecode(IInputStream stream)
        {
            this.user = stream.GetObjAs<ISerializable>();
            this.local_info = stream.GetUTF();
        }
        public override void DoEncode(IOutputStream stream)
        {
            stream.PutObj(this.user);
            stream.PutUTF(this.local_info);
        }
    }
    public class SystemHandshakeAck : SystemMessage
    {
        public ISerializable token;
        public string remote_info;
        public int heartbeat_interval_ms;
        public override string ToString()
        {
            return "SystemHandshakeAck: " + remote_info;
        }
        public override void DoDecode(IInputStream stream)
        {
            this.token = stream.GetObjAs<ISerializable>();
            this.remote_info = stream.GetUTF();
            this.heartbeat_interval_ms = stream.GetVS32();
        }
        public override void DoEncode(IOutputStream stream)
        {
            stream.PutObj(this.token);
            stream.PutUTF(this.remote_info);
            stream.PutVS32(this.heartbeat_interval_ms);
        }
    }

    public class SystemHeartbeat : SystemMessage
    {
        public double time;
        public override string ToString()
        {
            return "SystemHeartbeat: " + time;
        }
        public override void DoDecode(IInputStream stream)
        {
            this.time = stream.GetF64();
        }
        public override void DoEncode(IOutputStream stream)
        {
            stream.PutF64(this.time);
        }
    }

    //--------------------------------------------------------------------------------------------------------

}

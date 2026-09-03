using DeepCore.Concurrent;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DeepCore.PomeloClient.MessagePool;

namespace DeepCore.Pomelo
{
    public abstract class IProtocolMessage : IProtocol
    {
        protected readonly DeepCore.IO.MemoryStream buffer = new DeepCore.IO.MemoryStream();

        public byte[] Buffer
        {
            get { return buffer.GetBuffer(); }
        }
        public int BufferPosition
        {
            get { return (int)buffer.Position; }
            set { buffer.Position = value; }
        }
        public int BufferLength
        {
            get { return (int)buffer.Length; }
            set
            {
                if (value < FIXED_HEAD_SIZE)
                {
                    throw new Exception("缓冲区不能小于固定长度 " + FIXED_HEAD_SIZE);
                }
                buffer.SetLength(value);
            }
        }
        protected abstract void Disposing();
        public void FillBuffer(byte[] data, int offset, int count)
        {
            this.buffer.Write(data, offset, count);
        }
    }

    //----------------------------------------------------------------------------------------------------

    public abstract class ISendMessage : IProtocolMessage
    {
        //-----------------------------------------------------------
        private readonly MemoryOutputStream buffer_stream;
        private ISerializable sending_msg;
        private SystemMessage sending_sys;
        private BinaryMessage sending_bin;
        public object SendingObject { get; private set; }
        //-----------------------------------------------------------
        protected MemoryOutputStream BufferStream { get { return buffer_stream; } }
        public ISendMessage(IExternalizableFactory codec)
        {
            this.buffer_stream = PomeloFactory.Instance.CreateOutputStream(buffer, codec);
            this.BufferLength = SendMessage.FIXED_HEAD_SIZE;
            this.BufferPosition = 0;
        }
        public override object BodyObject => SendingObject;
        public override Type BodyType => SendingObject?.GetType();
        public override ISerializable GetBody() => sending_msg;
        protected override void Disposing()
        {
            this.BufferLength = FIXED_HEAD_SIZE;
            this.BufferPosition = 0;
            this.sending_msg = null;
            this.sending_sys = null;
            this.sending_bin = BinaryMessage.NULL;
            this.SendingObject = null;
        }
        public int InitWithSystemMessage(SystemMessage msg)
        {
            if (msg is SystemKick)
            {
                this.InitWithHead(PackageType.PKG_KICK);
                this.sending_sys = msg;
                this.SendingObject = msg;
                this.BeginSend();
            }
            else if (msg is SystemHandshake)
            {
                this.InitWithHead(PackageType.PKG_HANDSHAKE);
                this.sending_sys = msg;
                this.SendingObject = msg;
                this.BeginSend();
            }
            else if (msg is SystemHandshakeAck)
            {
                this.InitWithHead(PackageType.PKG_HANDSHAKE_ACK);
                this.sending_sys = msg;
                this.SendingObject = msg;
                this.BeginSend();
            }
            else if (msg is SystemHeartbeat)
            {
                this.InitWithHead(PackageType.PKG_HEARTBEAT);
                this.sending_sys = msg;
                this.SendingObject = msg;
                this.BeginSend();
            }
            else
            {
                throw new Exception("Unknown System Message : " + msg);
            }
            return BufferLength;
        }
        public int InitWithMessage(MessageType msg_type, uint send_id, ISerializable msg)
        {
            this.InitWithMessage(msg_type, send_id);
            this.sending_msg = msg;
            this.SendingObject = msg;
            this.BeginSend();
            return BufferLength;
        }
        public int InitWithMessage(MessageType msg_type, uint send_id, BinaryMessage msg)
        {
            this.InitWithMessage(msg_type, send_id);
            this.sending_bin = msg;
            this.SendingObject = msg;
            this.BeginSend();
            return BufferLength;
        }

        protected void InitWithHead(PackageType pktType)
        {
            this.PkgType = pktType;
            this.PkgLength = 0;
            this.BufferLength = FIXED_HEAD_SIZE;
            this.BufferPosition = 0;
            this.DoEncodeHead(this.buffer);
        }
        protected void InitWithMessage(MessageType msg_type, uint send_id)
        {
            this.MsgType = msg_type;
            this.MsgSendID = send_id;
            this.PkgType = PackageType.PKG_MESSAGE;
            this.PkgLength = 0;
            this.BufferLength = FIXED_HEAD_SIZE;
            this.BufferPosition = FIXED_HEAD_SIZE;
        }
        protected void BeginSend()
        {
            this.buffer.Position = FIXED_HEAD_SIZE;
            if (sending_msg != null)
            {
                this.BeginEncodeBody(this.buffer_stream, sending_msg, out var codec);
                this.buffer_stream.EncodeSerializable(codec, sending_msg);
                if (PomeloFactory.Instance.CompressStream(this.buffer, this))
                {
                    this.PkgMask = PackageMask.Compressed;
                }
            }
            else if (sending_bin.HasRoute)
            {
                this.BeginEncodeBody(this.buffer_stream, sending_bin.Route);
                if (sending_bin.HasData)
                {
                    this.buffer_stream.EncodeBinaryMessage(in sending_bin);
                    if (PomeloFactory.Instance.CompressStream(this.buffer, this))
                    {
                        this.PkgMask = PackageMask.Compressed;
                    }
                }
            }
            else if (sending_sys != null)
            {
                this.DoEncodeSystemMessage(this.buffer_stream, sending_sys);
                if (PomeloFactory.Instance.CompressStream(this.buffer, this))
                {
                    this.PkgMask = PackageMask.Compressed;
                }
            }
            else
            {
                throw new Exception("Sending Body Is Empty");
            }
            this.PkgLength = this.BufferPosition - FIXED_HEAD_SIZE;
            this.buffer.Position = 0;
            this.DoEncodeHead(this.buffer);
        }
    }

    //----------------------------------------------------------------------------------------------------

    public abstract class IRecvMessage : IProtocolMessage
    {
        //-----------------------------------------------------------
        private readonly MemoryInputStream buffer_stream;
        private ISerializable readed_msg;
        private SystemMessage readed_sys;
        private BinaryMessage readed_bin;
        public object ReceivedObject { get; private set; }
        //-----------------------------------------------------------
        public bool IsBufferFinish { get; private set; }
        public MemoryInputStream BufferStream { get { return buffer_stream; } }
        public IRecvMessage(IExternalizableFactory codec)
        {
            this.buffer_stream = PomeloFactory.Instance.CreateInputStream(buffer, codec);
            this.BufferLength = RecvMessage.FIXED_HEAD_SIZE;
            this.BufferPosition = 0;
        }
        public override object BodyObject => ReceivedObject;
        public override Type BodyType => buffer_stream?.Factory?.GetCodec(this.MsgRoute)?.MessageType;
        protected override void Disposing()
        {
            this.BufferLength = FIXED_HEAD_SIZE;
            this.BufferPosition = 0;
            this.readed_msg = null;
            this.readed_sys = null;
            this.readed_bin = BinaryMessage.NULL;
        }
        //-----------------------------------------------------------
        public void ReadHead()
        {
            var p = buffer.Position;
            try
            {
                buffer.Position = 0;
                base.DoDecodeHead(buffer);
                this.BufferLength = PkgLength + FIXED_HEAD_SIZE;
            }
            finally { buffer.Position = p; }
        }
        public void BeginBody()
        {
            var p = buffer.Position;
            try
            {
                buffer.Position = FIXED_HEAD_SIZE;
                base.BeginDecodeBody(buffer_stream);
                this.IsBufferFinish = true;
            }
            finally { buffer.Position = p; }
        }
        public override ISerializable GetBody() => ReadBody();
        public ISerializable ReadBody()
        {
            if (readed_msg == null)
            {
                var p = buffer.Position;
                try
                {
                    buffer.Position = this.BodyStartPistion;
                    if ((PkgMask & PackageMask.Compressed) != 0)
                    {
                        PomeloFactory.Instance.DecompressStream(this.buffer, this);
                    }
                    var codec = buffer_stream.Factory.GetCodec(this.MsgRoute);
                    if (codec != null)
                    {
                        buffer.Position = this.BodyStartPistion;
                        readed_msg = (ISerializable)codec.DoCreate(codec.MessageType);
                        buffer_stream.DecodeSerializable(codec, in readed_msg);
                        ReceivedObject = readed_msg;
                    }
                    else
                    {
                        throw new Exception("Can not find object codec : ID=" + MsgRoute);
                    }
                }
                finally { buffer.Position = p; }
            }
            return readed_msg;
        }
        public BinaryMessage ReadBodyBinary()
        {
            if (readed_bin.IsNoRoute)
            {
                var p = buffer.Position;
                try
                {
                    buffer.Position = this.BodyStartPistion;
                    if ((PkgMask & PackageMask.Compressed) != 0)
                    {
                        PomeloFactory.Instance.DecompressStream(this.buffer, this);
                    }
                    buffer.Position = this.BodyStartPistion;
                    readed_bin = buffer_stream.DecodeBinaryMessage(MsgRoute, this.BodyStartPistion, this.BodyLength);
                    ReceivedObject = readed_bin;
                }
                finally { buffer.Position = p; }
            }
            return readed_bin;
        }

        public SystemMessage ReadBodySystemMessage()
        {
            if (readed_sys == null)
            {
                var p = buffer.Position;
                try
                {
                    buffer.Position = FIXED_HEAD_SIZE;
                    if ((PkgMask & PackageMask.Compressed) != 0)
                    {
                        PomeloFactory.Instance.DecompressStream(this.buffer, this);
                    }
                    base.DoDecodeSystemMessage(buffer_stream, out readed_sys);
                    ReceivedObject = readed_sys;
                }
                finally { buffer.Position = p; }
            }
            return readed_sys;
        }

    }

}

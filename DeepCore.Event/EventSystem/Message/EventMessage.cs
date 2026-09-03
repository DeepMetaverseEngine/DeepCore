using System;
using DeepCore.IO;

namespace DeepCore.Event.EventSystem.Message
{
    public abstract class EventMessage : IExternalizable
    {
        //name + AddressSeparatorChar + tag
        public string From;
        public int FromEvent;

        public string To;
        public static readonly EventMessageCodec Codec = new EventMessageCodec();

        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutUTF(From);
            output.PutUTF(To);
            output.PutS32(FromEvent);
        }

        public override string ToString()
        {
            return $"{From}->{To}";
        }

        public virtual void ReadExternal(IInputStream input)
        {
            From = input.GetUTF();
            To = input.GetUTF();
            FromEvent = input.GetS32();
        }

        public byte[] ToBytes()
        {
            ArraySegment<byte> bin = IOUtil.ZERO_BYTES;
            if (Codec.doEncode(this, out bin))
            {
                return bin.Array;
            }
            return null;
        }

        public static EventMessage FromBytes(byte[] bin)
        {
            object msg;
            if (Codec.doDecode(new ArraySegment<byte>(bin), out msg))
            {
                return msg as EventMessage;
            }
            return null;
        }
    }
}
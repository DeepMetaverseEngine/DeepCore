using DeepCore.IO;
using DeepCore.Reflection;
using System;

namespace DeepCore.VoxelWorld
{
    public class Codec : MessageFactoryGenerator
    {
        public static Codec Instance { get; } = new Codec();
        public Codec() : base("")
        {
            base.RegistCodec(new DeepCore.Voxel.StreamingVoxel.Data.Codec());
            int msgid = 0xFA0000;
            var asm = typeof(Codec).Assembly;
            foreach (var type in asm.GetTypes())
            {
                if (!type.IsAbstract && typeof(IExternalizable).IsAssignableFrom(type))
                {
                    if (PropertyUtil.TryGetAttribute<MessageTypeAttribute>(type, out var attr))
                    {
                        this.RegistExternalizable(type, static t => DeepActivator.CreateInstance(t), attr.MessageTypeID);
                    }
                    else
                    {
                        this.RegistExternalizable(type, static t => DeepActivator.CreateInstance(t), ++msgid);
                    }
                }
            }
        }
    }
}

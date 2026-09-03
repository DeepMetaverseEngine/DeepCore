using DeepCore.Game3D.Slave.Layer;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Slave.Data
{
    public abstract class LayerStatus : BattleAutoRecycle
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(LayerStatus));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        protected LayerStatus() { Alloc.RecordConstructor(GetType()); }
        ~LayerStatus()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing() { Alloc.RecordDispose(this.GetType()); }
        sealed protected override void RecordReuse() { Alloc.RecordReuse(GetType()); }
    }
}

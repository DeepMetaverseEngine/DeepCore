using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Host.Data
{
    public abstract class InstanceStatus : BattleAutoRecycle
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(InstanceStatus));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }

        protected InstanceStatus()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~InstanceStatus()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        sealed protected override void RecordReuse()
        {
            Alloc.RecordReuse(GetType());
        }
    }
}

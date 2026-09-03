using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Template;
using System;
using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Slave.Layer
{
    public abstract class LayerObject : Recyclable
    {
        public static implicit operator bool(in LayerObject value)
        {
            return value != null;
        }
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(LayerObject));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public static int ActiveObjectCount { get { return Alloc.ActiveCount; } }
        public static int AllocObjectCount { get { return Alloc.AllocCount; } }

        public virtual float X { get { return mRemotePos.X; } }
        public virtual float Y { get { return mRemotePos.Y; } }
        public virtual float Z { get { return mRemotePos.Z; } }
        public virtual Geometry.Vector3 Position { get => mRemotePos.ToGeometry3(); }
        public float RadiusSize { get => BodyBlockSize; }
        public abstract float BodyBlockSize { get; }
        public abstract float BodyHeight { get; }
        public abstract string DisplayName { get; }
        public abstract string Name { get; }
        public abstract float Direction { get; }
        public abstract float BodyDirection { get; }
        /// <summary>
        /// 腰部
        /// </summary>
        public virtual float WaistZ { get => this.Z + BodyHeight / 2; }
        /// <summary>
        /// 顶部
        /// </summary>
        public virtual float TopZ { get => this.Z + BodyHeight; }
        /// <summary>
        /// 腰部
        /// </summary>
        public virtual Geometry.Vector3 WaistPosition { get { var pos = this.Position; pos.Z += BodyHeight / 2; return pos; } }

        public Geometry.Vector3 RemotePos { get => this.mRemotePos.ToGeometry3(); }
        //--------------------------------------------------------------------------------------------------------------------------------
        protected LayerObject()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~LayerObject()
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
        //--------------------------------------------------------------------------------------------------------------------------------
        public ZoneDataFactory DataFactory { get => Parent.DataFactory; }
        public ZoneSlaveFactory SlaveFactory { get=> Parent.SlaveFactory; }
        //--------------------------------------------------------------------------------------------------------------------------------
        protected readonly VectorObject3 mRemotePos = new VectorObject3();
        private LayerZone mParent;
        protected LayerObject Init(LayerZone parent)
        {
            this.mParent = parent;
            return this;
        }
        protected override void Disposing()
        {
            mParent = null;
        }
        protected override void Destructing()
        {

        }
        //--------------------------------------------------------------------------------------------------------------------------------
        public LayerZone Parent
        {
            get { return mParent; }
        }
        public Random RandomN
        {
            get { return mParent.RandomN; }
        }
        public BattleObjectPool ObjectPool { get => mParent.ObjectPool; }
        public TemplateManager Templates { get { return mParent.Templates; } }
        public Config CFG { get { return mParent.CFG; } }
        virtual public void ForceSyncPos(in Geometry.Vector3 pos)
        {
            mRemotePos.X = pos.X;
            mRemotePos.Y = pos.Y;
            mRemotePos.Z = pos.Z;
        }
        abstract public void ForceFaceTo(float dir, float body_dir);

    }



}

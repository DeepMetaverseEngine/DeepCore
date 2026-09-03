using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Threading;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore.Game3D.Host.Instance
{

    public abstract partial class InstanceZonePosition : InstanceAttributes, IPositionObject
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(InstanceZonePosition));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public static int ActiveObjectCount { get { return Alloc.ActiveCount; } }
        public static int AllocObjectCount { get { return Alloc.AllocCount; } }
        //-------------------------------------------------------------------------------------------------------
        sealed public override BattleObjectPool ObjectPool { get => mZone.ObjectPool; }
        public EditorTemplates DataRoot { get { return mZone.DataRoot; } }
        public TemplateManager Templates { get { return mZone.Templates; } }
        public EditorDataCenter DataCenter { get => mZone.DataCenter; }
        public C DataCenterAs<C>() where C : EditorDataCenter => DataCenter as C;
        public Config CFG { get { return mZone.CFG; } }
        public ICommonConfig ExtCFG { get; }
        public T ExtCFGAs<T>() where T : class, ICommonConfig { return this.ExtCFG as T; }
        public Random RandomN { get { return Parent.RandomN; } }
        public EditorScene Parent => mZone as EditorScene;
        public override InstanceZone Zone => mZone;
        public ZoneDataFactory DataFactory { get => Zone.DataFactory; }
        public ZoneHostFactory HostFactory { get => Zone.HostFactory; }
        //-------------------------------------------------------------------------------------------------------
        private InstanceZone mZone;
        public InstanceZonePosition(InstanceZone zone)
        {
            Alloc.RecordConstructor(GetType());
            this.mZone = zone;
        }
        public InstanceZonePosition()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~InstanceZonePosition()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        protected override void OnAlloc(bool NEW, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                this.mZone = args[0] as InstanceZone;
            }
            else
            {
                throw new Exception("Alloc with null args");
            }
        }
        protected override void Disposing()
        {
            base.Disposing();
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        sealed protected override void RecordReuse()
        {
            Alloc.RecordReuse(this.GetType());
        }
        //-------------------------------------------------------------------------------------------------------
        public Logger Log { get { return mZone.Log; } }
        public abstract float X { get; }
        public abstract float Y { get; }
        public abstract float Z { get; }
        public abstract Vector3 Position { get; }
        public abstract float Direction { get; }
        public abstract float BodyDirection { get; }
        public abstract float BodySize { get; }
        public abstract float BodyHeight { get; }
        public float Direction360 { get => CMath.RadianToAngle(Direction); }
        public float BodyDirection360 { get => CMath.RadianToAngle(BodyDirection); }
        public virtual Geometry.VoxelCylinder VoxelBody { get { return new Geometry.VoxelCylinder(Position, BodySize, BodyHeight); } }

        /// <summary>
        /// 腰部
        /// </summary>
        public virtual float WaistZ { get => this.Z + BodyHeight * 0.5f; }
        /// <summary>
        /// 顶部
        /// </summary>
        public virtual float TopZ { get => this.Z + BodyHeight; }
        public virtual Vector3 WaistPosition
        {
            get
            {
                var pos = this.Position;
                pos.Z += BodyHeight / 2;
                return pos;
            }
        }
        public virtual Vector3 HeadPosition
        {
            get
            {
                var pos = this.Position;
                pos.Z += BodyHeight;
                return pos;
            }
        }
        public abstract Geometry.Vector3 GetRandomPos();
        public override ZoneTimeExpire AllocTimeExpire(float delayMS)
        {
            return ZoneTimeExpire.Alloc(Zone, delayMS);
        }
        public override ZoneTimeInterval AllocTimeInterval(float intervalMS)
        {
            return ZoneTimeInterval.Alloc(Zone, intervalMS);
        }

        public T CloneData<T>(T src) where T : ISerializable => Zone.CloneData<T>(src);
        public ArrayList<T> CloneList<T>(IEnumerable<T> src) where T : ISerializable => Zone.CloneList<T>(src);
    }

}

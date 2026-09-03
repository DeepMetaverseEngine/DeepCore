using DeepCore.Components;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using System;
using DeepMetaGame.Data.Helper;

namespace DeepCore.Game3D.Slave.Layer
{
    public abstract class LayerComponent<O> : BattleAutoRecycle, IComponent<O>
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(LayerComponent<O>));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public O Owner { get; private set; }
        protected LayerComponent()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~LayerComponent()
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
        void IComponent<O>.InternalAdded(O owner)
        {
            if (this.Owner != null) throw new Exception("Component already added : " + this.Owner);
            this.Owner = owner;
            this.OnAdded();
        }
        void IComponent<O>.InternalRemoved(O owner)
        {
            if (!object.Equals(this.Owner, owner)) throw new Exception("Component not object owner : " + this.Owner);
            this.OnRemoved();
            this.Owner = default;
        }
        internal protected void InternalUpdate(float intervalMS)
        {
            this.OnUpdate(intervalMS);
        }
        protected virtual void OnAdded() { }
        protected virtual void OnRemoved() { }
        protected virtual void OnUpdate(float intervalMS) { }
    }

    //----------------------------------------------------------------------------------------------------------------

    public abstract class LayerZoneComponent : LayerComponent<LayerZone>
    {
        public LayerZone Layer => Owner;
        public int Priority { get; protected set; }
        sealed protected override void Disposing()
        {
            var owner = this.Owner;
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }
        protected virtual void OnDispose(LayerZone owner) { }
    }
    public abstract class LayerZoneComponent<T> : LayerZoneComponent where T : LayerZone
    {
        new public T Owner { get => base.Owner as T; }
        new public T Layer => this.Owner;
    }

    public class LayerZoneCollection : ComponentCollection<LayerZone, LayerZoneComponent>
    {
        public LayerZoneCollection(LayerZone owner, Comparison<LayerZoneComponent> compare) : base(owner, compare)
        {
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    public abstract class LayerObjectComponent : LayerComponent<LayerZoneObject>
    {
        public Type OwnerType { get => Owner?.GetType(); }
        public int Priority { get; protected set; }
        sealed protected override void Disposing()
        {
            var owner = this.Owner;
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }
        protected virtual void OnDispose(LayerZoneObject owner) { }
    }

    public abstract class LayerObjectComponent<T> : LayerObjectComponent where T : LayerZoneObject
    {
        new public T Owner { get => base.Owner as T; }
        new public Type OwnerType { get => typeof(T); }
    }
    public class LayerObjectComponentCollection : ComponentCollection<LayerZoneObject, LayerObjectComponent>
    {
        public LayerObjectComponentCollection(LayerZoneObject owner, Comparison<LayerObjectComponent> compare) : base(owner, compare)
        {
        }
    }
    //----------------------------------------------------------------------------------------------------------------
}

using DeepCore.Components;
using DeepCore.Geometry;
using DeepCore.GUI.Display;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Input;
using System;

namespace DeepCore.GUI.SceneGraph
{

    public abstract class DisplayNodeComponent : Disposable, IComponent<DisplayNode>
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(DisplayNodeComponent));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        protected DisplayNodeComponent()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~DisplayNodeComponent()
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
        sealed protected override void Disposing()
        {
            var owner = this.Owner; 
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }

        public DisplayNode Owner { get; private set; }
        public DisplayNode Parent { get => Owner?.parent; }
        public Type OwnerType { get => Owner?.GetType(); }
        public int Priority { get; protected set; }
        public bool Enable { get; set; } = true;
        void IComponent<DisplayNode>.InternalAdded(DisplayNode owner)
        {
            if (this.Owner != null) throw new Exception("Component already added : " + this.Owner);
            this.Owner = owner;
            this.OnAdded();
        }
        void IComponent<DisplayNode>.InternalRemoved(DisplayNode owner)
        {
            if (this.Owner != owner) throw new Exception("Component not object owner : " + this.Owner);
            this.OnRemoved();
            this.Owner = null;
        }
        internal void InternalUpdate() { this.OnUpdate(); }
        protected virtual void OnRemoved() { }
        protected virtual void OnAdded() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnDispose(DisplayNode owner) { }


    }

    public class DisplayNodeComponentCollection : ComponentCollection<DisplayNode, DisplayNodeComponent>
    {
        public DisplayNodeComponentCollection(DisplayNode owner, Comparison<DisplayNodeComponent> compare) : base(owner, compare)
        {
        }
    }
}

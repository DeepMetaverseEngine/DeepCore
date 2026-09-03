using DeepCore.Components;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.EventSystems;

namespace DeepCore.Unity3D.UGUI
{
    public abstract class DisplayNodeComponent : Disposable, IComponent
    {
        public DisplayNode Owner { get; private set; }
        public int Priority { get; protected set; }
        internal void InternalAdded(DisplayNode owner)
        {
            if (this.Owner != null) throw new Exception("Component already added : " + this.Owner);
            this.Owner = owner;
            this.OnAdded();
        }
        internal void InternalRemoved(DisplayNode owner)
        {
            if (this.Owner != owner) throw new Exception("Component not object owner : " + this.Owner);
            this.OnRemoved();
            this.Owner = null;
        }
        internal void DoUpdate() { this.OnUpdate(); }
        internal void DoPointerDown(PointerEventData e) { this.OnPointerDown(e); }
        internal void DoPointerUp(PointerEventData e) { this.OnPointerUp(e); }
        internal void DoPointerClick(PointerEventData e) { this.OnPointerClick(e); }


        protected virtual void OnAdded() { }
        protected virtual void OnRemoved() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnPointerDown(PointerEventData e) { }
        protected virtual void OnPointerUp(PointerEventData e) { }
        protected virtual void OnPointerClick(PointerEventData e) { }
    }
}

using DeepCore.Components;
using DeepCore.Log;
using DeepCore.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCore.Meta.Layout
{
    public abstract class MetaObjectComponent : Disposable, IComponent<MetaObject>
    {
        public MetaObject Owner { get; private set; }
        public Type OwnerType { get => Owner?.GetType(); }
        public int Priority { get; protected set; }
        sealed protected override void Disposing()
        {
            OnDispose();
        }
        void IComponent<MetaObject>.InternalAdded(MetaObject owner)
        {
            if (Owner != null) throw new Exception("Component already added : " + Owner);
            Owner = owner;
            OnAdded();
        }
        void IComponent<MetaObject>.InternalRemoved(MetaObject owner)
        {
            if (Owner != owner) throw new Exception("Component not object owner : " + Owner);
            OnRemoved();
            Owner = null;
        }
        internal void InternalUpdate()
        {
            OnUpdate();
        }
        protected virtual void OnRemoved() { }
        protected virtual void OnAdded() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnDispose() { }

    }

    public abstract class MetaObjectComponent<T> : MetaObjectComponent where T : MetaObject
    {
    }

    public class MetaComponentCollection : ComponentCollection<MetaObject, MetaObjectComponent>
    {
        public MetaComponentCollection(MetaObject owner, Comparison<MetaObjectComponent> compare) : base(owner, compare)
        {
        }
    }
}

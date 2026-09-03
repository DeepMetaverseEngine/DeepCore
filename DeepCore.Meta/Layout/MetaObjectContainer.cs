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
    public abstract class MetaObjectContainer : MetaObject
    {
        public MetaObjectContainer()
        {
        }
        protected override void Disposing()
        {
            base.Disposing();
            this.DisposeChildren();
        }
        //-------------------------------------------------------------------------------------------------------------
        #region Update

        protected override void OnUpdateChilds(float intervalMS)
        {
            if (NumChildren > 0)
            {
                ForEachChildren(c => c.InternalUpdate(intervalMS));
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Children

        public abstract int NumChildren { get; }
        public abstract IEnumerable<MetaObject> Children { get; }
        protected bool InternalAddChild(MetaObject child, Predicate<MetaObject> collection)
        {
            if (child.parent != null) { return false; }
            if (collection(child) )
            {
                child.InternalSetParent(this);
                child.InternalAdded(this);
                OnChildAdded(child);
                InvokeAddChild(child);
                return true;
            }
            return false;
        }
        protected bool InternalRemoveChild(MetaObject child, Predicate<MetaObject> collection, bool dispose)
        {
            if (child.parent != this) { return false; }
            if (collection(child))
            {
                child.InternalRemoved(this);
                OnChildRemoved(child);
                InvokeRemoveChild(child);
                if (dispose)
                {
                    child.Dispose();
                }
                child.parent = null;
                child.root = null;
                return true;
            }
            return false;
        }
        protected abstract void CollectionClearChildren();
        protected abstract bool CollectionRemoveChild(MetaObject c);

        protected virtual void OnChildAdded(MetaObject child) {   }
        protected virtual void OnChildRemoved(MetaObject child) {   }

        public virtual void DisposeChildren()
        {
            ForEachChildren(c => c.Dispose());
            CollectionClearChildren();
        }
        public bool RemoveChild(MetaObject child, bool dispose = false)
        {
            return InternalRemoveChild(child, CollectionRemoveChild, dispose);
        }
        public bool ContainsChild(MetaObject child)
        {
            while (child != null)
            {
                if (child == this)
                    return true;
                else
                    child = child.Parent;
            }
            return false;
        }
       
        public MetaObject ForEachChildren(Predicate<MetaObject> action, bool recursion = false)
        {
            if (NumChildren > 0)
            {
                using (var list = ObjectPool.AllocList(Children))
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (action(list[i])) { return list[i]; }
                    }
                    if (recursion)
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i] is MetaObjectContainer cc)
                            {
                                var sc = cc.ForEachChildren(action, recursion);
                                if (sc != null) { return sc; }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public void ForEachChildren(Action<MetaObject> action, bool recursion = false)
        {
            if (NumChildren > 0)
            {
                using (var list = ObjectPool.AllocList(Children))
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        action(list[i]);
                    }
                    if (recursion)
                    {
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i] is MetaObjectContainer cc)
                            {
                                cc.ForEachChildren(action, recursion);
                            }
                        }
                    }
                }
            }
        }
        public MetaObject FindChild(Predicate<MetaObject> action, bool recursion = false)
        {
            return ForEachChildren(action, recursion);
        }

        public V ForEachChildren<V>(Predicate<V> action, bool recursion = false) where V : MetaObject
        {
            return ForEachChildren(c =>
            {
                if (c is V v)
                {
                    return action(v);
                }
                else
                {
                    return false;
                }
            }, recursion) as V;
        }
        public void ForEachChildren<V>(Action<V> action, bool recursion = false)
        {
            ForEachChildren(c =>
            {
                if (c is V v)
                {
                    action(v);
                }
            }, recursion);
        }
        public V FindChild<V>(Predicate<V> action, bool recursion = false) where V : MetaObject
        {
            return ForEachChildren(action, recursion);
        }


        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Events

        public delegate void AddChildHandler(MetaObjectContainer sender, MetaObject child);
        public delegate void RemoveChildHandler(MetaObjectContainer sender, MetaObject child);
        public event AddChildHandler OnAddChild { add { event_OnAddChild += value; } remove { event_OnAddChild -= value; } }
        public event RemoveChildHandler OnRemoveChild { add { event_OnRemoveChild += value; } remove { event_OnRemoveChild -= value; } }
        private AddChildHandler event_OnAddChild;
        private RemoveChildHandler event_OnRemoveChild;
        protected virtual void InvokeAddChild(MetaObject child) { event_OnAddChild?.Invoke(this, child); }
        protected virtual void InvokeRemoveChild(MetaObject child) { event_OnRemoveChild?.Invoke(this, child); }
        protected override void OnDisposingEvents()
        {
            base.OnDisposingEvents();
            event_OnAddChild = null;
            event_OnRemoveChild = null;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
    }

    public abstract class MetaObjectContainer<V> : MetaObjectContainer where V : MetaObject
    {
        public V ForEachChildren(Predicate<V> action, bool recursion = false)
        {
            return base.ForEachChildren<V>(action, recursion) as V;
        }
        public void ForEachChildren(Action<V> action, bool recursion = false)
        {
            base.ForEachChildren<V>(action, recursion);
        }
        public V FindChild(Predicate<V> action, bool recursion = false)
        {
            return base.ForEachChildren<V>(action, recursion);
        }

    }

}

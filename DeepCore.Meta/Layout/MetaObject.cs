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
    public abstract class MetaObject : Disposable
    {
        public string Name { get; set; }
        public object Tag { get; set; }
        public SingleThreadCollectionPool ObjectPool { get => root?.objectPool; }
        public Logger Log { get => root?.log; }
        public MetaObject()
        {
            components = CreateComponents();
        }
        protected override void Disposing()
        {
            OnDisposingEvents();
        }
        //-------------------------------------------------------------------------------------------------------------
        #region Compnents

        private readonly MetaComponentCollection components;
        public MetaComponentCollection Components { get => components; }
        private MetaComponentCollection CreateComponents()
        {
            var ret = new MetaComponentCollection(this, static (a, b) => a.Priority - b.Priority);
            return ret;
        }
        private void UpdateComponents()
        {
            components.ForEach(0, static (st, c) => c.InternalUpdate());
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Children

        internal MetaObjectContainer parent;
        internal MetaStage root;
        public MetaObjectContainer Parent { get { return parent; } }
        public MetaStage Root { get { return root; } }
        public bool IsRoot { get => root == this; }

        internal void InternalAdded(MetaObjectContainer parent) { this.OnAdded(parent); }
        internal void InternalRemoved(MetaObjectContainer parent) { this.OnRemoved(parent); }
        internal void InternalSetParent(MetaObjectContainer node)
        {
            // check for a recursion
            MetaObjectContainer ancestor = node;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.parent;
            }
            if (ancestor == this)
            {
                throw new Exception("An object cannot be added as a child to itself or one of its children (or children's children, etc.)");
            }
            else
            {
                parent = node;
                root = parent?.root;
            }
        }

        public void RemoveFromParent(bool dispose = true)
        {
            if (parent is MetaObjectContainer container)
            {
                container.RemoveChild(this, dispose);
            }
        }

        protected virtual void OnAdded(MetaObjectContainer parent) { }
        protected virtual void OnRemoved(MetaObjectContainer parent) { }
        protected virtual void OnUpdateChilds(float intervalMS) { }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Update
        internal void InternalUpdate(float intervalMS)
        {
            UpdateComponents();
            OnUpdate(intervalMS);
            InvokeUpdate(intervalMS);
            OnUpdateChilds(intervalMS);
            OnEndUpdate(intervalMS);
            InvokeEndUpdate(intervalMS);
        }
        protected virtual void OnUpdate(float intervalMS) { }
        protected virtual void OnEndUpdate(float intervalMS) { }

        #endregion
        //-------------------------------------------------------------------------------------------------------------
        #region Events

        public delegate void UpdateHandler(MetaObject sender, float intervalMS);
        public delegate void ErrorHandler(MetaObject sender, Exception err);
        public event UpdateHandler Update { add { event_OnUpdate += value; } remove { event_OnUpdate -= value; } }
        public event UpdateHandler EndUpdate { add { event_OnEndUpdate += value; } remove { event_OnEndUpdate -= value; } }
        public event ErrorHandler Error { add { event_OnError += value; } remove { event_OnError -= value; } }
        private UpdateHandler event_OnUpdate;
        private UpdateHandler event_OnEndUpdate;
        private ErrorHandler event_OnError;
        protected virtual void InvokeUpdate(float intervalMS) { event_OnUpdate?.Invoke(this, intervalMS); }
        protected virtual void InvokeEndUpdate(float intervalMS) { event_OnEndUpdate?.Invoke(this, intervalMS); }
        protected virtual void InvokeError(Exception err)
        {
            if (event_OnError == null) { Log.Error(err.Message, err); }
            else { event_OnError.Invoke(this, err); }
        }
        protected virtual void OnDisposingEvents()
        {
            event_OnUpdate = null;
            event_OnEndUpdate = null;
            event_OnError = null;
        }

        #endregion
    }
}

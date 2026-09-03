using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Unity.BattleView
{
    public abstract partial class UnityZoneObject : UnityLayerObject
    {
        public LayerZoneObject zObject { get => layerZoneObject; }
        public LayerZoneObject layerZoneObject { get => layerObject as LayerZoneObject; }
        public uint objectID { get => layerZoneObject.ObjectID; }
        public UnityZoneObject(UnityZone zone) : base(zone) { }
        internal void Leave()
        {
            if (gameObject) gameObject.SetActive(false);
            this.Dispose();
        }
        protected override void OnInit()
        {
            InitObjectEvents();
        }
        protected override void OnUpdate(float deltaMS)
        {
            //UpdateMaterialActions(deltaMS);
        }
        protected override void OnDisposing()
        {
            CleanObjectEvents();
            //CleanMaterialActions();
        }
        protected override void OnDestory()
        {
        }
        //----------------------------------------------------------------------------------------------------------------------------
        #region Events

        private HashMap<Type, List<Action<ObjectNotify>>> _objectEvens = new HashMap<Type, List<Action<ObjectNotify>>>();

        protected virtual void InitObjectEvents()
        {
            layerZoneObject.OnMessageReceived += ZoneObject_OnDoEvent;
            RegistObjectEvent<UnitEffectEvent>(ObjectEvent_UnitEffectEvent);
        }
        protected virtual void CleanObjectEvents()
        {
            layerZoneObject.OnMessageReceived -= ZoneObject_OnDoEvent;
            _objectEvens.Clear();
        }

        protected virtual void ZoneObject_OnDoEvent(LayerZoneObject obj, ObjectNotify e)
        {
            DoObjectEvent(e);
        }
        public virtual void RegistObjectEvent<T>(Action<T> action) where T : ObjectNotify
        {
            var type = typeof(T);
            _objectEvens.TryGetOrCreate(type, out var outVal, t => new List<Action<ObjectNotify>>());
            {
                outVal.Add(e => action(e as T));
            }
        }
        protected virtual void DoObjectEvent(ObjectNotify ev)
        {
            if (_objectEvens.TryGetValue(ev.GetType(), out var action))
            {
                foreach (var a in action)
                {
                    a(ev);
                }
            }
        }
        protected virtual void ObjectEvent_UnitEffectEvent(UnitEffectEvent ev)
        {
            PlayEffect(ev.effect);
        }
        public virtual void PlayEffect(LaunchEffect effect)
        {
            if (effect != null)
            {
                parent.PlayObjectEffect(this, effect);
            }
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //         #region MaterialActions
        // 
        //         private HashMap<Type, UnityMaterialAction> _materialActions = new HashMap<Type, UnityMaterialAction>();
        //         protected virtual void CleanMaterialActions()
        //         {
        //             foreach (var key in _materialActions)
        //             {
        //                 ((IPoolingObject)key.Value).Dispose();
        //                 parent.objectPool.Release(key.Value);
        //             }
        //             _materialActions.Clear();
        //         }
        //         protected virtual void UpdateMaterialActions(float deltaMS)
        //         {
        //             if (_materialActions.Count > 0)
        //             {
        //                 using (var finished = objectPool.AllocList<KeyValuePair<Type, UnityMaterialAction>>())
        //                 {
        //                     foreach (var pair in _materialActions)
        //                     {
        //                         pair.Value.Update(deltaMS);
        //                         if (pair.Value.isDone)
        //                         {
        //                             finished.Add(pair);
        //                         }
        //                     }
        //                     if (finished.Count > 0)
        //                     {
        //                         foreach (var key in finished)
        //                         {
        //                             ((IPoolingObject)key.Value).Dispose();
        //                             _materialActions.Remove(key.Key);
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //         public virtual void StartMaterialAction<T>() where T : UnityMaterialAction, new()
        //         {
        //             var type = typeof(T);
        //             var action = objectPool.Alloc<T>();
        //             if (_materialActions.TryGetValue(type, out var old))
        //             {
        //                 ((IPoolingObject)old).Dispose();
        //                 parent.objectPool.Release(old);
        //             }
        //             _materialActions[type] = action;
        //             action.Start(this);
        //         }
        //         #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
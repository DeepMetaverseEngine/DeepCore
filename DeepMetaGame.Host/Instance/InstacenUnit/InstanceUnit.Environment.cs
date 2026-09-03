using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//


        //-----------------------------------------------------------------------------------------------------//
        public EnvironmentVarMap<InstanceUnit> EnvironmentVarMap { get; }
        private void EnvironmentVarMap_OnEnvironmentVarChangeHandler(InstanceUnit st, string key, EnvironmentVar var, object value, bool syncToClient)
        {
            if (EnvironmentVar.ALWAYS_SYNC_ENVIRONMENT_VAR || var.SyncToClient || syncToClient)
            {
                PostEvent(ObjectPool.Alloc<UnitSyncEnvironmentVarEvent>().Init (ID, new ClientStruct.ZoneEnvironmentVar()
                {
                    Key = key,
                    Value = st.HostFactory.EncodeZoneVar(value),
                    SyncToClient = syncToClient
                }));
            }
            cb_EnvironmentVarMapChanged(st, key, var, value, syncToClient);
        }
        protected void cb_EnvironmentVarMapChanged(InstanceUnit st, string key, EnvironmentVar var, object value, bool syncToClient)
        {
            OnEnvironmentVarChangeHandler?.Invoke(this, key, value);
        }
        //private HashMap<string, EnvironmentVar> EnvironmentVarMap = new HashMap<string, EnvironmentVar>(1);
        /* private Action<InstanceUnit, string, object> event_OnEnvironmentVarChangeHandler;

         public event Action<InstanceUnit, string, object> OnEnvironmentVarChangeHandler
         {
             add { event_OnEnvironmentVarChangeHandler += value; }
             remove { event_OnEnvironmentVarChangeHandler -= value; }
         }*/
        // 
        //         public void SetEnvironmentVar(string key, object value, bool syncToClient = false)
        //         {
        //             if (!string.IsNullOrEmpty(key))
        //             {
        //                 EnvironmentVar var = EnvironmentVarMap.Get(key);
        //                 if (var != null)
        //                 {
        //                     if (EnvironmentVar.ALWAYS_SYNC_ENVIRONMENT_VAR || (var.SyncToClient && var.Value != value))
        //                     {
        //                         PostEvent(new PlayerSyncEnvironmentVarEvent(ID, key, value));
        //                         event_OnEnvironmentVarChangeHandler?.Invoke(this, key, value);
        //                     }
        // 
        //                     var.Value = value;
        //                 }
        //             }
        //         }

        public void SetEnvironmentVar(string key, object value, bool syncToClient = true)
        {
            this.EnvironmentVarMap.SetEnvironmentVar(key, value, syncToClient);
        }
        public T GetEnvironmentVarAs<T>(string key)
        {
            return this.EnvironmentVarMap.GetEnvironmentVarAs<T>(key);
        }
        public bool TryGetEnvironmentVar(string key, out object value)
        {
            return this.EnvironmentVarMap.TryGetEnvironmentVar(key, out value);
        }
        public bool TryGetEnvironmentVarAs<T>(string key, out T value)
        {
            return this.EnvironmentVarMap.TryGetEnvironmentVarAs<T>(key, out value);
        }
        public int ListEnvironmentVars(List<EnvironmentVar> list)
        {
            return this.EnvironmentVarMap.ListEnvironmentVars(list);
        }

        public List<EnvironmentVar> ListEnvironmentVars()
        {
            return new List<EnvironmentVar>(EnvironmentVarMap.Values);
        }

        public void GetCurrentUnitVars(IList<ClientStruct.ZoneEnvironmentVar> ret)
        {
            int i = 0;
            foreach (EnvironmentVar var in EnvironmentVarMap.Values)
            {
                var o = new ClientStruct.ZoneEnvironmentVar();
                {
                    o.Key = var.Key;
                    o.SyncToClient = var.SyncToClient;
                    if (var.SyncToClient)
                    {
                        o.Value = var.Value;
                    }
                }
                ret.Add(o);
                i++;
            }
        }


        //-----------------------------------------------------------------------------------------------------//


    }
}
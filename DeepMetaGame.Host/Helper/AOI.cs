using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Helper
{
    public class ObjectAoiStatus : Recyclable
    {
        private readonly HashMap<uint, InstanceZoneObject> mObjects = new HashMap<uint, InstanceZoneObject>(2);
        private IPostChannel mChannel;
        private InstancePlayer mCreatorOwner;

        public InstancePlayer CreatorOwner { get => mCreatorOwner; }
        public IPostChannel Channel { get => mChannel; }
        public static T Alloc<T>(InstancePlayer owner) where T : ObjectAoiStatus, new()
        {
            return owner.ObjectPool.Alloc<T>().Init(owner) as T;
        }
        public virtual ObjectAoiStatus Init(InstancePlayer owner)
        {
            this.mCreatorOwner = owner;
            this.mChannel = owner.HostFactory.CreateChannel(this);
            return this;
        }
        protected override void Disposing()
        {
            this.mChannel = null;
            this.mCreatorOwner = null;
            m_OnObjectLeave = null;
            m_OnObjectEnter = null;
            mObjects.Clear();
        }

        internal void AddObject(InstanceZoneObject o)
        {
            if (mObjects.TryAdd(o.ID, o))
            {
                onObjectEnter(o);

                if (o is InstanceUnit u)
                    o.Parent?.cb_unitEnterAOI(u, this);

                if (m_OnObjectEnter != null)
                {
                    m_OnObjectEnter.Invoke(this, o);
                }
            }
        }
        internal void RemoveObject(InstanceZoneObject o)
        {
            if (mObjects.Remove(o.ID))
            {
                onObjectLeave(o);

                if (o is InstanceUnit u)
                    o.Parent?.cb_unitLeaveAOI(u, this);

                if (m_OnObjectLeave != null)
                {
                    m_OnObjectLeave.Invoke(this, o);
                }
            }
        }
        protected virtual void onObjectEnter(InstanceZoneObject o) { }
        protected virtual void onObjectLeave(InstanceZoneObject o) { }


        //-------------------------------------------------------------------------------------------------------------------------
        #region EVENTS


        public delegate void OnObjectEnterHandler(ObjectAoiStatus aoi, InstanceZoneObject o);
        public delegate void OnObjectLeaveHandler(ObjectAoiStatus aoi, InstanceZoneObject o);
        private OnObjectEnterHandler m_OnObjectEnter;
        private OnObjectLeaveHandler m_OnObjectLeave;
        [EventTriggerDescAttribute("单位进入AOI")]
        public event OnObjectEnterHandler OnObjectEnter { add { m_OnObjectEnter += value; } remove { m_OnObjectEnter -= value; } }
        [EventTriggerDescAttribute("单位离开AOI")]
        public event OnObjectLeaveHandler OnObjectLeave { add { m_OnObjectLeave += value; } remove { m_OnObjectLeave -= value; } }


        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region API


        public IEnumerable<InstanceZoneObject> Objects { get { return mObjects.Values; } }

        public InstanceZoneObject GetObject(uint id)
        {
            return mObjects.Get(id);
        }

        /// <summary>
        /// 获取AOI内指定类型单位数量
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="select">选择器</param>
        /// <returns></returns>
        public int GetObjectCount<T>(Predicate<T> select) where T : InstanceZoneObject
        {
            int ret = 0;
            foreach (var o in this.Objects)
            {
                if (o is T)
                {
                    var u = o as T;
                    if (select(u))
                    {
                        ret++;
                    }
                }
            }
            return ret;
        }
        public T FindObject<T>(Predicate<T> select) where T : InstanceZoneObject
        {
            foreach (var o in this.Objects)
            {
                if (o is T)
                {
                    var u = o as T;
                    if (select(u))
                    {
                        return u;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取AOI内指定name单位数量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetUnitCountByName(string name)
        {
            return GetObjectCount<InstanceUnit>((u) => { return u.Name == name; });
        }
        /// <summary>
        /// 获取AOI内指定Force单位数量
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        public int GetUnitCountByForce(int force)
        {
            return GetObjectCount<InstanceUnit>((u) => { return u.Force == force; });
        }
        /// <summary>
        /// 获取AOI内指定模板单位数量
        /// </summary>
        /// <param name="unit_template_id"></param>
        /// <returns></returns>
        public int GetUnitCountByTemplateID(int unit_template_id)
        {
            return GetObjectCount<InstanceUnit>((u) => { return u.Info.ID == unit_template_id; });
        }
        /// <summary>
        /// 获取AOI内指定Force和指定模板单位数量
        /// </summary>
        /// <param name="force"></param>
        /// <param name="unit_template_id"></param>
        /// <returns></returns>
        public int GetUnitCount(int force, int unit_template_id)
        {
            return GetObjectCount<InstanceUnit>((u) => { return u.Force == force && u.Info.ID == unit_template_id; });
        }
        public int GetUnitCountByForceTemplateID(int force, int unit_template_id)
        {
            return this.GetUnitCount(force, unit_template_id);
        }


        public InstanceUnit FindUnitByName(string name)
        {
            return FindObject<InstanceUnit>((u) => { return u.Name == name; });
        }
        public InstanceUnit FindUnitByTemplateID(int unit_template_id)
        {
            return FindObject<InstanceUnit>((u) => { return u.Info.ID == unit_template_id; });
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------------
        #region IAoiStatus

        public InstancePlayer Owner => this.CreatorOwner;

        public int ObjectCount => this.mObjects.Count;

        public int UnitCount => this.mObjects.Count;

        public T ForEachObjects<T>(BreakPredicate<T> select) where T : InstanceZoneObject
        {
            foreach (var o in mObjects.Values)
            {
                if ((o is T t) && select(t))
                {
                    return o as T;
                }
            }
            return null;
        }


        #endregion
        //-------------------------------------------------------------------------------------------------------------------------

        /*      public virtual void ClearAllMonster()
              {

              }*/
    }
}

using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using DeepMetaGame.Data.Misc;
using DeepCore.Game3D.Host.Data;
using System.Diagnostics;

namespace DeepCore.Game3D.Host.Helper
{
    public class HateSystem : InstanceStatus, IComparer<HateSystem.HateInfo>
    {
        //----------------------------------------------------------------------------------------------------------
        protected class HateInfo : InstanceStatus
        {
            private InstanceUnit unit;
            private long hateValue;
            private AttackReason reason;
            protected HateInfo() { }
            public static HateInfo Alloc(InstanceUnit owner, long hateValue, AttackReason reason)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<HateInfo>(static s => new HateInfo());
                ret.Init(owner, hateValue, reason);
                return ret;
            }
            protected virtual HateInfo Init(InstanceUnit owner, long hateValue, AttackReason reason)
            {
                this.unit = owner;
                this.hateValue = hateValue;
                this.reason = reason;
                return this;
            }
            protected override void Disposing()
            {
                unit = null;
                hateValue = 0;
            }
            public InstanceUnit Unit => unit;
            public AttackReason Reason => reason;
            public long HateValue { get => hateValue; set => hateValue = value; }
        }
        //----------------------------------------------------------------------------------------------------------
        private InstanceUnit owner;
        private TimeInterval m_Timer;
        private int m_Capacity;
        private readonly HashMap<uint, HateInfo> unitMap = new HashMap<uint, HateInfo>();
        private readonly List<HateInfo> unitList = new List<HateInfo>(); // 第一个是仇恨值最高的
        private readonly List<HateInfo> removing = new List<HateInfo>(); // 

        public static HateSystem Alloc(InstanceUnit owner)
        {
            var ret = owner.ObjectPool.AllocOrCreateAutoRelease<HateSystem>(static s => new HateSystem());
            ret.Init(owner, owner.CFG.AI_HATE_SYSTEM_CAPACITY, owner.CFG.AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS);
            return ret;
        }
        protected HateSystem() { }
        protected virtual HateSystem Init(InstanceUnit owner, int capacity, int updateIntervalMS)
        {
            this.owner = owner;
            this.m_Capacity = capacity;
            this.m_Timer = owner.AllocTimeInterval(updateIntervalMS);
            return this;
        }
        protected override void Disposing()
        {
            this.m_Timer?.Dispose();
            this.m_Timer = null;
            this.m_Capacity = 0;
            foreach (var unit in unitMap.Values)
            {
                unit.Dispose();
            }
            foreach (var unit in removing)
            {
                unit.Dispose();
            }
            this.unitMap.Clear();
            this.unitList.Clear();
            this.removing.Clear();
            this.owner = null;
        }

        //----------------------------------------------------------------------------------------------------------
        public int Count
        {
            get { return unitList.Count; }
        }
        public int Capacity
        {
            get { return m_Capacity; }
        }
        public InstanceUnit Owner
        {
            get { return owner; }
        }

        public event HateTargetHandler TargetAdded;
        public event HateTargetHandler TargetRemoved;

        protected virtual void OnTargetAdded(HateInfo target, AttackReason reason, long hateValue) { }
        protected virtual void OnTargetRemoved(HateInfo target) { }
        public virtual void OnHitted(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
            Add(attacker, AttackReason.Attack, reduceHP);
        }

        public virtual void Add(InstanceUnit target, AttackReason reason, long hateValue = 0)
        {
            if (!unitMap.TryGetValue(target.ID, out var ret))
            {
                if (TrimCapacity(hateValue))
                {
                    ret = HateInfo.Alloc(target, hateValue, reason);
                    unitMap.Add(target.ID, ret);
                    unitList.Add(ret);
                    OnTargetAdded(ret, reason, hateValue);
                    TargetAdded?.Invoke(this, target, reason);
                    if (hateValue != 0) Sort();
                }
            }
            else if (ret != null)
            {
                if (hateValue != 0)
                {
                    ret.HateValue += hateValue;
                    Sort();
                }
            }
        }
        public virtual void Clear()
        {
            unitList.Clear();
            foreach (var u in unitMap.Values)
            {
                removing.Add(u);
            }
            unitMap.Clear();
        }
        public virtual bool Remove(InstanceUnit unit)
        {
            var u = unitMap.RemoveByKey(unit.ID);
            if (u != null)
            {
                try
                {
                    unitList.Remove(u);
                }
                finally
                {
                    removing.Add(u);
                }
                return true;
            }
            return false;
        }
        public virtual InstanceUnit RemoveAt(int i)
        {
            if (i < unitList.Count)
            {
                var u = unitList[i];
                unitList.RemoveAt(i);
                var uu = u.Unit;
                try
                {
                    if (uu != null)
                    {
                        unitMap.Remove(uu.ID);
                    }
                    else
                    {
                        //throw new Exception();
                    }
                }
                finally
                {
                    removing.Add(u);
                }
                return uu;
            }
            return null;
        }
        public virtual bool Contains(InstanceUnit unit)
        {
            return unitMap.ContainsKey(unit.ID);
        }
        public virtual bool ContainsID(uint unitID)
        {
            return unitMap.ContainsKey(unitID);
        }
        public virtual void Reset()
        {
            for (int i = 0; i < unitList.Count; ++i)
            {
                HateInfo u = unitList[i];
                u.HateValue = 0;
            }
            this.Sort();
        }
        protected virtual bool TrimCapacity(long hateValue)
        {
            while (this.Count > this.Capacity)
            {
                RemoveAt(this.Count - 1);
            }
            if (this.Count >= this.Capacity)
            {
                if (hateValue == 0) return false;
                //移除一个低仇恨值
                for (int i = this.Count - 1; i >= 0; --i)
                {
                    var hate = unitList[i];
                    if (hate.Unit != null)
                    {
                        if (!hate.Unit.Enable ||
                        !hate.Unit.IsActive ||
                        hate.HateValue < hateValue)
                        {
                            RemoveAt(i);
                            return true;
                        }
                    }
                    else
                    {
                        RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        public virtual void Update()
        {
            if (m_Timer.Update(owner.Parent.UpdateIntervalMS))
            {
                this.Sort();
                while (unitList.Count > m_Capacity)
                {
                    RemoveAt(unitList.Count - 1);
                }
                for (int i = this.Count - 1; i >= 0; --i)
                {
                    HateInfo u = unitList[i];
                    if (u.Unit.Enable && u.Unit.IsActive)
                    {
                    }
                    else
                    {
                        RemoveAt(i);
                    }
                }
            }
            if (removing.Count > 0)
            {
                for (int i = 0; i < removing.Count; ++i)
                {
                    var u = removing[i];
                    try
                    {
                        OnTargetRemoved(u);
                        TargetRemoved?.Invoke(this, u.Unit, u.Reason);
                    }
                    finally
                    {
                        //                         if (unitList.Contains(u) || unitMap.ContainsValue(u))
                        //                         {
                        // 
                        //                         }
                        u.Dispose();
                    }
                }
                removing.Clear();
            }
        }
        public virtual void Sort()
        {
            unitList.Sort(this);
        }


        /// <summary>
        /// 当前首要目标
        /// </summary>
        public virtual InstanceUnit GetHated()
        {
            for (int i = 0; i < unitList.Count; ++i)
            {
                HateInfo u = unitList[i];
                if (u.Unit.Enable && u.Unit.IsActive)
                {
                    return u.Unit;
                }
            }
            return null;
        }
        public bool TryGetHated(out InstanceUnit hated)
        {
            hated = GetHated();
            return hated != null;
        }

        public virtual int GetHateList(List<InstanceUnit> ret)
        {
            int count = 0;
            for (int i = 0; i < unitList.Count; ++i)
            {
                HateInfo u = unitList[i];
                if (u.Unit.Enable && u.Unit.IsActive)
                {
                    ret.Add(u.Unit);
                    count++;
                }
            }
            return count;
        }
        int IComparer<HateSystem.HateInfo>.Compare(HateSystem.HateInfo x, HateSystem.HateInfo y)
        {
            return Compare(x, y);
        }
        protected virtual int Compare(HateSystem.HateInfo x, HateSystem.HateInfo y)
        {
            return (int)(y.HateValue - x.HateValue);
        }

        public bool ForEachHateList<ST>(in ST state, ForEachPredicate<ST, InstanceUnit> action)
        {
            for (int i = 0; i < unitList.Count; ++i)
            {
                var u = unitList[i];
                if (u.Unit.Enable && u.Unit.IsActive)
                {
                    if (action.Invoke(state, u.Unit))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool TryGetHateList<ST>(in ST state, TryGetPredicate<ST, InstanceUnit> action, out InstanceUnit result)
        {
            for (int i = 0; i < unitList.Count; ++i)
            {
                var u = unitList[i];
                if (u.Unit.Enable && u.Unit.IsActive)
                {
                    if (action.Invoke(state, u.Unit))
                    {
                        result = u.Unit;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }
    }

    public delegate void HateTargetHandler(HateSystem hate, InstanceUnit target, AttackReason reason);

}

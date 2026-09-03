using DeepMetaGame.Data.Template;
using DeepCore.IO;
using System.Collections.Generic;
using DeepMetaGame.Data.Message;
using System.Net.Mail;
using DeepMetaGame.Data.Misc;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 代理用类
    /// </summary>
    partial class InstanceZone
    {
        private void BeginEventsRecord()
        {
            //             mLastAddedUnit = null;
            //             mLastActivatedUnit = null;
            //             mLastRebirthUnit = null;
            //             mLastHittedUnit = null;
            //             mLastAttackUnit = null;
            //             mLastKilledUnit = null;
            // 
            //             mLastCreatedInstanceItem = null;
            //             mLastUnitGotInstanceItem = null;
            // 
            //             mLastUnitGotItem = null;
            //             mLastUnitLostItem = null;
            //             mLastUnitUseItem = null;
            //             mLastUnitGotBuff = null;
            // 
            //             mLastPickingItemUnit = null;
            //             mLastPickingItem = null;
            //             mLastPickableUnit = null;
            // 
            //             mLastLaunchSkillUnit = null;
            //             mLastLaunchSkill = null;
            //             mLastLaunchSpell = null;
            // 
            //             mLastRecvMessageR2B = null;
            //             mLastSentMessageB2R = null;
        }


        private HashMap<byte, int> mForceTotalDead = new HashMap<byte, int>();

        public int GetTotalForceDead(byte force)
        {
            int ret = 0;
            mForceTotalDead.TryGetValue(force, out ret);
            return ret;
        }

        private void StatisticForceDead(InstanceUnit unit)
        {
            int ret = 0;
            mForceTotalDead.TryGetValue(unit.Force, out ret);
            ret += 1;
            mForceTotalDead[unit.Force] = ret;
        }


        public InstanceUnit GetEditUnit(string name)
        {
            return GetUnitByName(name);
        }

        public InstanceFlag GetEditFlag(string name)
        {
            return GetFlag(name);
        }

        public void DoClientScript(string filename)
        {
            PostEvent(ObjectPool.Alloc<DoScriptEvent>().Init(filename));
        }

        public bool SendScriptCommand(string reason)
        {
            PostEvent(ObjectPool.Alloc<ScriptCommandEvent>().Init(reason));
            return true;
        }
        //-------------------------------------------------------------------------------
        public GameOverEvent LastGameOver { get; set; }
        public bool GameOver(byte force, string reason)
        {
            if (LastGameOver == null)
            {
                LastGameOver = ObjectPool.Alloc<GameOverEvent>().Init(force, reason);
                LastGameOver.Retain();
                PostEvent(LastGameOver);
                return true;
            }
            return false;
        }
        public bool GameOver(byte force, string reason, IExternalizable ext)
        {
            if (LastGameOver == null)
            {
                LastGameOver = ObjectPool.Alloc<GameOverEvent>().Init(force, reason, ext);
                LastGameOver.Retain();
                PostEvent(LastGameOver);
                return true;
            }
            return false;
        }
        //-------------------------------------------------------------------------------
        public void GetAllItems(List<InstanceItem> ret)
        {
            ret.AddRange(this.AllItems);
        }
        public void GetAllUnits(List<InstanceUnit> ret)
        {
            ret.AddRange(this.AllUnits);
        }
        public void GetAllUnits<T>(List<T> ret) where T : InstanceUnit
        {
            foreach (var u in this.AllUnits)
            {
                if (u is T t)
                {
                    ret.Add(t);
                }
            }
        }
        public int GetAllUnitsCount()
        {
            return this.AllUnitsCount;
        }
        public void GetForceUnits(byte force, List<InstanceUnit> ret)
        {
            var units = this.AllUnits;
            int count = units.Count;
            InstanceUnit u;
            for (int i = 0; i < count; i++)
            {
                u = units[i];
                if (u.Force == force)
                {
                    ret.Add(u);
                }
            }
            //foreach (InstanceUnit u in this.AllUnits)
            //{
            //    if (u.Force == force)
            //    {
            //        ret.Add(u);
            //    }
            //}
        }

        public int GetForceAliveUnitsCount(byte force)
        {
            int Count = 0;
            var units = this.AllUnits;
            int unitCount = units.Count;
            InstanceUnit u;
            for (int i = 0; i < unitCount; i++)
            {
                u = units[i];
                if (u.Force == force && u.CurrentActionStatus != UnitActionStatus.Dead)
                {
                    Count++;
                }
            }
            return Count;
        }
        public int GetForceUnitsCount(byte force)
        {
            int Count = 0;
            var units = this.AllUnits;
            int unitCount = units.Count;
            InstanceUnit u;
            for (int i = 0; i < unitCount; i++)
            {
                u = units[i];
                if (u.Force == force)
                {
                    Count++;
                }
            }
            //foreach (InstanceUnit u in this.AllUnits)
            //{
            //    if (u.Force == force)
            //    {
            //        Count++;
            //    }
            //}
            return Count;
        }
        public InstanceUnit FindUnit(Predicate<InstanceUnit> find)
        {
            foreach (var att in AllUnits)
            {
                if (find(att))
                {
                    return att;
                }
            }
            return null;
        }
        public InstanceUnit FindUnit<ST>(in ST st, TryGetPredicate<ST, InstanceUnit> find)
        {
            foreach (var att in AllUnits)
            {
                if (find(st, att))
                {
                    return att;
                }
            }
            return null;
        }
        public T GetRandomUnit<ST, T>(in ST st, TryGetPredicate<ST, InstanceUnit> find) where T : InstanceUnit
        {
            using (var list = ObjectPool.AllocList<T>())
            {
                GetAllUnits<T>(list);
                RandomN.RandomList<T>(list);
                foreach (var att in list)
                {
                    if (find(st, att))
                    {
                        return att;
                    }
                }
            }
            return null;
        }
        public T GetRandomUnit<T>() where T : InstanceUnit
        {
            using (var list = ObjectPool.AllocList<T>())
            {
                GetAllUnits<T>(list);
                return RandomN.GetRandomInList(list);
            }
        }
    }
}

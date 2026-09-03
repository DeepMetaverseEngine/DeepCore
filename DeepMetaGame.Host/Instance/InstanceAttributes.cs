using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.IO;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    abstract public class InstanceAttributes : AttributesObject
    {
        public abstract InstanceZone Zone { get; }
        public abstract BattleObjectPool ObjectPool { get; }

        public abstract ZoneTimeExpire AllocTimeExpire(float delayMS);
        public abstract ZoneTimeInterval AllocTimeInterval(float intervalMS);


        private HashMap<Type, List<Ability>> mDataAbilities;

        protected override void Disposing()
        {
            base.Disposing();
            if (mDataAbilities != null)
            {
                foreach (var e in mDataAbilities)
                {
                    foreach (var a in e.Value)
                    {
                        a.Dispose();
                    }
                }
                mDataAbilities.Clear();
            }
        }
        //-------------------------------------------------------------------------------------------
        public void AddAbilities(IEnumerable<EditorAbilityData> abilities)
        {
            if (abilities != null)
            {
                foreach (EditorAbilityData td in abilities)
                {
                    AddAbility(td);
                }
            }
        }
        public Ability AddAbility(EditorAbilityData data)
        {
            var ab = Zone.CreateAbility(data, this);
            if (ab != null)
            {
                if (mDataAbilities == null) mDataAbilities = new HashMap<Type, List<Ability>>();
                var list = mDataAbilities.Get(data.GetType());
                if (list == null)
                {
                    list = new List<Ability>();
                    mDataAbilities.Add(data.GetType(), list);
                }
                list.Add(ab);
                ab.Start(this);
            }
            return ab;
        }
        //-------------------------------------------------------------------------------------------
        public bool TryGetAbility<T>(Type type, out T ab) where T : Ability
        {
            if (mDataAbilities != null)
            {
                var list = mDataAbilities.Get(type);
                if (list != null && list.Count > 0)
                {
                    ab = (list[0] as T);
                    return true;
                }
            }
            ab = default;
            return false;
        }
        public bool TryGetAbility<D, T>(out T ab) where T : Ability
        {
            return TryGetAbility<T>(typeof(D), out ab);
        }
        public int GetAbilities<T>(Type dataType, List<T> ret) where T : Ability
        {
            if (mDataAbilities == null) return 0;
            var list = mDataAbilities.Get(dataType);
            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item is T t)
                    {
                        ret.Add(t);
                    }
                }
                return list.Count;
            }
            return 0;
        }
        public int GetAbilities<D>(List<Ability> ret) where D : EditorAbilityData
        {
            return GetAbilities<Ability>(typeof(D), ret);
        }
        public int GetAbilities<D, T>(List<T> ret) where D : EditorAbilityData where T : Ability
        {
            return GetAbilities<T>(typeof(D), ret);
        }

    }

}

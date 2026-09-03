using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepGame3D.Unity.BattleView
{
    public partial class UnityZoneUnit : UnityZoneObject
    {
        private HashMap<LayerUnit.BuffState, VisibleBuffStatus> buffs = new();
        protected void InitBuffs()
        {
            using (var list = zone.objectPool.AllocList<LayerUnit.BuffState>())
            {
                layerUnit.GetBuffStatus(list);
                foreach (var buff in list)
                {
                    DoAddBuff(buff);
                }
            }
        }
        protected void CleanBuffs()
        {
            using (var list = zone.objectPool.AllocMap(buffs))
            {
                buffs.Clear();
                foreach (var b in list)
                {
                    try
                    {
                        b.Value?.Dispose();
                    }
                    catch (Exception err) { Debug.LogError(err); }
                }
            }
        }
        protected void DoAddBuff(LayerUnit.BuffState buff)
        {
            if (!buffs.TryGetOrCreate(buff, out var vbuff, this, static (st, buff) => VisibleBuffStatus.Alloc(st.parent, buff)))
            {
                try
                {
                    vbuff?.Start(this, buff);
                }
                catch (Exception err) { Debug.LogError(err); }
            }
        }
        protected void DoRemoveBuff(LayerUnit.BuffState buff)
        {
            if (buffs.TryRemove(buff, out var vbuff))
            {
                try
                {
                    vbuff?.Dispose();
                }
                catch (Exception err) { Debug.LogError(err); }
            }
        }

        public class VisibleBuffStatus : Recyclable
        {
            public LayerUnit.BuffState Buff { get; private set; }
            private List<UnityEffectPlay> effects = new List<UnityEffectPlay>();
            public static VisibleBuffStatus Alloc(UnityZone zone, LayerUnit.BuffState bs)
            {
                var ret = zone.objectPool.AllocOrCreateAutoRelease<VisibleBuffStatus>(static t => new VisibleBuffStatus());

                return ret;
            }
            private VisibleBuffStatus() { }
            public void Start(UnityZoneUnit unit, LayerUnit.BuffState buff)
            {
                this.Buff = buff;
                if (buff.Data.Abilities.TryGetComponentAs<BuffEffectAbility>(out var effects))
                {
                    if (effects.BindingEffect != null)
                    {
                        this.effects.Add(unit.parent.BindObjectEffect(unit, effects.BindingEffect));
                    }
                    if (effects.BindingEffectList != null)
                    {
                        foreach (var effect in effects.BindingEffectList)
                        {
                            this.effects.Add(unit.parent.BindObjectEffect(unit, effect));
                        }
                    }
                }
            }
            protected override void Disposing()
            {
                this.Buff = null;
                foreach (var resource in effects)
                {
                    resource?.Stop();
                }
                effects.Clear();
            }
            protected override void Destructing()
            {

            }
        }

    }
}

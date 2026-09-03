using DeepCore.Components;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.Instance.Components
{
    //---------------------------------------------------------------------------------------------------------------------------
    public abstract class BuffComponent : InstanceComponent<InstanceUnit.EquipBuff>
    {
        public InstanceZone Zone { get => Owner.Zone; }
        public InstanceUnit Unit { get => Owner.Owner; }
        public InstanceUnit.EquipBuff Buff { get => Owner; }
        public int Priority { get; protected set; }
        public Logger Log { get => Unit.Log; }

        internal void InternalStart()
        {
            try
            {
                this.OnStart();
            }
            catch (Exception e)
            {
                Log.Error($"{this.Buff.Data.TemplateID}OnStart Error: {e}");
            }

        }
        internal void InternalTick() { this.OnTick(); }
        internal void InternalEnd(byte result) { this.OnEnd(result); }

        protected virtual void OnStart() { }
        protected virtual void OnTick() { }
        protected virtual void OnEnd(byte result) { }

    }
    //---------------------------------------------------------------------------------------------------------------------------
    public abstract class BuffAbilityComponent<T> : BuffComponent where T : IBuffTemplateAbility
    {
        public T Ability { get; private set; }
        protected override void OnStart()
        {
            this.Ability = base.Buff.Data.Abilities.GetComponentAs<T>();
            try
            {
                base.OnStart();
            }
            catch (Exception e)
            {
                Log.Error($"{this.Buff.Data.TemplateID}OnStart Error: {e}");
            }
        }
    }
    //---------------------------------------------------------------------------------------------------------------------------
    public class BuffComponentCollection : ComponentCollection<InstanceUnit.EquipBuff, BuffComponent>
    {
        private static HashMap<Type, Type> s_buffs = new HashMap<Type, Type>();
        /// <summary>
        /// 注册所有 Buff Ability Data 对应的 BuffComponent 类型
        /// </summary>
        /// <param name="parentType"></param>
        public static void RegistNestedBuffAbilityComponents(Type parentType)
        {
            var types = parentType.GetNestedTypes();
            foreach (var btype in types)
            {
                RegistBuffAbilityComponent(btype);
            }
        }
        /// <summary>
        /// 注册所有 Buff Ability Data 对应的 BuffComponent 类型
        /// </summary>
        /// <param name="parentType"></param>
        public static void RegistBuffAbilityComponent(in Type btype)
        {
            if (!btype.IsAbstract && typeof(BuffAbilityComponent<>).IsAssignableFrom(btype))
            {
                var rbtype = btype.BaseType;
                while (rbtype != null && rbtype.GenericTypeArguments.Length == 1)
                {
                    var dtype = btype.BaseType.GenericTypeArguments[0];
                    if (typeof(IBuffTemplateAbility).IsAssignableFrom(dtype))
                    {
                        s_buffs.Add(dtype, btype);
                        return;
                    }
                    rbtype = rbtype.BaseType;
                }
            }
        }
        public BuffComponentCollection(InstanceUnit.EquipBuff owner, Comparison<BuffComponent> compare) : base(owner, compare)
        {
        }
        internal static BuffComponentCollection Create(InstanceUnit unit, InstanceUnit.EquipBuff buff, InstanceUnit sender)
        {
            var ret = default(BuffComponentCollection);
            foreach (var ab in buff.Data.Abilities)
            {
                if (s_buffs.TryGetValue(ab.GetType(), out var btype))
                {
                    ret = ret ?? new(buff, static (a, b) => a.Priority - b.Priority);
                    ret.AddComponent((BuffComponent)DeepActivator.CreateInstance(btype));
                }
            }
            return ret;
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------

}

using DeepCore.Components;
using DeepCore.Log;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DeepCore.Game3D.Host.Instance.Components
{
    public abstract class InstanceComponent<ZO> : BattleAutoRecycle, IComponent<ZO> where ZO : class
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(InstanceComponent<ZO>));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public ZO Owner { get; private set; }
        public bool Active
        {
            get => active; set
            {
                if (active != value)
                {
                    this.active = value;
                    OnActiveChanged?.Invoke();
                }
            }
        }
        private bool inted = false;
        private bool active = true;
        protected InstanceComponent()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~InstanceComponent()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        sealed protected override void RecordReuse()
        {
            Alloc.RecordReuse(GetType());
        }
        void IComponent<ZO>.InternalAdded(ZO owner)
        {
            if (this.Owner != null) throw new Exception("Component already added : " + this.Owner);
            this.Owner = owner;
            this.active = true;
            if (inted == false)
            {
                inted = true;
                this.OnInit();
            }
            this.OnAdded();
        }
        void IComponent<ZO>.InternalRemoved(ZO owner)
        {
            if (this.Owner != owner) throw new Exception("Component not object owner : " + this.Owner);
            this.OnRemoved();
            this.Owner = null;
        }
        internal void InternalUpdate() { this.OnUpdate(); }
        protected virtual void OnInit() { }
        protected virtual void OnAdded() { }
        protected virtual void OnRemoved() { }
        protected virtual void OnUpdate()
        {
            //    FlushFieldChange();
        }
        public Action OnActiveChanged;
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    public abstract class InstanceObjectComponent : InstanceComponent<InstanceZoneObject>
    {
        //------------------------------------------------------------------------------------------------------------

        //------------------------------------------------------------------------------------------------------------
        public InstanceZone Zone { get => Owner.Zone; }
        public Logger Log { get => Owner.Log; }
        public Type OwnerType { get => Owner?.GetType(); }
        public int Priority { get; protected set; }
        sealed protected override void Disposing()
        {
            var owner = Owner;
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }
        protected virtual void OnDispose(InstanceZoneObject owner) { }
    }
    public abstract class InstanceObjectComponent<T> : InstanceObjectComponent where T : InstanceZoneObject
    {
        new public T Owner { get => base.Owner as T; }
        new public Type OwnerType { get => typeof(T); }
    }
    public class InstanceObjectComponentCollection : ComponentCollection<InstanceZoneObject, InstanceObjectComponent>
    {
        public InstanceObjectComponentCollection(InstanceZoneObject owner, Comparison<InstanceObjectComponent> compare) : base(owner, compare)
        {
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    #region UnitComponent
    public abstract class UnitComponent : InstanceObjectComponent<InstanceUnit>
    {
        public bool IsUnitActive { get => this.Owner != null && this.Owner.IsActive; }
        public InstanceUnit Unit { get => base.Owner; }
        //         private readonly TimeInterval aiinterval = new TimeInterval(0);
        //         public int AIIntervalMS
        //         {
        //             get => aiinterval.IntervalTimeMS;
        //             set { aiinterval.Reset(value); }
        //         }
        internal void InternalUpdateAI()
        {
            //             if (aiinterval.IntervalTimeMS > 0)
            //             {
            //                 if (aiinterval.Update(Zone.UpdateIntervalMS))
            //                 {
            //                     this.OnUpdateAI();
            //                 }
            //                 else
            //                 {
            //                     // skip update
            //                 }
            //             }
            //             else
            {
                this.OnUpdateAI();
            }
        }
        protected virtual void OnUpdateAI() { }
    }
    public abstract class UnitComponent<UT> : UnitComponent where UT : InstanceUnit
    {
        new public UT Unit { get => base.Owner as UT; }
        new public UT Owner { get => base.Owner as UT; }
    }
    public abstract class UnitAbilityComponent<A> : InstanceObjectComponent<InstanceUnit> where A : IUnitTemplateAbility
    {
        public InstanceUnit Unit { get => base.Owner; }
        public A Ability { get; }
        protected UnitAbilityComponent(A a)
        {
            this.Ability = a;
        }
    }
    public abstract class UnitAbilityComponent<UT, A> : UnitAbilityComponent<A> where UT : InstanceUnit where A : IUnitTemplateAbility
    {
        new public UT Unit { get => base.Owner as UT; }
        new public UT Owner { get => base.Owner as UT; }
        protected UnitAbilityComponent(A a) : base(a)
        {
        }
    }

    public abstract class PlayerComponent : UnitComponent
    {
        new public InstancePlayer Owner { get => base.Owner as InstancePlayer; }
        public InstancePlayer Player => Owner;


        //         /// <summary>
        //         /// 获得化玩家上下线或者切换场景，需要带到逻辑服务器或者需要存储的数据。
        //         /// 该数据最初值由 AddUnit.last_zone_save_data 初始化。
        //         /// </summary>
        //         /// <returns></returns>
        //         public virtual ISerializable GetLastZoneSaveData()
        //         {
        //             return null;
        //         }
        // 
        // 
        //         /// <summary>
        //         /// 联网模式，断开连接。
        //         /// </summary>
        //         public virtual void OnDisconnected()
        //         {
        //  //           IsReady = false;
        //         }
        //         /// <summary>
        //         /// 联网模式，重新连接。
        //         /// </summary>
        //         public virtual void OnReconnected(AddUnit add)
        //         {
        //             
        // //                 this.mCustomAction.OnReconnected();
        // //                 this.mControlMove.OnReconnected();
        // //                 this.mControlUpdateMove.OnReconnected();
        // //                 this.mControlJump.OnReconnected();
        //             
        //         }
        // 
        //         /// <summary>
        //         /// 联网，连接成功.
        //         /// </summary>
        //         /// <param name="add"></param>
        //         public virtual void OnConnected(AddUnit add) { }

    }
    //-------------------------------------------------------------------------------------------------------------------------------------------------



    #endregion
    //-------------------------------------------------------------------------------------------------------------------------------------------------
    #region Zone
    public abstract class InstanceZoneComponent : InstanceComponent<InstanceZone>
    {
        public InstanceZone Zone => Owner;
        public Logger Log { get => Owner.Log; }
        public Type OwnerType { get => Owner?.GetType(); }
        public int Priority { get; protected set; }
        sealed protected override void Disposing()
        {
            var owner = Owner;
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }
        protected virtual void OnDispose(InstanceZone owner) { }
    }

    public abstract class InstanceZoneComponent<T> : InstanceZoneComponent where T : InstanceZone
    {
        new public T Owner { get => base.Owner as T; }
        new public Type OwnerType { get => typeof(T); }
        new public T Zone => Owner;
    }

    public abstract class ZoneComponent : InstanceZoneComponent<InstanceZone>
    {
        protected override void Destructing()
        {

        }
    }


    public class ZoneComponentCollection : ComponentCollection<InstanceZone, InstanceZoneComponent>
    {
        public ZoneComponentCollection(InstanceZone owner, Comparison<InstanceZoneComponent> compare) : base(owner, compare)
        {
        }
    }

    #endregion
}


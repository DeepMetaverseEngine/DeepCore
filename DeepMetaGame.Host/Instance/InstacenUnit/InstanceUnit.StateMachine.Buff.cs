using DeepCore.Game3D.Host.Helper;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.Reflection;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceUnit
    {
        public class StateBuffAction : State
        {
            private EquipBuff buff;
            private BuffStateChangeAbility change;
            public static StateBuffAction Alloc(InstanceUnit unit, EquipBuff buff, BuffStateChangeAbility change)
            {
                return unit.AllocState<StateBuffAction>().Init(unit, buff, change);
            }
            protected virtual StateBuffAction Init(InstanceUnit unit, EquipBuff buff, BuffStateChangeAbility change)
            {
                this.buff = buff;
                this.change = change;
                buff.Retain();
                return this;
            }
            protected override void Disposing()
            {
                buff?.Release();
            }
            public override bool OnBlock(State new_state)
            {
                return buff.IsEnd;
            }
            protected override void OnStart()
            {
                unit.SetActionStatus(change.LockMainStateAction, change.LockSubStateAction);
            }
            protected override void OnUpdate()
            {
                if (buff.IsEnd)
                {
                    this.unit.DoSomething();
                    return;
                }
                unit.SetActionStatus(change.LockMainStateAction, change.LockSubStateAction);
            }
            protected override void OnStop()
            {
            }
        }

        //--------------------------------------------------------------------------------------------------------

    }
}

using DeepCore.EventTrigger.Data;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Host.EventTrigger.UI
{

    [Desc("设置Camera偏移", "[游戏]/HUD")]
    public class SetCameraOffsetAction : ZoneAbstractAction
    {
        [Desc("角度")]
        public AbstractValue<double> Angle360 = new RealValue.VALUE(90);
        [Desc("距离")]
        public AbstractValue<double> Distance = new RealValue.VALUE(1);
        [Desc("高度")]
        public AbstractValue<double> OffsetZ = new RealValue.VALUE(0);

        [Desc("锁定横轴")]
        public AbstractValue<bool> LockYaw = new BooleanValue.VALUE(false);
        [Desc("锁定纵轴")]
        public AbstractValue<bool> LockPitch = new BooleanValue.VALUE(false);

        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var msg = api.ZoneAPI.ObjectPool.Alloc<CameraOffset>();
            {
                msg.Angle = CMath.AngleToRadian((float)Angle360.GetValueAs(api, args));
                msg.Radius = (float)Distance.GetValueAs(api, args);
                msg.OffsetZ = (float)OffsetZ.GetValueAs(api, args);
                msg.LockYaw = LockYaw.GetValueAs(api, args);
                msg.LockPitch = LockPitch.GetValueAs(api, args);
            }
            api.ZoneAPI.PostSystemMessage(msg);
            return null;
        }
    }
}

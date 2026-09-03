using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("摄像机聚焦单位", "[游戏]/摄像机")]
    public class CameraFocusUnitAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Focus = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("摄像机聚焦单位({0});", Focus);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            if (Focus.GetValueAs(api, args) is InstanceUnit unit)
            {
                var focus = api.ZoneAPI.ObjectPool.Alloc<CameraFocusUnitEvent>();
                focus.ObjectID = unit.ObjectID;
                api.ZoneAPI.PostEvent(focus);
            }
            return null;
        }

    }

    [Desc("摄像机控制", "[游戏]/摄像机")]
    public class CameraControlAction : ZoneAbstractAction
    {
        [Desc("控制器")]
        public AbstractValue<string> Name = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("摄像机控制({0});", Name);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var focus = api.ZoneAPI.ObjectPool.Alloc < CameraControlEvent>();
            focus.Name = Name?.GetValueAs(api, args);
            api.ZoneAPI.PostEvent(focus);
            return null;
        }

    }
}

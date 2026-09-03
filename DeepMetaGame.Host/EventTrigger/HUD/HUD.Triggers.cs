using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.GUI;
using DeepCore.GUI.Input;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message.UI;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepMetaGame.Host.EventTrigger.UI
{
    //-------------------------------------------------------------------------------------------------------
    public abstract class HUDEventTrigger : ZoneAbstractTrigger
    {
        public UIInteractiveAction TriggingEvent(EventArguments args) => args.GetArgAs<UIInteractiveAction>(0);
        [TriggingArg("事件发送的单位")] public InstanceUnit SenderObjectID(EventArguments args) => args.TriggingUnit;
    }
    public abstract class HUDMouseEventTrigger<T> : HUDEventTrigger where T : MouseInputAction
    {
        public MouseInputAction TriggingMouse(EventArguments args) => args.GetArgAs<MouseInputAction>(0);
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new ProcessZoneActionHandler((z, a) =>
            {
                if (a is UIInteractiveAction ui)
                {
                    args.TriggingUnit = z.GetUnit(ui.SenderObjectID);
                }
                if (a is T mouse && Select(mouse))
                {
                    a.Retain();
                    args.PutArg(0, a);
                    args.PutArg(1, mouse);
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(api.ZoneAPI, handler,
                (z, h) => z.OnProcessZoneAction += h,
                (z, h) => z.OnProcessZoneAction -= h);
        }
        protected virtual bool Select(T mouse) => true;
        [TriggingArg("鼠标点选控件名字")]
        public string MouseComponentName(EventArguments args)
            => TriggingMouse(args)?.ComponentName;

        [TriggingArg("鼠标按钮")]
        public double MouseButtonArg(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse)
            {
                return (double)mouse.Button;
            }
            return 0;
        }
        [TriggingArg("鼠标屏幕坐标")]
        public Vector3? MouseScreenPoint(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse)
            {
                return mouse.ScreenPoint;
            }
            return null;
        }
        [TriggingArg("鼠标点击次数")]
        public double MouseClicks(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse)
            {
                return mouse.Clicks;
            }
            return 0;
        }
        [TriggingArg("鼠标滚动量")]
        public double MouseDelta(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse)
            {
                return mouse.Delta;
            }
            return 0;
        }
        [TriggingArg("鼠标是否点到地表")]
        public bool MouseRayCastIsHitTerrain(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse && mouse.raycast != null)
            {
                return mouse.raycast.IsHitTerrain;
            }
            return false;
        }
        [TriggingArg("鼠标点到地表坐标")]
        public Vector3? MouseRayCastHitTerrainPosition(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse && mouse.raycast != null)
            {
                return mouse.raycast.HitTerrainPosition;
            }
            return null;
        }
        [TriggingArg("鼠标点到的单位")]
        public InstanceUnit MouseRayCastHitObjectID(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse && mouse.raycast != null)
            {
                return args.API.ZoneAPI.GetUnit(mouse.raycast.HitObjectID);
            }
            return null;
        }
        [TriggingArg("鼠标点到的FLAG")]
        public InstanceFlag MouseRayCastHitFlagName(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse && mouse.raycast != null)
            {
                return args.API.ZoneAPI.GetFlag(mouse.raycast.HitFlagName);
            }
            return null;
        }
        [TriggingArg("鼠标点到的单位坐标")]
        public Vector3? MouseRayCastHitObjectPosition(EventArguments args)
        {
            if (TriggingMouse(args) is MouseInputAction mouse && mouse.raycast != null)
            {
                return mouse.raycast.HitObjectPosition;
            }
            return null;
        }
    }
    public abstract class HUDKeyEventTrigger<T> : HUDEventTrigger where T : KeyInputAction
    {
        public KeyInputAction TriggingKey(EventArguments args) => args.GetArgAs<KeyInputAction>(0);
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new ProcessZoneActionHandler((z, a) =>
            {
                if (a is UIInteractiveAction ui)
                {
                    args.TriggingUnit = z.GetUnit(ui.SenderObjectID);
                }
                if (a is T keyDown && Select(keyDown))
                {
                    a.Retain();
                    args.PutArg(0, a);
                    args.PutArg(1, keyDown);
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnProcessZoneAction += h, (z, h) => z.OnProcessZoneAction -= h);
        }
        protected virtual bool Select(T key) => true;
        [TriggingArg("触发的按键")]
        public double KeyCodeArg(EventArguments args)
        {
            if (TriggingKey(args) is KeyInputAction key) { return (double)key.Key; }
            return 0;
        }
        [TriggingArg("触发的辅助按键(Ctrl Alt Shift)")]
        public double KeyModifiersArg(EventArguments args)
        {
            if (TriggingKey(args) is KeyInputAction key) { return (double)key.Modifiers; }
            return 0;
        }
    }
    //-------------------------------------------------------------------------------------------------------
    [Desc("鼠标按下", "[游戏]/HUD")]
    public class MouseDownTrigger : HUDMouseEventTrigger<MouseDownAction> { }
    [Desc("鼠标松开", "[游戏]/HUD")]
    public class MouseUpTrigger : HUDMouseEventTrigger<MouseUpAction> { }
    [Desc("鼠标移动", "[游戏]/HUD")]
    public class MouseMoveTrigger : HUDMouseEventTrigger<MouseMoveAction> { }
    [Desc("鼠标点击", "[游戏]/HUD")]
    public class MouseClickTrigger : HUDMouseEventTrigger<MouseClickAction> { }
    //-------------------------------------------------------------------------------------------------------

    [Desc("鼠标(键)按下", "[游戏]/HUD")]
    public class MouseDownTriggerWithKey : HUDMouseEventTrigger<MouseDownAction>
    {
        [Desc("鼠标按钮常量", "[游戏]/HUD")]
        public MouseButton Button = MouseButton.Left;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("鼠标({0})按下", Button);
        }
        protected override bool Select(MouseDownAction mouse) => mouse.Button == Button;
    }
    [Desc("鼠标(键)松开", "[游戏]/HUD")]
    public class MouseUpTriggerWithKey : HUDMouseEventTrigger<MouseUpAction>
    {
        [Desc("鼠标按钮常量", "[游戏]/HUD")]
        public MouseButton Button = MouseButton.Left;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("鼠标({0})松开", Button);
        }
        protected override bool Select(MouseUpAction mouse) => mouse.Button == Button;
    }
    [Desc("鼠标(键)移动", "[游戏]/HUD")]
    public class MouseMoveTriggerWithKey : HUDMouseEventTrigger<MouseMoveAction>
    {
        [Desc("鼠标按钮常量", "[游戏]/HUD")]
        public MouseButton Button = MouseButton.Left;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("鼠标({0})移动", Button);
        }
        protected override bool Select(MouseMoveAction mouse) => mouse.Button == Button;
    }

    [Desc("鼠标(键)点击", "[游戏]/HUD")]
    public class MouseClickTriggerWithKey : HUDMouseEventTrigger<MouseClickAction>
    {
        [Desc("鼠标按钮常量", "[游戏]/HUD")]
        public MouseButton Button = MouseButton.Left;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("鼠标({0})点击", Button);
        }
        protected override bool Select(MouseClickAction mouse) => mouse.Button == Button;
    }
    //-------------------------------------------------------------------------------------------------------

    [Desc("键盘按下", "[游戏]/HUD")]
    public class KeyDownTrigger : HUDKeyEventTrigger<KeyDownAction> { }
    [Desc("键盘松开", "[游戏]/HUD")]
    public class KeyUpTrigger : HUDKeyEventTrigger<KeyUpAction> { }
    //-------------------------------------------------------------------------------------------------------

    [Desc("键盘(键)按下", "[游戏]/HUD")]
    public class KeyDownTriggerWithKey : HUDKeyEventTrigger<KeyDownAction>
    {
        [Desc("按键", "[游戏]/HUD")]
        public KeyCode Key = KeyCode.Space;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("键盘(键)按下", Key);
        }
        protected override bool Select(KeyDownAction key) => key.Key == this.Key;

    }
    [Desc("键盘(键)松开", "[游戏]/HUD")]
    public class KeyUpTriggerWithKey : HUDKeyEventTrigger<KeyUpAction>
    {
        [Desc("按键", "[游戏]/HUD")]
        public KeyCode Key = KeyCode.Space;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("键盘(键)松开", Key);
        }
        protected override bool Select(KeyUpAction key) => key.Key == this.Key;
    }
    //-------------------------------------------------------------------------------------------------------
    [Desc("鼠标选中单位", "[游戏]/HUD")]
    public class MouseSelectUnitTrigger : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new ProcessZoneActionHandler((z, a) =>
            {
                if (a is MouseSelectObjectAction select)
                {
                    a.Retain();
                    args.TriggingUnit = z.GetUnit(select.HitObjectID);
                    args.PutArg(0, a);
                    args.PutArg(1, select);
                    api.TestAndDoAction(args);
                }
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnProcessZoneAction += h, (z, h) => z.OnProcessZoneAction -= h);
        }
        [TriggingArg("点选的单位")] public InstanceUnit HitObjectID(EventArguments args) => args.TriggingUnit;
    }
    //-------------------------------------------------------------------------------------------------------
}

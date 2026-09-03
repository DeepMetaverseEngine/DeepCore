using DeepCore.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GUI.Input;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.GUI;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;

namespace DeepMetaGame.Host.EventTrigger.UI
{
    //---------------------------------------------------------------------------
    [Desc("HUD交互发送单位", "[游戏]/HUD")]
    public class InteractiveSenderUnit : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<UIInteractiveAction>(0);
            if (a != null)
            {
                return api.ZoneAPI.GetUnit(a.SenderObjectID);
            }
            return null;
        }
    }

    //---------------------------------------------------------------------------
    [Desc("鼠标交互UI元素", "[游戏]/HUD")]
    public class MouseComponentName : ZoneStringValue
    {
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0);
            if (a != null)
            {
                return a.ComponentName;
            }
            return null;
        }
    }
    [Desc("鼠标按钮", "[游戏]/HUD")]
    public class MouseButtonValue : ZoneIntegerValue
    {
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0);
            if (a != null)
            {
                return (int)a.Button;
            }
            return 0;
        }
    }
    [Desc("鼠标按钮常量", "[游戏]/HUD")]
    public class MouseButtonConstant : ZoneIntegerValue
    {
        [Desc("鼠标按钮常量", "[游戏]/HUD")]
        public MouseButton Button = MouseButton.Left;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append(Button).Append("</c>");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return (int)this.Button;
        }
    }

    //---------------------------------------------------------------------------
    [Desc("射线检测场景坐标", "[游戏]/HUD")]
    public class RaycastHitPos : PositionValue
    {
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            if (a != null && a.IsHitTerrain)
            {
                return a.HitTerrainPosition;
            }
            return Vector3.Zero;
        }
    }
    [Desc("射线检测单位坐标", "[游戏]/HUD")]
    public class RaycastHitObjectPos : PositionValue
    {
        protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            if (a != null && a.IsHitObject)
            {
                return a.HitObjectPosition;
            }
            return Vector3.Zero;
        }
    }

    [Desc("是否射到单位", "[游戏]/HUD")]
    public class RaycastIsHitUnit : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            return (a != null && a.HitObjectID > 0);
        }
    }
    [Desc("是否射到Flag", "[游戏]/HUD")]
    public class RaycastIsHitFlag : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            return (a != null && !string.IsNullOrEmpty(a.HitFlagName));
        }
    }
    [Desc("是否射到地面", "[游戏]/HUD")]
    public class RaycastIsHitTerrain : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            return (a != null && a.IsHitTerrain);
        }
    }

    [Desc("射线检测到的单位", "[游戏]/HUD")]
    public class RaycastHitUnit : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            if (a != null)
            {
                return api.ZoneAPI.GetUnit(a.HitObjectID);
            }
            return null;
        }
    }
    [Desc("射线检测到的Flag", "[游戏]/HUD")]
    public class RaycastHitFlag : FlagValue
    {
        protected override InstanceFlag GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseInputAction>(0).raycast;
            if (a != null)
            {
                return api.ZoneAPI.GetFlag(a.HitFlagName);
            }
            return null;
        }
    }

    [Desc("拖拽中的单位", "[游戏]/HUD")]
    public class DraggingUnit : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.ZoneAPI.DraggingUnit;
        }
    }

    //---------------------------------------------------------------------------
    [Desc("键盘按键", "[游戏]/HUD")]
    public class KeyCodeValue : ZoneIntegerValue
    {
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<KeyInputAction>(0);
            if (a != null)
            {
                return (int)a.Key;
            }
            return 0;
        }
    }
    [Desc("键盘按键常量", "[游戏]/HUD")]
    public class KeyCodeConstant : ZoneIntegerValue
    {
        [Desc("按键", "[游戏]/HUD")]
        public KeyCode Key = KeyCode.Space;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append(Key).Append("</c>");
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return (int)this.Key;
        }
    }
    [Desc("键盘同时按下Control", "[游戏]/HUD")]
    public class KeyModiferIsControl : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<KeyInputAction>(0);
            if (a != null)
            {
                return a.IsControl;
            }
            return false;
        }
    }

    [Desc("键盘同时按下Alt", "[游戏]/HUD")]
    public class KeyModiferIsAlt : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<KeyInputAction>(0);
            if (a != null)
            {
                return a.IsAlt;
            }
            return false;
        }
    }

    [Desc("键盘同时按下Shift", "[游戏]/HUD")]
    public class KeyModiferIsShift : ZoneBooleanValue
    {
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<KeyInputAction>(0);
            if (a != null)
            {
                return a.IsShift;
            }
            return false;
        }
    }
    //---------------------------------------------------------------------------
    //---------------------------------------------------------------------------

    [Desc("鼠标选择的单位", "[游戏]/HUD")]
    public class MouseSelectUnitValue : UnitValue
    {
        protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = args.GetArgAs<MouseSelectObjectAction>(0);
            if (a != null)
            {
                return api.ZoneAPI.GetUnit(a.HitObjectID);
            }
            return null;
        }
    }

}

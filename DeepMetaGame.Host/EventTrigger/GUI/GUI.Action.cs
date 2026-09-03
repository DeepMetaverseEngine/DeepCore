using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Data.AI;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.GUI.Data;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using static DeepCore.Game3D.Host.Instance.InstanceZone;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.GUIValue;

namespace DeepCore.Game3D.Host.EventTrigger.UI
{

    [Desc("显示游戏窗口", "[GUI]")]
    public class ShowHostFormAction : ZoneAbstractAction
    {
        [Desc("界面ID")]
        [TemplateID(typeof(BattleUITemplate))]
        public int GUITemplateID = 0;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("显示HOST窗口({0});", GUITemplateID);
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ShowForm(GUITemplateID, api);
            return null;
        }
    }
    [Desc("显示游戏对话框", "[GUI]")]
    public class ShowHostDialogAction : ZoneAbstractAction
    {
        [Desc("界面ID")]
        [TemplateID(typeof(BattleUITemplate))]
        public int GUITemplateID = 0;
        [Desc("点击任意关闭")]
        public AbstractValue<bool> CloseOnClick = new BooleanValue.VALUE(true);
        [Desc("对话框选择后")]
        public AbstractAction OnSelectedDialog;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("显示HOST对话框({0});", GUITemplateID);
        }
        protected override void GetEndText(EventStringBuilder sw)
        {
            if (OnSelectedDialog != null)
            {
                sw.AppendLine().AppendLine("对话框选择后:");
                sw.IndentBegin("{");
                sw.AppendLine(OnSelectedDialog);
                sw.IndentEnd("}");
            }
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var dialog = api.ZoneAPI.ShowDialog(GUITemplateID, CloseOnClick.GetValueAs(api, args), api);
            if (dialog != null)
            {
                if (OnSelectedDialog != null)
                {
                    dialog.OnSelectDialog += (form, sender, click, state) =>
                    {
                        args.DialogResult = click.DialogResult;
                        args.TriggingComponentName = click.NodeName;
                        args.TriggingComponentSubURL = click.SubNodeURL;
                        args.TriggingComponent = sender;
                        args.TriggingForm = dialog;
                        OnSelectedDialog.Invoke(api, args);
                    };
                }
            }

            return null;
        }
        [TriggingArg("DialogResult")] public string DialogResult(EventArguments args) => args.DialogResult;
        [TriggingArg("TriggingComponentName")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("TriggingComponentSubURL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("TriggingComponent")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("TriggingForm")] public HostGUIForm TriggingForm(EventArguments args) => args.TriggingForm;
    }


    [Desc("显示玩家窗口", "[GUI]")]
    public class ShowHostPlayerFormAction : ZoneAbstractAction
    {
        [Desc("玩家单位")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        [Desc("界面ID")]
        [TemplateID(typeof(BattleUITemplate))]
        public int GUITemplateID = 0;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})显示窗口({1});", Player, GUITemplateID);
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            api.ZoneAPI.ShowPlayerForm(player, GUITemplateID, api);

            return null;
        }
    }

    [Desc("显示玩家对话框", "[GUI]")]
    public class ShowHostPlayerDialogAction : ZoneAbstractAction
    {
        [Desc("玩家单位")]
        public AbstractValue<InstanceUnit> Player = new UnitValue.Trigging();
        [Desc("界面ID")]
        [TemplateID(typeof(BattleUITemplate))]
        public int GUITemplateID = 0;
        [Desc("点击任意关闭")]
        public AbstractValue<bool> CloseOnClick = new BooleanValue.VALUE(true);
        [Desc("对话框选择后")]
        public AbstractAction OnSelectedDialog;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})显示对话框({1});", Player, GUITemplateID);
        }
        protected override void GetEndText(EventStringBuilder sw)
        {
            if (OnSelectedDialog != null)
            {
                sw.AppendLine().AppendLine("对话框选择后:");
                sw.IndentBegin("{");
                sw.AppendLine(OnSelectedDialog);
                sw.IndentEnd("}");
            }
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var player = Player.GetValueAs(api, args) as InstancePlayer;
            var dialog = api.ZoneAPI.ShowPlayerDialog(GUITemplateID, player, CloseOnClick.GetValueAs(api, args), api);
            if (dialog != null)
            {
                if (OnSelectedDialog != null)
                {
                    dialog.OnSelectDialog += (form, sender, click, state) =>
                    {
                        args.TriggingComponent = sender;
                        args.TriggingComponentName = click.NodeName;
                        args.TriggingComponentSubURL = click.SubNodeURL;
                        args.TriggingForm = dialog;
                        args.DialogResult = click.DialogResult;
                        OnSelectedDialog.Invoke(api, args);
                    };
                }
            }

            return null;
        }
        [TriggingArg("DialogResult")] public string DialogResult(EventArguments args) => args.DialogResult;
        [TriggingArg("TriggingComponentName")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("TriggingComponentSubURL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("TriggingComponent")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("TriggingForm")] public HostGUIForm TriggingForm(EventArguments args) => args.TriggingForm;
    }


    [Desc("关闭绑定游戏窗口", "[GUI]")]
    public class CloseBindingFormAction : ZoneAbstractAction
    {
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.FormAPI.Close();
            return null;
        }
    }



    [Desc("GUI数据绑定", "[GUI]")]
    public class GUIDataBindingAction : AbstractAction
    {
        [Desc("UI控件")]
        public AbstractValue<HostGUIComponent> GUI = new GUIValue.TriggingComponent();
        [Desc("Key")]
        public AbstractValue<string> Key = new ZoneStringValue.VALUE();
        [Desc("是否递归")]
        public AbstractValue<bool> Deep = new ZoneBooleanValue.VALUE(true);
        [Desc("Value")]
        public AbstractValue Value;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({2})绑定数据: ({0}) = ({1});", Key, Value, GUI);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var gui = GUI.GetValueAs(api, args);
            if (gui is HostGUINode node)
            {
                var key = Key?.GetValueAs(api, args);
                var value = Value?.GetRunValue(api, args);
                var deep = Deep == null || Deep.GetValueAs(api, args);
                node.BindData($"{key}", value, deep);
            }
            return null;
        }
    }



    [Desc("设置GUI节点显示", "[GUI]")]
    public class GUISetNodeVisibleAction : ZoneAbstractAction
    {
        [Desc("UI节点")]
        public AbstractValue<HostGUIComponent> Node = new GUIValue.BindingEditChild();
        [Desc("是否显示")]
        public AbstractValue<bool> Visible = new ZoneBooleanValue.VALUE(true);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})显示窗口=({1});", Node, Visible);
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Node.GetValueAs(api, args);
            if (o != null)
            {
                o.Visible = Visible.GetValueAs(api, args);
            }
            return null;
        }
    }
    //-------------------------------------------------------------------------------


    [Desc("(扩展)设置GUI节点显示", "[GUI]")]
    public class GUISetSubNodeNameVisibleAction : ZoneAbstractAction
    {
        [Desc("UI节点")]
        public AbstractValue<HostGUIComponent> Node = new GUIValue.BindingEditChild();
        [Desc("(扩展)子控件名")]
        [ResourceID(ResourceType.GUIComponent)]
        public string SubNameURL;
        [Desc("是否显示")]
        public AbstractValue<bool> Visible = new ZoneBooleanValue.VALUE(true);
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0}.{1})显示窗口=({2});", Node, SubNameURL, Visible);
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Node.GetValueAs(api, args);
            if (o != null)
            {
                o.SubVisible(SubNameURL, Visible.GetValueAs(api, args));
            }
            return null;
        }
    }
    [Desc("(扩展)控制GUI节点", "[GUI]")]
    public class GUIControlSubNodeAction : ZoneAbstractAction
    {
        [Desc("UI节点")]
        public AbstractValue<HostGUIComponent> Node = new GUIValue.BindingEditChild();
        [Desc("(扩展)控制器")]
        [ResourceID(ResourceType.GUIController)]
        public string SubNameURL;
        [Desc("自定义参数")]
        public AbstractValue Value;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("控制窗口({0}.{1} 参数{2}));", Node, SubNameURL, Value);
        }
        sealed protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Node.GetValueAs(api, args);
            if (o != null)
            {
                var value = Value?.GetRunValue(api, args);
                o.SubControl(SubNameURL, value);
            }
            return null;
        }
    }
    [Desc("(扩展)GUI数据绑定", "[GUI]")]
    public class GUISubNodeDataBindingAction : AbstractAction
    {
        [Desc("UI控件")]
        public AbstractValue<HostGUIComponent> GUI = new GUIValue.TriggingComponent();
        [Desc("(扩展)子控件名")]
        [ResourceID(ResourceType.GUIComponent)]
        public string SubNameURL;
        [Desc("Key")]
        public AbstractValue<string> Key = new ZoneStringValue.VALUE();
        [Desc("是否递归")]
        public AbstractValue<bool> Deep = new ZoneBooleanValue.VALUE(true);
        [Desc("Value")]
        public AbstractValue Value;
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({2})绑定数据: ({0}) = ({1});", Key, Value, GUI);
        }
        protected override object Run(EventExecutor api, IEventArguments args)
        {
            var gui = GUI.GetValueAs(api, args);
            if (gui is HostGUINode node)
            {
                var key = Key?.GetValueAs(api, args);
                var value = Value?.GetRunValue(api, args);
                var deep = Deep == null || Deep.GetValueAs(api, args);
                node.BindData($"{key}", value, deep, SubNameURL);
            }
            return null;
        }
    }

}


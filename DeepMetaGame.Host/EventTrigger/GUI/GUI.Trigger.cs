using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.GUI.Data;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.Game3D.Host.EventTrigger.UI
{

    //----------------------------------------------------------------------------------------
    #region Generic

    [Desc("某个UI窗体显示", "[GUI]")]
    public class GenericFormShown : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnFormHandler((z, gui) =>
            {
                args.TriggingForm = gui;
                args.TriggingComponent = gui;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnFormShown += h, (z, h) => z.OnFormShown -= h);
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;

    }
    [Desc("某个UI窗体关闭", "[GUI]")]
    public class GenericFormClose : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnFormHandler((z, gui) =>
            {
                args.TriggingForm = gui;
                args.TriggingComponent = gui;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnFormClosed += h, (z, h) => z.OnFormClosed -= h);            
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
    }
    [Desc("某个UI窗体控件点击", "[GUI]")]
    public class GenericFormNodeClick : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnNodeEventHandler<GUINodeClickAction>((z, form, gui, click) =>
            {
                args.TriggingForm = form;
                args.TriggingComponent = gui;
                args.TriggingComponentName = click.NodeName;
                args.TriggingComponentSubURL = click.SubNodeURL;
                args.DialogResult = click.DialogResult;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnFormNodeClick += h, (z, h) => z.OnFormNodeClick -= h);
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("对话框结果")] public string DialogResult(EventArguments args) => args.DialogResult;
    }
    [Desc("某个UI窗体控件数据改变", "[GUI]")]
    public class GenericFormNodeDataChanged : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var handler = new OnNodeEventHandler<GUINodeDataChangedAction>((z, form, gui, action) =>
            {
                args.TriggingForm = form;
                args.TriggingComponent = gui;
                args.TriggingComponentName = action.NodeName;
                args.TriggingComponentSubURL = action.SubNodeURL;
                args.TriggingStringValue = action.TextValue;
                args.TriggingNumberValue = action.NumberValue;
                args.TriggingBoolValue = action.BooleanValue;
                api.TestAndDoAction(args);
            });
            api.Listen(api.ZoneAPI, handler, (z, h) => z.OnFormNodeDataChanged += h, (z, h) => z.OnFormNodeDataChanged -= h);
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;

        [TriggingArg("绑定的String")] public string TextValue(EventArguments args) => args.TriggingStringValue;
        [TriggingArg("绑定的Number")] public double NumberValue(EventArguments args) => args.TriggingNumberValue;
        [TriggingArg("绑定的Bool")] public bool BooleanValue(EventArguments args) => args.TriggingBoolValue;

    }

    #endregion
    //----------------------------------------------------------------------------------------

    #region Binding

    [Desc("绑定的UI窗体显示", "[GUI]")]
    public class BindingFormShown : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = api.FormAPI;
            if (gui != null)
            {
                var handler = new UIFormHandler((gui) =>
                {
                    args.TriggingForm = gui;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = gui.Name;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnShown += h, (z, h) => z.OnShown -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
    }
    [Desc("绑定的UI窗体关闭", "[GUI]")]
    public class BindingFormClose : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = api.FormAPI;
            if (gui != null)
            {
                var handler = new UIFormHandler((gui) =>
                {
                    args.TriggingForm = gui;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = gui.Name;
                    args.DialogResult = gui.DialogResult;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnClose += h, (z, h) => z.OnClose -= h);             
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
    }
    [Desc("绑定的UI窗体控件点击", "[GUI]")]
    public class BindingFormNodeClick : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = api.FormAPI;
            if (gui != null)
            {
                var handler = new UINodeEventHandle<GUINodeClickAction>((form, gui, action) =>
                {
                    args.TriggingForm = form;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = action.NodeName;
                    args.TriggingComponentSubURL = action.SubNodeURL;
                    args.DialogResult = action.DialogResult;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnNodeClick += h, (z, h) => z.OnNodeClick -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("对话框结果")] public string DialogResult(EventArguments args) => args.DialogResult;
    }
    [Desc("绑定的UI窗体控件数据改变", "[GUI]")]
    public class BindingFormNodeDataChanged : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = api.FormAPI;
            if (gui != null)
            {
                var handler = new UINodeEventHandle<GUINodeDataChangedAction>((form, gui, action) =>
                {
                    args.TriggingForm = form;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = action.NodeName;
                    args.TriggingComponentSubURL = action.SubNodeURL;
                    args.TriggingStringValue = action.TextValue;
                    args.TriggingNumberValue = action.NumberValue;
                    args.TriggingBoolValue = action.BooleanValue;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnNodeDataChanged += h, (z, h) => z.OnNodeDataChanged -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;

        [TriggingArg("绑定的String")] public string TextValue(EventArguments args) => args.TriggingStringValue;
        [TriggingArg("绑定的Number")] public double NumberValue(EventArguments args) => args.TriggingNumberValue;
        [TriggingArg("绑定的Bool")] public bool BooleanValue(EventArguments args) => args.TriggingBoolValue;
    }
    #endregion
    //----------------------------------------------------------------------------------------
    #region Specify


    [Desc("指定的UI窗体显示", "[GUI]")]
    public class SpecifyFormShown : ZoneAbstractTrigger
    {
        [Desc("UI窗体")]
        public AbstractValue<HostGUIComponent> Form = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("窗体({0})显示", Form);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = Form?.GetValueAs(api, args);
            if (gui is HostGUIForm form)
            {
                var handler = new UIFormHandler((gui) =>
                {
                    args.TriggingForm = gui;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = gui?.Name;
                    api.TestAndDoAction(args);
                });
                api.Listen(form, handler, (z, h) => z.OnShown += h, (z, h) => z.OnShown -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
    }
    [Desc("指定的UI窗体关闭", "[GUI]")]
    public class SpecifyFormClose : ZoneAbstractTrigger
    {
        [Desc("UI窗体")]
        public AbstractValue<HostGUIComponent> Form = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("窗体({0})关闭", Form);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = Form?.GetValueAs(api, args);
            if (gui is HostGUIForm form)
            {
                var handler = new UIFormHandler((gui) =>
                {
                    args.TriggingForm = gui;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = gui?.Name;
                    api.TestAndDoAction(args);
                });
                api.Listen(form, handler, (z, h) => z.OnClose += h, (z, h) => z.OnClose -= h);            
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
    }
    [Desc("指定的UI窗体控件点击", "[GUI]")]
    public class SpecifyFormNodeClick : ZoneAbstractTrigger
    {
        [Desc("UI窗体")]
        public AbstractValue<HostGUIComponent> Form = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("窗体({0})控件点击", Form);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = Form?.GetValueAs(api, args);
            if (gui is HostGUINode node)
            {
                var handler = new UINodeEventHandle<GUINodeClickAction>((form, node, click) =>
                {
                    if (gui == node)
                    {
                        args.TriggingForm = form;
                        args.TriggingComponent = gui;
                        args.TriggingComponentName = click.NodeName;
                        args.TriggingComponentSubURL = click.SubNodeURL;
                        args.DialogResult = click.DialogResult;
                        api.TestAndDoAction(args);
                    }
                });
                api.Listen(node, handler, (z, h) => z.OnClick += h, (z, h) => z.OnClick -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("对话框结果")] public string DialogResult(EventArguments args) => args.DialogResult;
    }


    [Desc("指定的UI控件点击", "[GUI]")]
    public class SpecifyUINodeClick : ZoneAbstractTrigger
    {
        [Desc("UI控件")]
        public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})点击", GUI);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = GUI?.GetValueAs<HostGUINode>(api, args);
            if (gui != null)
            {
                var handler = new UINodeEventHandle<GUINodeClickAction>((form, gui, click) =>
                {
                    args.TriggingForm = form;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = click.NodeName;
                    args.TriggingComponentSubURL = click.SubNodeURL;
                    args.DialogResult = click.DialogResult;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnClick += h, (z, h) => z.OnClick -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;
        [TriggingArg("对话框结果")] public string DialogResult(EventArguments args) => args.DialogResult;
    }


    [Desc("指定的UI控件数据改变", "[GUI]")]
    public class SpecifyUINodeDataChanged : ZoneAbstractTrigger
    {
        [Desc("UI控件")]
        public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})数据改变", GUI);
        }
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            var gui = GUI?.GetValueAs<HostGUINode>(api, args);
            if (gui != null)
            {
                var handler = new UINodeEventHandle<GUINodeDataChangedAction>((form, gui, action) =>
                {
                    args.TriggingForm = form;
                    args.TriggingComponent = gui;
                    args.TriggingComponentName = action.NodeName;
                    args.TriggingComponentSubURL = action.SubNodeURL;
                    args.TriggingStringValue = action.TextValue;
                    args.TriggingNumberValue = action.NumberValue;
                    args.TriggingBoolValue = action.BooleanValue;
                    api.TestAndDoAction(args);
                });
                api.Listen(gui, handler, (z, h) => z.OnDataChanged += h, (z, h) => z.OnDataChanged -= h);
            }
        }
        [TriggingArg("触发的窗体")] public HostGUIComponent TriggingForm(EventArguments args) => args.TriggingForm;
        [TriggingArg("触发的控件")] public HostGUIComponent TriggingComponent(EventArguments args) => args.TriggingComponent;
        [TriggingArg("触发的控件名字")] public string TriggingComponentName(EventArguments args) => args.TriggingComponentName;
        [TriggingArg("触发的控件URL")] public string TriggingComponentSubURL(EventArguments args) => args.TriggingComponentSubURL;

        [TriggingArg("绑定的String")] public string TextValue(EventArguments args) => args.TriggingStringValue;
        [TriggingArg("绑定的Number")] public double NumberValue(EventArguments args) => args.TriggingNumberValue;
        [TriggingArg("绑定的Bool")] public bool BooleanValue(EventArguments args) => args.TriggingBoolValue;
    }

    #endregion
    //----------------------------------------------------------------------------------------

}


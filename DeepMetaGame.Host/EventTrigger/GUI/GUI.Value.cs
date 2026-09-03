using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Formula;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepCore.GUI.Data;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.Game3D.Host.EventTrigger.UI
{
}


namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("[GUI]")]
    public abstract class GUIValue : ZoneAbstractValue<HostGUIComponent>
    {
        [Desc("没有GUI", "[GUI]/值")]
        public class NA : GUIValue
        {
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return null;
            }
        }
        [Desc("绑定的UI窗体", "[GUI]/功能")]
        public class BindingForm : GUIValue
        {
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.FormAPI;
            }
        }

        [Desc("绑定的UI窗体子控件", "[GUI]/功能")]
        public class BindingEditChild : GUIValue
        {
            [Desc("子控件名")]
            [UINodeName]
            public string NodeName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("绑定的UI窗体子控件'{0}'", NodeName);
            }
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (api.FormAPI != null && api.FormAPI.GetChild(NodeName) is HostGUINode node)
                {
                    return node;
                }
                return null;
            }
        }


        [Desc("最后打开的UI窗体", "[GUI]/功能")]
        public class LastShownForm : GUIValue
        {
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastShownForm;
            }
        }
        [Desc("触发的UI窗体", "[GUI]/功能")]
        public class TriggingForm : GUIValue
        {
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingForm;
            }
        }
        [Desc("触发的UI控件", "[GUI]/功能")]
        public class TriggingComponent : GUIValue
        {
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingComponent;
            }
        }

        [Desc("子控件", "[GUI]/功能")]
        public class ChildComponent : GUIValue
        {
            [Desc("父节点")]
            public AbstractValue<HostGUIComponent> Parent = new GUIValue.BindingForm();
            [Desc("子控件名")]
            [UEComponentName]
            public string ComponentName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})的子控件'{1}'", Parent, ComponentName);
            }
            protected override HostGUIComponent GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                if (Parent?.GetValueAs(api, args) is HostGUIComponent parent)
                {
                    return parent.GetChild(ComponentName);
                }
                return null;
            }
        }


    }

    public class GUIExt
    {

        [Desc("(扩展)子控件名", "[GUI]/扩展")]
        public class GUIResComponentName : ZoneStringValue
        {
            [Desc("(扩展)子控件名")]
            [ResourceID(ResourceType.GUIComponent)]
            public string SubNameURL;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat(SubNameURL);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return SubNameURL;
            }
        }
        [Desc("(扩展)窗体名", "[GUI]/扩展")]
        public class GUIResFormName : ZoneStringValue
        {
            [Desc("(扩展)窗体件名")]
            [ResourceID(ResourceType.GUIForm)]
            public string FormNameURL;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat(FormNameURL);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return FormNameURL;
            }
        }

        [Desc("节点中的(扩展)子控件名", "[GUI]/扩展")]
        public class GUISubNameURL : ZoneStringValue
        {
            [Desc("UI节点")]
            public AbstractValue<HostGUIComponent> Node = new GUIValue.BindingEditChild();
            [Desc("(扩展)子控件名")]
            [ResourceID(ResourceType.GUIComponent)]
            public string SubNameURL;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", Node, SubNameURL);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return SubNameURL;
            }
        }

    }

    public class GUIFields
    {
        [Desc("GUI控件名字", "[GUI]/字段")]
        public class GUIComponentName : ZoneStringValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Name", GUI);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return node.Name;
                }
                return null;
            }
        }


        //-------------------------------------------------------------------------------------------------------------------
        #region GUI控件数据
        [Desc("GUI控件文本", "[GUI]/字段")]
        public class GUIComponentText : ZoneStringValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Text", GUI);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return node.StringValue;
                }
                return null;
            }
        }
        [Desc("GUI控件文本整数", "[GUI]/字段")]
        public class GUIComponentTextAsInt : ZoneIntegerValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Text整数", GUI);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node && Parser.TryParseInt(node.StringValue, out var v))
                {
                    return v;
                }
                return 0;
            }
        }
        [Desc("GUI控件文本小数", "[GUI]/字段")]
        public class GUIComponentTextAsFloat : ZoneRealValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Text小数", GUI);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node && Parser.TryParseFloat(node.StringValue, out var v))
                {
                    return v;
                }
                return 0;
            }
        }
        [Desc("GUI控件文本布尔", "[GUI]/字段")]
        public class GUIComponentTextAsBool : ZoneBooleanValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Text布尔", GUI);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node && bool.TryParse(node.StringValue, out var v))
                {
                    return v;
                }
                return false;
            }
        }
        [Desc("GUI控件布尔", "[GUI]/字段")]
        public class GUIComponentBool : ZoneBooleanValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.GetBool()", GUI);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return node.BoolValue;
                }
                return false;
            }
        }
        [Desc("GUI控件整数", "[GUI]/字段")]
        public class GUIComponentInt : ZoneIntegerValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.GetInt()", GUI);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return (int)node.NumberValue;
                }
                return 0;
            }
        }
        [Desc("GUI控件小数", "[GUI]/字段")]
        public class GUIComponentReal : ZoneRealValue
        {
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingEditChild();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.GetFloat()", GUI);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return node.NumberValue;
                }
                return 0;
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------
        #region GUI字段

        [Desc("GUI字段(string)", "[GUI]/字段")]
        public class GUIFieldStringValue : ZoneStringValue
        {
            [GetObjectMemberName(typeof(HostGUIComponent), typeof(string))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o != null)
                {
                    return api.NameSpace.GetValueAs<string>(o, FieldName);
                }
                return null;
            }
        }

        [Desc("GUI字段(int)", "[GUI]/字段")]
        public class GUIFieldIntegerValue : ZoneIntegerValue
        {
            [GetObjectMemberName(typeof(HostGUIComponent), typeof(int))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o != null)
                {
                    return api.NameSpace.GetValueAs<int>(o, FieldName);
                }
                return 0;
            }
        }

        [Desc("GUI字段(float)", "[GUI]/字段")]
        public class GUIFieldRealValue : ZoneRealValue
        {
            [GetObjectMemberName(typeof(HostGUIComponent), typeof(float))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o != null)
                {
                    return api.NameSpace.GetValueAs<float>(o, FieldName);
                }
                return 0;
            }
        }

        [Desc("GUI字段(bool)", "[GUI]/字段")]
        public class GUIFieldBoolValue : ZoneBooleanValue
        {
            [GetObjectMemberName(typeof(HostGUIComponent), typeof(bool))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o != null)
                {
                    return api.NameSpace.GetValueAs<bool>(o, FieldName);
                }
                return false;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
        [Desc("GUI Meta字段(string)", "[GUI]/字段")]
        public class GUIMetaFieldStringValue : ZoneStringValue
        {
            [GetObjectMemberName(typeof(UEComponentMeta), typeof(string))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return api.NameSpace.GetValueAs<string>(node.Meta, FieldName);
                }
                return null;
            }
        }

        [Desc("GUI Meta字段(int)", "[GUI]/字段")]
        public class GUIMetaFieldIntegerValue : ZoneIntegerValue
        {
            [GetObjectMemberName(typeof(UEComponentMeta), typeof(int))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return api.NameSpace.GetValueAs<int>(node.Meta, FieldName);
                }
                return 0;
            }
        }

        [Desc("GUI Meta字段(float)", "[GUI]/字段")]
        public class GUIMetaFieldRealValue : ZoneRealValue
        {
            [GetObjectMemberName(typeof(UEComponentMeta), typeof(float))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return api.NameSpace.GetValueAs<float>(node.Meta, FieldName);
                }
                return 0;
            }
        }

        [Desc("GUI Meta字段(bool)", "[GUI]/字段")]
        public class GUIMetaFieldBoolValue : ZoneBooleanValue
        {
            [GetObjectMemberName(typeof(UEComponentMeta), typeof(bool))]
            [Desc("字段名")]
            public string FieldName = "";
            [Desc("UI控件")]
            public AbstractValue<HostGUIComponent> GUI = new GUIValue.BindingForm();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.{1}", GUI, FieldName);
            }
            protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var o = GUI.GetValueAs(api, args);
                if (o is HostGUINode node)
                {
                    return api.NameSpace.GetValueAs<bool>(node.Meta, FieldName);
                }
                return false;
            }
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------------------

    }

    //-------------------------------------------------------------------------------------------------------------------
    [Desc("触发的对话框结果", "[GUI]")]
    public class DialogResult : ZoneStringValue
    {
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.DialogResult;
        }
    }

    [Desc("触发的控件名", "[GUI]")]
    public class TriggingComponentName : ZoneStringValue
    {
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingComponentName;
        }
    }

    [Desc("触发的(扩展)控件名", "[GUI]")]
    public class TriggingComponentSubURL : ZoneStringValue
    {
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return args.TriggingComponentSubURL;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------
    [Desc("UI窗体模板ID", "[GUI]")]
    public class GUIFormTemplateID : ZoneIntegerValue
    {
        [Desc("物品")]
        public AbstractValue<HostGUIComponent> Form = new GUIValue.BindingForm();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("窗体({0})模板ID", Form);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var o = Form.GetValueAs(api, args);
            if (o is HostGUIForm form)
            {
                return form.Info.ID;
            }
            return 0;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------


    [Desc("GUI比较", "[GUI]")]
    public class GUIComponentCompare : ZoneBooleanValue
    {
        [Desc("UI控件A")]
        public AbstractValue<HostGUIComponent> A = new GUIValue.BindingEditChild();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("UI控件B")]
        public AbstractValue<HostGUIComponent> B = new GUIValue.BindingEditChild();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }

    [Desc("(扩展)控制器UI控件名字比较", "[GUI]")]
    public class GUISubUrlCheck : ZoneBooleanValue
    {
        [Desc("触发(扩展)的UI控制器名字")]
        public AbstractValue<string> controlName = new StringValue.VALUE("text");
        [Desc("(扩展)控制器")]
        [ResourceID(ResourceType.GUIController)]
        public string SubNameURL;

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("触发控件名字({0}是否是{1} ));", controlName, SubNameURL);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return SubNameURL == controlName.GetValueAs(api, args);
        }
    }

    [Desc("IF GUI 比较", "[GUI]")]
    public class IFGUIAction : IFAction<HostGUIComponent>
    {
        [Desc("UI控件A")]
        public AbstractValue<HostGUIComponent> A = new GUIValue.BindingEditChild();
        [Desc("比较符")]
        public ObjectComparisonOP Op = ObjectComparisonOP.EQUAL;
        [Desc("UI控件B")]
        public AbstractValue<HostGUIComponent> B = new GUIValue.BindingEditChild();
        protected override void GetCompareText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0} {1} {2}", A, Op, B);
        }
        protected override bool Compare(EventExecutor api, IEventArguments args)
        {
            var a = A.GetValueAs(api, args);
            var b = B.GetValueAs(api, args);
            return FormulaHelper.Compare(a, Op, b);
        }
    }
}

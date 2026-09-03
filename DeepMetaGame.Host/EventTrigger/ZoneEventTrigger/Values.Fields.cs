using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class ObjectFieldStringValue<T> : ZoneStringValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>(api.ZoneAPI, FName);
        }
    }

    public abstract class ObjectFieldIntegerValue<T> : ZoneIntegerValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>(api.ZoneAPI, FName);
        }
    }

    public abstract class ObjectFieldLongValue<T> : ZoneNumberValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>(api.ZoneAPI, FName);
        }
    }

    public abstract class ObjectFieldRealValue<T> : ZoneRealValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>(api.ZoneAPI, FName);
        }
    }

    public abstract class ObjectFieldDoubleValue<T> : ZoneNumberValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>(api.ZoneAPI, FName);
        }
    }

    public abstract class ObjectFieldBoolValue<T> : ZoneBooleanValue
    {
        abstract protected string CName { get; }
        abstract protected string FName { get; }
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", CName, FName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>(api.ZoneAPI, FName);
        }
    }

    //-----------------------------------------------------------------------------------------

    #region __Config__

    [Desc("配置字段(string)", "[游戏]/配置字段")]
    public class ConfigFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(Config), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>(api.ZoneAPI.CFG, FieldName);
        }
    }

    [Desc("配置字段(int)", "[游戏]/配置字段")]
    public class ConfigFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(Config), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>(api.ZoneAPI.CFG, FieldName);
        }
    }

    [Desc("配置字段(float)", "[游戏]/配置字段")]
    public class ConfigFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(Config), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>(api.ZoneAPI.CFG, FieldName);
        }
    }

    [Desc("配置字段(long)", "[游戏]/配置字段")]
    public class ConfigFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(Config), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>(api.ZoneAPI.CFG, FieldName);
        }
    }

    [Desc("配置字段(double)", "[游戏]/配置字段")]
    public class ConfigFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(Config), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>(api.ZoneAPI.CFG, FieldName);
        }
    }
    [Desc("配置字段(bool)", "[游戏]/配置字段")]
    public class ConfigFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(Config), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "配置", FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>(api.ZoneAPI.CFG, FieldName);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __场景实体__

    [Desc("场景字段(string)", "[游戏]/场景字段")]
    public class ZoneFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>(api.ZoneAPI, FieldName);
        }
    }

    [Desc("场景字段(int)", "[游戏]/场景字段")]
    public class ZoneFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>(api.ZoneAPI, FieldName);
        }
    }

    [Desc("场景字段(long)", "[游戏]/场景字段")]
    public class ZoneFieldLongValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>(api.ZoneAPI, FieldName);
        }
    }

    [Desc("场景字段(float)", "[游戏]/场景字段")]
    public class ZoneFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>(api.ZoneAPI, FieldName);
        }
    }

    [Desc("场景字段(double)", "[游戏]/场景字段")]
    public class ZoneFieldDoubleValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>(api.ZoneAPI, FieldName);
        }
    }

    [Desc("场景字段(bool)", "[游戏]/场景字段")]
    public class ZoneFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(InstanceZone), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", "场景", FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>(api.ZoneAPI, FieldName);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __单位实体__

    [Desc("单位字段(string)", "[游戏]/单位/字段")]
    public class UnitFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("单位字段(int)", "[游戏]/单位/字段")]
    public class UnitFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位字段(float)", "[游戏]/单位/字段")]
    public class UnitFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位字段(long)", "[游戏]/单位/字段")]
    public class UnitFieldLongValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位字段(double)", "[游戏]/单位/字段")]
    public class UnitFieldDoubleValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位字段(bool)", "[游戏]/单位/字段")]
    public class UnitFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(InstanceUnit), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();


        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Unit, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __物品实体__

    [Desc("物品字段(string)", "[游戏]/物品/字段")]
    public class ItemFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("物品字段(int)", "[游戏]/物品/字段")]
    public class ItemFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品字段(float)", "[游戏]/物品/字段")]
    public class ItemFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }



    [Desc("物品字段(long)", "[游戏]/物品/字段")]
    public class ItemFieldLongValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品字段(double)", "[游戏]/物品/字段")]
    public class ItemFieldDoubleValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品字段(bool)", "[游戏]/物品/字段")]
    public class ItemFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(InstanceItem), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<InstanceItem> Item = new ItemValue.NA();

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceItem o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }

    #endregion


    //-----------------------------------------------------------------------------------------


    //-------------------------------------------------------------------------------------------------------------------------------

    #region __场景模板__

    [Desc("场景模板字段(string)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<string>(api.ZoneAPI.Data, FieldName);
        }
    }

    [Desc("场景模板字段(int)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<int>(api.ZoneAPI.Data, FieldName);
        }
    }

    [Desc("场景模板字段(float)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<float>(api.ZoneAPI.Data, FieldName);
        }
    }

    [Desc("场景模板字段(long)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldLongValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<long>(api.ZoneAPI.Data, FieldName);
        }
    }

    [Desc("场景模板字段(double)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldDoubleValue : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<double>(api.ZoneAPI.Data, FieldName);
        }
    }


    [Desc("场景模板字段(bool)", "[游戏]/场景模板/字段")]
    public class SceneTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(SceneData), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("场景.模板.{0}", FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            return api.NameSpace.GetValueAs<bool>(api.ZoneAPI.Data, FieldName);
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __单位模板__

    [Desc("单位模板字段(string)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o.TemplateData, FieldName);
            }
            return null;
        }
    }

    [Desc("单位模板字段(int)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o.TemplateData, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位模板字段(float)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o.TemplateData, FieldName);
            }
            return 0;
        }
    }


    [Desc("单位模板字段(long)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o.TemplateData, FieldName);
            }
            return 0;
        }
    }

    [Desc("单位模板字段(double)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o.TemplateData, FieldName);
            }
            return 0;
        }
    }


    [Desc("单位模板字段(bool)", "[游戏]/单位模板/字段")]
    public class UnitTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(UnitInfo), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.模板.{1}", Unit, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit o = Unit.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o.TemplateData, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __物品模板__

    [Desc("物品模板字段(string)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("物品模板字段(int)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品模板字段(float)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("物品模板字段(long)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("物品模板字段(double)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("物品模板字段(bool)", "[游戏]/物品模板/字段")]
    public class ItemTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(ItemTemplate), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("物品")]
        public AbstractValue<ItemTemplate> Item = new ItemTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Item, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            ItemTemplate o = Item.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __技能模板__

    [Desc("技能模板字段(string)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("技能模板字段(int)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能模板字段(float)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("技能模板字段(long)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("技能模板字段(double)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("技能模板字段(bool)", "[游戏]/技能模板/字段")]
    public class SkillTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(SkillTemplate), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("技能")]
        public AbstractValue<SkillTemplate> Skill = new SkillTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Skill, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SkillTemplate o = Skill.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }

    #endregion

    //-----------------------------------------------------------------------------------------

    #region __法术模板__

    [Desc("法术模板字段(string)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("法术模板字段(int)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术模板字段(float)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术模板字段(long)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("法术模板字段(double)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }
    [Desc("法术模板字段(bool)", "[游戏]/法术模板/字段")]
    public class SpellTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(SpellTemplate), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("法术")]
        public AbstractValue<SpellTemplate> Spell = new SpellTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Spell, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            SpellTemplate o = Spell.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

    #region __BUFF模板__

    [Desc("BUFF模板字段(string)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("BUFF模板字段(int)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF模板字段(float)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("BUFF模板字段(long)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF模板字段(double)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("BUFF模板字段(bool)", "[游戏]/BUFF模板/字段")]
    public class BuffTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(BuffTemplate), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("BUFF")]
        public AbstractValue<BuffTemplate> Buff = new BuffTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Buff, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            BuffTemplate o = Buff.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

    #region __Aura模板__

    [Desc("光环模板字段(string)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldStringValue : ZoneStringValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(string))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override string GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<string>(o, FieldName);
            }
            return null;
        }
    }

    [Desc("光环模板字段(int)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldIntegerValue : ZoneIntegerValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(int))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<int>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环模板字段(float)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldRealValue : ZoneRealValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(float))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<float>(o, FieldName);
            }
            return 0;
        }
    }


    [Desc("光环模板字段(long)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldIntegerValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(long))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<long>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环模板字段(double)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldRealValue64 : ZoneNumberValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(double))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<double>(o, FieldName);
            }
            return 0;
        }
    }

    [Desc("光环模板字段(bool)", "[游戏]/光环模板/字段")]
    public class AuraTemplateFieldBoolValue : ZoneBooleanValue
    {
        [GetObjectMemberName(typeof(AuraTemplate), typeof(bool))]
        [Desc("字段名")]
        public string FieldName = "";
        [Desc("光环")]
        public AbstractValue<AuraTemplate> Aura = new AuraTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}.{1}", Aura, FieldName);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            AuraTemplate o = Aura.GetValueAs(api, args);
            if (o != null)
            {
                return api.NameSpace.GetValueAs<bool>(o, FieldName);
            }
            return false;
        }
    }


    #endregion

    //-----------------------------------------------------------------------------------------

}

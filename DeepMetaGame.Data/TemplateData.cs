using DeepCore;
using DeepCore.EventTrigger;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepMetaGame.Data
{



    [TableClass(nameof(ID))]
    [Reflectible]
    public abstract class TemplateData : ISerializable, IFuncTemplateData, IPropertiesOwner, IComparable<TemplateData>
    {
        public string TemplateID => ID.ToString();
        public string TemplateName { get => Name; set => Name = value; }
        IFuncTableGroup IFuncData.Tables { get => FuncID; set => FuncID = (FuncTableGroup)value; }
        public abstract IPropertiesData PropertiesData { get; }

        [Desc("ID", "0.模板", Editable = false)]
        public int ID;

        [LocalizationText]
        [Desc("名字", "0.模板")]
        public string Name;

        [Desc("图标", "0.模板")]
        [ResourceID(ResourceType.Image)]
        public string IconName;

        [Desc("注释", "0.模板")]
        public string Comment;

        [ColorValue]
        [Desc(Category = "0.模板", Desc = "Color(ARGB)", Editable = true)]
        public int ColorARGB = 0;//ColorValueAttribute.COLOR_GREEN;

        [Desc(Editable = false)]
        public FuncTableGroup FuncID;

        [Desc(Category = "9.扩展", Desc = "用户自定义标志")]
        public string UserTag;

        [Desc(Category = "9.扩展", Desc = "自定义字段")]
        public string[] Attributes;

        [Desc(Editable = false)]
        public string EditorPath;

        sealed public override string ToString()
        {
            return ID + "-" + Name;
        }

        public int CompareTo(TemplateData other) => this.ID.CompareTo(other.ID);

        public static implicit operator bool(in TemplateData value)
        {
            return value != null;
        }

        [Desc(Desc = "是否源生模板", Editable = false)] public bool IsOriginal { get; set; } = false;
    }

    public abstract class CustomEventTemplateData : TemplateData, IEventsTemplateData
    {
        [Desc("是否包含脚本", "0.模板")]
        public bool HasEvent => CustomEvents != null && CustomEvents.Count > 0;
        public IReadOnlyList<IEventDataNode> EventDataNodes => CustomEvents.ConvertAll(t => (IEventDataNode)t);
        //--------------------------------------------------------------------------------------------
        [Desc(Category = "1.基础", Desc = "所有事件", Editable = false)]
        public ArrayList<UnitCustomEvent> CustomEvents = new ArrayList<UnitCustomEvent>();
        //--------------------------------------------------------------------------------------------
    }

    public interface IEventsTemplateData : ISerializable
    {
        bool HasEvent { get; }
        IReadOnlyList<IEventDataNode> EventDataNodes { get; }
    }

    public interface IPropertiesData : ISerializable
    {

    }

    public interface IPropertiesOwner
    {
        public IPropertiesData PropertiesData { get; }
    }

    public abstract class ISNData : SerialData, IFuncData, IAfterExternalizable
    {
        IFuncTableGroup IFuncData.Tables { get => FuncID; set => FuncID = (FuncTableGroup)value; }


        [Desc(Editable = false)]
        [XmlSerializable()]
        public FuncTableGroup FuncID;

        public virtual void AfterWrite(IOutputStream output)
        {
            output.WriteFuncID(this.FuncID);
        }
        public virtual void AfterRead(IInputStream input)
        {
            this.FuncID = input.ReadFuncID();
        }
        public override string ToString()
        {
            if (GetType().TryGetAttribute<DescAttribute>(out var desc))
            {
                return desc.Desc;
            }
            return base.ToString();
        }
        public static implicit operator bool(in ISNData value)
        {
            return value != null;
        }
    }

    public abstract class IBaseFuncData : ISerializable, IFuncData, IAfterExternalizable
    {
        IFuncTableGroup IFuncData.Tables { get => FuncID; set => FuncID = (FuncTableGroup)value; }

        [Desc(Editable = false)]
        [XmlSerializable()]
        public FuncTableGroup FuncID;
        public virtual void AfterWrite(IOutputStream output)
        {
            output.WriteFuncID(this.FuncID);
        }
        public virtual void AfterRead(IInputStream input)
        {
            this.FuncID = input.ReadFuncID();
        }
        public override string ToString()
        {
            if (GetType().TryGetAttribute<DescAttribute>(out var desc))
            {
                return desc.Desc;
            }
            return base.ToString();
        }
    }
    [Expandable]
    public abstract class IDataAbility : IBaseFuncData, IComparable
    {
        public static implicit operator bool(in IDataAbility value)
        {
            return value != null;
        }
        public int CompareTo(object other)
        {
            if (other.GetType().TryGetAttribute<DescAttribute>(out var ad) && this.GetType().TryGetAttribute<DescAttribute>(out var bd))
            {
                return bd.Desc.CompareTo(ad.Desc);
            }
            if (other.GetType().TryGetAttribute<MessageTypeAttribute>(out var ar) && this.GetType().TryGetAttribute<MessageTypeAttribute>(out var br))
            {
                return br.MessageTypeID - ar.MessageTypeID;
            }
            return this.GetType().Name.CompareTo(other.GetType().Name);
        }
    }

    //     public static class EventData
    //     {
    // 
    //         public static bool HasCustomEvent<ET>(this ICollection<ET> events) 
    //         {  
    //             return events != null && events.Count > 0; 
    //         }
    //     }

    //     //[MessageType(0x800001)]
    //     public class DataAbilities<A> : DataComponentCollection<A>//, ISerializable
    //       where A : IDataAbility
    //     {
    //     }

    //     public abstract class ITemplateData : IExternalizable, IFuncTemplateData, ICloneable
    //     {
    //         string IFuncTemplateData.TemplateID { get => TemplateID.ToString(); }
    //         public abstract int TemplateID { get; }
    //         public abstract string TemplateName { get; }
    // 
    //         [Desc("功能ID", "A.功能")]
    //         [XmlSerializable()] public FuncTable FuncID { get; set; }
    // 
    //         [XmlSerializable()] public string EditorPath { get; set; }
    //
    //         sealed public override string ToString()
    //         {
    //             return TemplateID + "-" + TemplateName;
    //         }
    //         public virtual void WriteExternal(IOutputStream output)
    //         {
    //             output.PutUTF(EditorPath);
    //             output.WriteFuncID(this.FuncID);
    //         }
    //         public virtual void ReadExternal(IInputStream input)
    //         {
    //             this.EditorPath = input.GetUTF();
    //             this.FuncID = input.ReadFuncID();
    //         }
    //         public abstract object Clone();
    //     }
    //     public abstract class ISNData : SerialData, IFuncData, IExternalizable
    //     {
    //         [Desc("功能ID", "功能")]
    //         [XmlSerializable()]
    //         public FuncTable FuncID { get; set; }
    // 
    //         public virtual void WriteExternal(IOutputStream output)
    //         {
    //             output.WriteFuncID(this.FuncID);
    //         }
    //         public virtual void ReadExternal(IInputStream input)
    //         {
    //             this.FuncID = input.ReadFuncID();
    //         }
    //     }


}

using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data
{
    //-----------------------------------------------------------

    /// <summary>
    /// Int32 ARGB
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ColorValueAttribute : System.Attribute
    {
        public static readonly int COLOR_GREEN = FromARGB(0xff, 0x00, 0xff, 0x00);
        public static readonly int COLOR_LIGHT_GREEN = FromARGB(0xff, 0x80, 0xff, 0x80);
        public static readonly int COLOR_DARK_GRAY = FromARGB(0xff, 0x40, 0x40, 0x40);
        public static readonly int COLOR_RED = FromARGB(0xff, 0xff, 0x00, 0x00);
        public static readonly int COLOR_LIGHT_BLUE = FromARGB(0xff, 0x80, 0x80, 0xff);
        public static readonly int COLOR_YELLOW = FromARGB(0xff, 0xff, 0xff, 0);
        public static readonly int COLOR_OLIVE = FromARGB(0xff, 0x80, 0x80, 0);
        public static readonly int COLOR_BLUE = FromARGB(0xff, 0, 0, 0xff);

        public static int FromARGB(float a, float r, float g, float b)
        {
            int ARGB = 0;
            ARGB |= ((int)CMath.Clamp(a * 255, 0, 255)) << 24;
            ARGB |= ((int)CMath.Clamp(r * 255, 0, 255)) << 16;
            ARGB |= ((int)CMath.Clamp(g * 255, 0, 255)) << 8;
            ARGB |= ((int)CMath.Clamp(b * 255, 0, 255));
            return ARGB;
        }
        public static int FromARGB(int a, int r, int g, int b)
        {
            int ARGB = 0;
            ARGB |= ((int)CMath.Clamp(a, 0, 255)) << 24;
            ARGB |= ((int)CMath.Clamp(r, 0, 255)) << 16;
            ARGB |= ((int)CMath.Clamp(g, 0, 255)) << 8;
            ARGB |= ((int)CMath.Clamp(b, 0, 255));
            return ARGB;
        }
        public static void ToARGB(int Color, out float a, out float r, out float g, out float b)
        {
            a = ((Color & 0xFF000000L) >> 24) / 255.0f;
            r = ((Color & 0x00FF0000) >> 16) / 255.0f;
            g = ((Color & 0x0000FF00) >> 8) / 255.0f;
            b = ((Color & 0x000000FF) >> 0) / 255.0f;
        }
    }

    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field Int32 字段为 类型的模板ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TemplateIDAttribute : System.Attribute
    {
        private Type templateType;

        public TemplateIDAttribute(Type templateType)
        {
            this.templateType = templateType;
        }

        public Type TemplateType
        {
            get { return templateType; }
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class TemplateGroupAttribute : System.Attribute
    {
        private Type templateType;

        public TemplateGroupAttribute(Type templateType)
        {
            this.templateType = templateType;
        }
        public Type TemplateType
        {
            get { return templateType; }
        }
    }

    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field Int32 字段为 UnitInfo 模板ID的等级
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TemplateLevelAttribute : System.Attribute
    {
        public TemplateLevelAttribute()
        {
        }
    }

    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 数组或List字段为 Int32 类型的模板ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TemplatesIDAttribute : System.Attribute
    {
        private Type templateType;

        public TemplatesIDAttribute(Type templateType)
        {
            this.templateType = templateType;
        }

        public Type TemplateType
        {
            get { return templateType; }
        }
    }

    //-----------------------------------------------------------
    public enum ResourceType
    {
        Any             /**/= -1,

        Image           /**/= 0x0001,
        Object          /**/= 0x0002,
        Scene           /**/= 0x0004,
        Effect          /**/= 0x0008,

        Sound           /**/= 0x0010,
        Sound_Effect    /**/= 0x0020,
        Sound_Ambient   /**/= 0x0040,
        Sound_BGM       /**/= 0x0080,
        Sound_UI        /**/= 0x0800,

        Animation       /**/= 0x0100,
        Binary          /**/= 0x0200,
        Text            /**/= 0x0400,

        GUIForm         /**/= 0x1000,
        GUIComponent    /**/= 0x2000,
        GUIController   /**/= 0x4000,

        Object_Effect = Object | Effect,
        Sound_All = Sound | Sound_Effect | Sound_Ambient | Sound_BGM | Sound_UI,
    }


    /// <summary>
    /// 标识 Field 字段为资源文件
    /// </summary>
    [AttributeUsage(AttributeTargets.Field| AttributeTargets.Property)]
    public class ResourceIDAttribute : System.Attribute
    {
        public ResourceType ResType { get; }
        public ResourceIDAttribute(ResourceType resType)
        {
            ResType = resType;
        }
    }



    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为场景单位 Name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneObjectIDAttribute : System.Attribute
    {
        private Type objectType;
        public SceneObjectIDAttribute(Type objectType)
        {
            this.objectType = objectType;
        }

        public Type ObjectType
        {
            get { return objectType; }
        }
    }



    [AttributeUsage(AttributeTargets.Field)]
    public class SceneObjectGroupAttribute : System.Attribute
    {
        public SceneObjectGroupAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SceneSpacePositionAttribute : System.Attribute
    {
    }

    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为场景事件触发 Name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneEventIDAttribute : System.Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneEventGroupAttribute : System.Attribute
    {
    }
    //-----------------------------------------------------------
    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为单位事件触发 Name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class UnitEventIDAttribute : System.Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class UnitEventGroupAttribute : System.Attribute
    {
    }
    //-----------------------------------------------------------
    //     /// <summary>
    //     /// 标识 Field 字段为场景环境变量 Key
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class SceneVarIDAttribute : System.Attribute
    //     {
    //         public readonly Type VarType;
    // 
    //         public SceneVarIDAttribute(Type varType)
    //         {
    //             this.VarType = varType;
    //         }
    //     }
    //     /// <summary>
    //     /// 标识 Field 字段为临时变量 Key
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class LocalVarIDAttribute : System.Attribute
    //     {
    //         public readonly Type VarType;
    //         public LocalVarIDAttribute(Type varType)
    //         {
    //             this.VarType = varType;
    //         }
    // 
    //     }

    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为 脚本文件
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneScriptIDAttribute : System.Attribute
    {
    }


    //     //-----------------------------------------------------------
    //     /// <summary>
    //     /// 标记对象字段名字，用于AbstractValue取Field
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class ObjectMemberNameAttribute : System.Attribute
    //     {
    //         public readonly Type BaseOwnerType;
    //         public readonly Type FieldType;
    //         public ObjectMemberNameAttribute(Type objType, Type fieldType)
    //         {
    //             this.BaseOwnerType = objType;
    //             this.FieldType = fieldType;
    //         }
    //     }




    //-----------------------------------------------------------
    /// <summary>
    /// 标记对象字段名字，用于AbstractValue取QuestID
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QuestIDAttribute : System.Attribute
    {
        public QuestIDAttribute()
        {
        }
    }

    //-----------------------------------------------------------

    //     /// <summary>
    //     /// 标记 event 是否可以在编辑时反射成事件触发器
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Field)]
    //     public class EventTriggerDescAttribute : System.Attribute
    //     {
    //         public string Description { get; private set; }
    //         public EventTriggerDescAttribute(string desc)
    //         {
    //             this.Description = desc;
    //         }
    //     }

    //-----------------------------------------------------------

    [AttributeUsage(AttributeTargets.Field)]
    public class UINodeNameAttribute : System.Attribute
    {
    }


    //-----------------------------------------------------------
}

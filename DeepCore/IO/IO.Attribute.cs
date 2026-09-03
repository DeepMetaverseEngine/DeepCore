using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepCore.IO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreGenerateAttribute : System.Attribute
    {
    }

    /// <summary>
    /// 标记类型ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MessageTypeAttribute : System.Attribute
    {
        public int MessageTypeID { get; private set; }
        public MessageTypeAttribute(int messageType)
        {
            this.MessageTypeID = messageType;
        }
        public MessageTypeAttribute(object messageType)
        {
            this.MessageTypeID = Convert.ToInt32(messageType);
        }
    }


    //     /// <summary>
    //     /// 标记字段按照StructMapping存储
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    //     public class PersistStructFieldAttribute : System.Attribute
    //     {
    //         public PersistStructFieldAttribute() { }
    //     }

    //-------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 用于代码生成器标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GenCodeIDAttribute : System.Attribute
    {
        public Type MessageType { get; }
        public int MessageTypeID { get; }
        public GenCodeIDAttribute(Type messageType, int messageID)
        {
            this.MessageType = messageType;
            this.MessageTypeID = messageID;
        }
    }
    /// <summary>
    /// 用于代码生成器标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GenCodeCreateAttribute : System.Attribute
    {
        public Type MessageType { get; }
        public int MessageTypeID { get; }
        public GenCodeCreateAttribute(Type messageType, int messageID)
        {
            this.MessageType = messageType;
            this.MessageTypeID = messageID;
        }
    }
    /// <summary>
    /// 用于代码生成器标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GenCodeReadAttribute : System.Attribute
    {
        public int MessageTypeID { get; }
        public GenCodeReadAttribute(int messageID)
        {
            this.MessageTypeID = messageID;
        }
    }
    /// <summary>
    /// 用于代码生成器标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GenCodeWriteAttribute : System.Attribute
    {
        public int MessageTypeID { get; }
        public GenCodeWriteAttribute(int messageID)
        {
            this.MessageTypeID = messageID;
        }
    }
    /// <summary>
    /// 用于代码生成器标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class GenCodeCloneAttribute : System.Attribute
    {
        public int MessageTypeID { get; }
        public GenCodeCloneAttribute(int messageID)
        {
            this.MessageTypeID = messageID;
        }
    }


    //     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    //     public class CryptoAttribute : System.Attribute
    //     {
    //     }
    //      
    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class IgnoreParseMetadataAttribute : Attribute
    //     {
    // 
    //     }
}

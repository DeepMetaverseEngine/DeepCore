using System;

namespace DeepCore.FuncData
{
    //--------------------------------------------------------------------------------------------------------------------------

    //     /// <summary>
    //     /// 标记该字段取值为FuncID
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class FuncKeyFieldAttribute : System.Attribute
    //     {
    //     }
    // 
    //     /// <summary>
    //     /// 用于选择Func表ID,Level对应关系
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    //     public class OwnerFuncIDAttribute : Attribute
    //     {
    //     }
    // 
    //     /// <summary>
    //     /// 表示该集合元素可由Func表填充，保存时自动灌入数据
    //     /// </summary>
    //     [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field |AttributeTargets.Property, AllowMultiple = false)]
    //     public class FillFromFuncIDAttribute : Attribute
    //     {
    //         public Type ElementType { get; }
    //         public FillFromFuncIDAttribute(Type elementType)
    //         {
    //             this.ElementType = elementType;
    //         }
    //     }


    [AttributeUsage(AttributeTargets.Field)]
    public class PrimitiveFuncTypeAttribute : System.Attribute
    {
    }
    //--------------------------------------------------------------------------------------------------------------------------
}

using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.EventTrigger
{
    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为事件触发 Name
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EventIDAttribute : System.Attribute
    {
    }
    //-----------------------------------------------------------
    /// <summary>
    /// 标记 event 是否可以在编辑时反射成事件触发器
    /// </summary>
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Field)]
    public class EventTriggerDescAttribute : System.Attribute
    {
        public string Description { get; private set; }
        public EventTriggerDescAttribute(string desc)
        {
            this.Description = desc;
        }
    }
    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为场景环境变量 Key
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EnvironmentVarIDAttribute : System.Attribute
    {
        public readonly Type VarType;

        public EnvironmentVarIDAttribute(Type varType)
        {
            this.VarType = varType;
        }
    }
    //-----------------------------------------------------------
    /// <summary>
    /// 标识 Field 字段为临时变量 Key
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocalVarTypeAttribute : System.Attribute
    {
        public readonly Type VarType;
        public LocalVarTypeAttribute(Type varType)
        {
            this.VarType = varType;
        }
    }
    //-----------------------------------------------------------
    /// <summary>
    /// 标记对象字段名字，用于AbstractValue取Field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GetObjectMemberNameAttribute : System.Attribute
    {
        public readonly Type BaseOwnerType;
        public readonly Type FieldType;
        public GetObjectMemberNameAttribute(Type objType, Type fieldType)
        {
            this.BaseOwnerType = objType;
            this.FieldType = fieldType;
        }
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class SetObjectMemberNameAttribute : System.Attribute
    {
        public readonly Type BaseOwnerType;
        public readonly Type FieldType;
        public SetObjectMemberNameAttribute(Type objType, Type fieldType)
        {
            this.BaseOwnerType = objType;
            this.FieldType = fieldType;
        }
    }
    //-----------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class)]
    public class StereoOptionAttribute : System.Attribute
    {
        public Type InputType { get; }
        public Type OutputType { get; }
        public string InputName { get; }
        public string OutputName { get; }
        public StereoOptionAttribute(Type inputType, string inputName, Type outputType, string outputName)
        {
            InputType = inputType;
            OutputType = outputType;
            InputName = inputName;
            OutputName = outputName;
        }
    }
    //-----------------------------------------------------------
    [AttributeUsage(AttributeTargets.Method)]
    public class TriggingArgAttribute : System.Attribute
    {
        public string Desc { get; }
        public TriggingArgAttribute(string desc)
        {
            Desc = desc;
        }
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ReturnValueAttribute : System.Attribute
    {
        public string Desc { get; }
        public ReturnValueAttribute(string desc)
        {
            Desc = desc;
        }
    }
    //-----------------------------------------------------------
}

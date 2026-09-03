using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using System;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class ZoneAbstractValue<T> : AbstractValue<T>
    {
        //         sealed public override void ToFunctionText(DeepCore.EventTrigger.EventStringBuilder sw)
        //         {
        //             this.ToFunctionText(new EventStringBuilder(sw));
        //         }
        //         public virtual void ToFunctionText(EventStringBuilder sw) { }
        sealed protected override T GetValue(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.GetValue(api as IEventTriggerAdapter, (EventArguments)args);
        }
        protected abstract T GetValue(IEventTriggerAdapter api, EventArguments args);
    }
//     public abstract class ZoneAbstractArrayValue<T> : AbstractArrayValue<T>
//     {
//         sealed protected override T[] GetValue(EventExecutor api, IEventArguments args)
//         {
//             return this.GetValue(api as IEditorValueAdapter, (EventArguments)args);
//         }
//         protected abstract T[] GetValue(IEditorValueAdapter api, EventArguments args);
//     }
    //     public static class AbstractValueTypeMap
    //     {
    //         private static TypeDescAttribute[] types = new TypeDescAttribute[]{
    //                 new TypeDescAttribute(typeof(StringValue)),
    //                 new TypeDescAttribute(typeof(IntegerValue)),
    //                 new TypeDescAttribute(typeof(RealValue)),
    //                 new TypeDescAttribute(typeof(BooleanValue)),
    //                 new TypeDescAttribute(typeof(UnitValue)),
    //                 new TypeDescAttribute(typeof(ItemValue)),
    //                 new TypeDescAttribute(typeof(FlagValue)),
    //                 new TypeDescAttribute(typeof(PositionValue)),
    //                 new TypeDescAttribute(typeof(ItemTemplateValue)),
    //                 new TypeDescAttribute(typeof(BuffTemplateValue)),
    //                 new TypeDescAttribute(typeof(AuraTemplateValue)),
    //             };
    // 
    //         public static TypeDescAttribute[] DescTypes
    //         {
    //             get { return types; }
    //         }
    // 
    //         public static TypeDescAttribute GetBaseValueType(Type type)
    //         {
    //             foreach (TypeDescAttribute baseType in types)
    //             {
    //                 if (baseType.DataType.IsAssignableFrom(type))
    //                 {
    //                     return baseType;
    //                 }
    //             }
    //             return null;
    //         }
    // 
    //         public static object MakeDefault(Type desc)
    //         {
    //             if (desc.Equals(typeof(StringValue)))
    //                 return new StringValue.VALUE();
    //             if (desc.Equals(typeof(IntegerValue)))
    //                 return new IntegerValue.VALUE();
    //             if (desc.Equals(typeof(RealValue)))
    //                 return new RealValue.VALUE();
    //             if (desc.Equals(typeof(BooleanValue)))
    //                 return new BooleanValue.VALUE();
    // 
    //             if (desc.Equals(typeof(UnitValue)))
    //                 return new UnitValue.NA();
    //             if (desc.Equals(typeof(ItemValue)))
    //                 return new ItemValue.NA();
    //             if (desc.Equals(typeof(FlagValue)))
    //                 return new FlagValue.NA();
    //             if (desc.Equals(typeof(PositionValue)))
    //                 return new PositionValue.VALUE();
    //             if (desc.Equals(typeof(ItemTemplateValue)))
    //                 return new ItemTemplateValue.Template();
    //             if (desc.Equals(typeof(AuraTemplate)))
    //                 return new AuraTemplateValue.Template();
    // 
    //             return null;
    //         }
    //     }
    // 
    //     //-------------------------------------------------------------------------------------
    // 
    // 
    //     public static class GameFields
    //     {
    //         private static Type[] s32_types = new Type[] { typeof(int), typeof(sbyte), typeof(short), typeof(byte), typeof(ushort), typeof(uint), };
    //         private static Type[] f32_types = new Type[] { typeof(float), typeof(double), };
    //         private static FieldManager s_Manager;
    //         public static FieldManager Manager { get { return s_Manager; } }
    //         internal static void InitFiledManager()
    //         {
    //             if (s_Manager == null)
    //             {
    //                 s_Manager = new FieldManager();
    //                 AddObjectType(typeof(Config));
    //                 AddObjectType(typeof(UnitInfo));
    //                 AddObjectType(typeof(ItemTemplate));
    //                 AddObjectType(typeof(SkillTemplate));
    //                 AddObjectType(typeof(SpellTemplate));
    //                 AddObjectType(typeof(BuffTemplate));
    //                 AddObjectType(typeof(AuraTemplate));
    //                 foreach (Type type in ReflectionUtil.GetAllTypes())
    //                 {
    //                     if (IsAssignableType(type, typeof(ICommonConfig))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IUnitProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(ISkillProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(ISpellProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IItemProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IBuffProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IAuraProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(ISceneProperties))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IZone))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(InstanceUnit))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(InstanceItem))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IVirtualUnit))) AddObjectType(type);
    //                     if (IsAssignableType(type, typeof(IZoneUnitComponent))) AddObjectType(type);
    //                 }
    //             }
    //         }
    //         internal static bool IsAssignableType(Type type, Type baseType)
    //         {
    //             return (type.IsClass && type.IsPublic && !type.IsAbstract && baseType.IsAssignableFrom(type));
    //         }
    //         public static void AddObjectType(Type objType)
    //         {
    //             try
    //             {
    //                 s_Manager.AddFieldsMap(new FieldsMap(objType, typeof(int), s32_types));
    //                 s_Manager.AddFieldsMap(new FieldsMap(objType, typeof(float), f32_types));
    //                 s_Manager.AddFieldsMap(new FieldsMap(objType, typeof(string)));
    //                 s_Manager.AddFieldsMap(new FieldsMap(objType, typeof(bool)));
    //             }
    //             catch
    //             {
    //                 throw;
    //             }
    //         }
    //         public static Type[] GetCompatibilityTypes(Type type)
    //         {
    //             if (new List<Type>(s32_types).Contains(type)) return s32_types;
    //             if (new List<Type>(f32_types).Contains(type)) return f32_types;
    //             return new Type[] { };
    //         }
    //         public static T GetValue<T>(object owner, string fieldname)
    //         {
    //             var type = owner.GetType();
    //             var fm = s_Manager.GetFields(type, typeof(T));
    //             if (fm != null)
    //             {
    //                 return fm.GetValueAs<T>(owner, fieldname);
    //             }
    //             return default(T);
    //         }
    // 
    //     }
}

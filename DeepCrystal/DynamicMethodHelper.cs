using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DeepCrystal
{
    //-------------------------------------------------------------------------------------------------------------------------------

    public delegate object DynamicMethodInvoker(object target, object[] paramters);
    public delegate object DynamicGetField(object target);
    public delegate void DynamicSetField(object target, object value);
    public delegate object DynamicConstractor();

    //-------------------------------------------------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------------------------------------------------
    public class DynamicMethodTypeFactory : DynamicTypeFactory
    {
        private static Logger log = new LazyLogger("DynamicMethodTypeFactory");
        new public static DynamicMethodTypeFactory Instance { get; private set; } = new DynamicMethodTypeFactory();
        private HashMap<Type, DynamicTypeInfo> types = new HashMap<Type, DynamicTypeInfo>();
        public DynamicMethodTypeFactory()
        {
            DynamicMethodTypeFactory.Instance = this;
        }
        public override IDynamicTypeInfo GetTypeInfo(Type type)
        {
            if (types.TryGetValue(type, out var ret))
            {
                return ret;
            }
            if (!IsDynamicType(type))
            {
                return null;
            }
            lock (types)
            {
                return types.GetOrAdd(type, static t => new DynamicTypeInfo(t));
            }
        }
        public override bool CreateFieldInfo(FieldInfo field, out IDynamicFieldInfo info)
        {
            info = new DynamicFieldInfo(field);
            return true;
        }
        public override bool CreatePropertyInfo(PropertyInfo field, out IDynamicPropertyInfo info)
        {
            info = new DynamicPropertyInfo(field);
            return true;
        }
        public override bool CreateMethodInfo(MethodInfo field, out IDynamicMethodInfo info)
        {
            info = new DynamicMethodInfo(field);
            return true;
        }

        class DynamicTypeInfo : IDynamicTypeInfo
        {
            private HashMap<string, IDynamicFieldInfo> fields;
            private HashMap<string, IDynamicPropertyInfo> properties;
            private HashMap<string, IDynamicMethodInfo> methods;
            private IDynamicFieldInfo[] fields_list;
            private IDynamicPropertyInfo[] properties_list;
            private IDynamicMethodInfo[] methods_list;
            public Type DataType { get; private set; }
            public DynamicConstractor Constractor { get; private set; }
            public DynamicTypeInfo(Type type)
            {
                this.DataType = type;
                this.Constractor = DynamicMethodHelper.CreateConstractor(type);
                {
                    this.fields_list = DynamicMethodTypeFactory.Instance.GetFields(type).ToArray();
                    this.fields = new HashMap<string, IDynamicFieldInfo>();
                    foreach (var f in this.fields_list)
                    {
                        if (!fields.TryAdd(f.Name, f))
                        {
                            log.Warn($"重复的字段：{type.FullName}.{f.Name}");
                        }
                    }
                }
                {
                    this.properties_list = DynamicMethodTypeFactory.Instance.GetProperties(type).ToArray();
                    this.properties = new HashMap<string, IDynamicPropertyInfo>();
                    foreach (var p in this.properties_list)
                    {
                        if (!properties.TryAdd(p.Name, p))
                        {
                            log.Warn($"重复的属性：{type.FullName}.{p.Name}");
                        }
                    }
                }
                {
                    this.methods_list = DynamicMethodTypeFactory.Instance.GetMethods(type).ToArray();
                    this.methods = new HashMap<string, IDynamicMethodInfo>();
                    foreach (var m in this.methods_list)
                    {
                        if (!methods.TryAdd(m.Name, m))
                        {
                            log.Warn($"重复的方法：{type.FullName}.{m.Name}");
                        }
                    }
                }
            }
            public object CreateInstance(params object[] args)
            {
                return Constractor.Invoke();
            }
            public object CreateInstance()
            {
                return Constractor.Invoke();
            }
            public IDynamicFieldInfo[] GetFields()
            {
                return fields_list;
            }
            public IDynamicFieldInfo GetField(string fieldName)
            {
                return this.fields.Get(fieldName);
            }
            public IDynamicPropertyInfo[] GetProperties()
            {
                return properties_list;
            }
            public IDynamicPropertyInfo GetProperty(string fieldName)
            {
                return this.properties.Get(fieldName);
            }
            public IDynamicMethodInfo[] GetMethods()
            {
                return methods_list;
            }
            public IDynamicMethodInfo GetMethod(string fieldName)
            {
                return this.methods.Get(fieldName);
            }
        }
        abstract class DynamicMemberInfo<T> : IDynamicMemberInfo<T> where T : MemberInfo
        {
            private IDynamicTypeInfo ftype;
            private bool ftype_get = false;
            public bool IsDynamicFieldType { get; }
            public T Field { get; }
            public MemberInfo Member { get; }
            public Type MemberType { get; }
            public abstract bool CanRead { get; }
            public abstract bool CanWrite { get; }
            public abstract DynamicGetField Get { get; }
            public abstract DynamicSetField Set { get; }
            public string Name { get { return Field.Name; } }
            public IDynamicTypeInfo DynamicType
            {
                get
                {
                    if (!ftype_get)
                    {
                        lock (this)
                        {
                            if (!ftype_get)
                            {
                                ftype_get = true;
                                ftype = DynamicTypeFactory.Instance.GetTypeInfo(MemberType);
                            }
                        }
                    }
                    return ftype;
                }
            }
            public DynamicMemberInfo(T field, Type fieldType)
            {
                this.IsDynamicFieldType = DynamicTypeFactory.Instance.IsDynamicType(fieldType);
                this.Field = field;
                this.Member = field;
                this.MemberType = fieldType;
            }
            public virtual object GetValue(object target)
            {
                return CanRead ? Get.Invoke(target) : null;
            }
            public virtual void SetValue(object target, object fieldValue)
            {
                if (CanWrite) Set.Invoke(target, fieldValue);
            }
            public override string ToString()
            {
                return Field.Name;
            }
        }
        class DynamicFieldInfo : DynamicMemberInfo<FieldInfo>, IDynamicFieldInfo
        {
            public override bool CanRead { get => true; }
            public override bool CanWrite { get => true; }
            public override DynamicGetField Get { get; }
            public override DynamicSetField Set { get; }
            public DynamicFieldInfo(FieldInfo field) : base(field, field.FieldType)
            {
                this.Get = DynamicMethodHelper.CreateGetField(field.DeclaringType, field);
                this.Set = DynamicMethodHelper.CreateSetField(field.DeclaringType, field);
            }
        }
        class DynamicPropertyInfo : DynamicMemberInfo<PropertyInfo>, IDynamicPropertyInfo
        {
            public override bool CanRead { get => Field.CanRead; }
            public override bool CanWrite { get => Field.CanWrite; }
            public override DynamicGetField Get { get; }
            public override DynamicSetField Set { get; }
            public DynamicPropertyInfo(PropertyInfo field) : base(field, field.PropertyType)
            {
                this.Get = CanRead ? DynamicMethodHelper.CreateGetProperty(field.DeclaringType, field) : null;
                this.Set = CanWrite ? DynamicMethodHelper.CreateSetProperty(field.DeclaringType, field) : null;
            }
        }
        class DynamicMethodInfo : DynamicMemberInfo<MethodInfo>, IDynamicMethodInfo
        {
            public override bool CanRead { get; }
            public override bool CanWrite { get; }
            public override DynamicGetField Get { get; }
            public override DynamicSetField Set { get; }
            public DynamicMethodInvoker Invoker { get; }
            public DynamicMethodInfo(MethodInfo field) : base(field, field.ReturnType != null ? field.ReturnType : field.GetParameters()[0].ParameterType)
            {
                this.CanRead = field.ReturnType != typeof(void);
                this.CanWrite = field.GetParameters().Length > 0;
                this.Get = CanRead ? DynamicMethodHelper.CreateGetMethod(field.DeclaringType, field) : null;
                this.Set = CanWrite ? DynamicMethodHelper.CreateSetMethod(field.DeclaringType, field) : null;
                this.Invoker = DynamicMethodHelper.GetMethodInvoker(field);
            }
            public object Invoke(object owner, params object[] args)
            {
                return Invoker.Invoke(owner, args);
            }
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------

    public static class DynamicMethodHelper
    {
        public static bool USE_DYNAMIC_METHOD { get; set; } = true;
        private static object[] zero_args = new object[0];
        //---------------------------------------------------------------------------------------------------
        public static DynamicMethodInvoker GetMethodInvoker(Type type, string method)
        {
            var methodInfo = type.GetMethod(method);
            if (methodInfo != null) return GetMethodInvoker(methodInfo);
            return null;
        }
        public static DynamicGetField CreateGetField(Type type, string field)
        {
            var fieldInfo = type.GetField(field);
            if (fieldInfo != null) return CreateGetField(type, fieldInfo);
            return null;
        }
        public static DynamicSetField CreateSetField(Type type, string field)
        {
            var fieldInfo = type.GetField(field);
            if (fieldInfo != null) return CreateSetField(type, fieldInfo);
            return null;
        }
        public static DynamicGetField CreateGetProperty(Type type, string field)
        {
            var fieldInfo = type.GetProperty(field);
            if (fieldInfo != null) return CreateGetProperty(type, fieldInfo);
            return null;
        }
        public static DynamicSetField CreateSetProperty(Type type, string field)
        {
            var fieldInfo = type.GetProperty(field);
            if (fieldInfo != null) return CreateSetProperty(type, fieldInfo);
            return null;
        }
        //---------------------------------------------------------------------------------------------------
        public static DynamicMethodInvoker GetMethodInvoker(MethodInfo methodInfo)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return GetMethodInvokerInternal(methodInfo);
            }
            else
            {
                return new DynamicMethodInvoker((target, paramters) =>
                {
                    return Call_DynamicMethodInvoker(methodInfo, target, paramters);
                });
            }
        }

        public static DynamicGetField CreateGetMethod(Type type, MethodInfo fieldInfo)
        {
            var invoker = GetMethodInvoker(fieldInfo);
            return new DynamicGetField((o) => invoker(o, zero_args));
        }
        public static DynamicSetField CreateSetMethod(Type type, MethodInfo fieldInfo)
        {
            var invoker = GetMethodInvoker(fieldInfo);
            return new DynamicSetField((o, v) => invoker(o, new object[] { v }));
        }

        public static DynamicGetField CreateGetField(Type type, FieldInfo fieldInfo)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return CreateGetFieldInternal(type, fieldInfo);
            }
            else
            {
                return new DynamicGetField((target) => { return Call_DynamicGetField(fieldInfo, target); });
            }
        }
        public static DynamicSetField CreateSetField(Type type, FieldInfo fieldInfo)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return CreateSetFieldInternal(type, fieldInfo);
            }
            else
            {
                return new DynamicSetField((target, value) => { Call_DynamicSetField(fieldInfo, target, value); });
            }
        }
        public static DynamicGetField CreateGetProperty(Type type, PropertyInfo fieldInfo)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return CreateGetPropertyInternal(type, fieldInfo);
            }
            else
            {
                return new DynamicGetField((target) => { return Call_DynamicGetProperty(fieldInfo, target); });
            }
        }
        public static DynamicSetField CreateSetProperty(Type type, PropertyInfo fieldInfo)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return CreateSetPropertyInternal(type, fieldInfo);
            }
            else
            {
                return new DynamicSetField((target, value) => { Call_DynamicSetProperty(fieldInfo, target, value); });
            }
        }
        public static DynamicConstractor CreateConstractor(Type type)
        {
            if (USE_DYNAMIC_METHOD)
            {
                return CreateConstractorInternal(type);
            }
            else
            {
                return new DynamicConstractor(() => { return DeepActivator.CreateInstance(type); });
            }
        }
        //---------------------------------------------------------------------------------------------------
        public static DynamicMethodInvoker CompileCSharp(string code, string method = null, string clazz = null)
        {
            try
            {
                var cp = System.CodeDom.Compiler.CodeDomProvider.CreateProvider("CSharp");
                var pa = new System.CodeDom.Compiler.CompilerParameters();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    pa.ReferencedAssemblies.Add(asm.FullName);
                }
                pa.GenerateExecutable = false;
                pa.GenerateInMemory = true;

                var cr = cp.CompileAssemblyFromSource(pa, code);
                if (cr.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder("csc error");
                    foreach (System.CodeDom.Compiler.CompilerError err in cr.Errors)
                    {
                        sb.AppendLine(err.ErrorText);
                    }
                    sb.ToString();
                    throw new Exception(sb.ToString());
                }
                var objAssembly = cr.CompiledAssembly;
                var types = objAssembly.GetTypes();
                if (types.Length == 0)
                {
                    throw new Exception("No Class");
                }
                var objHelloWorld = clazz != null ? objAssembly.CreateInstance(clazz) : types[0];
                var methods = objHelloWorld.GetType().GetMethods();
                if (methods.Length == 0)
                {
                    throw new Exception("No Method in " + objHelloWorld.GetType().FullName);
                }
                var methodInfo = method != null ? objHelloWorld.GetType().GetMethod(method) : methods[0];
                return GetMethodInvoker(methodInfo);
            }
            catch (Exception err)
            {
                throw new Exception("Compile Error:\n" + code, err);
            }
        }
        //-------------------------------------------------------------------------------------------------------------------------------
        #region Internal

        private static DynamicMethodInvoker GetMethodInvokerInternal(MethodInfo methodInfo)
        {
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] ps = methodInfo.GetParameters();
            Type[] paramTypes = new Type[ps.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                else
                    paramTypes[i] = ps[i].ParameterType;
            }
            LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i], true);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
            if (methodInfo.IsStatic)
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            il.Emit(OpCodes.Ret);
            DynamicMethodInvoker invoder = (DynamicMethodInvoker)dynamicMethod.CreateDelegate(typeof(DynamicMethodInvoker));
            return invoder;
        }

        private static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        private static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        private static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        // CreateInstantiateObjectDelegate
        private static DynamicConstractor CreateConstractorInternal(Type type)
        {
            ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
            if (constructorInfo == null)
            {
                throw new ApplicationException(string.Format("The type {0} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).", type));
            }

            DynamicMethod dynamicMethod = new DynamicMethod("InstantiateObject", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object), null, type, true);
            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Newobj, constructorInfo);
            generator.Emit(OpCodes.Ret);
            return (DynamicConstractor)dynamicMethod.CreateDelegate(typeof(DynamicConstractor));
        }

        // CreateGetDelegate
        private static DynamicGetField CreateGetPropertyInternal(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Call, getMethodInfo);
            BoxIfNeeded(getMethodInfo.ReturnType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (DynamicGetField)dynamicGet.CreateDelegate(typeof(DynamicGetField));
        }
        // CreateSetDelegate
        private static DynamicSetField CreateSetPropertyInternal(Type type, PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
            setGenerator.Emit(OpCodes.Call, setMethodInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (DynamicSetField)dynamicSet.CreateDelegate(typeof(DynamicSetField));
        }

        // CreateGetDelegate
        private static DynamicGetField CreateGetFieldInternal(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicGet = CreateGetDynamicMethod(type);
            ILGenerator getGenerator = dynamicGet.GetILGenerator();

            getGenerator.Emit(OpCodes.Ldarg_0);
            getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            BoxIfNeeded(fieldInfo.FieldType, getGenerator);
            getGenerator.Emit(OpCodes.Ret);

            return (DynamicGetField)dynamicGet.CreateDelegate(typeof(DynamicGetField));
        }
        // CreateSetDelegate
        private static DynamicSetField CreateSetFieldInternal(Type type, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicSet = CreateSetDynamicMethod(type);
            ILGenerator setGenerator = dynamicSet.GetILGenerator();

            setGenerator.Emit(OpCodes.Ldarg_0);
            setGenerator.Emit(OpCodes.Ldarg_1);
            UnboxIfNeeded(fieldInfo.FieldType, setGenerator);
            setGenerator.Emit(OpCodes.Stfld, fieldInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (DynamicSetField)dynamicSet.CreateDelegate(typeof(DynamicSetField));
        }




        // CreateGetDynamicMethod
        private static DynamicMethod CreateGetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicGet", typeof(object), new Type[] { typeof(object) }, type, true);
        }

        // CreateSetDynamicMethod
        private static DynamicMethod CreateSetDynamicMethod(Type type)
        {
            return new DynamicMethod("DynamicSet", typeof(void), new Type[] { typeof(object), typeof(object) }, type, true);
        }

        // BoxIfNeeded
        private static void BoxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        // UnboxIfNeeded
        private static void UnboxIfNeeded(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, type);
            }
        }

        private static object Call_DynamicMethodInvoker(MethodInfo methodInfo, object target, object[] paramters)
        {
            return methodInfo.Invoke(target, paramters);
        }
        private static object Call_DynamicGetField(FieldInfo fieldInfo, object target)
        {
            return fieldInfo.GetValue(target);
        }
        private static void Call_DynamicSetField(FieldInfo fieldInfo, object target, object value)
        {
            fieldInfo.SetValue(target, value);
        }
        private static object Call_DynamicGetProperty(PropertyInfo fieldInfo, object target)
        {
            return fieldInfo.GetGetMethod().Invoke(target, new object[] { });
        }
        private static void Call_DynamicSetProperty(PropertyInfo fieldInfo, object target, object value)
        {
            fieldInfo.GetSetMethod().Invoke(target, new object[] { value });
        }
        #endregion

    }

    //-------------------------------------------------------------------------------------------------------------------------------
}

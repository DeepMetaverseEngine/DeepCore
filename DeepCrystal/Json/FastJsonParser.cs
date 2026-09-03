using DeepCore;
using DeepCore.Json;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeepCrystal.Json
{
    public class FastJsonParser : JsonParser
    {
        public FastJsonParser() { }
        public FastJsonParser(JsonFormat format) : base(format) { }

        private HashMap<Type, IDynamicTypeInfo> types_info = new HashMap<Type, IDynamicTypeInfo>();
        private HashMap<string, Type> types_name = new HashMap<string, Type>();


        protected override StringBuilder CreateStringBuilder()
        {
            return new StringBuilder(1024);
        }
        protected override IDynamicTypeInfo GetTypeInfo(Type type)
        {
            if (types_info.TryGetValue(type, out var ret))
            {
                return ret;
            }
            lock (types_name)
            {
                types_name.TryAdd(TypeToString(type), type);
            }
            lock (types_info)
            {
                return types_info.GetOrAdd(type, static t =>
                {
                    var tret = DynamicMethodTypeFactory.Instance.GetTypeInfo(t);
                    return tret;
                });
            }
        }
        protected override Type GetType(string name)
        {
            if (types_name.TryGetValue(name, out var ret))
            {
                return ret;
            }
            lock (types_name)
            {
                return types_name.GetOrAdd(name, static t =>
                {
                    var tret = ReflectionUtil.GetType(t);
                    return tret;
                });
            }
        }

    }
}

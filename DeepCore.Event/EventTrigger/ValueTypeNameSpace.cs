using DeepCore.AI.LLM;
using DeepCore.EventTrigger.Data;
using DeepCore.EventTrigger.Data.AI;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace DeepCore.EventTrigger
{
    public class ValueTypeNameSpace
    {
        public static ValueTypeNameSpace Instance { get; private set; }
        //-----------------------------------------------------------------------------------------------------------------------------------------
        #region BaseType

        private List<ValueTypeDefine> typeValues = new List<ValueTypeDefine>();
        private HashMap<Type, ValueTypeDefine> types = new HashMap<Type, ValueTypeDefine>();
        public class ValueTypeDefine
        {
            public readonly TypeDescAttribute ValueType;
            public readonly Type DataType;
            public readonly Func<Type, object> CreateDefault;
            public string Alias;
            public uint ColorARGB;
            public ValueTypeDefine(Type dataType, Type baseType, Func<Type, object> createDefault, string alias, uint colorARGB)
            {
                this.ValueType = new TypeDescAttribute(baseType);
                this.DataType = dataType;
                this.CreateDefault = createDefault;
                this.Alias = alias;
                this.ColorARGB = colorARGB;
            }
            public override string ToString()
            {
                return Alias ?? ValueType.Desc?.Desc ?? ValueType.OwnerType.Name;
            }
        }
        public ValueTypeNameSpace()
        {
            Instance = this;
            RegistValueType(typeof(double),    /**/(t) => new IntegerValue.VALUE(0),           /**/"Number", Colors.ARGB.DeepSkyBlue);//
            RegistValueType(typeof(bool),      /**/(t) => new BooleanValue.VALUE(false),       /**/"Boolean", Colors.ARGB.Orchid);//
            RegistValueType(typeof(string),    /**/(t) => new StringValue.VALUE(string.Empty), /**/"String", Colors.ARGB.LimeGreen);//
            RegistValueType(typeof(LLMAgent),  /**/(t) => new LLMAgentValue.Binding(),         /**/"AI", Colors.ARGB.DeepPink);//
        }
        public ValueTypeDefine RegistValueType(Type dataType, Func<Type, object> createDefault, string alias, uint colorARGB)
        {
            var baseType = typeof(AbstractValue<>).MakeGenericType(dataType);
            //var arrayType = typeof(AbstractArrayValue<>).MakeGenericType(dataType);
            //var defaultArrayType = typeof(ArrayValue<,>).MakeGenericType(baseType, dataType);
            var define = new ValueTypeDefine(dataType, baseType, createDefault, alias, colorARGB);
            typeValues.Add(define);
            types.Add(baseType, define);
            //types.Add(arrayType, new ValueTypeDefine(dataType, arrayType, expect, defaultArrayType));
            return define;
        }
        public IReadOnlyList<ValueTypeDefine> ValueTypes
        {
            get { return typeValues; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueType"> typeof AbstractValue </param>
        /// <returns></returns>
        public ValueTypeDefine GetValueType(Type valueType)
        {
            foreach (var baseType in ValueTypes)
            {
                if (baseType.ValueType.OwnerType.IsAssignableFrom(valueType))
                {
                    return baseType;
                }
            }
            return null;
        }
        public ValueTypeDefine GetBaseValueType(Type valueType)
        {
            do
            {
                if (types.TryGetValue(valueType, out var ret))
                {
                    return ret;
                }
                valueType = valueType.BaseType;
            } while (valueType != null);
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"> dataType is AbstractValue<dataType> </param>
        /// <returns></returns>
        public ValueTypeDefine GetValueTypeWithDataType(Type dataType)
        {
            foreach (var baseType in ValueTypes)
            {
                if (baseType.DataType.IsAssignableFrom(dataType))
                {
                    return baseType;
                }
            }
            foreach (var baseType in ValueTypes)
            {
                if (baseType.ValueType.OwnerType.IsAssignableFrom(dataType))
                {
                    return baseType;
                }
            }
            return null;
        }

        public virtual object MakeDefault(Type desc)
        {
            var descattr = GetBaseValueType(desc);
            if (descattr != null)
            {
                return descattr.CreateDefault(desc);
            }
            return null;
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------
        #region Fields
        private static Type[] fields_types = new Type[]
         {
                typeof(int), typeof(sbyte), typeof(short), typeof(byte), typeof(ushort), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double),
                typeof(string),
                typeof(bool),
         };
        private static Type[] number_types = new Type[]
        {
            typeof(int), typeof(sbyte), typeof(short), typeof(byte), typeof(ushort), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
        };
        private FieldManager s_Manager = new FieldManager();
        public FieldManager FieldsManager { get => s_Manager; }
        public void RegistFields(bool inherit, params Type[] dataTypes)
        {
            if (inherit)
            {
                var classTypes = ReflectionUtil.RegisteredTypes.Where(type => (type.IsPublic || type.IsNestedPublic) && type.IsClass && !type.IsAbstract && !type.IsInterface);
                foreach (var dataType in dataTypes)
                {
                    var subTypes = classTypes.Where(ctype => IsAssignableType(ctype, dataType));
                    foreach (var cType in subTypes)
                    {
                        if (IsAssignableType(cType, dataType))
                        {
                            RegistFields(cType);
                        }
                    }
                }
            }
            else
            {
                foreach (var dataType in dataTypes)
                {
                    RegistFields(dataType);
                }
            }
        }
        public bool IsAssignableType(Type type, Type baseType)
        {
            return baseType.IsAssignableFrom(type);
        }
        public void RegistFields(Type objType)
        {
            s_Manager.AddFieldsType(new FieldsType(objType, fields_types));
        }
        public static Type[] GetCompatibilityTypes(Type type)
        {
            if (number_types.Contains(type)) return number_types;
            //if (new List<Type>(f32_types).Contains(type)) return f32_types;
            return new Type[] { };
        }
        public void SetValue<T>(object owner, string fieldname, T value)
        {
            var type = owner.GetType();
            var fm = s_Manager.GetFields(type);
            if (fm != null)
            {
                fm.SetValue(owner, fieldname, value);
            }
        }

        public T GetValueAs<T>(object owner, string fieldname)
        {
            var type = owner.GetType();
            var fm = s_Manager.GetFields(type);
            if (fm != null)
            {
                return fm.GetValueAs<T>(owner, fieldname);
            }
            return default(T);
        }
        public object GetValue(object owner, string fieldname)
        {
            var type = owner.GetType();
            var fm = s_Manager.GetFields(type);
            if (fm != null && fm.TryGetValue(owner, fieldname, out var ret))
            {
                return ret;
            }
            return null;
        }
        public bool TryGetValueAs<T>(object owner, string fieldname, out T ret)
        {
            var type = owner.GetType();
            var fm = s_Manager.GetFields(type);
            if (fm != null && fm.TryGetValueAs(owner, fieldname, out ret))
            {
                return true;
            }
            ret = default(T);
            return false;
        }
        public bool TryGetValue(object owner, string fieldname, Type fieldType, out object ret)
        {
            var type = owner.GetType();
            var fm = s_Manager.GetFields(type);
            if (fm != null && fm.TryGetValue(owner, fieldname, out ret))
            {
                return true;
            }
            ret = null;
            return false;
        }
        #endregion
        //-----------------------------------------------------------------------------------------------------------------------------------------
    }


}

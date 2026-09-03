using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DeepCore.Reflection
{
    public class DeepConvert
    {
        public static T ConvertTo<T>(object src)
        {
            try
            {
                return (T)Convert.ChangeType(src, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
    public class DeepActivator
    {
        public static DeepActivator Instance { get; private set; } = new DeepActivator();

        private HashMap<Type, CreateFunc> createTables = new();

        public delegate object CreateFunc(Type type, object[] args);

        public DeepActivator()
        {
            Instance = this;
            OnInit();
            if (createTables.Count > 0)
            {
                ReflectionUtil.InitRuntimeTypes(createTables.Keys);
            }
        }
        protected virtual void OnInit() { }
        public void Regist(Type type, CreateFunc func = null)
        {
            createTables.Put(type, func);
        }   
        protected virtual object InnerCreateInstance(Type type, object[] args)
        {
            if (createTables.TryGetValue(type, out var func) && func != null)
            {
                return func(type, args);
            }
            return Activator.CreateInstance(type, args);
        }
        protected virtual object InnerCreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (createTables.TryGetValue(type, out var func) && func != null)
            {
                return func(type, args);
            }
            else
            {
                return Activator.CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
            }
        }

        //------------------------------------------------------------------------------------------
        #region API
        private static object[] zero_args = new object[0];
        public static object CreateInstance(Type type)
            => Instance.InnerCreateInstance(type, zero_args);
        public static object CreateInstance(Type type, params object[] args)
            => Instance.InnerCreateInstance(type, args);
        public static T CreateInstance<T>()
            => (T)Instance.InnerCreateInstance(typeof(T), zero_args);
        public static T CreateInstance<T>(params object[] args)
            => (T)Instance.InnerCreateInstance(typeof(T), args);
        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture)
            => Instance.InnerCreateInstance(type, bindingAttr, binder, args, culture, null);
        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
            => Instance.InnerCreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
        #endregion
        //------------------------------------------------------------------------------------------
    }
}

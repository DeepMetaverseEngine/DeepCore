using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepCore.Lua
{
    public interface ILuaObject
    {

    }
    public interface ILuaTable : ILuaObject, IDisposable, IReadOnlyDictionary<object, object>
    {
        new object this[object key] { get; set; }
        object InnerTable { get; }
        UnionValue ToUnionValue();
        ILuaSystem System { get; }
        int Length { get; }
        IEnumerable<KeyValuePair<object, object>> Pairs { get; }
        KeyValuePair<object, object> First { get; }
        T ConvertTo<T>();
    }

    public interface ILuaFunction : ILuaObject, IDisposable
    {
        object Call(params object[] args);
        object InnerFunction { get; }
        ILuaSystem System { get; }
    }

    public interface ILuaSystem : IDisposable
    {
        /// <summary>
        /// 执行字符串代码
        /// </summary>
        /// <param name="stringCode"></param>
        /// <returns></returns>
        object DoString(string stringCode);

        /// <summary>
        /// 执行指定路径脚本
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        object DoFile(string file);

        /// <summary>
        /// 创建一个ILuaTable
        /// </summary>
        /// <returns></returns>
        ILuaTable CreateTable();

        /// <summary>
        /// 将一个lua内部Table转换成ILuaTable
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        ILuaTable CastToLuaTable(object obj);

        /// <summary>
        /// 将一个lua内部Function转换为ILuaFunction
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        ILuaFunction CastToLuaFunction(object obj);

        /// <summary>
        /// 设置lua状态机全局变量
        /// </summary>
        /// <param name="key"></param>
        /// <param name="v"></param>
        void SetGlobalValue(string key, object v);

        /// <summary>
        /// 获取lua状态机全局变量
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        object GetGlobalValue(string key);

        /// <summary>
        /// UnionValue转换成Lua内部对象
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        object UnionValueToInnerObject(UnionValue v);

        /// <summary>
        /// Lua内部对象转换为UnionValue
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        UnionValue InnerObjectToUnionValue(object obj);
        /// <summary>
        /// CLR 转换为Lua内部对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        object CLRToInnerObject(object obj);

        /// <summary>
        /// Lua内部对象转换为CLR
        /// </summary>
        /// <param name="innerObj"></param>
        /// <returns></returns>
        object InnerObjectToCLR(object innerObj);

        /// <summary>
        /// 如果t是一个lua array ，则返回array的数组，否则返回[t]
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        object[] UnpackInnerArray(object t);

        /// <summary>
        /// 格式化一个异常
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        string FormatException(Exception e);

        /// <summary>
        /// Lua 虚拟机Update
        /// </summary>
        void Update();

        /// <summary>
        /// 转换成目标LuaSystem的内部对象
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="targetSystem"></param>
        /// <returns></returns>
        object ConvertToTargetInnerObject(object obj, ILuaSystem targetSystem);
    }

    public interface ILuaTableConvertible
    {
        void ReadFromLuaTable(ILuaTable t);
        void WriteToLuaTable(ILuaTable t);
    }

    /// <summary>
    /// Adapter
    /// </summary>
    public abstract class ILuaAdapter
    {
        public static ILuaAdapter DefaultAdapter { get; private set; }
        public ILuaAdapter() { DefaultAdapter = this; }

        public abstract ILuaSystem CreateLuaSystem(Action<string> logHandler, Action<string> errorHandler, params Type[] types);
        public abstract void ClearFileCache();
        public abstract void RemoveFileCache(string file);

        public virtual byte[] GetOrLoadFileBytes(string file)
        {
            throw new NotImplementedException();
        }

        public virtual Type[] GetInnerTypes()
        {
            throw new NotImplementedException();
        }
    }

}
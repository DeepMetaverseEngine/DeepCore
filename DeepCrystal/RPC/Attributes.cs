using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.RPC
{
    /// <summary>
    /// 标记 IService 的方法为RPC调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RpcHandlerAttribute : Attribute
    {
        public Type Route { get; private set; }
        public Type Return { get; private set; }
        public bool IsHandleAny { get; private set; }
        public bool IsBinary { get; private set; }
        public bool IsReturnVoid { get => (Return == null || Return == typeof(void)); }

        public RpcHandlerAttribute(Type route, Type ret, bool isBinary)
        {
            this.Route = route;
            this.Return = ret;
            this.IsHandleAny = (route == typeof(ISerializable));
            this.IsBinary = isBinary;
        }
        public RpcHandlerAttribute(Type route, bool isBinary)
        {
            this.Route = route;
            this.Return = null;
            this.IsHandleAny = (route == typeof(ISerializable));
            this.IsBinary = isBinary;
        }
        public RpcHandlerAttribute(bool isBinary)
        {
            this.Route = typeof(ISerializable);
            this.Return = null;
            this.IsHandleAny = true;
            this.IsBinary = isBinary;
        }
        public RpcHandlerAttribute(Type route, Type ret)
        {
            this.Route = route;
            this.Return = ret;
            this.IsHandleAny = (route == typeof(ISerializable));
            this.IsBinary = false;
        }
        public RpcHandlerAttribute(Type route)
        {
            this.Route = route;
            this.Return = null;
            this.IsHandleAny = (route == typeof(ISerializable));
            this.IsBinary = false;
        }
        public RpcHandlerAttribute()
        {
            this.Route = null;
            this.Return = null;
            this.IsHandleAny = false;
            this.IsBinary = false;
        }
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class WormholeHandlerAttribute : Attribute
    {

    }
}

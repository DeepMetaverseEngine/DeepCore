using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Reflection;
using System;
using System.Reflection;

namespace DeepCore.NetClient
{
    public class NotifyInvoker : Disposable
    {
        private HashMap<Type, Invoker> handlers = new HashMap<Type, Invoker>();
        protected override void Disposing()
        {
            foreach(var handler in handlers)
            {
                handler.Value.Dispose();
            }
            handlers.Clear();
        }
        public void Regist(object owner, INetClient session)
        {
            var serviceType = owner.GetType();
            var methods = serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                try
                {
                    var attr_msg = PropertyUtil.GetAttribute<NotifyHandlerAttribute>(method);
                    if (attr_msg != null)
                    {
                        if (ValidateMethod(serviceType, attr_msg, method, out var type_route))
                        {
                            if (DynamicTypeFactory.Instance.CreateMethodInfo(method, out var invokerMethod))
                            {
                                var invoker = new Invoker(owner, type_route, attr_msg, invokerMethod, session);
                                handlers.Add(type_route, invoker);
                            }
                            else
                            {
                                throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("Method Not Validate : {0} : {1}", method.DeclaringType.FullName, method));
                        }
                    }
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("Service Rpc Handler Error : {0}.{1} : {2}", serviceType.FullName, method.Name, err.Message), err);
                }
            }
        }
        private bool ValidateMethod(Type ownerType, NotifyHandlerAttribute attr, MethodInfo method, out Type type_route)
        {
            var args = method.GetParameters();
            if (args.Length == 1)
            {
                type_route = args[0].ParameterType;
                if (method.ReturnType == typeof(void))
                {
                    return true;
                }
            }
            type_route = null;
            return false;
        }

        class Invoker : Disposable
        {
            public readonly object owner;
            public readonly IDynamicMethodInfo invokerMethod;
            public readonly IPushHandler pushHandler;
            public Invoker(object owner, Type route, NotifyHandlerAttribute attr, IDynamicMethodInfo method, INetClient session)
            {
                this.owner = owner;
                this.invokerMethod = method;
                this.pushHandler = session.Listen(route, Invoke, attr.Recursion);
            }
            protected override void Disposing()
            {
                pushHandler.Clear();
            }
            public void Invoke(ISerializable notify)
            {
                invokerMethod.Invoke(owner, notify);
            }

        }
    }
}

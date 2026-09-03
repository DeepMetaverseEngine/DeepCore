using DeepCore.Log;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using static DeepCore.Colors;
using static DeepCore.EventTrigger.Data.SwitchCaseInteger;

namespace DeepCore.EventTrigger.Data
{
    //-------------------------------------------------------------------

    [Desc("事件动作")]
    [Expandable]
    public abstract class AbstractAction : EventExternalizable
    {
        static AbstractAction()
        {
            XmlSerializer.AddDefaultConverter(((XmlSerializer ser, XmlElement dataElement, Type decleardType, Exception err, out object data, object root) =>
            {
                if (typeof(AbstractAction).IsAssignableFrom(decleardType))
                {
                    data = new CommentAction()
                    {
                        Comment = err.Message + "\n" + XmlUtil.ToXmlString(dataElement),
                    };
                    return true;
                }
                data = null;
                return false;
            }));
        }
        sealed public override Type BaseType
        {
            get => typeof(AbstractAction);
        }
        protected virtual void Disposing(EventExecutor api)
        {
        }
        protected override void GetText(EventStringBuilder sw)
        {
            if (GetType().TryGetAttribute<DescAttribute>(out var desc))
            {
                sw.Append(desc.Desc).Append(";");
            }
            else
            {
                sw.Append(GetType().Name).Append(";");
            }
        }
        public object Invoke(EventExecutor api, IEventArguments args)
        {
            if (EventExecutor.ENABLE_TRACE) api.Trace(this);
            args.ReturnValue = this.Run(api, args);
            api.InvokeActionDone(this, api, args);
            return args.ReturnValue;
        }
        public async Task<object> InvokeAsync(EventExecutor api, IEventArguments args)
        {
            if (EventExecutor.ENABLE_TRACE) api.Trace(this);
            args.ReturnValue = await this.InvokeRunAsync(api, args);
            api.InvokeActionDone(this, api, args);
            return args.ReturnValue;
        }
        protected abstract object Run(EventExecutor api, IEventArguments in_args);
        internal virtual Task<object> InvokeRunAsync(EventExecutor api, IEventArguments in_args)
        {
            var ret = Run(api, in_args);
            return Task.FromResult(ret);
        }
        internal void InvokeDispose(EventExecutor api)
        {
            //OnDone = null;
            Disposing(api);
        }
        //         internal void InvokeDone(EventExecutor api, IEventArguments in_args)
        //         {
        //             OnDone?.Invoke(api, in_args);
        //         }
        //public object ReturnValue { get; private set; }
        //public event DoActionHandler OnDone;
        //public delegate void DoActionHandler(EventExecutor api, IEventArguments args);
    }
    //-------------------------------------------------------------------
    [Desc("事件动作")]
    [Expandable]
    public abstract class AsyncAbstractAction : AbstractAction
    {
        sealed protected override object Run(EventExecutor api, IEventArguments args)
        {
            return RunAsync(api, args);
        }
        sealed internal override Task<object> InvokeRunAsync(EventExecutor api, IEventArguments in_args)
        {
            return RunAsync(api, in_args);
        }
        protected abstract Task<object> RunAsync(EventExecutor api, IEventArguments in_args);
    }
    //-------------------------------------------------------------------

    [Desc("带返回值的动作")]
    [Expandable]
    public abstract class AbstractAction<T> : AbstractAction
    {
        sealed protected override object Run(EventExecutor api, IEventArguments in_args)
        {
            return this.RunAs(api, in_args);
        }
        protected abstract T RunAs(EventExecutor api, IEventArguments in_args);
        [ReturnValue("返回值")] public T ReturnValueAs(IEventArguments args) => (T)args.API.GetReturnValue(this);
    }
    [Desc("带返回值的动作")]
    [Expandable]
    public abstract class AsyncAbstractAction<T> : AsyncAbstractAction
    {
        sealed protected override async Task<object> RunAsync(EventExecutor api, IEventArguments in_args)
        {
            return await RunAsyncAs(api, in_args);
        }
        protected abstract Task<T> RunAsyncAs(EventExecutor api, IEventArguments in_args);
        [ReturnValue("返回值")] public T ReturnValueAs(IEventArguments args) => (T)args.API.GetReturnValue(this);
    }

    //-------------------------------------------------------------------
}
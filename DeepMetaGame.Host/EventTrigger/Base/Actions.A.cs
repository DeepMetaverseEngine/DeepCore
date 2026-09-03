using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using System.Threading.Tasks;
using static DeepCore.Colors;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //-------------------------------------------------------------------
    public abstract class ZoneAbstractAction : DeepCore.EventTrigger.Data.AbstractAction
    {
        sealed protected override object Run(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.Run(api as IEventTriggerAdapter, (EventArguments)args);
        }
        abstract protected object Run(IEventTriggerAdapter api, EventArguments args);

        sealed protected override void Disposing(DeepCore.EventTrigger.EventExecutor api)
        {
            this.Disposing(api as IEventTriggerAdapter);
        }
        virtual protected void Disposing(IEventTriggerAdapter api) { }
    }
    public abstract class ZoneAsyncAbstractAction : DeepCore.EventTrigger.Data.AsyncAbstractAction
    {
        protected override Task<object> RunAsync(EventExecutor api, IEventArguments in_args)
        {
            return RunAsync(api as IEventTriggerAdapter, (EventArguments)in_args);
        }
        abstract protected Task<object> RunAsync(IEventTriggerAdapter api, EventArguments args);
        sealed protected override void Disposing(DeepCore.EventTrigger.EventExecutor api)
        {
            this.Disposing(api as IEventTriggerAdapter);
        }
        virtual protected void Disposing(IEventTriggerAdapter api) { }
    }
    public abstract class ZoneAbstractAction<T> : DeepCore.EventTrigger.Data.AbstractAction<T>
    {
        sealed protected override T RunAs(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            return this.Run(api as IEventTriggerAdapter, (EventArguments)args);
        }
        abstract protected T Run(IEventTriggerAdapter api, EventArguments args);

        sealed protected override void Disposing(DeepCore.EventTrigger.EventExecutor api)
        {
            this.Disposing(api as IEventTriggerAdapter);
        }
        virtual protected void Disposing(IEventTriggerAdapter api) { }
    }
    public abstract class ZoneAsyncAbstractAction<T> : DeepCore.EventTrigger.Data.AsyncAbstractAction<T>
    {
        protected override Task<T> RunAsyncAs(EventExecutor api, IEventArguments in_args)
        {
            return RunAsync(api as IEventTriggerAdapter, (EventArguments)in_args);
        }
        abstract protected Task<T> RunAsync(IEventTriggerAdapter api, EventArguments args);
        sealed protected override void Disposing(DeepCore.EventTrigger.EventExecutor api)
        {
            this.Disposing(api as IEventTriggerAdapter);
        }
        virtual protected void Disposing(IEventTriggerAdapter api) { }
    }
    //-------------------------------------------------------------------


    [Desc("消息框", "[游戏]")]
    public class ShowMessageBox : ZoneAbstractAction
    {
        [Desc("消息")]
        public AbstractValue<string> Message = new StringValue.VALUE("");
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("消息框({0});", Message);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.SendMessageBox(Message.GetValueAs(api, args));
            return null;
        }
    }

    [Desc("清理对象池", "[游戏]")]
    public class InvokeLowMemory : ZoneAbstractAction
    {
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            api.ZoneAPI.ObjectPool.LowMemory();
            if (api.ZoneAPI.BattleListener is LocalBattle local)
            {
                local.Layer.ObjectPool.LowMemory();
            }
            System.GC.Collect();
            return null;
        }
    }
}

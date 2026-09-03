using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepCore.EventTrigger;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    public abstract class ZoneAbstractTrigger : DeepCore.EventTrigger.Data.AbstractTrigger
    {
        //         sealed public override void ToFunctionText(DeepCore.EventTrigger.EventStringBuilder sw)
        //         {
        //             this.ToFunctionText(new EventStringBuilder(sw));
        //         }
        //         public virtual void ToFunctionText(EventStringBuilder sw) { }
        sealed protected override void Listen(DeepCore.EventTrigger.EventExecutor api, DeepCore.EventTrigger.IEventArguments args)
        {
            this.Listen(api as IEventTriggerAdapter, (EventArguments)args);
        }
        sealed protected override void Disposing(EventExecutor api)
        {
            this.Disposing(api as IEventTriggerAdapter);
        }
        protected abstract void Listen(IEventTriggerAdapter api, EventArguments args);
        protected virtual void Disposing(IEventTriggerAdapter api) { }
    }
    //-------------------------------------------------------------------


    //     [Desc("注释", "注释")]
    //     public class CommentTrigger : ZoneAbstractTrigger
    //     {
    //         [Desc("注释")]
    //         public string Comment = "注释";
    //         public override void ToFunctionText(EventStringBuilder sw)
    //         {
    //             sw.AppendFormat("<c color='" + sw.COLOR_COMMENT + "'><![CDATA[# {0}]]></c>", Comment);
    //         }
    //         protected override void Listen(IEventTriggerAdapter api, EventArguments args)
    //         {
    //         }
    //     }
}

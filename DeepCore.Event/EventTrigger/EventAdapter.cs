using DeepCore.EventTrigger.Data;
using DeepCore.Log;
using DeepCore.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static DeepCore.Colors;

namespace DeepCore.EventTrigger
{
    public interface IEventArguments : IDisposable
    {
        EventExecutor API { get; }
        public AbstractTrigger Listener { get; internal protected set; }
        EventBehaviorExecutor Behavior { get; }

        void PutArg(int id, object arg);
        object GetArg(int id);
        T GetArgAs<T>(int id);

        object Tag { get; set; }
        Int32 IteratingInt32 { get; set; }
        object IteratingObject { get; set; }

        Boolean TriggingBoolValue { get; set; }
        double TriggingNumberValue { get; set; }
        string TriggingStringValue { get; set; }

        //         void PutAttribute(object key, object value);
        //         object GetAttribute(object key);
        //         bool TryGetAttribute(object key, out object value);
        //         T GetAttributeAs<T>(object key);
        //         bool TryGetAttributeAs<T>(object key, out T ret);


        object ReturnValue { get; set; }



    }

    public interface IEventAPI
    {
        Random RandomN { get; }

        TimeSpan PassTime { get; }
        TimeSpanAlarm PassTimeAlarm { get; }

        DateTime DateTime { get; }
        DateTimeAlarm DateTimeAlarm { get; }
        AbstractCollectionPool ObjectPool { get; }

        /// <summary>
        /// 【线程安全】向主线程排一个任务
        /// </summary>
        /// <param name="task"></param>
        void Run(Action task);

        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler);
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler);
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler);


        void SetEnvironmentVar(string key, object value, bool syncToClient);
        T GetEnvironmentVarAs<T>(string key);
        bool TryGetEnvironmentVar(string key, out object value);
        bool TryGetEnvironmentVarAs<T>(string key, out T value);


        IEventArguments AllocEventArguments(EventExecutor exe, AbstractTrigger listener, EventBehaviorExecutor behavior);
        IEventArguments AllocEventArguments(EventExecutor exe, IEventArguments src);

    }


    //---------------------------------------------------------------------------------
    /// <summary>
    /// 编辑器事件集合，比如场景编辑器一个，单位触发器一个；
    /// 用于查找事件集合中的其他事件。
    /// </summary>
    public interface IEventExecutorCollection : IDisposable, IEnumerable<EventExecutor>
    {
        int TemplateID { get; }
        Type TemplateType { get; }
        string GUID { get; }
        string Name { get; }
        IEnumerable<IEventDataNode> DataNodes { get; }
        void Start();
        void ForEachEvents(Action<EventExecutor> action);
        EventExecutor GetEditEvent(string name);
        void EventActive(string name);
        void EventDeactive(string name);
    }

}

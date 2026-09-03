using DeepCore.EventTrigger.Data;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DeepCore.EventTrigger
{
    [Reflectible]
    public interface IEventData : ISerializable
    {
        string EventName { get; set; }
        string EditorPath { get; set; }
    }
    [Reflectible]
    public interface IEventValue
    {
        object GetEnvValue(EventExecutor api);
    }

    [Reflectible]
    public interface IStereoOption
    {
        EventExternalizable Input { get; set; }
        EventExternalizable Output { get; set; }
        string InputName { get; }
        string OutputName { get; }
    }

    [Reflectible]
    public interface IEventDataNode : IEventData
    {
        /// <summary>
        /// 启用
        /// </summary>
        bool EventIsActive { get; set; }
        /// <summary>
        /// 注释
        /// </summary>
        string EventComment { get; set; }
        /// <summary>
        /// 保存
        /// </summary>
        string EventTreePath { get; set; }

        /// <summary>
        /// 临时变量
        /// </summary>
        List<EventLocalVar> EventLocalVars { get; }
        /// <summary>
        /// 事件触发
        /// </summary>
        List<AbstractTrigger> EventTriggers { get; }
        /// <summary>
        /// 条件
        /// </summary>
        List<AbstractCondition> EventConditions { get; }
        /// <summary>
        /// 动作
        /// </summary>
        List<AbstractAction> EventActions { get; }

        EventBehaviorData EventBehavior { get; }

        EventBehaviorData GetRuntimeBehavior();
    }


    [Reflectible]
    public abstract class EventExternalizable : IExternalizable, IFuncData
    {
        [Desc(Editable = false)]
        [XmlSerializable()]
        public IFuncTableGroup Tables { get; set; }
        public EventBehaviorNode OwnerNode { get; internal set; }
        public IDynamicTypeInfo TypeInfo { get; private set; }

        public abstract Type BaseType { get; }

        public string TypeDesc
        {
            get
            {
                var text = GetType().Name;
                if (PropertyUtil.TryGetAttribute<DescAttribute>(GetType(), out var desc))
                {
                    text = desc.Desc;
                }
                return text;
            }
        }

        public IDynamicTypeInfo GetTypeInfo()
        {
            if (TypeInfo == null) TypeInfo = DynamicTypeFactory.Instance.GetTypeInfo(GetType());
            return TypeInfo;
        }

        public void WriteExternal(IOutputStream output)
        {
            var dtype = GetTypeInfo();
            output.PutExt(this.Tables);
            var fields = new List<IDynamicFieldInfo>(dtype.GetFields());
            fields.Sort((a, b) => a.Name.CompareTo(b.Name));
            foreach (var dfield in fields)
            {
                var fd = dfield.GetValue(this);
                if (fd is IRuntimeValue)
                {
                    continue;
                }
                else if (fd != null)
                {
                    output.PutUTF(dfield.Name);
                    output.PutRawData(dfield.Field.FieldType, fd);
                }
                else
                {
                    output.PutUTF($"-{dfield.Name}");
                }
            }
            output.PutUTF(".");
        }
        public void ReadExternal(IInputStream input)
        {
            var dtype = GetTypeInfo();
            this.Tables = input.GetExt<IFuncTableGroup>();
            string fname = string.Empty;
            try
            {
                do
                {
                    fname = input.GetUTF();
                    if (fname == ".")
                    {
                        break;
                    }
                    if (fname.StartsWith("-"))
                    {
                        fname = fname.Substring(1);
                        var dfield = dtype.GetField(fname);
                        if (dfield != null)
                        {
                            dfield.SetValue(this, null);
                        }
                    }
                    else
                    {
                        var dfield = dtype.GetField(fname);
                        if (dfield != null)
                        {
                            var fd = input.GetRawData(dfield.Field.FieldType, out var dt);
                            if (dt == DataType.NA || fd == null)
                            {
                                throw new Exception(string.Format("Can not read field '{0}' in '{1}'", fname, this.GetType().FullName));
                            }
                            dfield.SetValue(this, fd);
                        }
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} : Can not read field '{fname}' in '{this.GetType().FullName}'", ex);
            }

        }

        protected virtual void GetText(EventStringBuilder sw)
        {
            if (GetType().TryGetAttribute<DescAttribute>(out var desc))
            {
                sw.Append(desc.Desc);
            }
            else
            {
                sw.Append(GetType().Name);
            }
        }
        protected virtual void GetEndText(EventStringBuilder sw) { }
        internal void BuildText(EventStringBuilder sw)
        {
            try
            {
                if (this.OwnerNode is EventBehaviorNode node)
                {
                    this.GetText(sw);
                    if (node is EventBehaviorAction action)
                    {
                        if (action.NEXT != null && action.NEXT.Count > 0)
                        {
                            sw.AppendLine();
                            for (int i = 0; i < action.NEXT.Count; i++)
                            {
                                sw.Append(action.NEXT[i]?.EventData);
                                if (i < action.NEXT.Count - 1)
                                {
                                    sw.AppendLine();
                                }
                            }
                        }
                    }
                    this.GetEndText(sw);
                }
                else
                {
                    this.GetText(sw);
                    this.GetEndText(sw);
                }
            }
            catch (Exception err)
            {
                sw.AppendLine(err.Message);
                sw.AppendLine(err.StackTrace);
            }
        }
        public override string ToString()
        {
            try
            {
                var doc = EventStringBuilder.FunctionDocument(this);
                return doc.InnerText;
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
    }

    [Desc("分组")]
    public class BehaviorGroup : EventExternalizable
    {
        public override Type BaseType => typeof(BehaviorGroup);
        [Desc("标题")] public string Title = "GROUP";
    }


    [Reflectible]
    public interface IEnvironmentVar : ISerializable
    {
        string Key { get; set; }
        object Value { get; set; }
        bool SyncToClient { get; set; }
    }

}

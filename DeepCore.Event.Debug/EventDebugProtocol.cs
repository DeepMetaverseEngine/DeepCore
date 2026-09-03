using DeepCore.EventTrigger;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCore.Event.Debug
{
    public class EventDebugProtocolFactory : MessageFactoryGenerator
    {
        public EventDebugProtocolFactory(IExternalizableFactory factory) : base(factory)
        {
            RegistExternalizableAssembly(this.GetType(), AppDomain.CurrentDomain.GetAssemblies());
        }
    }

    public abstract class EventDebugProtocol : IExternalizable
    {
        public const int MESSAGE_START = 0x34560000;
        public IDynamicTypeInfo TypeInfo { get; private set; }
        public IDynamicTypeInfo GetTypeInfo()
        {
            if (TypeInfo == null) TypeInfo = DynamicTypeFactory.Instance.GetTypeInfo(GetType());
            return TypeInfo;
        }
        public void WriteExternal(IOutputStream output)
        {
            var dtype = GetTypeInfo();
            foreach (var dfield in dtype.GetFields())
            {
                var fd = dfield.GetValue(this);
                if (fd != null)
                {
                    output.PutUTF(dfield.Name);
                    output.PutRawData(dfield.Field.FieldType, fd);
                }
            }
            output.PutUTF(".");
        }
        public void ReadExternal(IInputStream input)
        {
            var dtype = GetTypeInfo();
            do
            {
                var fname = input.GetUTF();
                if (fname == ".")
                {
                    break;
                }
                var dfield = dtype.GetField(fname);
                var fd = input.GetRawData(dfield.Field.FieldType, out var dt);
                if (dt == DataType.NA || fd == null)
                {
                    throw new Exception(string.Format("Can not read field '{0}' in '{1}'", fname, this.GetType().FullName));
                }
                dfield.SetValue(this, fd);
            }
            while (true);
        }
    }

    [MessageType(MESSAGE_START + 1)]
    public class EventRuntimeState : EventDebugProtocol
    {
        public List<EventCollectionData> Collections;
    }

    [MessageType(MESSAGE_START + 2)]
    public class AddCollectionNotify : EventDebugProtocol
    {
        public EventCollectionData Add;
    }

    [MessageType(MESSAGE_START + 3)]
    public class RemoveCollectionNotify : EventDebugProtocol
    {
        public string GUID;
    }
    [MessageType(MESSAGE_START + 4)]
    public class ExecutorChangedNotify : EventDebugProtocol
    {
        public string CollectionGUID;
        public EventExecutorData ExeData;
    }


    [MessageType(MESSAGE_START + 11)]
    public class EventCollectionData : EventDebugProtocol
    {
        public Type TemplateType;
        public int TemplateID;
        public string Name;
        public string GUID;
        public List<EventExecutorData> Events;
    }
    [MessageType(MESSAGE_START + 12)]
    public class EventExecutorData : EventDebugProtocol
    {
        public string Name;
        public bool IsActive;
        public IEventDataNode EventData;
        public List<string> TracingNodes;
    }

    [MessageType(MESSAGE_START + 13)]
    public class EventTraceData : EventDebugProtocol
    {
        public string CollectionGUID;
        public string ExeName;
        public string NodeGUID;
    }
    [MessageType(MESSAGE_START + 14)]
    public class EventBeginTraceNotify : EventDebugProtocol
    {
        public string CollectionGUID;
        public string ExeName;
    }
}

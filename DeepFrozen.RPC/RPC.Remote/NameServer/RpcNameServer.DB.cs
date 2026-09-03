using DeepCore;
using DeepCore.ORM;
using DeepCore.Threading;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using Quartz.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepFrozen.RPC.Remote.NameServer
{
    //     public class StructMappingDictionary<K, T> where T : IStructMapping
    //     {
    //         public IMappingAdapter DB { get; }
    //         public IMappingHash Hash { get; }
    //         public StructMappingDictionary(string key, IMappingAdapter db)
    //         {
    // 
    //         }
    // 
    //         //         protected override MappingObject CreateSubMapping(string fieldName, Type fieldType)
    //         //         {
    //         //             throw new NotImplementedException();
    //         //         }
    //         //         protected override IWrapper CreateSubWrapper(string fieldName, Type fieldType)
    //         //         {
    //         //             return new WrapperStruct<T>(this, fieldName, fieldType);
    //         //         }
    //         //         protected override void OnDataTypeChanged(Type type)
    //         //         {
    //         //             base.OnDataTypeChanged(type);
    //         //             this.f_TeamType = base.InternalGetSubField("TeamType");
    //         //             this.Teams = base.GetMappingField("Teams") as MappingDictionary<int, Tiny.Data.Team, Tiny.Data.TeamWrapper>;
    //         // 
    //         //         }
    //     }

    //-----------------------------------------------------------------------------------------------------------------

    public class ServiceNodeStartInfoWrapper : WrapperStruct<ServiceNodeStartInfo>
    {
        public string NodeName => Data.NodeName;
        public string EndPoint => Data.EndPoint;
        public List<string> AcceptServiceType => Data.AcceptServiceType;
        public ServiceNodeStartInfoWrapper() { }
        public ServiceNodeStartInfoWrapper(ServiceNodeStartInfo src) : base(src) { }
    }
    public class ServiceNodeStateInfoWrapper : WrapperStruct<ServiceNodeStateInfo>
    {
        public string NodeName => Data.NodeName;
        public int ServiceCount => Data.ServiceCount;
        public float CpuPercent => Data.CpuPercent;
        public long MemoryUse => Data.MemoryUse;
        public long MemoryTotal => Data.MemoryTotal;
        public string Info => Data.Info;
        public ServiceNodeStateInfoWrapper() { }
        public ServiceNodeStateInfoWrapper(ServiceNodeStateInfo src) : base(src) { }
    }
    public class NodeInfoMapping : MappingReference<NodeInfo>, IRemoteNodeInfo
    {
        private DynamicFieldMapping f_serviceCount;
        public ServiceNodeStartInfoWrapper token { get; private set; }
        public ServiceNodeStateInfoWrapper state { get; private set; }
        public int serviceCount
        {
            get { return this.Data.serviceCount; }
            set { this.Data.serviceCount = value; base.InternalSetMappingField(f_serviceCount, value, false); }
        }
        public NodeInfoMapping(string key, ITaskExecutor exe = null, IMappingAdapter db = null) : base(key, exe, db)
        {
        }
        protected override IWrapper CreateSubWrapper(string fieldName, Type fieldType)
        {
            if (fieldName == "token") return new ServiceNodeStartInfoWrapper();
            if (fieldName == "state") return new ServiceNodeStateInfoWrapper();
            return base.CreateSubWrapper(fieldName, fieldType);
        }
        protected override void OnDataTypeChanged(Type type)
        {
            base.OnDataTypeChanged(type);
            this.token = base.GetWrapperField("token") as ServiceNodeStartInfoWrapper;
            this.state = base.GetWrapperField("state") as ServiceNodeStateInfoWrapper;
            this.f_serviceCount = base.InternalGetSubField("serviceCount");
        }
        public string NodeName { get => token.NodeName; }
        public string EndPoint { get => token.EndPoint; }
        List<string> IRemoteNodeInfo.AcceptServiceType => token.AcceptServiceType;
    }
    public class NodeInfoMappingDictionary : MappingDictionary<string, NodeInfo, NodeInfoMapping>
    {
        public NodeInfoMappingDictionary(string key, ITaskExecutor exe, IMappingAdapter db) : base(key, exe, db)
        {
        }
        protected override MappingObject CreateSubMapping(string fieldName, Type fieldType)
        {
            if (fieldType == typeof(NodeInfo)) return new NodeInfoMapping(GetSubMappingName(fieldName, fieldType), this.executor, this.adapter);
            return base.CreateSubMapping(fieldName, fieldType);
        }
    }

    //-----------------------------------------------------------------------------------------------------------------
    public class RemoteProxyInfoWrapper : WrapperStruct<RemoteProxyInfo>
    {
        public RemoteAddressInfo Address { get { return this.Data.Address; } }
        public Properties Config { get { return this.Data.Config; } }
        public string EndPoint { get { return this.Data.EndPoint; } }
        public bool IsStatic { get { return this.Data.IsStatic; } }
        public DateTime StartTimeUTC
        {
            get { return this.Data.StartTimeUTC; }
            set { this.Data.StartTimeUTC = value; base.FireDirty(); }
        }
        public RemoteProxyInfoWrapper() { }
        public RemoteProxyInfoWrapper(RemoteProxyInfo src) : base(src) { }
    }
    public class ServiceInfoMapping : MappingReference<ServiceInfo>, IRemoteServiceInfo
    {
        private DynamicFieldMapping f_creater;
        private DynamicFieldMapping f_isStatic;
        private DynamicFieldMapping f_status;
        private DynamicFieldMapping f_startTimeUTC;
        public RemoteProxyInfoWrapper info { get; private set; }
        public RemoteAddressInfo creater
        {
            get { return this.Data.creater; }
            //set { this.Source.creater = value; base.InternalSetMappingField(f_creater, value, false); }
        }
        public bool isStatic
        {
            get { return this.Data.isStatic; }
            //set { this.Source.isStatic = value; base.InternalSetMappingField(f_isStatic, value, false); }
        }
        public ServiceStatus status
        {
            get { return this.Data.status; }
            set { this.Data.status = value; base.InternalSetMappingField(f_status, value, false); }
        }
        public DateTime startTimeUTC
        {
            get { return this.Data.startTimeUTC; }
            set { this.Data.startTimeUTC = value; base.InternalSetMappingField(f_startTimeUTC, value, false); }
        }
        public ServiceInfoMapping(string key, ITaskExecutor exe = null, IMappingAdapter db = null) : base(key, exe, db)
        {
        }
        protected override IWrapper CreateSubWrapper(string fieldName, Type fieldType)
        {
            if (fieldName == "info") return new RemoteProxyInfoWrapper();
            return base.CreateSubWrapper(fieldName, fieldType);
        }
        protected override void OnDataTypeChanged(Type type)
        {
            base.OnDataTypeChanged(type);
            this.info = base.GetWrapperField("info") as RemoteProxyInfoWrapper;
            this.f_creater = base.InternalGetSubField("creater");
            this.f_isStatic = base.InternalGetSubField("isStatic");
            this.f_status = base.InternalGetSubField("status");
            this.f_startTimeUTC = base.InternalGetSubField("startTimeUTC");
        }
        public string ServiceName { get => info.Address.ServiceName; }
        public string ServiceNode { get => info.Address.ServiceNode; }
        public RemoteAddress Address { get => info.Address.ToAddress(); }
        Properties IRemoteServiceInfo.Config => info.Config;
        DateTime IRemoteServiceInfo.StartTimeUTC => startTimeUTC;
        bool IRemoteServiceInfo.IsStatic => isStatic;
    }
    public class ServiceInfoMappingDictionary : MappingDictionary<string, ServiceInfo, ServiceInfoMapping>
    {
        public ServiceInfoMappingDictionary(string key, ITaskExecutor exe, IMappingAdapter db) : base(key, exe, db)
        {
        }
        protected override MappingObject CreateSubMapping(string fieldName, Type fieldType)
        {
            if (fieldType == typeof(ServiceInfo)) return new ServiceInfoMapping(GetSubMappingName(fieldName, fieldType), this.executor, this.adapter);
            return base.CreateSubMapping(fieldName, fieldType);
        }
    }
    //-----------------------------------------------------------------------------------------------------------------
   

    public class ServiceMap : ProxyMap<string, ServiceInfo, ServiceInfoMapping>
    {
        protected override MappingDictionary<string, ServiceInfo, ServiceInfoMapping> map { get; }
        public ServiceMap(string key, ITaskExecutor exe, IMappingAdapter db)
        {
            this.map = new ServiceInfoMappingDictionary(key, exe, db);
        }
    }
    public class NodeMap : ProxyMap<string, NodeInfo, NodeInfoMapping>
    {
        protected override MappingDictionary<string, NodeInfo, NodeInfoMapping> map { get; }
        public NodeMap(string key, ITaskExecutor exe, IMappingAdapter db)
        {
            this.map = new NodeInfoMappingDictionary(key, exe, db);
        }
    }
}

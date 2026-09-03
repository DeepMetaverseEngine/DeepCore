using DeepCore.ORM;
using DeepCore;
using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.IO;
using DeepCore.Protocol;
using Gate.Data.Protocol;
using DeepCore.Geometry;
using DeepCore.Reflection;

namespace Gate.Data.Sample
{
    //------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// ORM存储映射
    /// </summary>
    [PersistType]
    [MessageType(Constants.SAMPLE + 1)]
    public class SampleORM : ISerializable, IObjectMapping
    {
        [PersistField(PersistStrategy.Primary)]
        public string uuid;

        [PersistField]// default is cache in memory
        public string token;


        [PersistField]// default is cache in memory
        public int intValue;


        [PersistField(PersistStrategy.CacheInMemory)]
        public string cacheInMemory;

        [PersistField(PersistStrategy.SaveImmediately)]
        public string saveImmediately;

        [PersistField(PersistStrategy.LoadImmediately)]
        public string loadImmediately;

        [PersistField(PersistStrategy.SaveLoadImmediately)]
        public string saveLoadImmediately;

        [PersistField]
        public SampleSubMapping subMapping;
        [PersistField]
        public SampleStructMapping structMapping;
        [PersistField]
        public SampleStructWrapper structWrapper;

        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;

        [PersistField]
        public HashMap<string, RoleIDSnap> roleMap = new HashMap<string, RoleIDSnap>();
        [PersistField]
        public HashMap<string, SampleSubMapping> subMap = new HashMap<string, SampleSubMapping>();
    }
    /// <summary>
    /// ORM存储映射（嵌套）
    /// </summary>
    [MessageType(Constants.SAMPLE + 2)]
    public class SampleSubMapping : ISerializable, IObjectMapping
    {
        [PersistField] public string userAgent;
        [PersistField] public string network;
        [PersistField] public string deviceId;
        [PersistField] public string deviceType;
        [PersistField] public string deviceModel;
        [PersistField] public string region;
        [PersistField] public string channel;
        [PersistField] public string subChannel;
        [PersistField] public string clientVersion;
        [PersistField] public string sdkVersion;
        [PersistField] public string sdkName;
        [PersistField] public string userSource1;
        [PersistField] public string userSource2;
        [PersistField] public string platformAcount;
        [PersistField] public string walletAddress;
        [PersistField] public string invateWalletAddress;

        [PersistField] public byte[] rawData;
        [PersistField] public string[] args;
        [PersistField] public List<byte> rawDataList;
        [PersistField] public List<string> argsList;
    }

    /// <summary>
    /// ORM存储映射，不包含子映射，适用于简单对象。
    /// 如果该类型存在于集合中，则集合容器Mapping作为父容器。
    /// 比如父节点为: MappingDictionary<string, IStructMapping>
    /// </summary>
    [MessageType(Constants.SAMPLE + 3)]
    public class SampleStructMapping : ISerializable, IStructMapping
    {
        public int userAgent;
        public float network;
        public double deviceId;
        public string deviceType;
        public string deviceModel;
        public string region;
        public string channel;
    }

    /// <summary>
    /// 无论存储在集合里还是本身，都作为一个整体存入ORM。
    /// 比如父节点为: HashMap<string, IStructWrapper>
    /// 则父节点也作为一个整体落地。
    /// </summary>
    [MessageType(Constants.SAMPLE + 4)]
    public class SampleStructWrapper : ISerializable, IStructWrapper
    {
        public string userAgent;
        public int network;
        public float deviceId;
        public double deviceType;
    }

    //------------------------------------------------------------------------------------------------------------

    [MessageType(Constants.SAMPLE + 20)]
    public class SampleTable : ISerializable
    {
        [Desc("id")] public int id;
        [Desc("整形值")]public int value;
        [Desc("字符串")] public string name;
        [Desc("字符串数组")] public string[] rates;
        [Desc("整形数组")] public int[] tags;
        [Desc("嵌套类")] public Raw raw;
        [Desc("整形数组")] public int[] numbers;
        [Desc("字符串数组")] public string[] texts;
    }
    [MessageType(Constants.SAMPLE + 21)]
    public class Raw : ISerializable
    {
        public int number;
        public string text;
    }


    //------------------------------------------------------------------------------------------------------------


    [MessageType(Constants.SAMPLE + 10)]
    public class SamplePing : Request, ILogicProtocol
    {
        public DateTime time = DateTime.Now;
        public byte[] rawdata;
    }
    [MessageType(Constants.SAMPLE + 11)]
    public class SamplePong : Response, ILogicProtocol
    {
        public DateTime time;
        public byte[] rawdata;
    }
    [MessageType(Constants.SAMPLE + 12)]
    public class SampleNotify : Notify, ILogicProtocol
    {
        public Vector3 Position;
        public List<Vector3> PositionList;
        public SampleStructWrapper raw;
        public List<SampleStructMapping> rawList;
        public int index;
        public DateTime time;
    }

    //------------------------------------------------------------------------------------------------------------
}

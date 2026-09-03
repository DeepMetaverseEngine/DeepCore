using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DeepCrystal.Threading;
using DeepCore;
using DeepCore.Log;
using DeepCore.Threading;

namespace DeepCrystal.ORM
{


    /// <summary>
    /// Object-relational mapping factory
    /// </summary>

    [Reflectible]
    public abstract class ORMFactory : Disposable
    {
        public static bool IsTest { get; set; }
#if RELEASE
= false;
#else
= true;
#endif
        //----------------------------------------------------------------------------------------------------
        #region Singleton
        private static ORMFactory s_instance;
        public static ORMFactory Instance { get { return s_instance; } }
        public static IMappingAdapter DefaultAdapterInstance { get { return s_instance.DefaultAdapter; } }
        public static IMappingDatabase DefaultDatabaseInstance { get { return s_instance.DefaultDatabase; } }
        //----------------------------------------------------------------------------------------------------
        protected readonly Logger log;
        protected ORMFactory()
        {
            log = LoggerFactory.GetLogger(GetType().Name);
            AsSynchronizedDisposing();
            s_instance = this;
        }
        public static bool IsMappingCollection(Type type)
        {
            return MappingConverter.Instance.IsMappingObjectCollection(type);
        }
        public static bool IsStructCollection(Type type)
        {
            return MappingConverter.Instance.IsMappingStructCollection(type);
        }
        public static string GetMappingName(Type dataType)
        {
            return MappingConverter.Instance.GetMappingName(dataType);
        }
        public static string GetWrapperName(Type dataType)
        {
            return MappingConverter.Instance.GetWrapperName(dataType);
        }
        public static string GetBaseMappingName(Type dataType)
        {
            return MappingConverter.Instance.GetBaseMappingName(dataType);
        }
        public static string GetBaseWrapperName(Type dataType)
        {
            return MappingConverter.Instance.GetBaseWrapperName(dataType);
        }
        public static bool IsRootMapping(Type dataType)
        {
            return MappingConverter.Instance.IsRootMapping(dataType);
        }
        public static string GetFieldAttribute(Type fieldType)
        {
            return MappingConverter.Instance.GetFieldAttribute(fieldType);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------
        public ParserAdapter KeyEncoder = new BaseParserAdapter();
        public virtual string EncodeKey(object key)
        {
            return KeyEncoder.ToString(key);
        }
        public virtual object DecodeKey(string key, Type keyType)
        {
            return KeyEncoder.TryParse(key, keyType, out var value) ? value : null;
        }
        //----------------------------------------------------------------------------------------------------       
        public IExternalizableFactory StructFactory { get; set; }
        public abstract IConvertible EncodeObject(object obj, Type type);
        public abstract object DecodeObject(IConvertible obj, Type type);
        public IConvertible EncodeObject<T>(T obj)
        {
            return EncodeObject(obj, typeof(T));
        }
        public IConvertible EncodeObject(object obj)
        {
            return EncodeObject(obj, obj.GetType());
        }
        public T DecodeObject<T>(IConvertible obj)
        {
            var ret = DecodeObject(obj, typeof(T));
            if (ret != null)
            {
                return (T)ret;
            }
            return default(T);
        }
        //----------------------------------------------------------------------------------------------------
        public abstract IMappingAdapter DefaultAdapter { get; }
        public abstract IMappingDatabase DefaultDatabase { get; }
        public abstract IConditions Conditions { get; }
        public abstract IMappingAdapter GetAdapter(string db);
        //----------------------------------------------------------------------------------------------------
        public abstract IMappingDatabase CreateDatabase(string db);
        public abstract ITransactionDatabase CreateTransaction(IMappingDatabase db);
        public abstract ITransactionDatabase CreateTransaction(IMappingDatabase db, ICondition condition);
        public abstract ITransactionDatabase CreateTransaction(IMappingDatabase db, ICondition[] conditions);
        //----------------------------------------------------------------------------------------------------
        public abstract IChannel GetChannel(string channel, ITaskExecutor exe = null);
        //----------------------------------------------------------------------------------------------------



    }

}
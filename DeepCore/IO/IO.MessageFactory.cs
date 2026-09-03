using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Globalization;

using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Protocol;
using System.Text;
using DeepCore.Log;
using DeepCore.ORM;
using System.IO;

namespace DeepCore.IO
{
    public class MessageFactoryGenerator : IExternalizableFactory, IComparer<Type>, IComparer<TypeCodec>
    {

        protected readonly Logger log;
        private static readonly Type[] null_args = new Type[0];
        private static readonly Type ext_type = typeof(IExternalizable);
        private HashMap<Type, TypeCodec> types_c2i = new HashMap<Type, TypeCodec>();
        private HashMap<int, TypeCodec> types_i2c = new HashMap<int, TypeCodec>();
        private HashMap<string, TypeCodec> all_types = new HashMap<string, TypeCodec>();
        private int array_limit = IOStream.DEFAULT_ARRAY_LIMIT;
        private int bytes_limit = IOStream.DEFAULT_BYTES_LIMIT;
        public string CodeHash { get; }
        public int ArrayLimit
        {
            get { return array_limit; }
            set { array_limit = value; }
        }
        public int BytesLimit
        {
            get { return bytes_limit; }
            set { bytes_limit = value; }
        }
        public bool UseVLQ { get; set; }
        public bool Verbose { get; set; }
        public bool IsConsistency { get; set; }

        public MessageFactoryGenerator(string codeHash)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.UseVLQ = false;
            this.Verbose = false; 
            this.IsConsistency = false;
            this.CodeHash = codeHash;
        }
        public MessageFactoryGenerator(IExternalizableFactory other) : this(other.CodeHash)
        {
            RegistCodec(other);
        }
        public MessageFactoryGenerator() : this("") { }
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        #region ISerializerFactory

        public IEnumerable<TypeCodec> AllTypes
        {
            get { return all_types.Values; }
        }

        public virtual TypeCodec GetCodec(int id)
        {
            return types_i2c.Get(id);
        }
        public virtual Type GetType(int id)
        {
            var codec = types_i2c.Get(id);
            if (codec != null)
            {
                return codec.MessageType;
            }
            return null;
        }
        public virtual TypeCodec GetCodec(Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            return types_c2i.Get(type);
        }
        public virtual int GetTypeID(Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            var codec = types_c2i.Get(type);
            if (codec != null)
            {
                return codec.MessageID;
            }
            return IOStream.INVALID_MESSAGE_CODE;
        }

        public virtual TypeCodec GetCodecByName(string name)
        {
            return all_types.Get(name);
        }
        public virtual Type GetTypeByName(string name)
        {
            var codec = all_types.Get(name);
            if (codec != null)
            {
                return codec.MessageType;
            }
            return null;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        #region IComparer

        public virtual int Compare(Type x, Type y)
        {
            var mtX = PropertyUtil.GetAttribute<MessageTypeAttribute>(x);
            var mtY = PropertyUtil.GetAttribute<MessageTypeAttribute>(y);
            if (mtX != null && mtY != null)
            {
                return mtX.MessageTypeID - mtY.MessageTypeID;
            }
            if (mtX != null)
            {
                return 1;
            }
            if (mtY != null)
            {
                return -1;
            }
            return x.FullName.CompareTo(y.FullName);
        }
        public virtual int Compare(TypeCodec x, TypeCodec y)
        {
            if (x.MessageID != y.MessageID)
            {
                return x.MessageID - y.MessageID;
            }
            return Compare(x.MessageType, y.MessageType);
        }
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        protected virtual bool TryAddType(TypeCodec codec)
        {
            try
            {
                if (codec.MessageType.IsAbstract)
                {
                    throw new Exception(string.Format("Can Not Regist Abstract Class: {0} ", codec.MessageType));
                }
                if (codec.MessageType.IsClass && codec.MessageType.GetConstructor(null_args) == null)
                {
                    throw new Exception(string.Format("No default null arguments constructor : {0}\n    {1}();",
                        codec.MessageType.FullName,
                        codec.MessageType.Name));
                }
                if (codec.MessageID != IOStream.INVALID_MESSAGE_CODE)
                {
                    if (all_types.TryAdd(codec.MessageType.FullName, codec))
                    {
                        if (types_i2c.ContainsKey(codec.MessageID))
                        {
                            var c = types_i2c[codec.MessageID];
                            if (c.MessageType != codec.MessageType)
                            {
                                throw new Exception(string.Format("Duplicate Type : id = 0x{0} with \"{1}\" - \"{2}\"",
                                    codec.MessageID.ToString("X"),
                                    codec.MessageType.FullName,
                                    c.MessageType.FullName));
                            }
                            return false;
                        }
                        types_c2i.Add(codec.MessageType, codec);
                        types_i2c.Add(codec.MessageID, codec);
                        if (Verbose) log.InfoFormat("RegistClass : 0x{0} : {1}",
                            CUtils.FillPlaceHolder(codec.MessageID.ToString("X8"), 8, ' ', 1),
                            codec.MessageType.FullName);
                        return true;
                    }
                    else
                    {
                        if (Verbose) log.Warn($"{codec.MessageType.FullName} : Already exist !!!");
                    }
                }
                else
                {
                    if (all_types.TryAdd(codec.MessageType.FullName, codec))
                    {
                        types_c2i.Add(codec.MessageType, codec);
                        return true;
                    }
                    else
                    {
                        if (Verbose) log.Warn($"{codec.MessageType.FullName} : Already exist !!!");
                    }
                    log.WarnFormat("No Message ID Class For TypeName : {0}", codec.MessageType.FullName);
                }
            }
            catch (Exception err)
            {
                log.Error("Add codec Error : " + codec.MessageType.FullName);
                throw err;
            }
            return false;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        public virtual bool RegistClass(int msgid, Type type, Func<Type, object> do_create, Action<IInputStream, object> do_read, Action<IOutputStream, object> do_write)
        {
            if (msgid == IOStream.INVALID_MESSAGE_CODE)
            {
                throw new Exception(string.Format("Type [{0}] MessageTypeID [{1}] INVALID_MESSAGE_CODE !!!", type, msgid));
            }
            if (msgid == IOStream.NULL_MESSAGE_CODE)
            {
                throw new Exception(string.Format("Type [{0}] MessageTypeID [{1}] NULL_MESSAGE_CODE !!!", type, msgid));
            }
            if (TryAddType(new TypeCodec(type, msgid, do_create, do_read, do_write)))
            {
                return true;
            }
            return false;
        }
        public virtual bool RegistClass<T>(int msgid, Action<IInputStream, T> do_read, Action<IOutputStream, T> do_write) where T : class, new()
        {
            return RegistClass(msgid, typeof(T), static (t) => new T(), (s, t) => { do_read(s, (T)t); }, (s, t) => { do_write(s, (T)t); });
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        public virtual bool RegistExternalizable<T>(int? msgid = null) where T : class, IExternalizable, new()
        {
            return RegistExternalizable(typeof(T), static t => new T(), msgid);
        }
        public virtual bool RegistExternalizable(Type type, Func<Type, object> create, int? msgid = null)
        {
            if (!ext_type.IsAssignableFrom(type))
            {
                throw new Exception(string.Format("Type [{0}] Not a IExternalizable !!!", type));
            }
            if (!msgid.HasValue)
            {
                var attr = PropertyUtil.GetAttribute<MessageTypeAttribute>(type);
                if (attr != null)
                {
                    if (attr.MessageTypeID == IOStream.INVALID_MESSAGE_CODE)
                    {
                        throw new Exception(string.Format("Type [{0}] MessageTypeID [{1}] INVALID_MESSAGE_CODE !!!", type, msgid));
                    }
                    msgid = attr.MessageTypeID;
                }
            }
            if (msgid.HasValue)
            {
                if (msgid.Value == IOStream.NULL_MESSAGE_CODE)
                {
                    throw new Exception(string.Format("Type [{0}] MessageTypeID [{1}] NULL_MESSAGE_CODE !!!", type, msgid));
                }
            }
            else
            {
                msgid = IOStream.INVALID_MESSAGE_CODE;
            }
            return TryAddType(new TypeCodec(type, msgid.Value, create, DoReadExternal, DoWriteExternal));
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 将一个程序集内的所有类符合[MessageType]的IExternalizable类全部注册到编解码器
        /// </summary>
        /// <param name="assembly"></param>
        protected void RegistExternalizableAssembly(Type codecType, Predicate<Type> filter, params Assembly[] assembly)
        {
            var create_map = new Dictionary<Type, Func<Type, object>>();
            var create_mapattr = new Dictionary<Type, GenCodeCreateAttribute>();
            foreach (var field in codecType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static))
            {
                try
                {
                    if (field.TryGetAttribute<GenCodeCreateAttribute>(out var create))
                    {
                        if (create.MessageTypeID == 0) continue;
                        create_map.Add(create.MessageType, (Func<Type, object>)field.GetValue(null));
                        create_mapattr.Add(create.MessageType, create);
                    }
                }
                catch (Exception err)
                {
                    log.Error($"Regist codec Error : Codec={codecType.FullName} Field={field?.Name}");
                    throw err;
                }
            }
            foreach (Assembly asm in assembly)
            {
                Type[] types = null;
                try
                {
                    if (Verbose) log.Info("Get Assembly Types : " + asm.FullName);
                    types = asm.GetTypes();
                }
                catch (Exception err)
                {
                    if (Verbose) log.ErrorFormat("Get Types Error : {1}\n    {0}", err.Message, asm.FullName);
                    continue;
                }
                try
                {
                    foreach (var type in types)
                    {
                        try
                        {
                            if (!type.IsAbstract && !type.IsInterface && ext_type.IsAssignableFrom(type))
                            {
                                var ign = type.GetAttributes<IgnoreGenerateAttribute>();
                                if (ign != null && ign.Length > 0)
                                {
                                    continue;
                                }
                                if (filter == null || filter(type))
                                {
                                    create_map.TryGetValue(type, out var create);
                                    int? msgid = null;
                                    if (create_mapattr.TryGetValue(type, out var attr))
                                    {
                                        msgid = attr.MessageTypeID;
                                    }
                                    else if (type.TryGetAttribute<MessageTypeAttribute>(out var attrID))
                                    {
                                        msgid = attrID.MessageTypeID;
                                    }
                                    if (RegistExternalizable(type, create ?? (static t => DeepActivator.CreateInstance(t)), msgid))
                                    {
                                        log.Info("Append Codec : " + type.FullName);
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            throw new Exception(err.Message + "@" + type.FullName, err);
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                    throw err;
                }
            }
        }
        protected void RegistExternalizableAssembly(Type codecType, params Assembly[] assembly)
        {
            RegistExternalizableAssembly(codecType, null, assembly);
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 注册代码生成器生成的Codec里所有Attributes
        /// </summary>
        /// <param name="codecType"></param>
        public void RegistCodeGenFields(Type codecType)
        {
            var type_map = new Dictionary<Type, GenCodeCreateAttribute>();
            var id_map = new Dictionary<int, GenCodeIDAttribute>();
            var create_map = new Dictionary<int, Func<Type, object>>();
            var read_map = new Dictionary<int, Action<IInputStream, object>>();
            var write_map = new Dictionary<int, Action<IOutputStream, object>>();
            foreach (var field in codecType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static))
            {
                try
                {
                    if (field.TryGetAttribute<GenCodeIDAttribute>(out var route))
                    {
                        if (route.MessageTypeID == 0) continue;
                        id_map.Add(route.MessageTypeID, route);
                    }
                    if (field.TryGetAttribute<GenCodeCreateAttribute>(out var create))
                    {
                        if (create.MessageTypeID == 0) continue;
                        create_map.Add(create.MessageTypeID, (Func<Type, object>)field.GetValue(null));
                        type_map.Add(create.MessageType, create);
                    }
                    if (field.TryGetAttribute<GenCodeReadAttribute>(out var read))
                    {
                        if (read.MessageTypeID == 0) continue;
                        read_map.Add(read.MessageTypeID, (Action<IInputStream, object>)field.GetValue(null));
                    }
                    if (field.TryGetAttribute<GenCodeWriteAttribute>(out var write))
                    {
                        if (write.MessageTypeID == 0) continue;
                        write_map.Add(write.MessageTypeID, (Action<IOutputStream, object>)field.GetValue(null));
                    }
                }
                catch (Exception err)
                {
                    log.Error($"Regist codec Error : Codec={codecType.FullName} Field={field?.Name}");
                    throw err;
                }
            }
            var added = new Dictionary<Type, Type>();
            foreach (var id in id_map.Values)
            {
                try
                {
                    var type = id.MessageType;
                    create_map.TryGetValue(id.MessageTypeID, out var create);
                    if (typeof(IExternalizable).IsAssignableFrom(type))
                    {
                        if (this.RegistExternalizable(type, create, id.MessageTypeID))
                        {
                            added.Add(id.MessageType, id.MessageType);
                        }
                    }
                    else
                    {
                        read_map.TryGetValue(id.MessageTypeID, out var read);
                        write_map.TryGetValue(id.MessageTypeID, out var write);
                        if (this.RegistClass(id.MessageTypeID, id.MessageType, create, read, write))
                        {
                            added.Add(id.MessageType, id.MessageType);
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Error($"Regist codec Error : Codec={codecType.FullName} MessageTypeID={id.MessageTypeID} MessageType={id.MessageType}");
                    throw err;
                }
            }
            foreach (var idtype in id_map.Values)
            {
                foreach (var field in idtype.MessageType.GetFields())
                {
                    if (field.IsPublic && !field.IsStatic)
                    {
                        var ftype = field.FieldType;
                        if (typeof(IExternalizable).IsAssignableFrom(ftype))
                        {
                            if (ftype.IsGenericType)
                            {
                                ftype = ftype.GetGenericTypeDefinition();
                            }
                            if (!ftype.IsAbstract && !added.ContainsKey(ftype))
                            {
                                int? msgid = null;
                                if (type_map.TryGetValue(ftype, out var attr))
                                {
                                    msgid = attr.MessageTypeID;
                                }
                                else if (ftype.TryGetAttribute<MessageTypeAttribute>(out var attrID))
                                {
                                    msgid = attrID.MessageTypeID;
                                }
                                //if (msgid.HasValue)
                                {
                                    create_map.TryGetValue(msgid ?? 0, out var create);
                                    if (RegistExternalizable(ftype, create ?? (static t => DeepActivator.CreateInstance(t)), msgid))
                                    {
                                        added.Add(ftype, ftype);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void RegistCodec(IExternalizableFactory codec)
        {
            foreach (var c in codec.AllTypes)
            {
                if (!types_c2i.ContainsKey(c.MessageType))
                {
                    TryAddType(c);
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------

        public string ListAll(string prefix = "")
        {
            var sb = new StringWriter();
            {
                var types = new List<TypeCodec>(all_types.Values);
                types.Sort(this);
                foreach (var type in types)
                {
                    sb.WriteLine(prefix + string.Format("0x{0:X8}", type.MessageID) + " - " + type.MessageType.FullName);
                }
                return sb.ToString();
            }
        }
        public List<TypeCodec> ListAllCodec()
        {
            return new List<TypeCodec>(all_types.Values);
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------

        protected virtual void DoReadExternal(IInputStream input, object data)
        {
            (data as IExternalizable).ReadExternal(input);
        }
        protected virtual void DoWriteExternal(IOutputStream output, object data)
        {
            (data as IExternalizable).WriteExternal(output);
        }
    }
}

using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeepCore.Protocol
{

    public class MessageCodeManager
    {
        public static MessageCodeManager Instance { get; private set; }
        public ISerializerFactory Factory { get => factory; }
        private readonly ISerializerFactory factory;
        private readonly HashMap<int, ResponseErrorCodes> id_codes = new HashMap<int, ResponseErrorCodes>();
        private readonly HashMap<string, ResponseErrorCodes> type_codes = new HashMap<string, ResponseErrorCodes>();
        private readonly HashMap<int, ValueTuple<string, string>> default_error_code = new();

        private Properties text_codes;
        public MessageCodeManager(ISerializerFactory factory)
        {
            Instance = this;
            this.factory = factory;
            foreach (var codec in factory.AllTypes)
            {
                var codes = new ResponseErrorCodes(codec.MessageType);
                if (codes.IsNotEmpty)
                {
                    if (codec.MessageID != 0) { id_codes.Add(codec.MessageID, codes); }
                    type_codes.Add(codes.TypeFullName, codes);
                }
            }
        }
        public void Load(Properties path)
        {
            this.text_codes = path;
            if (text_codes != null)
            {
                foreach (var codes in type_codes.Values)
                {
                    var sub = text_codes.SubProperties(codes.TypeFullName + ":");
                    codes.Load(sub);
                }
                foreach (var code in default_error_code.ToArray())
                {
                    if (text_codes.TryGetValue(code.Value.Item1, out var item2) && !string.IsNullOrEmpty(item2))
                    {
                        default_error_code.Put(code.Key, (code.Value.Item1, item2));
                    }
                }
            }
        }
        public void Load(string path)
        {
            this.text_codes = Properties.LoadFromResource(path);
            Load(text_codes);
        }
        public void Save(string path)
        {
            var prop = Save();
            var text = prop.ToString();
            CFiles.CreateFile(path);
            System.IO.File.WriteAllText(path, text, CUtils.UTF8_BOM);
        }
        public Properties Save()
        {
            Properties prop = new Properties();
            foreach (var e in type_codes)
            {
                var sub = e.Value.Save();
                prop.PutAll(sub, e.Key + ":");
            }
            foreach (var code in default_error_code)
            {
                prop.Put(code.Value.Item1, code.Value.Item2);
            }
            return prop;
        }

        //------------------------------------------------------------------------------------------------------------


        //------------------------------------------------------------------------------------------------------------

        public virtual string GetCodeMessage(Response rsp)
        {
            var codec = Factory.GetCodec(rsp.GetType());
            var s2c_code = rsp.s2c_code;
            var type = rsp.GetType();
            var typeName = type.ToTypeDefineFullName();
            if (codec != null)
            {
                var typecodes = codec.MessageID == 0 ? type_codes.Get(typeName) : id_codes.Get(codec.MessageID);
                if (typecodes != null)
                {
                    if (typecodes.TryGetCodeMessage(rsp, out var msg))
                    {
                        return msg;
                    }
                }
            }
            if (text_codes != null )
            {
                if (text_codes.TryGetValue($"{typeName}:{s2c_code}", out var text))
                {
                    return text;
                }
            }
            if (default_error_code.TryGetValue(s2c_code, out var code))
            {
                return code.Item2;
            }
            return string.Empty;
        }
//         public virtual string GetCodeMessage(string typeName, int s2c_code)
//         {
//             if (type_codes.TryGetValue(typeName, out var codes))
//             {
//                 if (codes.TryGetCodeMessage(s2c_code, out var msg))
//                 {
//                     return msg;
//                 }
//             }
//             if (text_codes != null && text_codes.TryGetValue($"{typeName}:{s2c_code}", out var text))
//             {
//                 return text;
//             }
//             if (default_error_code.TryGetValue(s2c_code, out var code))
//             {
//                 return code.Item2;
//             }
//             return string.Empty;
//         }
        public void RegistErrorCodeEnum(Type enumType)
        {
            Type type = enumType;
            foreach (var name in Enum.GetNames(type))
            {
                var field = type.GetField(name);
                if (PropertyUtil.TryGetEnumValueAndAttribute(type, name, out int id, out MessageCodeAttribute attr))
                {
                    default_error_code.Add(id, (name, attr.Message));
                }
            }
        }
        //------------------------------------------------------------------------------------------------------------

        public class ResponseErrorCodes
        {
            protected HashMap<int, ErrorCode> error_codes;
            public string TypeFullName { get; private set; }
            public ResponseErrorCodes(Type type)
            {
                this.TypeFullName = type.ToTypeDefineFullName();
                {
                    List<ErrorCode> list = default;
                    var fields = PropertyUtil.GetFields(type, BindingFlags.Static | BindingFlags.Public);
                    foreach (var field in fields)
                    {
                        var attr = PropertyUtil.GetAttribute<MessageCodeAttribute>(field);
                        if (attr != null)
                        {
                            if (field.DeclaringType == type && field.IsLiteral && field.IsStatic && field.FieldType == typeof(int))
                            {
                                var code = (int)field.GetValue(null);
                                if (list == null) list = new();
                                list.Add(new ErrorCode(type, code, attr));
                            }
                        }

                    }
                    if (list != null)
                    {
                        foreach (var error_code in list)
                        {
                            try
                            {
                                if (error_codes == null) error_codes = new HashMap<int, ErrorCode>();
                                error_codes.TryAdd(error_code.Code, error_code);
                            }
                            catch
                            {
                                throw new Exception($"AddErrorCodeError: Type={type.FullName} Code={error_code.Code}");
                            }
                        }
                    }
                }
            }
            public bool IsNotEmpty { get => error_codes != null && error_codes.Count > 0; }
            internal void Load(Properties text)
            {
                if (error_codes != null)
                {
                    foreach (var e in text)
                    {
                        if (Parser.TryParseInt(e.Key, out var id))
                        {
                            if (error_codes.TryGetValue(id, out var code))
                            {
                                code.Load(e.Value);
                            }
                        }
                    }
                }
            }
            internal Properties Save()
            {
                Properties ret = new Properties();
                if (error_codes != null)
                {
                    foreach (var e in error_codes)
                    {
                        ret[e.Key.ToString()] = e.Value.Save();
                    }
                }
                return ret;
            }
            internal bool TryGetCodeMessage(Response rsp, out string msg)
            {
                if (error_codes != null)
                {
                    if (error_codes.TryGetValue(rsp.s2c_code, out var code))
                    {
                        msg = code.GetCodeMessage(rsp);
                        return true;
                    }
                }
                msg = null;
                return false;
            }
            internal bool TryGetCodeMessage(int s2c_code, out string msg)
            {
                if (error_codes != null)
                {
                    if (error_codes.TryGetValue(s2c_code, out var code))
                    {
                        msg = code.GetCodeMessage();
                        return true;
                    }
                }
                msg = null;
                return false;
            }
        }
        public class ErrorCode
        {
            protected readonly int code;
            protected readonly MessageCodeAttribute owner_attribute;
            protected readonly FieldInfo[] args;
            protected readonly object[] args_str;
            private string message;
            public int Code { get { return code; } }
            public MessageCodeAttribute CodeAttribute { get { return owner_attribute; } }
            public FieldInfo[] ArgsField { get { return args; } }
            public ErrorCode(Type owner_type, int code, MessageCodeAttribute attr)
            {
                this.code = code;
                this.owner_attribute = attr;
                this.message = attr.Message;
                if (attr.Args != null)
                {
                    var list = new List<FieldInfo>(attr.Args.Length);
                    {
                        foreach (var fileName in attr.Args)
                        {
                            var field = owner_type.GetField(fileName);
                            if (field == null)
                            {
                                throw new Exception(string.Format("错误代码文字字段不存在: Type={0} Code={1} FieldName={2}", owner_type, code, fileName));
                            }
                            list.Add(field);
                        }
                        this.args = list.ToArray();
                        this.args_str = new object[args.Length];
                    }
                }
            }
            internal void Load(string text)
            {
                this.message = text;
            }
            internal string Save()
            {
                return this.message;
            }
            public string GetCodeMessage(Response rsp)
            {
                try
                {
                    if (args == null) return message;
                    for (int i = 0; i < args.Length; i++)
                    {
                        args_str[i] = args[i].GetValue(rsp);
                    }
                    return string.Format(message, args_str);
                }
                catch (Exception err)
                {
                    throw new Exception(string.Format("错误代码文字错误：Type={0} Code={1} Error={2}", rsp.GetType(), rsp.s2c_code, err.Message), err);
                }
            }
            public string GetCodeMessage()
            {
                return message;
            }
        }
    }

}

using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeepCore.Json
{
    public class JsonFormat
    {
        public static readonly JsonFormat Default = new JsonFormat() { };
    }

    //--------------------------------------------------------------------------------------------------------------------

    public class JsonParser : ParserAdapter
    {
        protected readonly Logger log;
        protected readonly JsonFormat format;
        protected readonly TextConverters converters = new TextConverters();
        public JsonParser(JsonFormat format)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.format = format;
        }
        public JsonParser()
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.format = JsonFormat.Default;
        }
        public virtual string ToString(object obj)
        {
            var output = CreateStringBuilder();
            EncodeObject(output, obj?.GetType(), null, obj);
            return output.ToString();
        }
        public virtual bool TryParse(string text, Type type, out object ret)
        {
            ret = DecodeObject(new JsonReader(text), type, null);
            return true;
        }
        //--------------------------------------------------------------------------------------
        protected virtual IDynamicTypeInfo GetTypeInfo(Type type)
        {
            return DynamicTypeFactory.Instance.GetTypeInfo(type);
        }
        protected virtual Type GetType(string name)
        {
            return ReflectionUtil.GetType(name);
        }
        protected virtual string TypeToString(Type type)
        {
            return type?.FullName;
        }
        protected virtual StringBuilder CreateStringBuilder()
        {
            return new StringBuilder(256);
        }
        //----------------------------------------------------------------------------------------------------------------------------
        // Basic IO
        //----------------------------------------------------------------------------------------------------------------------------
        #region Encoder

        private void EncodeArray(StringBuilder output, Type decleardType, Array array)
        {
            output.Append('{');
            var type = array.GetType();
            var etype = type.GetElementType();
            if (decleardType != type)
            {
                output.Append("\"@t\":\"").Append(TypeToString(type)).Append("\",");
            }
            int count = array.Length;
            if (count > 0)
            {
                converters.TryGetConverter(etype, out var elementConverter);
                output.Append("\"@c\":\"").Append(count).Append("\",");
                output.Append("\"@A\":[");
                int index = 0;
                foreach (var e in array)
                {
                    EncodeObject(output, etype, elementConverter, e);
                    if (++index < count) output.Append(',');
                }
                output.Append(']');
            }
            else
            {
                output.Append("\"@c\":\"0\"");
            }
            output.Append('}');
        }
        private void EncodeList(StringBuilder output, Type decleardType, IList list)
        {
            output.Append('{');
            var type = list.GetType();
            var etype = typeof(object);
            if (type.IsGenericType)
            {
                var gargs = type.GetGenericArguments();
                etype = gargs[0];
            }
            if (decleardType != type)
            {
                output.Append("\"@t\":\"").Append(TypeToString(type)).Append("\",");
            }
            int count = list.Count;
            if (count > 0)
            {
                converters.TryGetConverter(etype, out var elementConverter);
                output.Append("\"@L\":[");
                int index = 0;
                foreach (var e in list)
                {
                    EncodeObject(output, etype, elementConverter, e);
                    if (++index < count) output.Append(',');
                }
                output.Append(']');
            }
            else
            {
                output.Append("\"@c\":\"0\"");
            }
            output.Append('}');
        }
        private void EncodeDictionary(StringBuilder output, Type decleardType, IDictionary map)
        {
            output.Append('{');
            var type = map.GetType();
            var ktype = typeof(object);
            var vtype = typeof(object);
            if (type.IsGenericType)
            {
                var gargs = type.GetGenericArguments();
                ktype = gargs[0];
                vtype = gargs[1];
            }
            if (decleardType != type)
            {
                output.Append("\"@t\":\"").Append(TypeToString(type)).Append("\",");
            }
            int count = map.Count;
            if (count > 0)
            {
                if (converters.TryGetConverter(ktype, out var keyConverter))
                {
                    converters.TryGetConverter(vtype, out var valueConverter);
                    output.Append("\"@M\":{");
                    int index = 0;
                    var e = map.GetEnumerator();
                    while (e.MoveNext())
                    {
                        //output.Append('"').Append(keyConvert.Encode(ktype, e.Key)).Append("\":");
                        EncodePrimitive(output, ktype, keyConverter, e.Key);
                        output.Append(':');
                        EncodeObject(output, vtype, valueConverter, e.Value);
                        if (++index < count) output.Append(',');
                    }
                    output.Append('}');
                }
                else
                {
                    output.Append("\"@M\":{");
                    int index = 0;
                    var e = map.GetEnumerator();
                    while (e.MoveNext())
                    {
                        ktype = e.Key.GetType();
                        vtype = e.Value.GetType(); 
                        converters.TryGetConverter(ktype, out  keyConverter);
                        converters.TryGetConverter(vtype, out var valueConverter);
                        //output.Append('"').Append(keyConvert.Encode(ktype, e.Key)).Append("\":");
                        EncodePrimitive(output, ktype, keyConverter, e.Key);
                        output.Append(':');
                        EncodeObject(output, vtype, valueConverter, e.Value);
                        if (++index < count) output.Append(',');
                    }
                    output.Append('}');
                    //throw new Exception("Dictionary Key Type Not Primitive : " + type.ToTypeDefineFullName());
                }
            }
            else
            {
                output.Append("\"@c\":\"0\"");
            }
            output.Append('}');
        }
        private void EncodeFields(StringBuilder output, Type decleardType, object data)
        {
            output.Append('{');
            var type = data.GetType();
            var typeInfo = GetTypeInfo(type);
            var members = typeInfo.GetFields();
            if (decleardType != type)
            {
                output.Append("\"@t\":\"").Append(TypeToString(type)).Append("\",");
            }
            int count = members.Length;
            if (count > 0)
            {
                output.Append("\"@C\":{");
                int index = 0;
                foreach (var member in members)
                {
                    var memberValue = member.GetValue(data);
                    output.Append('"').Append(member.Name).Append("\":");
                    EncodeObject(output, member.Field.FieldType, null, memberValue);
                    if (++index < count) output.Append(',');
                }
                output.Append('}');
            }
            else
            {
                output.Append("\"@c\":\"0\"");
            }
            output.Append('}');
        }
        private void EncodePrimitive(StringBuilder output, Type decleardType, ITextConverter converter, object data)
        {
            if (decleardType != data.GetType())
            {
                output.Append("{\"@t\":\"");
                output.Append(TypeToString(data.GetType()));
                output.Append("\",\"@V\":\"");
                if (data is string str)
                {
                    output.Append(str.Length).Append('+').Append(str);
                }
                else
                {
                    output.Append(converter.Encode(decleardType, data));
                }
                output.Append("\"}");
            }
            else
            {
                if (data is string str)
                {
                    output.Append('"').Append(str.Length).Append('+').Append(str).Append('"');
                }
                else
                {
                    output.Append('"').Append(converter.Encode(decleardType, data)).Append('"');
                }
            }
        }
        private void EncodeObject(StringBuilder output, Type decleardType, ITextConverter converter, object data)
        {
            if (data != null)
            {
                Type type = data.GetType();
                if (converter != null)
                {
                    EncodePrimitive(output, decleardType, converter, data);
                }
                else if (converters.TryGetConverter(type, out converter))
                {
                    EncodePrimitive(output, decleardType, converter, data);
                }
                else if (type.IsArray)
                {
                    EncodeArray(output, decleardType, (Array)data);
                }
                else if (data is IDictionary)
                {
                    EncodeDictionary(output, decleardType, (IDictionary)data);
                }
                else if (data is IList)
                {
                    EncodeList(output, decleardType, (IList)data);
                }
                else
                {
                    EncodeFields(output, decleardType, data);
                }
            }
            else
            {
                output.Append("\"\"");
            }
        }
        public virtual string EncodeObject(object obj, Type decleardType)
        {
            var output = CreateStringBuilder();
            EncodeObject(output, decleardType, null, obj);
            return output.ToString();
        }
        public virtual string EncodeObject(object obj)
        {
            var output = CreateStringBuilder();
            EncodeObject(output, obj.GetType(), null, obj);
            return output.ToString();
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Decoder

        private void DecodeArray(JsonReader input, Array array)
        {
            input.MoveToChar('[');
            var type = array.GetType();
            var etype = type.GetElementType();
            var index = 0;
            converters.TryGetConverter(etype, out var elementConverter);
            while (true)
            {
                var element = DecodeObject(input, etype, elementConverter);
                array.SetValue(element, index);
                index++;
                if (input.TryEndRegion(REGION_NEXT_CHARS[2]))
                {
                    break;
                }
            }
        }
        private void DecodeList(JsonReader input, IList list)
        {
            input.MoveToChar('[');
            var type = list.GetType();
            var etype = typeof(object);
            if (type.IsGenericType)
            {
                var gargs = type.GetGenericArguments();
                etype = gargs[0];
            }
            converters.TryGetConverter(etype, out var elementConverter);
            while (true)
            {
                var element = DecodeObject(input, etype, elementConverter);
                list.Add(element);
                if (input.TryEndRegion(REGION_NEXT_CHARS[2]))
                {
                    break;
                }
            }
        }
        private void DecodeDictionary(JsonReader input, IDictionary map)
        {
            input.MoveToChar('{');
            var type = map.GetType();
            var ktype = typeof(object);
            var vtype = typeof(object);
            if (type.IsGenericType)
            {
                var gargs = type.GetGenericArguments();
                ktype = gargs[0];
                vtype = gargs[1];
            }
            if (converters.TryGetConverter(ktype, out var keyConverter))
            {
                converters.TryGetConverter(vtype, out var valueConverter);
                while (true)
                {
                    var key = DecodePrimitive(input, ktype, keyConverter);
                    //input.MoveToChar(':');//next is " //
                    var value = DecodeObject(input, vtype, valueConverter);
                    map.Add(key, value);
                    if (input.TryEndRegion(REGION_NEXT_CHARS[0]))
                    {
                        break;
                    }
                }
            }
            else
            {
                while (true)
                {
                    var key = DecodePrimitive(input, typeof(string), null);
                    //input.MoveToChar(':');//next is " //
                    var value = DecodePrimitive(input, typeof(string), null);
                    map.Add(key, value);
                    if (input.TryEndRegion(REGION_NEXT_CHARS[0]))
                    {
                        break;
                    }
                }
                //throw new Exception("Dictionary Key Type Not Primitive : " + type.ToTypeDefineFullName());
            }
        }
        private void DecodeFields(JsonReader input, IDynamicTypeInfo typeInfo, object obj)
        {
            input.MoveToChar('{');
            while (input.TryReadFieldKey(out var keyText))
            {
                var member = typeInfo.GetField(keyText);
                if (member != null)
                {
                    var value = DecodeObject(input, member.Field.FieldType, null);
                    member.SetValue(obj, value);
                }
                else
                {
                    log.Warn("已删除无用的字段:\n" + DumpJsonNode(input));
                }
                if (input.TryEndRegion(REGION_NEXT_CHARS[0]))
                {
                    break;
                }
            }
        }
        private object DecodePrimitive(JsonReader input, Type decleardType, ITextConverter converter)
        {
            input.MoveToChar('"');
            if (converter != null)
            {
                if (decleardType == typeof(string))
                {
                    input.ReadToAny(TEXT_CHARS, out var prefix, out char endChar);
                    if (endChar == '+')
                    {
                        var text = input.Read(Parser.ParseInt(prefix));
                        input.MoveToChar('"');
                        return text;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    input.ReadToChar('"', out var text);
                    try
                    {
                        return converter.Decode(decleardType, text);
                    }
                    catch (Exception err)
                    {
                        log.Error($"Decode Error Error With Type : {decleardType.FullName} : From Text '{text}' : {err.Message}", err);
                        if (decleardType.IsValueType)
                        {
                            return DeepActivator.CreateInstance(decleardType);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            else
            {
                if (decleardType == typeof(string))
                {
                    input.ReadToAny(TEXT_CHARS, out var prefix, out char endChar);
                    if (endChar == '+')
                    {
                        var text = input.Read(Parser.ParseInt(prefix));
                        input.MoveToChar('"');
                        return text;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    input.MoveToChar('"');
                    return null;
                }
            }
        }
        private object DecodeObject(JsonReader input, Type decleardType, ITextConverter converter)
        {
            try
            {
                input.MoveToAny(REGION_BEGIN, out var beginChar);
                if (beginChar == '{')
                {
                    object ret = null;
                    int count = 0;
                    while (input.TryReadFieldKey(out var keyText))
                    {
                        switch (keyText)
                        {
                            case "@t":
                                {
                                    var typeName = input.ReadFieldValue();
                                    if (typeName != TypeToString(decleardType))
                                    {
                                        var etype = this.GetType(typeName);
                                        if (etype != null) { decleardType = etype; }
                                    }
                                }
                                break;
                            case "@c":
                                {
                                    count = Parser.ParseInt(input.ReadFieldValue());
                                }
                                break;
                            case "@A":
                                {
                                    var array = Array.CreateInstance(decleardType.GetElementType(), count);
                                    DecodeArray(input, array);
                                    ret = array;
                                }
                                break;
                            case "@L":
                                {
                                    var list = (IList)DeepActivator.CreateInstance(decleardType);
                                    DecodeList(input, list);
                                    ret = list;
                                }
                                break;
                            case "@M":
                                {
                                    var map = (IDictionary)DeepActivator.CreateInstance(decleardType);
                                    DecodeDictionary(input, map);
                                    ret = map;
                                }
                                break;
                            case "@C":
                                {
                                    if (decleardType != null)
                                    {
                                        var typeInfo = GetTypeInfo(decleardType);
                                        var fields = typeInfo.CreateInstance();
                                        DecodeFields(input, typeInfo, fields);
                                        ret = fields;
                                    }
                                    else
                                    {
                                        log.Warn("已删除无用的类型:\n" + DumpJsonNode(input));
                                        return null;
                                    }
                                }
                                break;
                            case "@V":
                                {
                                    converters.TryGetConverter(decleardType, out converter);
                                    ret = DecodePrimitive(input, decleardType, converter);
                                }
                                break;
                        }
                        if (input.TryEndRegion(REGION_NEXT_CHARS[0]))
                        {
                            break;
                        }
                    }
                    if (ret == null)
                    {
                        ret = ReflectionUtil.CreateInstance(decleardType);
                    }
                    return ret;
                }
                else if (beginChar == '"')
                {
                    input.Move(-1);
                    if (converter == null)
                    {
                        converters.TryGetConverter(decleardType, out converter);
                    }
                    return DecodePrimitive(input, decleardType, converter);
                }
                else
                {
                    throw input.DumpError();
                }
            }
            catch
            {
                throw;
            }
        }
        public object DecodeObject(string input, Type type)
        {
            return DecodeObject(new JsonReader(input), type, null);
        }

        private bool TryDecodeObject(JsonReader input, Type decleardType, ITextConverter converter, out object ret)
        {
            try
            {
                ret = DecodeObject(input, decleardType, converter);
                return true;
            }
            catch
            {
                ret = null;
                return false;
            }

        }
        public bool TryDecodeObject(string input, Type type, out object ret)
        {
            return TryDecodeObject(new JsonReader(input), type, null, out ret);
        }


        public static void DecodeJsonNode(JsonReader input, JsonNode node)
        {
            input.MoveToAny(REGION_BEGIN, out var beginChar);
            if (beginChar == '{')
            {
                while (input.TryReadFieldKey(out var keyText))
                {
                    var sub = node.Add(keyText);
                    DecodeJsonNode(input, sub);
                    if (input.TryEndRegion(REGION_NEXT_CHARS[0]))
                    {
                        break;
                    }
                }
            }
            else if (beginChar == '[')
            {
                int index = 0;
                while (true)
                {
                    var sub = node.Add(index.ToString());
                    DecodeJsonNode(input, sub);
                    index++;
                    if (input.TryEndRegion(REGION_NEXT_CHARS[2]))
                    {
                        break;
                    }
                }
            }
            else if (beginChar == '"')
            {
                input.ReadToChar('"', out var valueText);
                node.Value = valueText;
            }
            else
            {
                throw input.DumpError();
            }
        }
        public static string DumpJsonNode(JsonReader input)
        {
            var begin = input.Position;
            var node = DecodeJsonNode(input);
            return input.Dump(begin, input.Position - begin);
        }
        public static JsonNode DecodeJsonNode(JsonReader input)
        {
            var ret = new JsonNode(string.Empty);
            DecodeJsonNode(input, ret);
            return ret;
        }
        public static JsonNode DecodeJsonNode(string input)
        {
            var ret = new JsonNode(string.Empty);
            var reader = new JsonReader(input);
            DecodeJsonNode(reader, ret);
            return ret;
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        private static readonly char[] REGION_BEGIN = { '{', '"', '[' };
        private static readonly char[] REGION_END = { '}', '"', ']' };
        private static readonly char[][] REGION_NEXT_CHARS = {
            new  char[] { '}', ',' },
            new  char[] { '"', ',' },
            new  char[] { ']', ',' }};
        private static readonly char[] TEXT_CHARS = { '+', '"' };
        public class JsonReader
        {
            private string input;
            private int pos;
            public JsonReader(string s)
            {
                this.input = s;
                this.pos = 0;
            }
            public int Position
            {
                get => pos;
            }
            public string DumpLeft
            {
                get
                {
                    var right = Math.Max(0, pos);
                    right = Math.Min(input.Length, pos);
                    var left = Math.Max(0, right - 128);
                    return input.Substring(left, right - left);
                }
            }
            public string DumpRight
            {
                get
                {
                    var left = Math.Max(0, pos);
                    left = Math.Min(input.Length, pos);
                    var right = Math.Min(input.Length, left + 128);
                    return input.Substring(left, right - left);
                }
            }
            public string DumpText
            {
                get
                {
                    return $"{DumpLeft}囧{DumpRight}";
                }
            }
            public string Dump(int begin, int count)
            {
                return input.Substring(begin, count);
            }
            public Exception DumpError()
            {
                return new Exception($"Decode error at : position={pos}...{Environment.NewLine}{DumpText}...");
            }
            public JsonReader Move(int offset)
            {
                this.pos += offset;
                return this;
            }
            public JsonReader MoveToChar(char ch)
            {
                int index = input.IndexOf(ch, pos);
                if (index < 0) { throw new EndOfStreamException("EOF", DumpError()); }
                this.pos = index + 1;
                return this;
            }
            public JsonReader MoveToAny(char[] ch, out char readed)
            {
                int index = input.IndexOfAny(ch, pos);
                if (index < 0) { throw new EndOfStreamException("EOF", DumpError()); }
                readed = input[index];
                this.pos = index + 1;
                return this;
            }
            public string Read(int count)
            {
                var ret = input.Substring(pos, count);
                this.pos += (count);
                return ret;
            }
            public JsonReader ReadToChar(char ch, out string text)
            {
                int index = input.IndexOf(ch, pos);
                if (index < 0) { throw new EndOfStreamException("EOF", DumpError()); }
                text = input.Substring(pos, index - pos);
                this.pos = index + 1;
                return this;
            }
            public JsonReader ReadToAny(char[] ch, out string text, out char readed)
            {
                int index = input.IndexOfAny(ch, pos);
                if (index < 0) { throw new EndOfStreamException("EOF", DumpError()); }
                text = input.Substring(pos, index - pos);
                readed = input[index];
                this.pos = index + 1;
                return this;
            }
            public bool TryEndRegion(char[] endChars)
            {
                MoveToAny(endChars, out char endChar);
                if (endChar == ',')
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public bool TryReadFieldKey(out string keyText)
            {
                MoveToAny(REGION_END, out char endChar);
                if (endChar == '"')
                {
                    ReadToChar('"', out keyText);
                    MoveToChar(':');
                    return true;
                }
                else
                {
                    keyText = null;
                    return false;
                }
            }
            public string ReadFieldValue()
            {
                MoveToChar('"');
                ReadToChar('"', out var text);
                return text;
            }
        }
        public class JsonNode : IEnumerable<KeyValuePair<string, JsonNode>>
        {
            private HashMap<string, JsonNode> childs;
            public string Name { get; private set; }
            public string Value { get; internal set; }
            public IEnumerable<string> Keys => childs.Keys;
            public IEnumerable<JsonNode> Values => childs.Values;
            public int Count => childs.Count;
            public JsonNode this[string key] { get => childs[key]; }
            public JsonNode(string name)
            {
                this.Name = name;
            }
            internal JsonNode Add(string key)
            {
                if (childs == null) childs = new HashMap<string, JsonNode>();
                return childs.GetOrAdd(key, static (k) => new JsonNode(k));
            }
            public override string ToString()
            {
                return Name;
            }
            public bool ContainsKey(string key)
            {
                return childs.ContainsKey(key);
            }
            public bool TryGetValue(string key, out JsonNode value)
            {
                return childs.TryGetValue(key, out value);
            }
            public IEnumerator<KeyValuePair<string, JsonNode>> GetEnumerator()
            {
                return childs.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)childs).GetEnumerator();
            }
        }
    }

}

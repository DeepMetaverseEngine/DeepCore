using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Json.JsonParser;

namespace DeepCore.Json
{
    public static class JsonUtil
    {
        private static JsonParser parser = new JsonParser();
        public static void SetParser(JsonParser parser)
        {
            JsonUtil.parser = parser;
        }
        public static string EncodeObject(object obj, Type decleardType)
        {
            return parser.EncodeObject(obj, decleardType);
        }
        public static string EncodeObject(object obj)
        {
            return parser.EncodeObject(obj, null);
        }
        public static string EncodeObject<T>(T obj)
        {
            return parser.EncodeObject(obj, typeof(T));
        }

        public static object DecodeObject(string input, Type type)
        {
            return parser.DecodeObject(input, type);
        }
        public static object DecodeObject(string input)
        {
            return parser.DecodeObject(input, null);
        }
        public static T DecodeObject<T>(string input)
        {
            return (T)parser.DecodeObject(input, typeof(T));
        }

        public static bool TryDecodeObject(string input, Type type, out object ret)
        {
            return parser.TryDecodeObject(input, type, out ret);
        }
        public static bool TryDecodeObject(string input, out object ret)
        {
            return parser.TryDecodeObject(input, null, out ret);
        }
        public static bool TryDecodeObject<T>(string input, out T ret)
        {
            if (parser.TryDecodeObject(input, null, out var _ret))
            {
                ret = (T)_ret;
                return true;
            }
            ret = default(T);
            return false;
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCrystal
{
    public static class JSON
    {
        public static JsonSerializerSettings settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };
        public static object Deserialize(string text)
        {
            return JsonConvert.DeserializeObject(text, settings);
        }
        public static object Deserialize(string text, Type type)
        {
            return JsonConvert.DeserializeObject(text, type, settings);
        }
        public static T Deserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text, settings);
        }
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, settings);
        }

    }
}

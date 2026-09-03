using DeepCore.Event.EventSystem.Events;
using DeepCore.Event.EventSystem.Message;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeepCore.Event.EventSystem
{
    public static class Decorator
    {
        public class ArgumentOutputCollect
        {
            public bool ArgIndex = true;
            public bool OutputIndex = true;
            public string Category;
            public List<KeyValuePair<FieldInfo, EventFieldAttribute>> Arg = new List<KeyValuePair<FieldInfo, EventFieldAttribute>>();
            public List<KeyValuePair<FieldInfo, EventFieldAttribute>> Output = new List<KeyValuePair<FieldInfo, EventFieldAttribute>>();

            public bool IsSyncEvent => Category.EndsWith("Sync");
        }

        private static readonly HashMap<Type, ArgumentOutputCollect> ArgumentFields = new HashMap<Type, ArgumentOutputCollect>();

        private static readonly SafeDictionary<string, Type> AllEventType = new SafeDictionary<string, Type>();

        public static ArgumentOutputCollect Get(Type t)
        {
            return ArgumentFields.Get(t);
        }

        public static bool IsSyncEvent(BaseEvent e)
        {
            var einfo = Get(e.GetType());
            return einfo?.IsSyncEvent ?? false;
        }

        public static Type GetType(string fullName)
        {
            return AllEventType.Get(fullName);
        }

        public static void Collect()
        {
            var all = ReflectionUtil.GetNoneVirtualSubTypes(typeof(BaseEvent));
            foreach (var type in all)
            {
                AllEventType.Add(type.FullName, type);

                var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var fs = new ArgumentOutputCollect();
                var es = type.GetCustomAttributes(typeof(EventAttribute), true);
                if (es.Length > 0)
                {
                    fs.Category = ((EventAttribute)es[0]).Category;
                }

                foreach (var f in fields)
                {
                    var attrs = f.GetCustomAttributes(typeof(EventArgumentAttribute), true);
                    if (attrs.Length > 0)
                    {
                        var argAtr = (EventArgumentAttribute)attrs[0];
                        if (fs.ArgIndex && argAtr.Index < 0)
                        {
                            fs.ArgIndex = false;
                        }

                        if (fs.ArgIndex && argAtr.IsNamedField)
                        {
                            throw new NotSupportedException("not support index & name");
                        }

                        if (!fs.ArgIndex && argAtr.IsIndexField)
                        {
                            throw new NotSupportedException("not support index & name");
                        }

                        fs.Arg.Add(new KeyValuePair<FieldInfo, EventFieldAttribute>(f, argAtr));
                        if (string.IsNullOrEmpty(argAtr.Name))
                        {
                            argAtr.Name = f.Name;
                        }
                    }

                    attrs = f.GetCustomAttributes(typeof(EventOutputAttribute), true);

                    if (f.GetCustomAttributes(typeof(EventOutputAttribute), true).Length > 0)
                    {
                        var oAtr = (EventOutputAttribute)attrs[0];
                        if (fs.OutputIndex && oAtr.Index < 0)
                        {
                            fs.OutputIndex = false;
                        }

                        fs.Output.Add(new KeyValuePair<FieldInfo, EventFieldAttribute>(f, (EventOutputAttribute)attrs[0]));
                        if (string.IsNullOrEmpty(oAtr.Name))
                        {
                            oAtr.Name = f.Name;
                        }
                    }
                }

                fs.Arg.Sort((x, y) =>
                {
                    if (fs.ArgIndex)
                    {
                        return x.Value.Index.CompareTo(y.Value.Index);
                    }

                    return string.Compare(x.Value.Name, y.Value.Name, StringComparison.Ordinal);
                });

                fs.Output.Sort((x, y) =>
                {
                    if (fs.OutputIndex)
                    {
                        return x.Value.Index.CompareTo(y.Value.Index);
                    }

                    return string.Compare(x.Value.Name, y.Value.Name, StringComparison.Ordinal);
                });

                ArgumentFields.Add(type, fs);
            }


            var allMessages = ReflectionUtil.GetNoneVirtualSubTypes(typeof(EventMessage));
            for (var i = 0; i < allMessages.Count; i++)
            {
                var type = allMessages[i];
                EventMessage.Codec.RegisterMessag(type, i + 1);
            }
        }
    }
}
using System;

namespace DeepCore.Event.EventSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EventAttribute : Attribute
    {
        public readonly string Desc;
        public readonly string Category;
        public EventAttribute(string desc, string category)
        {
            Desc = desc;
            Category = category;
        }
    }


    [AttributeUsage(AttributeTargets.Field)]
    public class EventFieldAttribute : Attribute
    {
        public readonly string Desc;
        public readonly int Index;
        public string Name { get; internal set; }

        public EventFieldAttribute(string desc, int index)
        {
            Desc = desc;
            Index = index;
            Name = null;
        }

        public EventFieldAttribute(string desc)
        {
            Desc = desc;
            Index = -1;
        }


        public bool IsIndexField => Index >= 0;
        public bool IsNamedField => !IsIndexField;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EventArgumentAttribute : EventFieldAttribute
    {
        public EventArgumentAttribute(string desc, int index) : base(desc, index)
        {
        }

        public EventArgumentAttribute(string desc) : base(desc)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EventOutputAttribute : EventFieldAttribute
    {
        public EventOutputAttribute(string desc, int index) : base(desc, index)
        {
        }

        public EventOutputAttribute(string desc) : base(desc)
        {
        }
    }
}
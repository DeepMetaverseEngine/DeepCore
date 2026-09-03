using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Display
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UEInstanceAttribute : System.Attribute
    {
        public readonly Type MetaType;
        public UEInstanceAttribute(Type type)
        {
            this.MetaType = type;
        }
    }
}

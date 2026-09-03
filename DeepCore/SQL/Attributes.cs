using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.SQL
{

    public enum SQLValueType
    {
        Primitive = 0,
        JsonObject = 1,
        BinaryObject = 2,
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SQLTableAttribute : Attribute
    {
        public SQLTableAttribute()
        {
        }

    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SQLFieldAttribute : Attribute
    {
        public SQLValueType ValueType { get; private set; }
        public int Length { get; set; }
        public bool PrimaryKey { get; set; }
        public bool NotNull { get; set; }
        public bool AutoIncrement { get; set; }
        public bool UniqueKey { get; set; }
        public SQLFieldAttribute(SQLValueType type = SQLValueType.Primitive)
        {
            this.ValueType = type;
        }
    }

}

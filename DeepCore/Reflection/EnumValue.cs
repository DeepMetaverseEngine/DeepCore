using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DeepCore.Reflection
{
    [Desc("枚举值"), Expandable(IsExpandable = false)]
    public class EnumValue
    {
        [Desc(Editable = false)]
        public Type EnumType;
        [Desc(Editable = false)]
        public int Value;
        public override string ToString()
        {
            if (EnumType != null)
            {
                if (EnumType.IsEnum)
                {
                    var enumValue = Enum.ToObject(EnumType, Value);
                    return string.Format("{0}.{1}", EnumType.ToDesc(), enumValue);
                }
                else
                {
                    return string.Format("{0}.{1}", EnumType.ToDesc(), Value);
                }
            }
            else
            {
                return string.Format("{0}.{1}", string.Empty, Value);
            }
        }

        public static int ConvertToInt32(object enumValue)
        {
            var int32 = 0;
            try
            {

                var uv = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()));
                int32 = Convert.ToInt32(uv);
            }
            catch { }
            return int32;
        }
        public static bool TryConvertToInt32(object enumValue, out int int32)
        {
            int32 = 0;
            try
            {
                var uv = Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumValue.GetType()));
                int32 = Convert.ToInt32(uv);
                return true;
            }
            catch { }
            return false;
        }
    }
}

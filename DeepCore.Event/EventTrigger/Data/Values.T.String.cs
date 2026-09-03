using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.EventTrigger.Data
{

    [Desc("[基础]-字符串")]
    public abstract class StringValue : AbstractValue<string>
    {
        [Desc("空", "[基础]")]
        public class NULL : StringValue
        {
            public NULL() { }

            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("<c color='" + sw.COLOR_CONST + "'>").Append("NULL").Append("</c>");
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                return null;
            }
        }

        [Desc("值", "[基础]")]
        public class VALUE : StringValue
        {
            [LocalizationTextAttribute]
            [Desc("值")]
            public string Value = string.Empty;

            public VALUE() { }
            public VALUE(string value) { this.Value = value; }

            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("<c color='" + sw.COLOR_CONST + "'><![CDATA[").Append(Value).Append("]]></c>");
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                return Value;
            }
        }
        [Desc("返回值", "[基础]")]
        public class ReturnVALUE : StringValue
        {
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                try
                {
                    return args.ReturnValue == null ? string.Empty : args.ReturnValue.ToString();
                }
                catch { return string.Empty; }
            }
        }
        [Desc("触发的值(String)", "[基础]")]
        public class TriggingValue : StringValue
        {
            public TriggingValue() { }
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的值");
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                return args.TriggingStringValue;
            }
        }
        //---------------------------------------------------------------------------------------------------------------------

        [Desc("整形转换为字符串", "[基础]/转换")]
        public class IntegerToString : StringValue
        {
            [Desc("值")]
            public AbstractValue<double> Value = new IntegerValue.VALUE(1);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})转换为字符串", Value);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                var value = (int)Value.GetValueAs(api, args);
                return value.ToString();
            }
        }

        [Desc("Bool转换为字符串", "[基础]/转换")]
        public class BooleanToString : StringValue
        {
            [Desc("值")]
            public AbstractValue<bool> Value = new BooleanValue.VALUE(false);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})转换为字符串", Value);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                var value = Value.GetValueAs(api, args);
                return value.ToString();
            }
        }

        [Desc("小数转换为字符串", "[基础]/转换")]
        public class RealToString : StringValue
        {
            [Desc("值")]
            public AbstractValue<double> Value = new RealValue.VALUE(1);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})转换为字符串", Value);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                float value = (float)Value.GetValueAs(api, args);
                return value.ToString();
            }
        }

        //---------------------------------------------------------------------------------------------------------------------


        [Desc("字符串拼接", "[基础]/字符串方法")]
        public class StringConcat : StringValue
        {
            [Desc("串0")]
            public AbstractValue<string> str0 = new StringValue.VALUE();
            [Desc("串1")]
            public AbstractValue<string> str1 = new StringValue.VALUE();
            [Desc("串2")]
            public AbstractValue<string> str2 = new StringValue.VALUE();
            [Desc("串3")]
            public AbstractValue<string> str3 = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}{1}{2}{3}", str0, str1, str2, str3);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                string[] array = new string[]
                {
                    str0.GetValueAs(api, args),
                    str1.GetValueAs(api, args),
                    str2.GetValueAs(api, args),
                    str3.GetValueAs(api, args),
                };
                StringBuilder sb = new StringBuilder();
                foreach (string c in array)
                {
                    if (c != null)
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }

        [Desc("字符串拼接（组）", "[基础]/字符串方法")]
        public class StringConcatArray : StringValue
        {
            [Desc("参数")]
            [ListDescAttribute(typeof(AbstractValue<string>))]
            public List<AbstractValue<string>> array = new List<AbstractValue<string>>();
            protected override void GetText(EventStringBuilder sw)
            {
                foreach (var sv in array)
                {
                    sw.Append(sv);
                }
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var sv in array)
                {
                    string c = sv.GetValueAs(api, args);
                    if (c != null)
                        sb.Append(c);
                }
                return sb.ToString();
            }
        }


        [Desc("字符串格式化", "[基础]/字符串方法")]
        public class StringFormat4 : StringValue
        {
            [Desc("格式化文本(最多4个参数)")]
            [LocalizationTextAttribute]
            public string FormatString = "{0}{1}{2}{3}";

            [Desc("串0")]
            public AbstractValue<string> str0 = new StringValue.VALUE();
            [Desc("串1")]
            public AbstractValue<string> str1 = new StringValue.VALUE();
            [Desc("串2")]
            public AbstractValue<string> str2 = new StringValue.VALUE();
            [Desc("串3")]
            public AbstractValue<string> str3 = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("<![CDATA[" + FormatString + "]]>", str0, str1, str2, str3);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                return string.Format(FormatString,
                    str0.GetValueAs(api, args),
                    str1.GetValueAs(api, args),
                    str2.GetValueAs(api, args),
                    str3.GetValueAs(api, args));
            }
        }

        [Desc("字符串格式化（组）", "[基础]/字符串方法")]
        public class StringFormatArray : StringValue
        {
            [Desc("格式化文本")]
            [LocalizationTextAttribute]
            public string FormatString = "{0}";

            [Desc("参数")]
            [ListDescAttribute(typeof(AbstractValue<string>))]
            public List<AbstractValue<string>> array = new List<AbstractValue<string>>();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("<![CDATA[" + FormatString + "]]>", array.ToArray());
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                object[] param = new object[array.Count];
                for (int i = 0; i < param.Length; i++)
                {
                    var c = array[i].GetValueAs(api, args);
                    param[i] = c;
                }
                return string.Format(FormatString, param);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------


        [Desc("子字符串", "[基础]/字符串方法")]
        public class SubString : StringValue
        {
            [Desc("源字符串")]
            public AbstractValue<string> src = new StringValue.VALUE();
            [Desc("开始")]
            public AbstractValue<double> index = new IntegerValue.VALUE(0);
            [Desc("长度")]
            public AbstractValue<double> count = new IntegerValue.VALUE(1);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.substring({1},{2})", src, index, count);
            }
            protected override string GetValue(EventExecutor api, IEventArguments args)
            {
                var s = src.GetValueAs(api, args);
                if (s != null)
                {
                    try
                    {
                        var i = (int)index.GetValueAs(api, args);
                        var n = (int)count.GetValueAs(api, args);
                        return s.Substring(i, n);
                    }
                    catch { }
                }
                return string.Empty;
            }
        }

        [Desc("字符串长度", "[基础]/字符串属性")]
        public class StringLength : IntegerValue
        {
            [Desc("字符串")]
            public AbstractValue<string> str = new StringValue.VALUE();
            protected override double GetValue(EventExecutor api, IEventArguments args)
            {
                var s = str.GetValueAs(api, args);
                if (s != null)
                {
                    return s.Length;
                }
                return 0;
            }
        }

    }

    //     [Desc("[基础]-字符串数组", "数组")]
    //     public abstract class StringArrayValue : AbstractArrayValue<string>
    //     {
    //         [Desc("字符串数组", "值")] public class VALUE : ArrayValue<AbstractValue<string>, string> { }
    //         [Desc("字符串数组索引", "数组")] public class INDEX : ArrayIndexValue<string> { }
    //         [Desc("字符串数组随机", "数组")] public class RANDOM : ArrayRandomValue<string> { }
    //         [Desc("迭代中的字符串", "数组")] public class ITERATOR : ArrayIteratingValue<string> { }
    //     }
}

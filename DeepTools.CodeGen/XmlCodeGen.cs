using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeepTools.CodeGen
{
    public static  class XmlCodeGen
    {
        public static void SetChildInnerText(this XmlNode parent, string childName, object value)
        {
            XmlUtil.ForEachChilds(parent, (e) =>
            {
                if (e.Name == childName)
                {
                    e.InnerText = ToAttributeString(e, $"{value}");
                }
            }, true);
        }
        public static void SetChildInnerText(this XmlNode parent, string childName, Func<XmlElement, string> tostring)
        {
            XmlUtil.ForEachChilds<XmlElement>(parent, (e) =>
            {
                if (e.Name == childName)
                {
                    e.InnerText = ToAttributeString(e, $"{tostring(e)}");
                }
            }, true);
        }
        public static string ToAttributeString(this XmlNode e, object src)
        {
            var text = $"{src}";
            if (e.TryGetAttribute("NotEquals", out var NotEquals))
            {
                if (string.Equals(text, NotEquals))
                {
                    return string.Empty;
                }
            }
            if (e.TryGetAttribute("Substring", out var Substring))
            {
                var kv = Substring.Split(',');
                text = text.Substring(int.Parse(kv[0]), int.Parse(kv[1]));
            }
            if (e.TryGetAttribute("RemovePrefix", out var RemovePrefix))
            {
                var i = int.Parse(RemovePrefix);
                text = text.Substring(i, text.Length - i);
            }
            if (e.TryGetAttribute("RemoveSuffix", out var RemoveSuffix))
            {
                var i = int.Parse(RemoveSuffix);
                text = text.Substring(0, text.Length - i);
            }
            if (e.TryGetAttribute("Upper", out var _))
            {
                text = text.ToUpper();
            }
            if (!string.IsNullOrEmpty(text))
            {
                if (e.TryFindChild("V", out XmlElement ev))
                {
                    ev.InnerText = text;
                    return e.InnerText;
                }
            }
            return text;
        }
    }
}

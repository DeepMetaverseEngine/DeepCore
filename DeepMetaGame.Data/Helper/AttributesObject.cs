using DeepCore;
using DeepCore.Reflection;

namespace DeepMetaGame.Data.Helper
{
    [Reflectible]
    abstract public class AttributesObject : Recyclable
    {
        private HashMap<string, object> mAttributes;
        private static readonly char[] attrSplit = { '=' };

        public AttributesObject() { }
        protected override void Destructing()
        {
            
        }
        protected override void Disposing()
        {
            if (mAttributes != null)
                mAttributes.Clear();
        }

        public bool IsAttribute(string key)
        {
            if (mAttributes == null) return false;
            return mAttributes.ContainsKey(key);
        }
        public void SetAttribute(string key, object value)
        {
            if (mAttributes == null) mAttributes = new HashMap<string, object>();
            mAttributes.Put(key, value);
        }
        public object RemoveAttribute(string key)
        {
            if (mAttributes == null) return null;
            return mAttributes.RemoveByKey(key);
        }
        public object GetAttribute(string key)
        {
            if (mAttributes == null) return null;
            return mAttributes.Get(key);
        }
        public T GetAttributeAs<T>(string key)
        {
            if (mAttributes == null) return default;
            object obj = mAttributes.Get(key);
            if (obj != null)
            {
                return (T)obj;
            }
            return default;
        }

        public void BindAttributes(string[] attrs)
        {
            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    var kv = attr.Split(attrSplit, 2);
                    if (kv != null && kv.Length > 1)
                    {
                        SetAttribute(kv[0].Trim(), kv[1].Trim());
                    }
                    else
                    {
                        SetAttribute(attr.Trim(), null);
                    }
                }
            }
        }

    }

}

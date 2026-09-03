using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DeepCore
{
    public struct TypeComparer : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            return x.FullName.CompareTo(y.FullName);
        }
    }
    public struct ToStringComparer<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            return x.ToString().CompareTo(y.ToString());
        }
    }
    public struct ToStringComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            return x.ToString().CompareTo(y.ToString());
        }
    }
    public struct FieldComparer : IComparer<FieldInfo>
    {
        public int Compare(FieldInfo x, FieldInfo y)
        {
            return x.Name.CompareTo(y.Name);
        }
    }
    public struct PropertyComparer : IComparer<PropertyInfo>
    {
        public int Compare(PropertyInfo x, PropertyInfo y)
        {
            return x.Name.CompareTo(y.Name);
        }
    }

    public struct ValueComparer<T> : IComparer<T>, IComparer
    {
        public readonly Comparison<T> Comparison;
        public ValueComparer(Comparison<T> c) { this.Comparison = c; }
        public int Compare(T x, T y)
        {
            return Comparison.Invoke(x, y);
        }
        public int Compare(object x, object y)
        {
            return Comparison.Invoke((T)x, (T)y);
        }
    }
    public struct ComparisonComparer<T> : IComparer<T>, IComparer
    {
        public readonly Comparison<T> Comparison;
        public ComparisonComparer(Comparison<T> c) { this.Comparison = c; }
        public int Compare(T x, T y)
        {
            return Comparison.Invoke(x, y);
        }
        public int Compare(object x, object y)
        {
            return Comparison.Invoke((T)x, (T)y);
        }
    }
}

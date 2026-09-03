using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DeepCore
{
    public class DelegateMap : Disposable
    {
        private HashMap<Delegate, Delegate> eventMap = new HashMap<Delegate, Delegate>();
        protected override void Disposing()
        {
            eventMap.Clear();
        }
        public T Add<T>(T value, T a_value) where T : Delegate
        {
            eventMap.Add(value, a_value);
            return a_value;
        }
        public T Remove<T>(T value) where T : Delegate
        {
            eventMap.TryRemove(value, out var a_value);
            return a_value as T;
        }
        public Action<T> AddWrap<T>(Action<T> value)
        {
            var a_value = new Action<T>(t => value(t));
            eventMap.Add(value, a_value);
            return a_value;
        }
    }

    public class MultiCastInvoker<T> : IDisposable where T : Delegate
    {
        private T Handler;
        private T[] InvocationList;
        public MultiCastInvoker()
        {
        }

        public void Add(T d2)
        {
            Handler = (T)Delegate.Combine(Handler, d2);
            RefreshList();
        }
        public void Remove(T d2)
        {
            Handler = (T)Delegate.Remove(Handler, d2);
            RefreshList();
        }
        private void RefreshList()
        {
            var list = Handler?.GetInvocationList();
            if (list != null)
            {
                InvocationList = Array.ConvertAll(list, t => (T)t);
            }
            else
            {
                InvocationList = null;
            }
        }
        public void Dispose()
        {
            Handler = null;
            InvocationList = null;
        }
        public T[] GetInvocationList()
        {
            return InvocationList;
        }
        public static MultiCastInvoker<T> operator +(MultiCastInvoker<T> d1, T d2)
        {
            if (d1 == null)
            {
                d1 = new MultiCastInvoker<T>();
            }
            d1.Add(d2);
            return d1;
        }
        public static MultiCastInvoker<T> operator -(MultiCastInvoker<T> d1, T d2)
        {
            if (d1 != null)
            {
                d1.Remove(d2);
                if (d1.Handler == null)
                {
                    d1 = null;
                }
            }
            return d1;
        }
        public static implicit operator bool(in MultiCastInvoker<T> value)
        {
            return value != null;
        }
    }

    public static class DelegateExt
    {
        public static bool TryGetInvocationList<T>(this MultiCastInvoker<T> m, out T[] delegates) where T : Delegate
        {
            if (m != null)
            {
                delegates = m.GetInvocationList();
                return delegates != null;
            }
            delegates = null;
            return false;
        }
    }
}

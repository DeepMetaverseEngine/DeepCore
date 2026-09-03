using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DeepCore.Components
{
    //----------------------------------------------------------------------------------------------------------------
    //Upgrade Done
    public interface IComponent : IDisposable
    {
        internal protected void InternalAdded();
        internal protected void InternalRemoved();
    }
    public interface IComponent<O> : IDisposable, IComponent
    {
        O Owner { get; }
        internal protected void InternalAdded(O owner);
        internal protected void InternalRemoved(O owner);
        void IComponent.InternalAdded() { }
        void IComponent.InternalRemoved() { }
    }

    //----------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Tag相同的Component之间可以进行字段同步
    /// </summary>
    public class ComponentTagAttribute : Attribute
    {
        public int Tag { get; }
        public string Desc { get; }

        public ComponentTagAttribute(int tag, string desc)
        {
            if (tag < 0)
            {
                throw new ArgumentException("tag must >= 0");
            }
            Tag = tag;
            Desc = desc;
        }
    }
    //----------------------------------------------------------------------------------------------------------------
    public class ComponentCollection<T> : Disposable, IComparer<KeyValuePair<Type, T>> where T : class, IComponent
    {
        private readonly Comparison<T> _compare;
        private bool isDirty = true;
        private List<KeyValuePair<Type, T>> _foreach_list;
        private HashMap<Type, T> _components;

        private Action<T> event_OnAdded;
        private Action<T> event_OnRemoved;
        public event Action<T> OnAdded { add { event_OnAdded += value; } remove { event_OnAdded -= value; } }
        public event Action<T> OnRemoved { add { event_OnRemoved += value; } remove { event_OnRemoved -= value; } }

        public int Count => _components?.Count ?? 0;
        public ComponentCollection(Comparison<T> compare)
        {
            this._compare = compare;
        }
        public ComponentCollection() : this(static (a, b) => 0)
        {
        }
        protected override void Disposing()
        {
            event_OnAdded = null;
            event_OnRemoved = null;
            Clear();
        }
        public int Compare(KeyValuePair<Type, T> x, KeyValuePair<Type, T> y)
        {
            return _compare(x.Value, y.Value);
        }
        public void Clear()
        {
            if (_foreach_list != null && _components != null)
            {
                _foreach_list.Clear();
                _foreach_list.AddRange(_components);
                _foreach_list.Sort(this);
                for (int i = 0; i < _foreach_list.Count; i++)
                {
                    var comp = _foreach_list[i].Value;
                    comp.Dispose();
                }
            }
            _foreach_list?.Clear();
            _components?.Clear();
        }
        //---------------------------------------------------------------------------------------------------------
        protected virtual void DoAdded(T comp)
        {
            comp.InternalAdded();
            event_OnAdded?.Invoke(comp);
        }
        protected virtual void DoRemoved(T comp)
        {
            comp.InternalRemoved();
            event_OnRemoved?.Invoke(comp);
        }
        //---------------------------------------------------------------------------------------------------------
        #region ForEach
        private bool TryGetSortList(out List<KeyValuePair<Type, T>> list)
        {
            if (_components == null)
            {
                list = null;
                return false;
            }
            else
            {
                if (isDirty)
                {
                    isDirty = false;
                    _foreach_list.Clear();
                    _foreach_list.AddRange(_components);
                    _foreach_list.Sort(this);
                }
                list = _foreach_list;
                return true;
            }
        }

        public void ForEach<ST>(in ST state, ForEachAction<ST, T> action)
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    action(state, c.Value);
                }
            }
        }
        public bool ForEach<ST>(in ST state, ForEachPredicate<ST, T> predicate)
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (predicate(state, c.Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool TryGet<ST, R>(in ST state, TryGetPredicateResult<ST, T, R> predicate, out R ret)
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (predicate(state, c.Value, out ret))
                    {
                        return true;
                    }
                }
            }
            ret = default(R);
            return false;
        }

        public void ForEachAs<ST, C>(in ST state, ForEachAction<ST, C> action) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (c is C cc) action(state, cc);
                }
            }
        }
        public bool ForEachAs<ST, C>(in ST state, ForEachPredicate<ST, C> predicate) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (c is C cc && predicate(state, cc))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool ForEachAs<ST, C, R>(in ST state, TryGetPredicateResult<ST, C, R> predicate, out R ret) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (c is C cc && predicate(state, cc, out ret))
                    {
                        return true;
                    }
                }
            }
            ret = default(R);
            return false;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Get
        public bool TryGetComponent(Type ctype, out T value, bool inherit = true)
        {
            value = null;
            if (_components == null)
            {
                return false;
            }
            if (_components.TryGetValue(ctype, out value))
            {
                return true;
            }
            if (inherit)
            {
                if (TryGetSortList(out var _temps))
                {
                    for (int i = 0; i < _temps.Count; i++)
                    {
                        var c = _temps[i];
                        if (ctype.IsInstanceOfType(c.Value))
                        {
                            value = c.Value;
                            return true;
                        }
                    }
                }
            }
            return value != null;
        }
        public T GetComponent(Type ctype, bool inherit = true)
        {
            if (TryGetComponent(ctype, out var tvalue, inherit))
            {
                return tvalue;
            }
            return null;
        }
        public bool TryGetComponentAs<C>(out C value, bool inherit = true) where C : class, T
        {
            value = null;
            if (TryGetComponent(typeof(C), out var tvalue, inherit))
            {
                value = tvalue as C;
                return true;
            }
            return false;
        }
        public C GetComponentAs<C>(bool inherit = true) where C : class, T
        {
            if (TryGetComponentAs<C>(out var tvalue, inherit))
            {
                return tvalue;
            }
            return null;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Add
        private HashMap<Type, T> GetOrCreateMap()
        {
            if (_components == null)
            {
                _components = new();
                _foreach_list = new();
            }
            return _components;
        }
        //---------------------------------------------------------------------------------------------------------
        public T AddComponent(T comp)
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return null;
        }
        public C AddComponent<C>(C comp) where C : class, T
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return null;
        }
        public C AddComponent<C>() where C : class, T, new()
        {
            return AddComponent(new C());
        }
        public C AddComponent<C>(params object[] args) where C : class, T
        {
            return AddComponent(DeepActivator.CreateInstance(typeof(C), args) as C);
        }
        public T AddComponent(Type t)
        {
            if (TryAddComponent(t, out var comp))
            {
                return comp;
            }

            return null;
        }
        public C AddComponentAs<C>() where C : class, T, new()
        {
            if (TryAddComponentAs<C>(out var comp))
            {
                return comp;
            }
            return null;
        }
        //---------------------------------------------------------------------------------------------------------
        public C GetOrAddComponentAs<C>() where C : class, T, new()
        {
            TryGetOrNewComponentAs<C>(out var comp);
            return comp;
        }
        //---------------------------------------------------------------------------------------------------------
        public bool TryAddComponent(T comp)
        {
            if (TryGetComponent(comp.GetType(), out var exist))
            {
                return false;
            }
            var ctype = comp.GetType();
            var map = GetOrCreateMap();
            isDirty = true;
            map.Add(ctype, comp);
            DoAdded(comp);
            return true;
        }
        public bool ReplaceComponent(Type ctype, T _new, out T _old)
        {
            var ret = TryRemoveComponent(ctype, out _old);
            var map = GetOrCreateMap();
            {
                isDirty = true;
                map.Add(ctype, _new);
                DoAdded(_new);
            }
            return ret;
        }
        public bool TryAddComponent(Type ctype, out T comp)
        {
            if (TryGetComponent(ctype, out var exist))
            {
                comp = exist;
                return false;
            }
            var map = GetOrCreateMap();
            comp = ReflectionUtil.CreateInstance(ctype) as T;
            if (comp != null)
            {
                isDirty = true;
                map.Add(ctype, comp);
                DoAdded(comp);
                return true;
            }
            return false;
        }
        public bool TryAddComponentAs<C>(out C comp) where C : class, T, new()
        {
            if (TryGetComponentAs<C>(out var exist))
            {
                comp = exist;
                return false;
            }
            var ctype = typeof(C);
            var map = GetOrCreateMap();
            isDirty = true;
            comp = new C();
            map.Add(ctype, comp);
            DoAdded(comp);
            return true;
        }
        public bool TryAddComponentAs<C>(out C comp, Func<C> create) where C : class, T
        {
            if (TryGetComponentAs<C>(out var exist))
            {
                comp = exist;
                return false;
            }
            var ctype = typeof(C);
            var map = GetOrCreateMap();
            isDirty = true;
            comp = create();
            map.Add(ctype, comp);
            DoAdded(comp);
            return true;
        }
        //---------------------------------------------------------------------------------------------------------
        private bool TryGetOrNewComponentAs<C>(out C comp) where C : class, T, new()
        {
            if (TryGetComponentAs<C>(out comp))
            {
                return true;
            }
            var ctype = typeof(C);
            var map = GetOrCreateMap();
            var ret = (map.TryGetOrCreate(ctype, out var tvalue, static t => new C()));
            comp = tvalue as C;
            if (!ret)
            {
                isDirty = true;
                DoAdded(comp);
                ret = comp != null;
            }
            return ret;
        }
        private bool TryGetOrCreateComponentAs<C>(out C comp, Func<Type, C> create) where C : class, T
        {
            if (TryGetComponentAs<C>(out comp))
            {
                return true;
            }
            var ctype = typeof(C);
            var map = GetOrCreateMap();
            var ret = (map.TryGetOrCreate(ctype, out var tvalue, create));
            comp = tvalue as C;
            if (!ret)
            {
                isDirty = true;
                DoAdded(comp);
                ret = comp != null;
            }
            return ret;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Remove
        public bool RemoveComponent(T comp)
        {
            if (_components == null)
            {
                return false;
            }
            if (IsDisposing)
            {
                if (_components.ContainsValue(comp))
                {
                    DoRemoved(comp);
                    return true;
                }
                return false;
            }
            if (TryGetSortList(out var _temps))
            {
                for (int i = 0; i < _temps.Count; i++)
                {
                    var c = _temps[i];
                    if (c.Value == comp)
                    {
                        _components.Remove(c.Key);
                        DoRemoved(comp);
                        isDirty = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryRemoveComponent(Type compType, out T comp)
        {
            if (TryGetComponent(compType, out comp))
            {
                return RemoveComponent(comp);
            }
            comp = null;
            return false;
        }
        public bool TryRemoveComponentAs<C>(out C value) where C : class, T
        {
            if (TryGetComponentAs<C>(out value))
            {
                RemoveComponent(value);
                return true;
            }
            return false;
        }
        public C RemoveComponentAs<C>() where C : class, T
        {
            if (TryGetComponentAs<C>(out var value))
            {
                RemoveComponent(value);
                return value;
            }
            return null;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------



    }
    public class ComponentCollection<O, T> : ComponentCollection<T> where T : class, IComponent<O>
    {

        private Action<O, T> event_OnAdded;
        private Action<O, T> event_OnRemoved;
        new public event Action<O, T> OnAdded { add { event_OnAdded += value; } remove { event_OnAdded -= value; } }
        new public event Action<O, T> OnRemoved { add { event_OnRemoved += value; } remove { event_OnRemoved -= value; } }

        public O Owner { get; }
        public ComponentCollection(O owner, Comparison<T> compare) : base(compare)
        {
            this.Owner = owner;
        }
        public ComponentCollection(O owner) : this(owner, static (a, b) => 0)
        {
        }
        protected override void Disposing()
        {
            event_OnAdded = null;
            event_OnRemoved = null;
            base.Disposing();
        }
        //---------------------------------------------------------------------------------------------------------
        protected override void DoAdded(T comp)
        {
            base.DoAdded(comp);
            comp.InternalAdded(Owner);
            event_OnAdded?.Invoke(Owner, comp);
        }
        protected override void DoRemoved(T comp)
        {
            base.DoRemoved(comp);
            comp.InternalRemoved(Owner);
            event_OnRemoved?.Invoke(Owner, comp);
        }
        //---------------------------------------------------------------------------------------------------------



    }

    //----------------------------------------------------------------------------------------------------------------

#if FALSE
    public abstract class DataComponentCollection<T> : IExternalizable where T : IDataComponent
    {
        private List<T> components;

        public virtual void ReadExternal(IInputStream input)
        {
            components = input.GetList(input.GetObjAs<T>);
        }
        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutList(components, output.PutObjAs<T>);
        }
        //---------------------------------------------------------------------------------------------------------
        #region Add
        private List<T> GetOrCreateMap()
        {
            if (components == null)
            {
                components = new List<T>();
            }
            return components;
        }
        public T AddComponent(T comp)
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return default(T);
        }
        public C AddComponent<C>(C comp) where C : T
        {
            if (TryAddComponent(comp))
            {
                return comp;
            }
            return default(C);
        }
        public T AddComponent(Type t)
        {
            if (TryAddComponent(t, out var comp))
            {
                return comp;
            }
            return default(T);
        }
        public bool TryAddComponent(T comp)
        {
            var map = GetOrCreateMap();
            map.Add(comp);
            return true;
        }
        public bool TryAddComponent(Type ctype, out T comp)
        {
            var map = GetOrCreateMap();
            comp = (T)ReflectionUtil.CreateInstance(ctype);
            map.Add(comp);
            return true;
        }
        public C AddComponentAs<C>() where C : T
        {
            if (TryAddComponentAs<C>(out var comp))
            {
                return comp;
            }
            return default(C);
        }
        public C GetOrAddComponentAs<C>() where C : T
        {
            if (TryGetOrCreateComponentAs<C>(out var comp))
            {
                return comp;
            }
            return default(C);
        }
        public bool TryAddComponentAs<C>(out C comp) where C : T
        {
            var ctype = typeof(C);
            var map = GetOrCreateMap();
            comp = (C)ReflectionUtil.CreateInstance(ctype);
            map.Add(comp);
            return true;
        }
        public bool TryAddComponentAs<C>(out C comp, Func<C> create) where C : T
        {
            var map = GetOrCreateMap();
            comp = create();
            map.Add(comp);
            return true;
        }

        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region Get
        public bool TryGetComponent(Type ctype, out T value, bool inherit = false)
        {
            value = default(T);
            if (components == null)
            {
                return false;
            }
            foreach (var comp in components)
            {
                if (comp.GetType() == ctype)
                {
                    value = (T)comp;
                    return true;
                }
            }
            if (inherit)
            {
                foreach (var comp in components)
                {
                    if (ctype.IsAssignableFrom(comp.GetType()))
                    {
                        value = (T)comp;
                        return true;
                    }
                }
            }
            return false;
        }
        public T GetComponent(Type ctype, bool inherit = false)
        {
            if (TryGetComponent(ctype, out var tvalue, inherit))
            {
                return tvalue;
            }
            return default(T);
        }
        public bool TryGetComponentAs<C>(out C value, bool inherit = false) where C : T
        {
            value = default(C);
            if (TryGetComponent(typeof(C), out var tvalue, inherit))
            {
                value = (C)tvalue;
                return true;
            }
            return false;
        }
        public C GetComponentAs<C>(bool inherit = false) where C : T
        {
            if (TryGetComponentAs<C>(out var tvalue, inherit))
            {
                return tvalue;
            }
            return default(C);
        }
        public bool TryGetOrCreateComponentAs<C>(out C comp) where C : T
        {
            if (TryGetComponentAs<C>(out comp, true) == false)
            {
                comp = (C)ReflectionUtil.CreateInstance(typeof(C));
                AddComponent(comp);
                return false;
            }
            return false;
        }
        public bool TryGetOrCreateComponentAs<C>(out C comp, Func<C> create) where C : T
        {
            if (TryGetComponentAs<C>(out comp, true) == false)
            {
                comp = create();
                AddComponent(comp);
                return false;
            }
            return false;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------
        #region ForEach
        private bool TryGetSortList(out ICollection<T> list)
        {
            if (components == null)
            {
                list = null;
                return false;
            }
            else
            {
                list = components;
                return true;
            }
        }

        public void ForEach(Action<T> action)
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    action(c);
                }
            }
        }
        public bool ForEach(BreakPredicate<T> predicate)
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    if (predicate(c))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void ForEachAs<C>(Action<C> action) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    if (c is C cc) action(cc);
                }
            }
        }
        public bool ForEachAs<C>(BreakPredicate<C> predicate) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    if (c is C cc && predicate(cc))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool TryGet<ST, R>(in ST state, TryGetPredicate<ST, T, R> predicate, out R ret)
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    if (predicate(state, c, out ret))
                    {
                        return true;
                    }
                }
            }
            ret = default(R);
            return false;
        }

        public bool TryGetAs<ST, C, R>(in ST state, TryGetPredicate<ST, C, R> predicate, out R ret) where C : T
        {
            if (TryGetSortList(out var _temps))
            {
                foreach (var c in _temps)
                {
                    if (c is C cc && predicate(state, cc, out ret))
                    {
                        return true;
                    }
                }
            }
            ret = default(R);
            return false;
        }
        #endregion
    }
#endif
    //----------------------------------------------------------------------------------------------------------------
}

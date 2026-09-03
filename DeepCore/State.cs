using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepCore
{
    public delegate void StateChanged<T>(T oldValue, T newValue);

    public abstract class IStateReference<T>
    {
        public abstract T Value { get; }
        private StateChanged<T> event_OnChanged = null;
        public event StateChanged<T> OnChanged { add { event_OnChanged += value; } remove { event_OnChanged -= value; } }
        protected void OnStateChanged(ref T oldValue, ref T newValue) { event_OnChanged?.Invoke(oldValue, newValue); }
    }

    public class State<T> : IStateReference<T>
    {
        private T mValue;
        private Func<T, T, bool> equals;
        public override T Value { get { return mValue; } }

        public State(T value, Func<T, T, bool> equals)
        {
            this.mValue = value;
            this.equals = equals;
        }
        public virtual bool Update(T value)
        {
            if (equals(mValue, value) == false)
            {
                var old = mValue;
                mValue = value;
                OnStateChanged(ref old, ref value);
                return true;
            }
            return false;
        }
        public virtual bool CompareChangeState(T expect, T new_value)
        {
            if (equals(expect, mValue))
            {
                var old = mValue;
                mValue = new_value;
                OnStateChanged(ref old, ref new_value);
                return true;
            }
            return false;
        }
    }
    public class AtomicState<T> : State<T>
    {
        public AtomicState(T value, Func<T, T, bool> compare) : base(value, compare)
        {
        }
        public override bool Update(T value)
        {
            lock (this)
            {
                return base.Update(value);
            }
        }
        public override bool CompareChangeState(T expect, T new_value)
        {
            lock (this)
            {
                return base.CompareChangeState(expect, new_value);
            }
        }
    }


}

using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Unity
{
    public abstract class UniSingleton<T> : Disposable where T : UniSingleton<T>, new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Lazy<T>(() =>
                    {
                        var ret = new T();
                        ret.Init();
                        return ret;
                    }).Value;
                }
                return instance;
            }
        }

        protected abstract void Init();
    }
}

using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepCore
{

    public delegate void ForEachAction<T>(T input) where T : ForEachInput;

    //-----------------------------------------------------------------------------------------------------------


    public delegate void ForEachAction<ST, T>(ST state, T iterator);
    public delegate void ForEachAction<ST, T, A1>(ST state, T iterator, A1 a1);
    public delegate void ForEachAction<ST, T, A1, A2>(ST state, T iterator, A1 a1, A2 a2);
    public delegate void ForEachAction<ST, T, A1, A2, A3>(ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate void ForEachAction<ST, T, A1, A2, A3, A4>(ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool ForEachPredicate<ST, T>(ST state, T iterator);
    public delegate bool ForEachPredicate<ST, T, A1>(ST state, T iterator, A1 a1);
    public delegate bool ForEachPredicate<ST, T, A1, A2>(ST state, T iterator, A1 a1, A2 a2);
    public delegate bool ForEachPredicate<ST, T, A1, A2, A3>(ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate bool ForEachPredicate<ST, T, A1, A2, A3, A4>(ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool TryGetPredicate<ST, T>(ST state, T iterator);
    public delegate bool TryGetPredicate<ST, T, A1>(ST state, T iterator, A1 a1);
    public delegate bool TryGetPredicate<ST, T, A1, A2>(ST state, T iterator, A1 a1, A2 a2);
    public delegate bool TryGetPredicate<ST, T, A1, A2, A3>(ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate bool TryGetPredicate<ST, T, A1, A2, A3, A4>(ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool TryGetPredicateResult<ST, T, R>(ST state, T iterator, out R ret);

    //-----------------------------------------------------------------------------------------------------------


    public delegate void ForEachActionT<ST, T>(ref ST state, T iterator);
    public delegate void ForEachActionT<ST, T, A1>(ref ST state, T iterator, A1 a1);
    public delegate void ForEachActionT<ST, T, A1, A2>(ref ST state, T iterator, A1 a1, A2 a2);
    public delegate void ForEachActionT<ST, T, A1, A2, A3>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate void ForEachActionT<ST, T, A1, A2, A3, A4>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool ForEachPredicateT<ST, T>(ref ST state, T iterator);
    public delegate bool ForEachPredicateT<ST, T, A1>(ref ST state, T iterator, A1 a1);
    public delegate bool ForEachPredicateT<ST, T, A1, A2>(ref ST state, T iterator, A1 a1, A2 a2);
    public delegate bool ForEachPredicateT<ST, T, A1, A2, A3>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate bool ForEachPredicateT<ST, T, A1, A2, A3, A4>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool TryGetPredicateT<ST, T>(ref ST state, T iterator);
    public delegate bool TryGetPredicateT<ST, T, A1>(ref ST state, T iterator, A1 a1);
    public delegate bool TryGetPredicateT<ST, T, A1, A2>(ref ST state, T iterator, A1 a1, A2 a2);
    public delegate bool TryGetPredicateT<ST, T, A1, A2, A3>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate bool TryGetPredicateT<ST, T, A1, A2, A3, A4>(ref ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate bool TryGetPredicateResultT<ST, T, R>(ref ST state, T iterator, out R ret);


    //-----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// return 'true' for break for each
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="iterator"></param>
    /// <returns>true for break for each</returns>
    public delegate bool BreakPredicate<T>(T iterator);
    public delegate bool BreakPredicate<T, A1>(T iterator, A1 a1);
    public delegate bool BreakPredicate<T, A1, A2>(T iterator, A1 a1, A2 a2);
    public delegate bool BreakPredicate<T, A1, A2, A3>(T iterator, A1 a1, A2 a2, A3 a3);
    public delegate bool BreakPredicate<T, A1, A2, A3, A4>(T iterator, A1 a1, A2 a2, A3 a3, A4 a4);
    //-----------------------------------------------------------------------------------------------------------

    public delegate Task ForEachActionAsync<ST, T>(ST state, T iterator);
    public delegate Task ForEachActionAsync<ST, T, A1>(ST state, T iterator, A1 a1);
    public delegate Task ForEachActionAsync<ST, T, A1, A2>(ST state, T iterator, A1 a1, A2 a2);
    public delegate Task ForEachActionAsync<ST, T, A1, A2, A3>(ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate Task ForEachActionAsync<ST, T, A1, A2, A3, A4>(ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate Task<bool> BreakPredicateAsync<T>(T iterator);
    public delegate Task<bool> BreakPredicateAsync<T, A1>(T iterator, A1 a1);
    public delegate Task<bool> BreakPredicateAsync<T, A1, A2>(T iterator, A1 a1, A2 a2);
    public delegate Task<bool> BreakPredicateAsync<T, A1, A2, A3>(T iterator, A1 a1, A2 a2, A3 a3);
    public delegate Task<bool> BreakPredicateAsync<T, A1, A2, A3, A4>(T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    public delegate Task<bool> ForEachPredicateAsync<ST, T>(ST state, T iterator);
    public delegate Task<bool> ForEachPredicateAsync<ST, T, A1>(ST state, T iterator, A1 a1);
    public delegate Task<bool> ForEachPredicateAsync<ST, T, A1, A2>(ST state, T iterator, A1 a1, A2 a2);
    public delegate Task<bool> ForEachPredicateAsync<ST, T, A1, A2, A3>(ST state, T iterator, A1 a1, A2 a2, A3 a3);
    public delegate Task<bool> ForEachPredicateAsync<ST, T, A1, A2, A3, A4>(ST state, T iterator, A1 a1, A2 a2, A3 a3, A4 a4);

    //-----------------------------------------------------------------------------------------------------------


    public class ForEachInput : Recyclable
    {
        public bool Break = false;
        protected override void Disposing()
        {
            this.Break = false;
        }
        protected override void Destructing()
        {

        }
    }
    public class ForEachInput<T> : ForEachInput
    {
        public T Iterator;
        protected override void Disposing()
        {
            base.Disposing();
            this.Iterator = default;
        }
        protected override void Destructing()
        {

        }
    }
    public class ForEachInput<T, A1> : ForEachInput<T>
    {
        public A1 Arg1;
    }
    public class ForEachInput<T, A1, A2> : ForEachInput<T, A1>
    {
        public A2 Arg2;
    }
    public class ForEachInput<T, A1, A2, A3> : ForEachInput<T, A1, A2>
    {
        public A3 Arg3;
    }
    public class ForEachInput<T, A1, A2, A3, A4> : ForEachInput<T, A1, A2, A3>
    {
        public A4 Arg4;
    }
    public class ForEachInput<T, A1, A2, A3, A4, A5> : ForEachInput<T, A1, A2, A3, A4>
    {
        public A5 Arg5;
    }
    public class ForEachInput<T, A1, A2, A3, A4, A5, A6> : ForEachInput<T, A1, A2, A3, A4, A5>
    {
        public A6 Arg6;
    }
    //-----------------------------------------------------------------------------------------------------------
    public static class ForEachUtils
    {
        public static ForEachInput<T> AllocForEach<T>(this AbstractCollectionPool ObjectPool)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T>>();
            return ret;
        }
        public static ForEachInput<T, A1> AllocForEach1<T, A1>(this AbstractCollectionPool ObjectPool, A1 a1, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1>>();
            ret.Arg1 = a1;
            return ret;
        }
        public static ForEachInput<T, A1, A2> AllocForEach2<T, A1, A2>(this AbstractCollectionPool ObjectPool, A1 a1, A2 a2, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            return ret;
        }
        public static ForEachInput<T, A1, A2, A3> AllocForEach3<T, A1, A2, A3>(this AbstractCollectionPool ObjectPool, A1 a1, A2 a2, A3 a3, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            return ret;
        }
        public static ForEachInput<T, A1, A2, A3, A4> AllocForEach4<T, A1, A2, A3, A4>(this AbstractCollectionPool ObjectPool, A1 a1, A2 a2, A3 a3, A4 a4, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3, A4>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            ret.Arg4 = a4;
            return ret;
        }
        public static ForEachInput<T, A1, A2, A3, A4, A5> AllocForEach5<T, A1, A2, A3, A4, A5>(this AbstractCollectionPool ObjectPool, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3, A4, A5>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            ret.Arg4 = a4;
            ret.Arg5 = a5;
            return ret;
        }
        public static ForEachInput<T, A1, A2, A3, A4, A5, A6> AllocForEach6<T, A1, A2, A3, A4, A5, A6>(this AbstractCollectionPool ObjectPool, A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, T t = default)
        {
            var ret = ObjectPool.AllocAutoRelease<ForEachInput<T, A1, A2, A3, A4, A5, A6>>();
            ret.Arg1 = a1;
            ret.Arg2 = a2;
            ret.Arg3 = a3;
            ret.Arg4 = a4;
            ret.Arg5 = a5;
            ret.Arg6 = a6;
            return ret;
        }



        public static R Total<T, R>(this ICollection<T> list, R input, Func<T, R, R> action)
        {
            var r = input;
            foreach (var item in list)
            {
                r = action(item, r);
            }
            return r;
        }
    


    }




}

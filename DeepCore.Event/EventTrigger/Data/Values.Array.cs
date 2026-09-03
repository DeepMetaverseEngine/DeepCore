using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.EventTrigger.Data
{
    //----------------------------------------------------------------------------------------------------------------
//      [Desc("值", "[基础]/值")]
//      public class ArrayValue<V, T> : AbstractArrayValue<T> where V : AbstractValue<T>
//      {
//          [Desc("值")]
//          [ListDesc()]
//          public V[] Values;
//          public ArrayValue() { }
//          public ArrayValue(V[] values) { this.Values = values; }
//          protected override void GetText(EventStringBuilder sw)
//          {
//              if (Values != null)
//              {
//                  foreach (var v in Values)
//                  {
//                      sw.Append(v);
//                  }
//              }
//          }
//          protected override T[] GetValue(EventExecutor api, IEventArguments args)
//          {
//              if (Values == null) return null;
//              return Array.ConvertAll(Values, t => t == null ? default(T) : t.GetValueAs(api, args));
//          }
//      }

//     [Desc("数组索引", "[基础]/数组")]
//     public class ArrayIndexValue<T> : AbstractValue<T>
//     {
//         [Desc("索引")]
//         public AbstractValue<double> Index = new IntegerValue.VALUE();
//         [Desc("数组")]
//         public AbstractArrayValue<T> Array;
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.Append(Array).Append("[").Append(Index).Append("]");
//         }
//         protected override T GetValue(EventExecutor api, IEventArguments args)
//         {
//             var array = Array.GetValueAs(api, args);
//             if (array != null)
//             {
//                 var i = Index.GetValueAs(api, args);
//                 if (i >= 0 && i < array.Length)
//                 {
//                     return array[i];
//                 }
//             }
//             return default(T);
//         }
//     }
// 
//     [Desc("数组随机", "[基础]/数组")]
//     public class ArrayRandomValue<T> : AbstractValue<T>
//     {
//         [Desc("数组")]
//         public AbstractArrayValue<T> Array;
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.Append("随机选取(").Append(Array).Append(")中的一个");
//         }
//         protected override T GetValue(EventExecutor api, IEventArguments args)
//         {
//             var array = Array.GetValueAs(api, args);
//             if (array != null)
//             {
//                 var i = api.API.RandomN.Next(array.Length);
//                 return array[i];
//             }
//             return default(T);
//         }
//     }
// 
//     [Desc("迭代中的对象", "[基础]/数组")]
//     public class ArrayIteratingValue<T> : AbstractValue<T>
//     {
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.Append("迭代中的对象");
//         }
//         protected override T GetValue(EventExecutor api, IEventArguments args)
//         {
//             return (T)args.IteratingObject;
//         }
//     }

    //----------------------------------------------------------------------------------------------------------------
//     [Desc("数组长度", "[基础]/数组")]
//     public class ArrayLength : IntegerValue
//     {
//         [Desc("数组")]
//         public AbstractArrayValue Array;
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.Append(Array).Append("Length");
//         }
//         protected override double GetValue(EventExecutor api, IEventArguments args)
//         {
//             var array = Array.GetRunArrayValue(api, args);
//             if (array != null)
//             {
//                 return array.Length;
//             }
//             return 0;
//         }
//     }

//     [Desc("遍历数组", "[基础]/数组")]
//     public class ArrayForEachDoAction : AbstractAction
//     {
//         [Desc("数组")]
//         public AbstractArrayValue Array;
//         [Desc("动作")]
//         public AbstractAction Action = new DoNoting();
//         protected override void GetText(EventStringBuilder sw)
//         {
//             sw.AppendForEach(
//                 sw1 => sw.Append(Array),
//                 sw2 => sw.AppendLine(Action));
//         }
//         override protected object Run(EventExecutor api, IEventArguments args)
//         {
//             var array = Array.GetRunArrayValue(api, args);
//             if (array != null)
//             {
//                 for (int i = 0; i < array.Length; i++)
//                 {
//                     args.IteratingInt32 = i;
//                     args.IteratingObject = array.GetValue(i);
//                     Action?.DoAction(api, args);
//                     args.IteratingObject = null;
//                 }
//             }
//             return null;
//         }
//     }

    //----------------------------------------------------------------------------------------------------------------
}

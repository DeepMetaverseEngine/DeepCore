using DeepCore.IO;
using System;
using System.Reflection;

namespace DeepCore.Components
{
//     public abstract class IDataSyncComponent : Disposable, IComponent
//     {
//         private int? mTag;
//         public int Tag
//         {
//             get
//             {
//                 if (mTag == null)
//                 {
//                     var attr = GetType().GetCustomAttribute<ComponentTagAttribute>(true);
//                     if (attr != null)
//                     {
//                         mTag = attr.Tag;
//                     }
//                     else
//                     {
//                         mTag = -1;
//                     }
//                 }
//                 return mTag.Value;
//             }
//         }
// 
//         private bool mEnableSyncableFields = false;
// 
//         public bool EnableSyncableFields
//         {
//             get => mEnableSyncableFields && Tag >= 0;
//             protected set => mEnableSyncableFields = value;
//         }
// 
//         protected internal readonly DiffTrackingBitSetFields SyncableFields = new DiffTrackingBitSetFields();
// 
//         protected abstract bool IsOverrideFieldFromRemote(int index, object obj);
// 
//         protected virtual void OnFieldChangedFromRemote(int index)
//         {
// 
//         }
//         protected internal void MergerFields(BitSetFields fields, bool flushNow)
//         {
//             SyncableFields.Merger(fields, flushNow, IsOverrideFieldFromRemote, OnFieldChangedFromRemote);
//         }
// 
//         protected internal void SetFieldsDirty()
//         {
//             SyncableFields.SetAllDirty();
//         }
// 
//         protected override void Disposing()
//         {
//         }
//     }


    //----------------------------------------------------------------------------------------------------------------
//     public interface IDataComponent : ISerializable
//     {
//     }
    //----------------------------------------------------------------------------------------------------------------
    /*
    public abstract class DataComponentCollection<T> : ArrayList<T> where T : IDataComponent
    {
//         [XmlSerializable]
//         private List<T> components;
//         public override int Count => components == null ? 0 : components.Count;
//         public override object this[int index] { get => components == null ? null : components[index]; }
//         public override void ForEachComponent(Action<int, object> action)
//         {
//             int i = 0;
//             this.ForEach(o =>
//             {
//                 action(i, o); i++;
//             });
//         }
//         public override void AddComponent(object obj)
//         {
//             AddComponent((T)obj);
//         }
//         public override IEnumerator GetEnumerator()
//         {
//             return components?.GetEnumerator();
//         }
//         public override void CopyTo(Array array, int index)
//         {
//             if (components != null)
//             {
//                 ((ICollection)components).CopyTo(array, index);
//             }
//         }

//         public virtual void ReadExternal(IInputStream input)
//         {
//             components = input.GetList(input.GetObjAs<T>);
//         }
//         public virtual void WriteExternal(IOutputStream output)
//         {
//             output.PutList(components, output.PutObjAs<T>);
//         }
  

        //---------------------------------------------------------------------------------------------------------

    }
    */
    //----------------------------------------------------------------------------------------------------------------
#if FALSE
    public static class DataSyncComponentExt
    {
       


        public static HashMap<int, BitSetFields> CollectComponentsFields<T>(this ComponentCollection<T> comps) where T : class, IComponent
        {
            var lazyMap = new Lazy<HashMap<int, BitSetFields>>();
            comps.ForEach(0, static (st, e) =>
            {
                if (e is IDataSyncComponent comp && comp.EnableSyncableFields && !comp.SyncableFields.IsEmpty)
                {
                    lazyMap.Value.Add(comp.Tag, comp.SyncableFields);
                }
            });

            return lazyMap.IsValueCreated ? lazyMap.Value : null;
        }

        public static void ForceSyncAllComponentFields<T>(this ComponentCollection<T> comps) where T : class, IComponent
        {
            comps.ForEach(e =>
            {
                if (e is IDataSyncComponent dataSyncComponent)
                {
                    dataSyncComponent.SetFieldsDirty();
                }
            });
        }
        /// <summary>
        /// 同步组件字段
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="other"></param>
        /// <param name="tryCreate">不存在时,尝试创建</param>
        /// <param name="flushNow">是否立即刷新，为false时会进入change缓存</param>
        /// <exception cref="ArgumentException"></exception>
        public static void SyncComponentFields<T>(this ComponentCollection<T> comps, int tag, BitSetFields other, bool tryCreate = false, bool flushNow = true) where T : class, IComponent
        {
            if (tag < 0)
            {
                throw new ArgumentException("tag must >= 0");
            }
            comps.ForEach(static (e) =>
            {
                if (e is IDataSyncComponent dataSyncComponent && dataSyncComponent.Tag == tag)
                {
                    dataSyncComponent.MergerFields(other, flushNow);
                    return true;
                }
                return false;
            });
            if (tryCreate)
            {
                var t = comps.GetRegistryComponentType(tag);
                if (t != null && typeof(IDataSyncComponent).IsAssignableFrom(t))
                {
                    var dataSyncComponent = comps.AddComponent(t) as IDataSyncComponent;
                    dataSyncComponent?.MergerFields(other, flushNow);
                }
            }
        }


    }

#endif
    //----------------------------------------------------------------------------------------------------------------
}

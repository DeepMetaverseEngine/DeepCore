using DeepCore.ORM;
using DeepCore.Threading;
using DeepCrystal.ORM.Generic;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCrystal.ORM.Utils
{
//     public class StructMappingDictionary<K, T> : MappingDictionary<int, T, WrapperStruct<T>> where T : IStructMapping
//     {
//         public StructMappingDictionary(string key, ITaskExecutor svc = null, IMappingAdapter db = null) : base(key, svc, db) { }
// 
//         //         protected override MappingObject CreateSubMapping(string fieldName, Type fieldType)
//         //         {
//         //             throw new NotImplementedException();
//         //         }
//         //         protected override IWrapper CreateSubWrapper(string fieldName, Type fieldType)
//         //         {
//         //             return new WrapperStruct<T>(this, fieldName, fieldType);
//         //         }
//         //         protected override void OnDataTypeChanged(Type type)
//         //         {
//         //             base.OnDataTypeChanged(type);
//         //             this.f_TeamType = base.InternalGetSubField("TeamType");
//         //             this.Teams = base.GetMappingField("Teams") as MappingDictionary<int, Tiny.Data.Team, Tiny.Data.TeamWrapper>;
//         // 
//         //         }
//     }
    public class StructMappingDictionary<K, T>
    {
        public IMappingAdapter DB { get; }
        public IMappingHash Hash { get; }
        public StructMappingDictionary(string key, ITaskExecutor svc = null, IMappingAdapter db = null) : base(key, svc, db) {
        
        
        }

        //         protected override MappingObject CreateSubMapping(string fieldName, Type fieldType)
        //         {
        //             throw new NotImplementedException();
        //         }
        //         protected override IWrapper CreateSubWrapper(string fieldName, Type fieldType)
        //         {
        //             return new WrapperStruct<T>(this, fieldName, fieldType);
        //         }
        //         protected override void OnDataTypeChanged(Type type)
        //         {
        //             base.OnDataTypeChanged(type);
        //             this.f_TeamType = base.InternalGetSubField("TeamType");
        //             this.Teams = base.GetMappingField("Teams") as MappingDictionary<int, Tiny.Data.Team, Tiny.Data.TeamWrapper>;
        // 
        //         }
    }
}

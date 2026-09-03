using DeepCore;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCrystal.ORM
{
    public static class MappingDataExtensions
    {
        //--------------------------------------------------------------------------------------------------------------
        #region Async

        public static async Task<T> GetAsync<T>(this IMappingHash hash, string hashField)
        {
            var query = await hash.GetAsync(hashField);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T[]> GetAsync<T>(this IMappingHash hash, string[] hashFields)
        {
            var query = await hash.GetAsync(hashFields);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        public static async Task<HashQueryEntry<T>[]> GetAllAsync<T>(this IMappingHash hash)
        {
            var array = await hash.GetAllAsync();
            return Array.ConvertAll(array, e => new HashQueryEntry<T>(
                e.FieldName,
                ORMFactory.Instance.DecodeObject<T>(e.FieldValue)));
        }
//         public static async Task<HashMap<string, T>> GetAllAsync<T>(this IMappingHash hash)
//         {
//             var ret = new HashMap<string, T>();
//             foreach (var e in await hash.GetAllAsync())
//             {
//                 ret.Add(e.FieldName, ORMFactory.Instance.DecodeObject<T>(e.FieldValue));
//             }
//             return ret;
//         }
        public static async Task<T[]> ValuesAsync<T>(this IMappingHash hash)
        {
            var query = await hash.ValuesAsync();
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        //--------------------------------------------------------------------------------------------------------------

        public static async Task<T> GetRangeAsync<T>(this IMappingString @string, long start, long end)
        {
            var query = await @string.GetRangeAsync(start, end);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T> GetAsync<T>(this IMappingString @string)
        {
            var query = await @string.GetAsync();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T> GetSetAsync<T>(this IMappingString @string, object value)
        {
            var query = await @string.GetSetAsync(value);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        //--------------------------------------------------------------------------------------------------------------
        public static async Task<T[]> MembersAsync<T>(this IMappingSet set)
        {
            var query = await set.MembersAsync();
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        public static async Task<T> PopAsync<T>(this IMappingSet set)
        {
            var query = await set.PopAsync();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T> RandomMemberAsync<T>(this IMappingSet set)
        {
            var query = await set.RandomMemberAsync();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T[]> RandomMembersAsync<T>(this IMappingSet set, long count)
        {
            var query = await set.RandomMembersAsync(count);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        //--------------------------------------------------------------------------------------------------------------
        public static async Task<T> GetByIndexAsync<T>(this IMappingList list, long index)
        {
            var query = await list.GetByIndexAsync(index);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T> LeftPopAsync<T>(this IMappingList list)
        {
            var query = await list.LeftPopAsync();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T> RightPopAsync<T>(this IMappingList list)
        {
            var query = await list.RightPopAsync();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static async Task<T[]> RangeAsync<T>(this IMappingList list, long start = 0, long stop = -1)
        {
            var query = await list.RangeAsync(start, stop);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        #endregion
//--------------------------------------------------------------------------------------------------------------
#if ORM_SYNC
        #region Sync

        //--------------------------------------------------------------------------------------------------------------
        public static T Get<T>(this IMappingHash hash, string hashField)
        {
            var query = hash.Get(hashField);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T[] Get<T>(this IMappingHash hash, string[] hashFields)
        {
            var query = hash.Get(hashFields);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        public static HashMap<string, T> GetAll<T>(this IMappingHash hash)
        {
            var ret = new HashMap<string, T>();
            foreach (var e in hash.GetAll())
            {
                ret.Add(e.FieldName, ORMFactory.Instance.DecodeObject<T>(e.FieldValue));
            }
            return ret;
        }
        public static T[] Values<T>(this IMappingHash hash)
        {
            var query = hash.Values();
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        //--------------------------------------------------------------------------------------------------------------

        public static T GetRange<T>(this IMappingString @string, long start, long end)
        {
            var query = @string.GetRange(start, end);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T Get<T>(this IMappingString @string)
        {
            var query = @string.Get();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T GetSet<T>(this IMappingString @string, object value)
        {
            var query = @string.GetSet(value);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        //--------------------------------------------------------------------------------------------------------------
        public static T[] Members<T>(this IMappingSet set)
        {
            var query = set.Members();
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        public static T Pop<T>(this IMappingSet set)
        {
            var query = set.Pop();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T RandomMember<T>(this IMappingSet set)
        {
            var query = set.RandomMember();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T[] RandomMembers<T>(this IMappingSet set, long count)
        {
            var query = set.RandomMembers(count);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        //--------------------------------------------------------------------------------------------------------------
        public static T GetByIndex<T>(this IMappingList list, long index)
        {
            var query = list.GetByIndex(index);
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T LeftPop<T>(this IMappingList list)
        {
            var query = list.LeftPop();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T RightPop<T>(this IMappingList list)
        {
            var query = list.RightPop();
            return ORMFactory.Instance.DecodeObject<T>(query);
        }
        public static T[] Range<T>(this IMappingList list, long start = 0, long stop = -1)
        {
            var query = list.Range(start, stop);
            return Array.ConvertAll(query, e => ORMFactory.Instance.DecodeObject<T>(e));
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------
#endif
    }
}

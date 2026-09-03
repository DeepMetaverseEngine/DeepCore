using DeepCore;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepTools.CodeGen
{
    public class StringHash
    {
        private HashMap<int, Type> map_attr = new HashMap<int, Type>();
        private HashMap<Type, int> map_type = new HashMap<Type, int>();
        public StringHash(ICollection<Type> types)
        {
            //原始ID
            foreach (var type in types)
            {
                map_type.Add(type, 0);
                if (!type.IsAbstract)
                {
                    var cas = type.GetCustomAttributes(typeof(MessageTypeAttribute), false);
                    var attr = cas != null && cas.Length > 0 ? (MessageTypeAttribute)cas[0] : null;
                    if (attr != null)
                    {
                        if (map_attr.TryGetValue(attr.MessageTypeID, out var exist))
                        {
                            throw new Exception($"Duplicate Message ID : {attr.MessageTypeID} : 0x{attr.MessageTypeID.ToString("X8")} : {type.FullName} == {exist.FullName}");
                        }
                        try
                        {
                            map_attr.Add(attr.MessageTypeID, type);
                            map_type.Put(type, attr.MessageTypeID);
                        }
                        catch (Exception err)
                        {
                            throw new Exception(">>> " + type.FullName + " <<< : " + err.Message, err);
                        }
                    }
                    //int id = (attr != null) ? attr.MessageTypeID : id_gen.IncrementAndGet();
                    //id += t_msg_id_plus;
                    //types_id.Add(type, id);
                    //Console.WriteLine(string.Format("Sync Type ID : {0} : {1}(0x{2})", type.FullName, id, id.ToString("X8")));
                }
            }
            //自动ID
            foreach (var type in types)
            {
                if (!type.IsAbstract)
                {
                    if (map_type[type] == 0)
                    {
                        var id = GenID(type);
                        map_attr.Add(id, type);
                        map_type.Put(type, id);
                    }
                }
            }
        }
        private int GenID(Type type)
        {
            int id = (int)BKDRHash(type.FullName);
            while (true)
            {
                if (id != IOStream.NULL_MESSAGE_CODE && id != IOStream.INVALID_MESSAGE_CODE && !map_attr.ContainsKey(id))
                {
                    return id;
                }
                id++;
            }
        }
        public int GetID(Type type)
        {
            return map_type[type];
        }


        private static uint SDBMHash(string str)
        {
            uint hash = 0;

            foreach (var ch in str)
            {
                // equivalent to: hash = 65599*hash + (ch);
                hash = (ch) + (hash << 6) + (hash << 16) - hash;
            }

            return (hash & 0x7FFFFFFF);
        }

        // RS Hash Function
        private static uint RSHash(string str)
        {
            uint b = 378551;
            uint a = 63689;
            uint hash = 0;

            foreach (var ch in str)
            {
                hash = hash * a + (ch);
                a *= b;
            }

            return (hash & 0x7FFFFFFF);
        }

        // JS Hash Function
        private static uint JSHash(string str)
        {
            uint hash = 1315423911;

            foreach (var ch in str)
            {
                hash ^= ((hash << 5) + (ch) + (hash >> 2));
            }

            return (hash & 0x7FFFFFFF);
        }
        // 
        //         // P. J. Weinberger Hash Function
        //         private static uint PJWHash(string str)
        //         {
        //             uint BitsInUnignedInt = (uint)(sizeof(uint) *8);
        //             uint ThreeQuarters = (uint)((BitsInUnignedInt * 3) / 4);
        //             uint OneEighth = (uint)(BitsInUnignedInt / 8);
        //             uint HighBits = (uint)(0xFFFFFFFF) << (BitsInUnignedInt - OneEighth);
        //             uint hash = 0;
        //             uint test = 0;
        // 
        //             foreach (var ch in str)
        //             {
        //                 hash = (hash << OneEighth) + (ch);
        //                 if ((test = hash & HighBits) != 0)
        //                 {
        //                     hash = ((hash ^ (test >> ThreeQuarters)) & (~HighBits));
        //                 }
        //             }
        // 
        //             return (hash & 0x7FFFFFFF);
        //         }
        // 
        //         // ELF Hash Function
        //         private static uint ELFHash(string str)
        //         {
        //             uint hash = 0;
        //             uint x = 0;
        // 
        //             foreach (var ch in str)
        //             {
        //                 hash = (hash << 4) + (ch);
        //                 if ((x = hash & 0xF0000000L) != 0)
        //                 {
        //                     hash ^= (x >> 24);
        //                     hash &= ~x;
        //                 }
        //             }
        // 
        //             return (hash & 0x7FFFFFFF);
        //         }

        // BKDR Hash Function
        private static uint BKDRHash(string str)
        {
            uint seed = 131; // 31 131 1313 13131 131313 etc..
            uint hash = 0;

            foreach (var ch in str)
            {
                hash = hash * seed + (ch);
            }

            return (hash & 0x7FFFFFFF);
        }

        // DJB Hash Function
        private static uint DJBHash(string str)
        {
            uint hash = 5381;

            foreach (var ch in str)
            {
                hash += (hash << 5) + (ch);
            }

            return (hash & 0x7FFFFFFF);
        }

        // AP Hash Function
        private static uint APHash(string str)
        {
            uint hash = 0;
            int i;

            for (i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                if ((i & 1) == 0)
                {
                    hash ^= ((hash << 7) ^ (ch) ^ (hash >> 3));
                }
                else
                {
                    hash ^= (~((hash << 11) ^ (ch) ^ (hash >> 5)));
                }
            }

            return (hash & 0x7FFFFFFF);
        }
    }
}

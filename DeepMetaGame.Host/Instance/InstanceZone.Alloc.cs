using DeepMetaGame.Data.Template;
using DeepCore.IO;
using System.Collections.Generic;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data;

namespace DeepCore.Game3D.Host.Instance
{
    /// <summary>
    /// 代理用类
    /// </summary>
    partial class InstanceZone
    {
        public T CloneData<T>(T src) where T : ISerializable
        {
            return ObjectPool.Clone(ZoneDataFactory.Factory.PersistCodec, src);
        }

        public ArrayList<T> CloneList<T>(IEnumerable<T> src) where T : ISerializable
        {
            var list = new ArrayList<T>();
            foreach(var d in src)
            {
               var c= ObjectPool.Clone(ZoneDataFactory.Factory.PersistCodec, d);
                list.Add(c);
            }
            return list;
        }
    }
}

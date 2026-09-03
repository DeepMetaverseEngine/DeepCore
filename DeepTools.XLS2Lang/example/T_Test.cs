using DeepCore.Reflection;
using DeepCore.IO;

namespace Example
{
    public class T_Test : ISerializable
    {
        [Desc("索引key")]
        public int id;
        [Desc("buffid")]
        public int buffid;
        [Desc("buff等级")]
        public int bufflevel;
        [Desc("buff参数")]
        public string[] buffargs;
        [Desc("自定义参数")]
        public string customarg;


        public System.Numerics.Vector3 v3;

        public System.Numerics.Vector3 vv3;
    }

}

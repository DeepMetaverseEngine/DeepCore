using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
namespace DeepCore.UnityEditor
{

    public class LightmapManager
    {
        public class LMItem
        {
            public int id;
            public string name;
            public float scale;
            public int index;
            public float tilingX;
            public float tilingY;
            public float offsetX;
            public float offsetY;

            public LMItem(int _id, string _name,
                float _scale, int _index,
                float _tilingX, float _tilingY,
                float _offsetX, float _offsetY)
            {
                id = _id;
                name = _name;
                scale = _scale;
                index = _index;
                tilingX = _tilingX;
                tilingY = _tilingY;
                offsetX = _offsetX;
                offsetY = _offsetY;
            }

            public LMItem()
            {
            }
        }

        public static void GenerateLMData(GameObject o, string path)
        {
            List<LMItem> datas = new List<LMItem>();
            Renderer[] rs = o.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in rs)
            {
                int id = r.GetInstanceID();
                string name = r.name;
                float scale = r.lightmapScaleOffset.x;
                int index = r.lightmapIndex;
                float tilingX = r.lightmapScaleOffset.x;
                float tilingY = r.lightmapScaleOffset.y;
                float offsetX = r.lightmapScaleOffset.z;
                float offsetY = r.lightmapScaleOffset.w;
                datas.Add(new LMItem(id, name, scale, index, tilingX, tilingY, offsetX, offsetY));
            }
            XmlSerializer serializer = new XmlSerializer(typeof(List<LMItem>));
            StringWriter sw = new StringWriter();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            serializer.Serialize(sw, datas, ns);

            string res = sw.ToString();

            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(path))
            {
                file.Write(res);
            }
        }
    }
}
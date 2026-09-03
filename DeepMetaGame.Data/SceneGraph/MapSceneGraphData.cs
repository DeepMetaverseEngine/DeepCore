
using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.SceneGraph
{
    //     public class MapTemplateData : ISerializable
    //     {
    //         static MapTemplateData()
    //         {
    //             Parser.RegistParser(new SceneNextLinkParser());
    //         }
    // 
    //         /**<summary>场景ID<summary/> */
    //         public int id;
    //         /**<summary>场景名称<summary/> */
    //         public string name;
    //         /**<summary>战斗地图ID<summary/> */
    //         public int zone_template_id;
    //         /**<summary>重置时间<summary/> */
    //         public string reset_time;
    //         /**<summary>复活地图ID<summary/> */
    //         public int revival_map_id;
    //         /**<summary>场景小地图<summary/> */
    //         public string small_map;
    //         /**<summary>场景连接<summary/> */
    //         public ArrayList<SceneNextLink> connect;
    //         /**<summary>人数软上限<summary/> */
    //         public int full_players;
    //         /**<summary>人数硬上限<summary/> */
    //         public int max_players;
    //         /**<summary>开放策略<summary/> */
    //         public int open_rule;
    //         /**<summary>开放日<summary/> */
    //         public string open_time;
    //         /**<summary>结束后倒计时时间<summary/> */
    //         public int countdown_time_sec;
    //         /**<summary>是否为公共地图<summary/> */
    //         public bool is_public;
    //         /// <summary>
    //         /// 是否允许主动切线.
    //         /// </summary>
    //         public int is_changeline;
    // 
    //         public override string ToString()
    //         {
    //             return string.Format("{0}({1})", name, id);
    //         }
    //     }

    /// <summary>
    /// 场景连接数据
    /// </summary>
    [MessageType(BattleConstants.SceneNextLink)]
    public class SceneNextLink : IExternalizable
    {
        public string from_flag_name;
        public Vector3 from_flag_pos;
        public int to_map_id;
        public string to_flag_name;
        public Vector3 to_flag_pos;
        public override string ToString()
        {
            return $"from_flag_name={from_flag_name} to_map_id={to_map_id} to_flag_name={to_flag_name}";
        }
        public void ReadExternal(IInputStream input)
        {
            from_flag_name = input.GetUTF();
            from_flag_pos = input.GetStruct<Vector3>();
            to_map_id = input.GetS32();
            to_flag_name = input.GetUTF();
            to_flag_pos = input.GetStruct<Vector3>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(from_flag_name);
            output.PutStruct(from_flag_pos);
            output.PutS32(to_map_id);
            output.PutUTF(to_flag_name);
            output.PutStruct(to_flag_pos);
        }

        public static SceneNextLink Parse(string text)
        {
            try
            {
                var kvc = text.Split(',');
                var ret = new SceneNextLink();
                ret.from_flag_name = kvc[0];
                ret.to_map_id = Parser.ParseInt(kvc[1]);
                ret.to_flag_name = kvc[2];
                return ret;
            }
            catch (Exception err)
            {
                throw new Exception("Parse SceneNextLink Error : " + text + " : " + err.Message, err);
            }
        }
        public static bool TryParse(string text, out SceneNextLink ret)
        {
            try
            {
                var kvc = text.Split(',');
                ret = new SceneNextLink();
                ret.from_flag_name = kvc[0];
                ret.to_map_id = Parser.ParseInt(kvc[1]);
                ret.to_flag_name = kvc[2];
                return true;
            }
            catch
            {
                ret = null;
                return false;
            }
        }

    }

    /// <summary>
    /// 场景大地图节点数据
    /// </summary>
    [MessageType(BattleConstants.SceneMapNode)]
    public class SceneMapNode : IExternalizable
    {
        /**<summary>场景ID<summary/> */
        public int id;

        /**<summary>场景大地图位置<summary/> */
        public int worldX;
        public int worldY;
        public int worldW;
        public int worldH;
        public int worldGround;
        public int worldAltitude;

        /**<summary>场景连接<summary/> */
        public List<SceneNextLink> connect;

        public void ReadExternal(IInputStream input)
        {
            id = input.GetS32();
            worldX = input.GetS32();
            worldY = input.GetS32();
            worldW = input.GetS32();
            worldH = input.GetS32();
            worldGround = input.GetS32();
            worldAltitude = input.GetS32();
            connect = input.GetListAny<SceneNextLink>();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutS32(id);
            output.PutS32(worldX);
            output.PutS32(worldY);
            output.PutS32(worldW);
            output.PutS32(worldH);
            output.PutS32(worldGround);
            output.PutS32(worldAltitude);
            output.PutListAny(connect);
        }
    }

    /// <summary>
    /// 场景大地图节点数据
    /// </summary>
    [MessageType(BattleConstants.SceneGraphData)]
    public class SceneGraphData : IExternalizable
    {
        public HashMap<int, SceneMapNode> nodes;
        public void ReadExternal(IInputStream input)
        {
            nodes = input.GetMap(
                static input => input.GetS32(),
                static input => input.GetExt<SceneMapNode>());
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutMap(nodes,
                static (o, v) => o.PutS32(v),
                static (o, v) => o.PutExt(v));
        }
    }
}

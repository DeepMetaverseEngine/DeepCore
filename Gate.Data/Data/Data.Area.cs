
using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using DeepCore.SQL;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Gate.Data
{
    //---------------------------------------------------------------------------------
    /// <summary>
    /// 表示一个场景的位置，实际坐标或者FlagName
    /// </summary>
    [PersistType]
    [MessageType(Constants.DATA_START + 13)]
    public class ZonePosition : ISerializable, IStructMapping
    {
        [PersistField]
        public string flagName;
        [PersistField]
        public float x = -1;
        [PersistField]
        public float y = -1;
        [PersistField]
        public float z = -1;

        public bool HasFlag { get { return !string.IsNullOrEmpty(flagName); } }
        public bool HasPos { get { return x >= 0 && y >= 0 && z >= 0; } }

        public DeepCore.Geometry.Vector3 Position { get => new DeepCore.Geometry.Vector3(x, y, z); }
    }

    //---------------------------------------------------------------------------------
    /// <summary>
    /// 在线玩家信息
    /// </summary>
    [MessageType(Constants.DATA_START + 14)]
    public class OnlinePlayerData : ISerializable
    {
        public string name;
        public string serverGroupId;
    }

    //---------------------------------------------------------------------------------
    /// <summary>
    /// 当前场景快照信息.
    /// </summary>
    [MessageType(Constants.DATA_START + 15)]
    public class ZoneInfoSnap : ISerializable
    {
        /// <summary>
        /// 场景ID.
        /// </summary>
        public string uuid;
        /// <summary>
        /// 活动服批量创建分线返回结果需要场景模板ID
        /// </summary>
        public int TemplateID;
        /// <summary>
        /// 线.
        /// </summary>
        public int lineIndex;
        /// <summary>
        /// 当前玩家数量.
        /// </summary>
        public int curPlayerCount;
        /// <summary>
        /// 人数硬上限数量.
        /// </summary>
        public int playerMaxCount;
        /// <summary>
        /// 人数软上限.
        /// </summary>
        public int playerFullCount;
    }

    //----------------------------------------------------------------------------------------------------------
}

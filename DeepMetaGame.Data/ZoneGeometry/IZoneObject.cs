using DeepCore;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.ZoneGeometry
{

    public interface IZoneObject
    {
        IZone Zone { get; }
        bool Enable { get; }
        /// <summary>
        /// 方向
        /// </summary>
        float Direction { get; }
        float BodyDirection { get; }
        /// <summary>
        /// 半径
        /// </summary>
        float BodySize { get; }
        /// <summary>
        /// 高度
        /// </summary>
        float BodyHeight { get; }
        /// <summary>
        /// 位置
        /// </summary>
        Vector3 Position { get; }
        /// <summary>
        /// 腰部位置
        /// </summary>
        public Vector3 WaistPosition { get => Position + new Vector3(0, 0, BodyHeight / 2f); }
        public Vector3 HeadPosition { get => Position + new Vector3(0, 0, BodyHeight); }
        public bool IsHost { get => Zone.IsHost; }
    }
    public interface IZoneUnit : IZoneObject
    {
        UnitInfo Template { get; }
        UnitSkillAbility ASkill { get; }
        float BodyScale { get; }
        bool IsActive { get; }

        bool IsControllable { get; }
        float LayerUpward { get; }

        /// <summary>
        /// 当前动做主状态
        /// </summary>
        UnitActionStatus CurrentActionStatus { get; }

        /// <summary>
        /// 当前动做子状态
        /// </summary>
        string CurrentActionSubstate { get; }

    }
    public interface IZoneItem : IZoneObject
    {
        ItemTemplate Template { get; }
    }

    public interface IVector2 : ICloneable
    {
        float X { get; set; }
        float Y { get; set; }
        DeepCore.Geometry.Vector2 ToGeometry2();
    }
    public interface IVector3 : IVector2, ICloneable
    {
        float Z { get; set; }
        DeepCore.Geometry.Vector3 ToGeometry3();
    }
    public interface IRoundObject : IVector2, ICloneable
    {
        /// <summary>
        /// 半径
        /// </summary>
        float RadiusSize { get; }
    }

}

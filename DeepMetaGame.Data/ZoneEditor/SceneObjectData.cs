using DeepCore.FuncData;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepCore;
using System.Security.Cryptography;
using System.Runtime.InteropServices.ComTypes;

namespace DeepMetaGame.Data.ZoneEditor
{

    // ----------------------------------------------------------------------------------

    [TableClass("Name")]
    public abstract class SceneObjectData : IBaseFuncData
    {
        [Desc("", "", false)]
        public string Name;
        [Desc(Category = "0.基础", Desc = "别名")]
        [LocalizationText]
        public string Alias;
        [LocalizationText]
        [Desc(Category = "0.基础", Desc = "显示名字")]
        public string DisplayName;

        [Desc(Category = "0.基础", Desc = "坐标X")]
        public float X;
        [Desc(Category = "0.基础", Desc = "坐标Y")]
        public float Y;
        [Desc(Category = "0.基础", Desc = "坐标Z")]
        public float Z;
        [Desc(Category = "0.基础", Desc = "方向")]
        public float Direction;
        [Desc(Category = "0.基础", Desc = "缩放")]
        public float Scale = 1;

        [Desc(Category = "0.基础", Desc = "方向(角度)", Editable = true)]
        public float Direction360
        {
            get => CMath.RadianToAngle(Direction);
            set { Direction = CMath.AngleToRadian(value); }
        }
        [Desc(Category = "0.基础", Desc = "体素高度")]
        public float Height = 1f;
        [Desc(Category = "0.基础", Desc = "逻辑开关")]
        public bool Enable = true;

        [Desc(Category = "8.编辑器", Desc = "编辑器是否锁定单位")]
        public bool IsLocked = false;
        [ColorValue]
        [Desc(Category = "8.编辑器", Desc = "Color(ARGB)", Editable = true)]
        public int Color = ColorValueAttribute.COLOR_GREEN;
        [Desc(Category = "8.编辑器", Desc = "", Editable = false)]
        public string SavePath;

        [Desc(Category = "9.扩展", Desc = "事件")]
        [SceneScriptID]
        public string Script;
        [Desc(Category = "9.扩展", Desc = "Tag")]
        public string Tag;
        [Desc(Category = "9.扩展", Desc = "扩展属性")]
        public string[] Attributes;

        public abstract float Radius { get; }

        [Desc(Category = "0.基础", Desc = "坐标", Editable = true)]
        public DeepCore.Geometry.Vector3 Position
        {
            get => new Vector3(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }
        [SceneSpacePosition]
        [Desc(Category = "0.基础", Desc = "世界坐标", Editable = true)]
        public string WorldPosition { get; set; }

        public abstract IReadOnlyList<EditorAbilityData> GetAbilities();
        public EditorAbilityData GetAbilityWithType(Type type)
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab != null && type.IsAssignableFrom(ab.GetType()))
                    {
                        return ab;
                    }
                }
            }
            return null;
        }
        public T GetAbilityOf<T>() where T : EditorAbilityData
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab is T)
                    {
                        return ab as T;
                    }
                }
            }
            return null;
        }
        public bool TryGetAbilityOf<T>(out T ret) where T : EditorAbilityData
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab is T)
                    {
                        ret = ab as T;
                        return true;
                    }
                }
            }
            ret = null;
            return false;
        }
        public bool TryGetAbilitiesOf<T>(out List<T> ret) where T : EditorAbilityData
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                List<T> list = null;
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab is T t)
                    {
                        list = list ?? new List<T>();
                        list.Add(t);
                    }
                }
                if (list != null)
                {
                    ret = list;
                    return true;
                }
            }
            ret = null;
            return false;
        }
        public bool TryGetAbilitiesOf<T>(List<T> ret) where T : EditorAbilityData
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab is T t)
                    {
                        ret.Add(t);
                    }
                }
                if (ret.Count == 0)
                {
                    return false;
                }
                return true;
            }
            ret = null;
            return false;
        }
        public T ForEachAs<ST, T>(ST st, BreakPredicate<ST, T> action, T defaultT = null) where T : EditorAbilityData
        {
            var abs = GetAbilities();
            if (abs != null)
            {
                for (int i = 0; i < abs.Count; i++)
                {
                    var ab = abs[i];
                    if (ab is T t && action(st, t))
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        sealed public override string ToString()
        {
            return Name + "";
        }

        public object GetObjectField(string field)
        {
            try
            {
                var fi = GetType().GetField(field);
                if (fi != null)
                {
                    return fi.GetValue(this);
                }
            }
            catch { }
            return null;
        }

        public bool SetObjectField(string field, object value)
        {
            try
            {
                var fi = GetType().GetField(field);
                if (fi != null)
                {
                    fi.SetValue(this, value);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public void ToColorARGB(out float a, out float r, out float g, out float b)
        {
            a = ((Color & 0xFF000000L) >> 24) / 255.0f;
            r = ((Color & 0x00FF0000) >> 16) / 255.0f;
            g = ((Color & 0x0000FF00) >> 8) / 255.0f;
            b = ((Color & 0x000000FF) >> 0) / 255.0f;
        }
    }
    //-------------------------------------------------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.SceneUnitData)]
    [Desc("场景单位数据")]
    public class UnitData : SceneObjectData
    {
        [Desc(Category = "1.单位", Desc = "单位模板ID")]
        [TemplateID(typeof(UnitInfo))]
        public int UnitTemplateID;
        [Desc(Category = "1.单位", Desc = "覆盖单位类型")]
        public UnitType OverrideType = UnitType.TYPE_NA;

        [Desc(Category = "1.单位", Desc = "等级")]
        [TemplateLevel]
        public int UnitLevel;
        [Desc(Category = "1.单位", Desc = "阵营")]
        public byte Force;

        [SceneObjectID(typeof(PointData))]
        [Desc(Category = "1.单位", Desc = "单位初始路点")]
        public string StartPointName;
        [SceneObjectID(typeof(DecorationData))]
        [Desc(Category = "1.单位", Desc = "复制空气墙碰撞")]
        public string CopyDecorationShape;


        [Desc(Category = "2.单位动作", Desc = "动作")]
        public UnitActionStatus MainStatus = UnitActionStatus.NA;
        [Desc(Category = "2.单位动作", Desc = "动作")]
        public string SubStatus = null;


        [Desc(Category = "3.事件", Desc = "单位绑定触发事件ID")]
        [TemplatesID(typeof(UnitEventTemplate)), Expandable]
        public ArrayList<int> Events = new ArrayList<int>();

        [Desc(Category = "5.单位", Desc = "绑定的能力")]
        [ListDesc(typeof(UnitAbilityData))]
        public ArrayList<UnitAbilityData> Abilities = new ArrayList<UnitAbilityData>();

        [Desc(Category = "5.单位", Desc = "单位标记")]
        public string UnitTag;

        public override float Radius
        {
            get { return 1; }
        }
        public UnitData()
        {
            Color = ColorValueAttribute.COLOR_BLUE;
        }
        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;

    }

    [MessageType(BattleConstants.SceneItemData)]
    [Desc("场景物品数据")]
    public class ItemData : SceneObjectData
    {
        [Desc(Category = "1.物品", Desc = "物品模板ID")]
        [TemplateID(typeof(ItemTemplate))]
        public int ItemTemplateID;
        [Desc(Category = "1.物品", Desc = "阵营")]
        public byte Force;

        [Desc(Category = "5.物品", Desc = "绑定的能力")]
        [ListDesc(typeof(ItemAbilityData))]
        public ArrayList<ItemAbilityData> Abilities = new ArrayList<ItemAbilityData>();

        public ItemData()
        {
            Color = ColorValueAttribute.COLOR_YELLOW;
        }
        public override float Radius
        {
            get { return 1; }
        }

        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;


    }

    //-------------------------------------------------------------------------------------------------------------------------------------
    public abstract class SceneVirtualObjectData : SceneObjectData
    {
        [Desc(Category = "2.显示", Desc = "绑定特效")]
        public LaunchEffect BindingEffect;


        [Desc(Category = "2.路点", Desc = "临近的所有点")]
        [NotNull]
        public ArrayList<string> NextNames = new ArrayList<string>();

        public bool HasNext => NextNames != null && NextNames.Count > 0;

        public bool ContainsNext(string name)
        {
            if (NextNames != null && NextNames.Contains(name)) return true;
            return false;
        }
        public bool LinkNext(string name)
        {
            if (NextNames == null) NextNames = new ArrayList<string>();
            if (NextNames.Contains(name)) return false;
            NextNames.Add(name);
            return true;
        }
        public bool DisLinkNext(string name)
        {
            if (NextNames == null) return false;
            return NextNames.Remove(name);
        }
    }
    //---------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.SceneRegionData)]
    [Desc("场景区域数据")]
    public class RegionData : SceneVirtualObjectData
    {
        //-----------------------------------------------------------------------------------------------
        [Desc(Category = "2.显示", Desc = "对应的模型文件名")]
        [ResourceID(ResourceType.Object)] public string ResourceName;
        [Desc(Category = "2.显示", Desc = "对应的模型文件名ID")]
        public int ResourceID
        {
            get
            {
                if (Parser.TryParseInt(ResourceName, out var resId))
                    return resId;
                return 0;
            }
        }
        [Desc(Category = "2.显示", Desc = "对应的模型文件(偏移)")]
        public Vector3 ResourceOffset;
        //-----------------------------------------------------------------------------------------------
        public enum Shape
        {
            ROUND = 0,
            RECTANGLE = 1,
            STRIP = 2,
        }
        [DependOnProperty(nameof(RegionType))] public bool IsRound { get { return RegionType == Shape.ROUND; } }
        [DependOnProperty(nameof(RegionType))] public bool IsShapDirectionality { get { return RegionType == Shape.STRIP; } }

        [Desc(Category = "2.尺寸", Desc = "区域类型")] public Shape RegionType = Shape.ROUND;
        [Desc(Category = "2.尺寸", Desc = "半径"), DependOnProperty(nameof(IsRound), true)] public float R = 10;
        [Desc(Category = "2.尺寸", Desc = "宽度"), DependOnProperty(nameof(IsRound), false)] public float W = 10;
        [Desc(Category = "2.尺寸", Desc = "长度"), DependOnProperty(nameof(IsRound), false)] public float H = 10;
        //-----------------------------------------------------------------------------------------------
        [Desc(Category = "5.能力", Desc = "绑定的能力"), ListDesc(typeof(RegionAbilityData)), NotNull()] public ArrayList<RegionAbilityData> Abilities = new ArrayList<RegionAbilityData>();
        //-----------------------------------------------------------------------------------------------
        public override float Radius
        {
            get { return IsRound ? R : Math.Max(W, H) / 2; }
        }

        public RegionData()
        {
            Color = ColorValueAttribute.COLOR_OLIVE;
        }

        public BoundingBox AABB
        {
            get
            {
                if (RegionType == RegionData.Shape.RECTANGLE)
                {
                    return new BoundingBox(
                        Position - new Vector3(W / 2, H / 2, 0),
                        Position + new Vector3(W / 2, H / 2, Height));
                }
                else if (RegionType == Shape.ROUND)
                {
                    return new BoundingBox(
                        Position - new Vector3(R / 2, R / 2, 0),
                        Position + new Vector3(R / 2, R / 2, Height));
                }
                else
                {
                    var r = Math.Max(W, H) / 2;
                    return new BoundingBox(
                        Position - new Vector3(r / 2, r / 2, 0),
                        Position + new Vector3(r / 2, r / 2, Height));
                }
            }
        }
        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;

        public IZoneShape ToZoneShape()
        {
            IZoneShape zoneShape = null;
            switch (RegionType)
            {
                case Shape.RECTANGLE:
                    zoneShape = new ZoneShapeRect
                    {
                        x = X - W / 2,
                        y = Y - H / 2,
                        w = W,
                        h = H,
                        z = Z,
                    };
                    break;
                case Shape.ROUND:
                    zoneShape = new ZoneShapeRound()
                    {
                        x = X,
                        y = Y,
                        r = R,
                        z = Z,
                    };
                    break;
                case Shape.STRIP:
                    var p = new Vector2(X, Y);
                    var q = new Vector2(X, Y);
                    VectorHelper.MovePolar(ref p, Direction, -H / 2);
                    VectorHelper.MovePolar(ref q, Direction, +H / 2);
                    zoneShape = new ZoneShapeStripWidth()
                    {
                        sx = p.X,
                        sy = p.Y,
                        dx = q.X,
                        dy = q.Y,
                        r_wide = W / 2,
                        z = Z,
                    };
                    break;
            }
            return zoneShape;
        }
    }
    //---------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.SceneDecorationData)]
    [Desc("场景装饰物")]
    public class DecorationData : SceneVirtualObjectData
    {

        public enum Shape
        {
            ROUND = 0,
            RECTANGLE = 1,
            STRIP = 2,
        }
        [Desc(Category = "2.尺寸", Desc = "区域类型")]
        public Shape RegionType = Shape.ROUND;
        [Desc(Category = "2.尺寸", Desc = "宽度")]
        [DependOnProperty(nameof(IsRound), true)]
        public float R = 5;
        [Desc(Category = "2.尺寸", Desc = "宽度")]
        [DependOnProperty(nameof(IsRound), false)]
        public float W = 10;
        [Desc(Category = "2.尺寸", Desc = "长度")]
        [DependOnProperty(nameof(IsRound), false)]
        public float H = 10;

        [DependOnProperty(nameof(RegionType))] public bool IsShapDirectionality { get { return RegionType == Shape.STRIP; } }
        [DependOnProperty(nameof(RegionType))] public bool IsRound { get { return RegionType == Shape.ROUND; } }
        //---------------------------------------------------------------------------------------------
        [Desc(Category = "3.阻挡", Desc = "是否为阻挡")]
        public bool Blockable = false;
        [ColorValue]
        [DependOnProperty(nameof(Blockable))]
        [Desc(Category = "3.阻挡", Desc = "若为阻挡，则阻挡值(ARGB)", Editable = true)]
        public int BlockValue = ColorValueAttribute.COLOR_GREEN;
        //---------------------------------------------------------------------------------------------
        [Desc(Category = "4.显示", Desc = "空气墙内部平铺网格(宽)")]
        public float GridSizeW;
        [Desc(Category = "3.显示", Desc = "空气墙内部平铺网格(高)")]
        public float GridSizeH;
        [Desc(Category = "3.显示", Desc = "对应的模型文件名")]
        [ResourceID(ResourceType.Object)] public string ResourceName;
        [Desc(Category = "2.显示", Desc = "对应的模型文件名ID")]
        public int ResourceID
        {
            get
            {
                if (Parser.TryParseInt(ResourceName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc(Category = "3.显示", Desc = "对应的模型文件(偏移)")]
        public Vector3 ResourceOffset;
        [Desc(Category = "3.显示", Desc = "出生特效")]
        [ResourceID(ResourceType.Effect)] public string EffectName_Enabled;
        [Desc(Category = "3.显示", Desc = "出生特效")]
        public int EffectID_Enabled
        {
            get
            {
                if (Parser.TryParseInt(EffectName_Enabled, out var resId))
                    return resId;
                return 0;
            }
        }
        [Desc(Category = "3.显示", Desc = "消亡特效")]
        [ResourceID(ResourceType.Effect)] public string EffectName_Disabled;
        [Desc(Category = "3.显示", Desc = "消亡特效")]
        public int EffectID_Disabled
        {
            get
            {
                if (Parser.TryParseInt(EffectName_Disabled, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc(Category = "3.显示", Desc = "当前动画名字")]
        public string AnimName;
        [Desc(Category = "3.显示", Desc = "环境音效")]
        [ResourceID(ResourceType.Sound_Ambient)] public string SoundAmbient;
        //---------------------------------------------------------------------------------------------
        [Desc(Category = "5.能力", Desc = "绑定的能力")]
        [ListDesc(typeof(DecorationAbilityData))]
        public ArrayList<DecorationAbilityData> Abilities = new ArrayList<DecorationAbilityData>();

        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;

        public DecorationData()
        {
            Color = ColorValueAttribute.COLOR_LIGHT_GREEN;
        }


        public override float Radius
        {
            get { return RegionType == Shape.ROUND ? R : Math.Max(W, H) / 2; }
        }

        public IZoneShape ToZoneShape()
        {
            IZoneShape zoneShape = null;
            switch (RegionType)
            {
                case Shape.RECTANGLE:
                    zoneShape = new ZoneShapeRect
                    {
                        x = X - W / 2,
                        y = Y - H / 2,
                        w = W,
                        h = H,
                        z = Z,
                    };
                    break;
                case Shape.ROUND:
                    zoneShape = new ZoneShapeRound()
                    {
                        x = X,
                        y = Y,
                        r = R,
                        z = Z,
                    };
                    break;
                case Shape.STRIP:
                    var p = new Vector2(X, Y);
                    var q = new Vector2(X, Y);
                    VectorHelper.MovePolar(ref p, Direction, -H / 2);
                    VectorHelper.MovePolar(ref q, Direction, +H / 2);
                    zoneShape = new ZoneShapeStripWidth()
                    {
                        sx = p.X,
                        sy = p.Y,
                        dx = q.X,
                        dy = q.Y,
                        r_wide = W / 2,
                        z = Z,
                    };
                    break;
            }
            return zoneShape;
        }

        public void GetResourcePoints(ArrayList<Vector2> list)
        {
            switch (RegionType)
            {
                case Shape.RECTANGLE:
                    ForEachExpand(GridSizeW, W, (rx) =>
                    {
                        ForEachExpand(GridSizeH, H, (ry) =>
                        {
                            list.Add(new Vector2(rx, ry));
                        });
                    });
                    break;
                case Shape.ROUND:
                    ForEachExpand(GridSizeW, R * 2, (rx) =>
                      {
                          ForEachExpand(GridSizeH, R * 2, (ry) =>
                          {
                              if (CMath.IncludeRoundPoint(0, 0, R, rx, ry))
                              {
                                  list.Add(new Vector2(rx, ry));
                              }
                          });
                      });
                    break;
                case Shape.STRIP:
                    ForEachExpand(GridSizeW, W, (rx) =>
                    {
                        ForEachExpand(GridSizeH, H, (ry) =>
                        {
                            list.Add(new Vector2(ry, rx));
                        });
                    });
                    break;
            }
        }
        public static void ForEachExpand(float cell, float len, Action<float> action)
        {
            action(0);
            if (cell > 0 && len > 0)
            {
                float half = len / 2;
                for (float r = cell; r < half; r += cell)
                {
                    action(+r);
                    action(-r);
                }
            }
        }
    }
    //---------------------------------------------------------------------------------------------

    [MessageType(BattleConstants.ScenePointData)]
    [Desc("场景路点数据")]
    public class PointData : SceneVirtualObjectData
    {
        [Desc(Category = "2.显示", Desc = "对应的模型文件名")]
        [ResourceID(ResourceType.Object)] public string ResourceName;
        [Desc(Category = "2.显示", Desc = "对应的模型文件名")]
        public int ResourceID
        {
            get
            {
                if (Parser.TryParseInt(ResourceName, out var resId))
                    return resId;
                return 0;
            }
        }

        [Desc(Category = "2.显示", Desc = "尺寸")]
        public float Size = 1;
        [Desc(Category = "2.显示", Desc = "贝塞尔切线长度")]
        public float TangentSize = 0;

        [Desc(Category = "2.显示", Desc = "对应的模型文件(偏移)")]
        public Vector3 ResourceOffset;

        [Desc(Category = "2.路点", Desc = "是否为寻路参考点")]
        public bool IsPathAnchor = false;
        [Desc(Category = "2.路点", Desc = "路点被选择概率(权重)%")]
        public float NextPercent = 100;


        [Desc(Category = "5.能力", Desc = "绑定的能力")]
        [ListDesc(typeof(PointAbilityData))]
        public ArrayList<PointAbilityData> Abilities = new ArrayList<PointAbilityData>();

        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;

        public PointData()
        {
            Color = ColorValueAttribute.COLOR_LIGHT_BLUE;
        }

        public override float Radius
        {
            get { return Size; }
        }


    }
    //---------------------------------------------------------------------------------------------
    [MessageType(BattleConstants.SceneAreaData)]
    [Desc("场景Area标识数据")]
    public class AreaData : SceneVirtualObjectData
    {
        [Desc(Category = "1.基础", Desc = "阵营")]
        public byte Force;
        [Desc(Category = "1.基础", Desc = "宽度")]
        public float W = 10;
        [Desc(Category = "1.基础", Desc = "长度")]
        public float H = 10;
        [Desc(Category = "5.能力", Desc = "绑定的能力")]
        [ListDesc(typeof(AreaAbilityData))]
        public ArrayList<AreaAbilityData> Abilities = new ArrayList<AreaAbilityData>();

        public override float Radius
        {
            get { return Math.Max(W, H) / 2; }
        }

        public AreaData()
        {
            Color = ColorValueAttribute.COLOR_RED;
        }

        public override IReadOnlyList<EditorAbilityData> GetAbilities() => Abilities;

    }

    //--------------------------------------------------------------------------------------------------------



}

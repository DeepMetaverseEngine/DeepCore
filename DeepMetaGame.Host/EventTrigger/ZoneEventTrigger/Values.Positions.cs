using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Formula;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Geometry;
using DeepCore.Reflection;
using System;
using static DeepCore.Game3D.Host.Instance.InstanceZone;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("场景-坐标")]
    public abstract class PositionValue : ZoneAbstractValue<Vector3?>
    {
        [Desc("NULL", "[游戏]/值")]
        public class NULL : PositionValue
        {
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return  null;
            }
        }
        [Desc("值", "[游戏]/值")]
        public class VALUE : PositionValue
        {
            public float X;
            public float Y;
            public float Z;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("坐标(<c color='" + sw.COLOR_CONST + "'>{0},{1},{2}</c>)", X, Y, Z);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return new Vector3(X, Y, Z);
            }
        }
        [Desc("值-变量", "[游戏]/值")]
        public class VALUEVar : PositionValue
        {
            [Desc("坐标X")]
            public AbstractValue<double> X = new RealValue.VALUE();
            [Desc("坐标Y")]
            public AbstractValue<double> Y = new RealValue.VALUE();
            [Desc("坐标Z")]
            public AbstractValue<double> Z = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("坐标({0},{1},{2})", X, Y, Z);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return new Vector3((float)X.GetValueAs(api, args), (float)Y.GetValueAs(api, args), (float)Z.GetValueAs(api, args));
            }
        }
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : PositionValue
        {
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                try
                {
                    if (args.ReturnValue is Vector3 v3) { return v3; }
                }
                catch { }
                return null;
            }
        }

        #region 功能-------------------------------------------------------------------------------------------------


        [Desc("场景物品坐标", "[游戏]/功能")]
        public class PositionOfItem : PositionValue
        {
            [Desc("道具")]
            public AbstractValue<InstanceItem> Item = new ItemValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})坐标", Item);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var item = Item.GetValueAs(api, args);
                if (item != null)
                {
                    return item.Position;
                }
                return null;
            }
        }




        [Desc("对齐矗立在网格", "[游戏]/功能")]
        public class AlignToGridCenterOnLayer : PositionValue
        {
            [Desc("网格尺寸")]
            public AbstractValue<double> GridSize = new ZoneRealValue.VALUE(1f);
            [Desc("坐标")]
            public AbstractValue<Vector3?> Pos = new PositionOfUnit();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})对其矗立在网格({1})", Pos, GridSize);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var grid = (float)GridSize.GetValueAs(api, args);
                var pos = Pos.GetValueAs(api, args);
                if (pos.HasValue && grid != 0)
                {
                    var layer = api.ZoneAPI.Terrain3D.GetVoxelLayerByPos(pos.Value);
                    if (layer != null)
                    {
                        return new Vector3(
                        CMath.AlignToCenter(pos.Value.X, grid),
                        CMath.AlignToCenter(pos.Value.Y, grid),
                        layer.Upward);
                    }
                    else
                    {
                        return new Vector3(
                        CMath.AlignToCenter(pos.Value.X, grid),
                        CMath.AlignToCenter(pos.Value.Y, grid),
                        CMath.AlignToCenter(pos.Value.Z, grid));
                    }
                }
                return null;
            }
        }


        [Desc("矗立在体素", "[游戏]/功能")]
        public class VoxelUpward : PositionValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> Pos = new PositionOfUnit();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})体素上檐", Pos);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = Pos.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    var layer = api.ZoneAPI.Terrain3D.GetVoxelLayerByPos(pos.Value);
                    if (layer != null)
                    {
                        return new Vector3(pos.Value.X, pos.Value.Y, layer.Upward);
                    }
                    else
                    {
                        return pos;
                    }
                }
                return null;
            }
        }


        [Desc("对其到体素中心", "[游戏]/功能")]
        public class AlignToLayerCenter : PositionValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> Pos = new PositionOfUnit();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})对其到体素中心", Pos);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = Pos.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    var layer = api.ZoneAPI.Terrain3D.GetVoxelLayerByPos(pos.Value);
                    if (layer != null)
                    {
                        return layer.UpwardCenterPos;
                    }
                }
                return null;
            }
        }

        [Desc("单位坐标", "[游戏]/功能")]
        public class PositionOfUnit : PositionValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})坐标", Unit);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = Unit.GetValueAs(api, args);
                if (unit != null)
                {
                    return unit.Position;
                }
                return null;
            }
        }

        [Desc("建筑出兵点", "[游戏]/功能")]
        public class PositionOfBuildingSpawn : PositionValue
        {
            [Desc("单位")]
            public AbstractValue<InstanceUnit> Unit = new UnitValue.NA();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})出兵点", Unit);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var unit = Unit.GetValueAs<InstanceBuilding>(api, args);
                if (unit != null)
                {
                    return unit.GetSpawnPos();
                }
                return null;
            }
        }


        [Desc("触发中的Spell坐标", "[游戏]/功能")]
        public class PositionOfTriggingSpell : PositionValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发中的Spell坐标");
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var s = args.TriggingSpell;
                if (s != null)
                {
                    return s.Position;
                }
                return null;
            }
        }


        [Desc("Flag坐标", "[游戏]/功能")]
        public class CenterOfFlag : PositionValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorPoint();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})坐标", Flag);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceFlag flag = Flag.GetValueAs(api, args);
                if (flag != null)
                {
                    return flag.Position;
                }
                return null;
            }
        }

        [Desc("Flag内随机点", "[游戏]/功能")]
        public class RandomPointInFlag : PositionValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})内随机点", Flag);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceFlag flag = Flag.GetValueAs(api, args);
                if (flag != null)
                {
                    var pos = flag.GetRandomPos();
                    return pos;
                }
                return null;
            }
        }
        [Desc("Flag内可移动随机点", "[游戏]/功能")]
        public class RandomMovablePointInFlag : PositionValue
        {
            [Desc("Flag")]
            public AbstractValue<InstanceFlag> Flag = new FlagValue.EditorRegion();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})内随机点", Flag);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceFlag flag = Flag.GetValueAs(api, args);
                if (flag != null)
                {
                    var pos = flag.GetSpawnPos();
                    return pos;
                }
                return null;
            }
        }


        [Desc("相对范围内随机可移动点", "[游戏]/功能")]
        public class RandomMoveableInRange : PositionValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            [Desc("半径")]
            public AbstractValue<double> Radius = new RealValue.VALUE(20);

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于位置:{0}半径:{1}范围内随机可移动点", SrcPosition, Radius);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = SrcPosition.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    //                     var node = api.ZoneAPI.PathFinderTerrain.FindNearRandomMoveableNode(api.ZoneAPI.RandomN, pos.x, pos.y, Radius.GetValueAs(api, args));
                    //                     if (node != null)
                    //                     {
                    //                         api.ZoneAPI.TerrainSrc.TryGetHeightByPos(node.x, node.y, out var z);
                    //                         return new Vector3(node.X, node.Y, z);
                    //                     }
                    var dst = api.ZoneAPI.FindNearRandomMoveablePos(pos.Value, (float)Radius.GetValueAs(api, args));
                    return dst;
                }
                return null;
            }
        }

        #endregion
        #region 数学-------------------------------------------------------------------------------------------------

        [Desc("对其到网格", "[游戏]/数学")]
        public class AlignToGridCenter : PositionValue
        {
            [Desc("网格尺寸")]
            public AbstractValue<double> GridSize = new ZoneRealValue.VALUE(1f);
            [Desc("坐标")]
            public AbstractValue<Vector3?> Pos = new PositionOfUnit();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})对其网格({1})", Pos, GridSize);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var grid = (float)GridSize.GetValueAs(api, args);
                var pos = Pos.GetValueAs(api, args);
                if (pos.HasValue && grid != 0)
                {
                    return new Vector3(
                        CMath.AlignToCenter(pos.Value.X, grid),
                        CMath.AlignToCenter(pos.Value.Y, grid),
                        CMath.AlignToCenter(pos.Value.Z, grid));
                }
                return null;
            }
        }

        [Desc("半径内随机点", "[游戏]/数学")]
        public class RandomPointInRound : PositionValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> Position = new PositionValue.PositionOfUnit();

            [Desc("范围")]
            public AbstractValue<double> Range = new RealValue.VALUE(5);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("坐标({0})范围({1})内随机", Position, Range);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = Position.GetValueAs(api, args);
                if (pos != null)
                {
                    var range = (float)Range.GetValueAs(api, args);
                    CMath.RandomPosInRound(api.ZoneAPI.RandomN, pos.Value, range, out var ret);
                    return ret;
                }
                return null;
            }
        }

        [Desc("圆环半径内随机点", "[游戏]/数学")]
        public class RandomPointInCycle : PositionValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> Position = new PositionValue.PositionOfUnit();

            [Desc("范围")]
            public AbstractValue<double> Range = new RealValue.VALUE(5);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("坐标({0})范围({1})内随机", Position, Range);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = Position.GetValueAs(api, args);
                if (pos != null)
                {
                    var range = (float)Range.GetValueAs(api, args);
                    CMath.RandomPosInCycle(api.ZoneAPI.RandomN, pos.Value, range, out var ret);
                    return ret;
                }
                return null;
            }
        }

        [Desc("相对坐标", "[游戏]/数学")]
        public class PointWithOffset : PositionValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            [Desc("偏移位置X")]
            public float OffsetX;
            [Desc("偏移位置Y")]
            public float OffsetY;
            [Desc("偏移位置Z")]
            public float OffsetZ;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于({0})位置偏移({1},{2},{3})", SrcPosition, OffsetX, OffsetY, OffsetZ);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = SrcPosition.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    var p = pos.Value;
                    p.X += OffsetX;
                    p.Y += OffsetY;
                    p.Z += OffsetZ;
                    return p;
                }
                return null;
            }
        }

        [Desc("相对坐标（变量）", "[游戏]/数学")]
        public class PointWithOffsetVar : PositionValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            [Desc("偏移位置X")]
            public AbstractValue<double> OffsetX = new RealValue.VALUE();
            [Desc("偏移位置Y")]
            public AbstractValue<double> OffsetY = new RealValue.VALUE();
            [Desc("偏移位置Z")]
            public AbstractValue<double> OffsetZ = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于({0})位置偏移({1},{2},{3})", SrcPosition, OffsetX, OffsetY, OffsetZ);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = SrcPosition.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    var p = pos.Value;
                    p.X += (float)OffsetX.GetValueAs(api, args);
                    p.Y += (float)OffsetY.GetValueAs(api, args);
                    p.Z += (float)OffsetZ.GetValueAs(api, args);
                    return p;
                }
                return null;
            }
        }

        [Desc("相对极坐标", "[游戏]/数学")]
        public class PointWithPolarOffset : PositionValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            [Desc("半径")]
            public float Radius = 1;
            [Desc("角度(0~360)")]
            public float Angle;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于({0})位置偏移(半径:{1} 角度:{2})", SrcPosition, Radius, Angle);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = SrcPosition.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    float da = CMath.AngleToRadian(Angle);
                    float dx = (float)(Math.Cos(da) * Radius);
                    float dy = (float)(Math.Sin(da) * Radius);
                    var p = pos.Value;
                    p.X += dx;
                    p.Y += dy;
                    return p;
                }
                return null;
            }
        }

        [Desc("相对极坐标（变量）", "[游戏]/数学")]
        public class PointWithPolarOffsetVar : PositionValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            [Desc("半径")]
            public AbstractValue<double> Radius = new RealValue.VALUE();
            [Desc("角度(0~360)")]
            public AbstractValue<double> Angle = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于({0})位置偏移(半径:{1} 角度:{2})", SrcPosition, Radius, Angle);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var pos = SrcPosition.GetValueAs(api, args);
                if (pos.HasValue)
                {
                    float va = (float)Angle.GetValueAs(api, args);
                    float vr = (float)Radius.GetValueAs(api, args);
                    float da = CMath.AngleToRadian(va);
                    float dx = (float)(Math.Cos(da) * vr);
                    float dy = (float)(Math.Sin(da) * vr);
                    var p = pos.Value;
                    p.X += dx;
                    p.Y += dy;
                    return p;
                }
                return null;
            }
        }

        [Desc("单位相对极坐标", "[游戏]/数学")]
        public class PointWithPolarOfUnit : PositionValue
        {
            [Desc("参照单位")]
            public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();
            [Desc("半径")]
            public float Radius = 1;
            [Desc("角度(0~360)")]
            public float Angle;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于单位({0})位置朝向偏移(半径:{1} 角度:{2})", SrcUnit, Radius, Angle);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = SrcUnit.GetValueAs(api, args);
                if (unit != null)
                {
                    var pos = unit.Position;
                    float da = CMath.AngleToRadian(Angle);
                    float dx = (float)(Math.Cos(da) * Radius);
                    float dy = (float)(Math.Sin(da) * Radius);
                    pos.X += dx;
                    pos.Y += dy;
                    return pos;
                }
                return null;
            }
        }

        [Desc("单位相对极坐标（变量）", "[游戏]/数学")]
        public class PointWithPolarOfUnitVar : PositionValue
        {
            [Desc("参照单位")]
            public AbstractValue<InstanceUnit> SrcUnit = new UnitValue.Trigging();
            [Desc("半径")]
            public AbstractValue<double> Radius = new RealValue.VALUE();
            [Desc("角度(0~360)")]
            public AbstractValue<double> Angle = new RealValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("相对于单位({0})位置朝向偏移(半径:{1} 角度:{2})", SrcUnit, Radius, Angle);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                InstanceUnit unit = SrcUnit.GetValueAs(api, args);
                if (unit != null)
                {
                    float va = (float)Angle.GetValueAs(api, args);
                    float vr = (float)Radius.GetValueAs(api, args);
                    var pos = unit.Position;//new Vector3(unit.X, unit.Y, unit.Z);
                    float da = CMath.AngleToRadian(va);
                    float dx = (float)(Math.Cos(da) * vr);
                    float dy = (float)(Math.Sin(da) * vr);
                    pos.X += dx;
                    pos.Y += dy;
                    return pos;
                }
                return null;
            }
        }

        [Desc("向量计算", "[游戏]/数学")]
        public class PointOP : PositionValue
        {
            [Desc("值1")]
            public AbstractValue<Vector3?> Value1 = new PositionValue.VALUE();
            [Desc("运算符")]
            public NumericOP OP = NumericOP.ADD;
            [Desc("值2")]
            public AbstractValue<Vector3?> Value2 = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0}) {1} ({2})", Value1, FormulaHelper.ToString(OP), Value2);
            }
            protected override Vector3? GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = Value1.GetValueAs(api, args) ?? Vector3.Zero;
                var b = Value2.GetValueAs(api, args) ?? Vector3.Zero;
                var ret = VectorHelper.Calculate(a.Value, OP, b.Value);
                return ret;
            }
        }


        #endregion

        [Desc("向量点积", "[游戏]/向量坐标")]
        public class PointDOT : ZoneRealValue
        {
            [Desc("值1")]
            public AbstractValue<Vector3?> Value1 = new PositionValue.VALUE();
            [Desc("值2")]
            public AbstractValue<Vector3?> Value2 = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("向量点积({0},{1})", Value1, Value2);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = Value1.GetValueAs(api, args) ?? Vector3.Zero;
                var b = Value2.GetValueAs(api, args) ?? Vector3.Zero;
                var ret = VectorHelper.VectorDot(a.Value, b.Value);
                return ret;
            }
        }

        [Desc("向量角度", "[游戏]/向量坐标")]
        public class PointDEGREE : ZoneRealValue
        {
            [Desc("值1")]
            public AbstractValue<Vector3?> Value1 = new PositionValue.VALUE();
            [Desc("值2")]
            public AbstractValue<Vector3?> Value2 = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("向量角度({0},{1})", Value1, Value2);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = Value1.GetValueAs(api, args) ?? Vector3.Zero;
                var b = Value2.GetValueAs(api, args) ?? Vector3.Zero;
                var ret = VectorHelper.GetDegree(a.Value, b.Value);
                return ret;
            }
        }

        [Desc("X坐标", "[游戏]/向量坐标")]
        public class PointX : ZoneRealValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> P = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.X", P);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P.GetValueAs(api, args) ?? Vector3.Zero;
                return a.X;
            }
        }
        [Desc("Y坐标", "[游戏]/向量坐标")]
        public class PointY : ZoneRealValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> P = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Y", P);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P.GetValueAs(api, args) ?? Vector3.Zero;
                return a.Y;
            }
        }
        [Desc("Z坐标", "[游戏]/向量坐标")]
        public class PointZ : ZoneRealValue
        {
            [Desc("坐标")]
            public AbstractValue<Vector3?> P = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}.Z", P);
            }
            protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var a = P.GetValueAs(api, args) ?? Vector3.Zero;
                return a.Z;
            }
        }
    }
}

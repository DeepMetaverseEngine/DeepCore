using DeepCore;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.ZoneGeometry
{
    public abstract class ZoneSpaceTransverter
    {
        public abstract Vector3 BattleToWorldPosition(VoxelInfo terrain, in DeepCore.Geometry.Vector3 p);

        public abstract DeepCore.Geometry.Vector3 WorldToBattlePosition(VoxelInfo terrain, in Vector3 Pos);

        public abstract Vector3 BattleToWorldOffset(DeepCore.Geometry.Vector3 p);

        public abstract DeepCore.Geometry.Vector3 WorldToBattleOffset(Vector3 Pos);

        public class Space3D : ZoneSpaceTransverter
        { 
            public override Vector3 BattleToWorldPosition(VoxelInfo terrain, in DeepCore.Geometry.Vector3 p)
            {
                return new Vector3(
                    p.X + terrain.ResourceStartX,
                    p.Z,
                    (terrain.TerrainH - p.Y) + terrain.ResourceStartY);
            }
            public override DeepCore.Geometry.Vector3 WorldToBattlePosition(VoxelInfo terrain, in Vector3 Pos)
            {
                return new DeepCore.Geometry.Vector3(
                    Pos.X - terrain.ResourceStartX,
                    terrain.TerrainH - (Pos.Z - terrain.ResourceStartY),
                    Pos.Y);
            }
            public override Vector3 BattleToWorldOffset(DeepCore.Geometry.Vector3 p)
            {
                return new Vector3(p.X, p.Z, -p.Y);
            }
            public override DeepCore.Geometry.Vector3 WorldToBattleOffset(Vector3 Pos)
            {
                return new DeepCore.Geometry.Vector3(Pos.X, -Pos.Z, Pos.Y);
            }
        }
        public class Space2D : ZoneSpaceTransverter
        {
            public override Vector3 BattleToWorldPosition(VoxelInfo terrain, in DeepCore.Geometry.Vector3 p)
            {
                return new Vector3(
                    p.X + terrain.ResourceStartX,
                    p.Y + terrain.ResourceStartY,
                    p.Z);
            }
            public override DeepCore.Geometry.Vector3 WorldToBattlePosition(VoxelInfo terrain, in Vector3 Pos)
            {
                return new DeepCore.Geometry.Vector3(
                    Pos.X - terrain.ResourceStartX,
                    Pos.Y - terrain.ResourceStartY,
                    Pos.Z);
            }
            public override Vector3 BattleToWorldOffset(DeepCore.Geometry.Vector3 p)
            {
                return new Vector3(p.X, p.Y, p.Z);
            }
            public override DeepCore.Geometry.Vector3 WorldToBattleOffset(Vector3 Pos)
            {
                return new DeepCore.Geometry.Vector3(Pos.X, Pos.Y, Pos.Z);
            }
        }
    }
    public static class TerrainHelper
    {
        public delegate void TerrainMapBlockAction(ZoneInfo terrain, int bx, int by);

        public static Rectangle ForEachTerrainCenterRound(this ZoneInfo terrain, float wx, float wy, float radius, TerrainMapBlockAction action)
        {
            float cw = terrain.GridCellW;
            float ch = terrain.GridCellH;
            float rw = cw / 2f;
            float rh = ch / 2f;
            int cx1 = (int)((wx - radius) / cw);
            int cy1 = (int)((wy - radius) / ch);
            int cx2 = (int)((wx + radius) / cw);
            int cy2 = (int)((wy + radius) / ch);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, terrain.XCount - 1);
            cy2 = Math.Min(cy2, terrain.YCount - 1);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    if (CMath.IncludeRoundPoint(wx, wy, radius, cx * cw + rw, cy * ch + rh))
                    {
                        action(terrain, cx, cy);
                    }
                }
            }
            return new Rectangle(cx1, cy1, cx2 - cx1 + 1, cy2 - cy1 + 1);
        }

        public static Rectangle ForEachTerrainCenterRect(this ZoneInfo terrain, float wx, float wy, float width, float height, TerrainMapBlockAction action)
        {
            var cw = terrain.GridCellW;
            var ch = terrain.GridCellH;
            int cx1 = (int)((wx - width / 2) / cw);
            int cy1 = (int)((wy - height / 2) / ch);
            int cx2 = (int)((wx + width / 2) / cw);
            int cy2 = (int)((wy + height / 2) / ch);
            cx1 = Math.Max(cx1, 0);
            cy1 = Math.Max(cy1, 0);
            cx2 = Math.Min(cx2, terrain.XCount - 1);
            cy2 = Math.Min(cy2, terrain.YCount - 1);
            for (int cx = cx1; cx <= cx2; ++cx)
            {
                for (int cy = cy1; cy <= cy2; ++cy)
                {
                    action(terrain, cx, cy);
                }
            }
            return new Rectangle(cx1, cy1, cx2 - cx1 + 1, cy2 - cy1 + 1);
        }
    }
}

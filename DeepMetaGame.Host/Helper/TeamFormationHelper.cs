using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Geometry;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Helper
{
    public class TeamFormationGroup
    {
        private readonly ISpawnContainer mRegion;
        private readonly TeamFormation mData;
        public ISpawnContainer Region { get { return mRegion; } }
        public InstanceZone Zone { get { return mRegion.Zone; } }
        public TeamFormation Data { get { return mData; } }
        public TeamFormation.Formation TFormation { get { return mData.TFormation; } }

        // 单位和预期坐标对应关系 //
        private List<IVector2> mObjects = new List<IVector2>();
        private TeamFormationGen genPos;
        public TeamFormationGroup(TeamFormation team, ISpawnContainer region)
        {
            this.mData = team;
            this.mRegion = region;
            this.genPos = new TeamFormationGen(region.Zone.RandomN, Zone.ObjectPool);
        }

        public void AddPos(float x, float y, float z, float dr)
        {
            mObjects.Add(new TeamMember(x, y, z, dr));
        }

        public TeamMember PopPos()
        {
            if (mObjects.Count > 0)
            {
                var index = mObjects.Count - 1;
                var ret = mObjects[index] as TeamMember;
                mObjects.RemoveAt(index);
                return ret;
            }
            return null;
        }
        public void Clear()
        {
            mObjects.Clear();
        }

        /// <summary>
        /// 初始化，直接放置坐标位置
        /// </summary>
        public void ResetPos(AbstractSpawnAbility spawn)
        {
            var pos = mRegion.Position;
            var center = new Vector2(pos.X, pos.Y);
            genPos.GenPos(mData.TFormation, mData.SpacingSize, mObjects, center);
            //             switch (mData.TFormation)
            //             {
            //                 case TeamFormation.Formation.Random:
            //                     ResetRandom(center);
            //                     break;
            //                 case TeamFormation.Formation.RandomCycle:
            //                     ResetRandomCycle(center);
            //                     break;
            //                 case TeamFormation.Formation.Square:
            //                     ResetSquare(center);
            //                     break;
            //                 case TeamFormation.Formation.Round:
            //                     ResetRound(center);
            //                     break;
            //                 case TeamFormation.Formation.Cycle:
            //                     ResetCycle(center);
            //                     break;
            //                 case TeamFormation.Formation.Beehive:
            //                     ResetBeehive(center);
            //                     break;
            //                 default:
            //                     ResetRandom(center);
            //                     break;
            //             }TeamFormationGen
            for (int i = mObjects.Count - 1; i >= 0; i--)
            {
                var tm = mObjects[i] as TeamMember;
                tm.SetToExpect(spawn, mRegion);
            }
        }
        //         private void ResetRandom(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeRandom(center, mObjects, mData.SpacingSize, Zone.RandomN);
        //         }
        //         private void ResetRandomCycle(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeRandomCycle(center, mObjects, mData.SpacingSize, Zone.RandomN);
        //         }
        //         private void ResetSquare(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeSquare(center, mObjects, mData.SpacingSize);
        //         }
        //         private void ResetRound(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeRound(center, mObjects, mData.SpacingSize);
        //         }
        //         private void ResetCycle(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeCycle(center, mObjects, mData.SpacingSize);
        //         }
        //         private void ResetBeehive(IVector2 center)
        //         {
        //             VectorGroupHelper.DistributeSpacingSizeBeehive(center, mObjects, mData.SpacingSize);
        //         }


        public class TeamMember : IVector3
        {
            private Geometry.Vector3 pos;
            private float direction;

            public TeamMember(float x, float y, float z, float dr)
            {
                this.pos.X = x;
                this.pos.Y = y;
                this.pos.Z = z;
                this.direction = dr;
            }
            public float X { get { return pos.X; } set { pos.X = value; } }
            public float Y { get { return pos.Y; } set { pos.Y = value; } }
            public float Z { get { return pos.Z; } set { pos.Z = value; } }
            public float Direction { get { return direction; } set { direction = value; } }
            public float RadiusSize { get { return 1; } }
            public Geometry.Vector3 Position => pos;
            public VoxelCylinder VoxelBody => new VoxelCylinder(pos, RadiusSize, 1f);

            public object Clone()
            {
                return new TeamMember(pos.X, pos.Y, pos.Z, direction);
            }

            public Geometry.Vector2 ToGeometry2() { return pos; }
            public Geometry.Vector3 ToGeometry3() { return pos; }
            internal void SetToExpect(AbstractSpawnAbility spawn, ISpawnContainer region)
            {
                region.KeepInSpawnRegion(spawn, ref pos);
            }
        }
    }
}

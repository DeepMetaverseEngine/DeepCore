using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.Game3D.Host.Instance
{

    public partial class InstanceBuilding : InstanceUnit, ISpawnContainer
    {
        protected SpawnCollection mSpawnTriggers;
        public override bool IsNoneBlock { get { return true; } }
        public override bool Moveable { get { return false; } }
        public override bool IsStaticBlock => base.IsStaticBlock && this.IsActive;


        public InstanceBuilding(InstanceZone zone, TAddUnit add)
            : base(zone, add, (add.info.BodySize > 0 && !add.info.NoTouch))
        {
            if (this.Info.Abilities.TryGetComponentAs<UnitSpawnAbility>(out var spawn))
            {
                mSpawnTriggers = new SpawnCollection(this);
                AddAbilities(spawn.SpawnUnit);
                AddAbilities(spawn.SpawnItem);
            }
        }
        protected override void Disposing()
        {
            mSpawnTriggers?.Dispose();
            base.Disposing();
        }

        override protected void OnResetAI()
        {
            DoSomething();
        }
        protected override bool onTryAdd(ref Vector3 pos, float direction)
        {
            if (IsStaticBlock)
            {
                if (Parent.Terrain3D.TryGetVoxelLayerByPos(pos, out var ground, true))
                {
                    pos.Z = ground.Upward;
                    originPos = pos;
                }
            }
            return true;
        }
        protected override void onAdded()
        {
            base.onAdded();
            updateStaticBlock();
            if (AGuard && !IsNoneSkill)
            {
                this.Components.AddComponent<UnitAutoAttackComponent>();
            }
        }
        protected override void onRemoved()
        {
            base.onRemoved();
            cleanStaticBlock();
        }
        protected override void updatePosBegin()
        {
            if (originPos.HasValue)
            {
                if (Position != originPos.Value)
                {
                    this.InternalSetPos(originPos.Value);
                }
            }

        }
        protected override void updatePhysical()
        {
            base.updatePhysical();
        }
        override protected void onUpdateAI()
        {
            updateStaticBlock();
            //             if (CurrentState is StateSkill skill)
            //             {
            //                 SetActionStatus(UnitActionStatus.Skill);
            //             }
            //             else
            //             {
            //                 SetActionStatus(UnitActionStatus.Idle);
            //             }
        }

        protected override void onStateChanged(State old_state, State state)
        {

        }
        protected override void onDamaged(InstanceUnit attacker, in TAttackSource attack, in TAttackResult result, long reduceHP)
        {
        }

        //----------------------------------------------------------------------------------------------------------------------------------------
        #region StaticBlock ---------------------------------------------------------------------------------------------------------------------
        private Vector3? originPos;
        public IZoneShape ToZoneShape()
        {
            switch (Info.FillZoneShape)
            {
                case UnitInfo.Shape.RECTANGLE:
                    return new ZoneShapeRect()
                    {
                        x = this.X - this.BodyBlockSize,
                        y = this.Y - this.BodyBlockSize,
                        z = this.Z,
                        h = this.BodyBlockSize * 2,
                        w = this.BodyBlockSize * 2,
                    };
                case UnitInfo.Shape.ROUND:
                    return new ZoneShapeRound()
                    {
                        x = this.X,
                        y = this.Y,
                        z = this.Z,
                        r = this.BodyBlockSize,
                    };
            }
            return new ZoneShapeRound()
            {
                x = this.X,
                y = this.Y,
                z = this.Z,
                r = this.BodyBlockSize,
            };
        }
        protected virtual void cleanStaticBlock()
        {
            if (this.ZoneShape != null)
            {
                onStaticBlockChanged(this.ZoneShape, false);
                this.ZoneShape = null;
            }
        }
        protected virtual void updateStaticBlock()
        {
            if (this.IsStaticBlock)
            {
                if (this.ZoneShape == null)
                {
                    this.ZoneShape = ToZoneShape();
                    onStaticBlockChanged(this.ZoneShape, true);
                }
            }
            else
            {
                if (this.ZoneShape != null)
                {
                    onStaticBlockChanged(this.ZoneShape, false);
                    this.ZoneShape = null;
                }
            }

        }
        protected virtual void onStaticBlockChanged(IZoneShape zoneShape, bool enable)
        {
            var mmap = Parent.TerrainWorld.PathFinder;
            mmap.FillMapBlockByShape(zoneShape, enable);
        }

        public bool RebuildAt(Vector3 newPos)
        {
            if (Parent.Terrain3D.TryGetVoxelLayerByPos(newPos, out var ground, true))
            {
                if (IsStaticBlock)
                {
                    newPos.Z = ground.Upward;
                }
                if (newPos != this.Position)
                {
                    cleanStaticBlock();
                    originPos = newPos;
                    updateStaticBlock();
                    Transport(newPos, true);
                    return true;
                }
            }
            return false;
        }

        #endregion StaticBlock ---------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------
        #region Spawn ---------------------------------------------------------------------------------------------------------------------
        InstanceZone ISpawnContainer.Zone => Parent;
        public SpawnCollection SpawnCollection { get => mSpawnTriggers; }
        public void BeginSpawnOnce(AbstractSpawnAbility spawn)
        {
            if (spawn is SpawnUnitAbility spawnUnitAbility)
            {
                spawnUnitAbility.setUnitForce(this.Force);
            }
            else if (spawn is SpawnItemAbility spawnItemAbility)
            {
                spawnItemAbility.setUnitForce(this.Force);
            }
        }
        public virtual Vector3 GetSpawnPos()
        {
            var pos = this.Position;
            VectorHelper.MovePolar(ref pos, this.Direction, this.BodyBlockSize * 1.5f);
            return pos;
        }
        public virtual Vector3 GetSpawnPos(AbstractSpawnAbility spawn)
        {
            var pos = this.Position;
            VectorHelper.MovePolar(ref pos, this.Direction, this.BodyBlockSize * 1.5f);
            return pos;
        }
        public virtual void KeepInSpawnRegion(AbstractSpawnAbility spawn, ref Geometry.Vector3 pos)
        {
            //             float d = MathVector.getDistance(pos.X, pos.Y, this.X, this.Y);
            //             if (d > this.BodySize)
            //             {
            //                 float a = MathVector.getDegree(pos.X, pos.Y, this.X, this.Y);
            //                 Geometry.VectorHelper.MovePolar(ref pos, a, d - this.R);
            //             }
        }
        protected internal override void cb_ActiveChanged(bool active)
        {
            base.cb_ActiveChanged(active);
            if (active)
            {
                OnSpawnEnabled?.Invoke(this);
            }
            else
            {
                OnSpawnDisabled?.Invoke(this);
            }
        }

        public event Action<ISpawnContainer> OnSpawnEnabled;
        public event Action<ISpawnContainer> OnSpawnDisabled;
        #endregion Spawn ---------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------

    }

}

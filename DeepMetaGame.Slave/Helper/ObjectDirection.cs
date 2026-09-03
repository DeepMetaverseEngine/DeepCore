using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Game3D.Slave.Helper
{
    public struct ObjectDirection
    {
        public float Direction { get; private set; }
        public float BodyDirection { get; private set; }
        public float ServerDirection { get; private set; }
        public float ServerBodyDirection { get; private set; }

        public void ForceSync(float dir, float bodyDir)
        {
            this.Direction = ServerDirection = dir;
            this.BodyDirection = ServerBodyDirection = bodyDir;
        }
        public void SyncFace(float dir, bool smooth = false)
        {
            if (smooth)
            {
                this.ServerDirection = dir;
            }
            else
            {
                this.Direction = ServerDirection = dir;
            }
        }
        public void SyncBody(float dir, bool smooth = false)
        {
            if (smooth)
            {
                this.ServerBodyDirection = dir;
            }
            else
            {
                this.BodyDirection = ServerBodyDirection = dir;
            }
        }
        public void TurnFace(float add, bool smooth = false)
        {
            if (smooth)
            {
                this.ServerDirection += add;
            }
            else
            {
                this.Direction += add;
                this.ServerDirection = this.Direction;
            }
        }
        public void TurnBody(float bodyAdd, bool smooth = false)
        {
            if (smooth)
            {
                this.ServerBodyDirection += bodyAdd;
            }
            else
            {
                this.BodyDirection += bodyAdd;
                this.ServerBodyDirection = this.BodyDirection;
            }
        }
        public void FaceTo(float direction, bool smooth = true)
        {
            if (smooth)
            {
                ServerDirection = direction;
            }
            else
            {
                ServerDirection = Direction = direction;
            }
        }
        public void BodyTo(float direction, bool smooth = true)
        {
            if (smooth)
            {
                ServerBodyDirection = direction;
            }
            else
            {
                ServerBodyDirection = BodyDirection = direction;
            }
        }

        public void Update(float intervalMS, float turnSpeed, float turnBodySpeed)
        {
            if (Direction != ServerDirection)
            {
                Direction = MoveHelper.DirectionChange(
                    Direction,
                    ServerDirection,
                    turnSpeed,
                    intervalMS);
            }
            if (BodyDirection != ServerBodyDirection)
            {
                BodyDirection = MoveHelper.DirectionChange(
                    BodyDirection,
                    ServerBodyDirection,
                    turnBodySpeed,
                    intervalMS);
            }
        }

        public void FinishUpdate()
        {
            Direction = ServerDirection;
            BodyDirection = ServerBodyDirection;
        }
    }
}

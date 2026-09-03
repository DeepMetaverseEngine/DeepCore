using DeepCore.Game3D.Slave.Helper;
using DeepCore.Geometry;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer
    {
        public virtual bool IsCanControlMove
        {
            get
            {
                if (CurrentState == UnitActionStatus.Skill)
                {
                    if (this.CurrentActorSkillAction is PreSkillByClient)
                    {
                        var st = this.CurrentActorSkillAction as PreSkillByClient;
                        return st.IsControlMoveable;
                    }
                    else if (this.CurrentActorSkillAction is PreSkillByServer)
                    {
                        var st = this.CurrentActorSkillAction as PreSkillByServer;
                        return st.IsControlMoveable;
                    }
                    return true;
                }
                return CurrentState.IsControllable();
            }
        }
        public virtual bool IsCanControlFaceTo
        {
            get
            {
                if (CurrentState == UnitActionStatus.Skill)
                {
                    if (this.CurrentActorSkillAction is PreSkillByClient)
                    {
                        var st = this.CurrentActorSkillAction as PreSkillByClient;
                        return st.IsControlFaceable;
                    }
                    else if (this.CurrentActorSkillAction is PreSkillByServer)
                    {
                        var st = this.CurrentActorSkillAction as PreSkillByServer;
                        return st.IsControlFaceable;
                    }
                    return false;
                }
                return CurrentState.IsControllable();
            }
        }
        //自动战斗中的移动
        private void updateCustomAxisAction(float intervalMS)
        {
            if (mSendingCustomAxis.HasValue)
            {
                this.PreMoveTo(mSendingCustomAxis.Value.angle, mSendingCustomAxis.Value.distanceRate * this.MoveSpeedSEC, intervalMS);
                this.SendAction(mSendingCustomAxis.Value);
            }
        }


        /// <summary>
        /// 获取当前最适合攻击的目标
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="directionChange"></param>
        /// <returns></returns>
        public virtual LayerUnit PreGetSkillAttackableFirstTarget(SkillTemplate skill, ref bool directionChange)
        {
            using (var list = this.ObjectPool.AllocList<LayerUnit>())
            {
                Parent.GetSkillAttackableUnits(this, skill, list);
                if (list.Count > 0)
                {
                    float rg = GetSkillAttackRange(skill);
                    // 检测攻击范围内的单位 //
                    float dr = skill.AttackAngle / 2;
                    var fan = new Geometry.VoxelFan(this.Position, rg, this.BodyHeight, this.Direction - dr, this.Direction + dr);
                    foreach (LayerUnit u in list)
                    {
                        //if (CMath.intersectFanRound(this.X, this.Y, rg, u.X, u.Y, u.BodyHitSize, this.Direction - dr, this.Direction + dr))
                        if (u.VoxelHitBody.Intersects(in fan))
                        {
                            directionChange = false;
                            return u;
                        }
                    }
                    // 优先当前朝向的目标 //
                    dr = CMath.PI_DIV_2;
                    directionChange = true;
                    foreach (LayerUnit u in list)
                    {
                        //if (CMath.intersectFanRound(this.X, this.Y, rg, u.X, u.Y, u.BodyHitSize, this.Direction - dr, this.Direction + dr))
                        if (u.VoxelHitBody.Intersects(in fan))
                        {
                            return u;
                        }
                    }
                    // 最后选取最近的目标 //
                    LayerUnit min = null;
                    float min_len = float.MaxValue;
                    foreach (LayerUnit u in list)
                    {
                        float len = Geometry.Vector3.DistanceSquared(u.Position, this.Position);// MathVector.getDistanceSquare(u.X, u.Y, X, Y);
                        if (min_len > len)
                        {
                            min_len = len;
                            min = u;
                        }
                    }
                    return min;
                }
            }
            return null;
        }

    }
}




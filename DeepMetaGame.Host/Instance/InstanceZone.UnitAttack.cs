using DeepCore.Game3D.Host.Helper;
using DeepCore.GameData.Data;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.XCSV;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Xml.Linq;
using static DeepCore.Colors;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;
using static DeepCore.GameData.Zone.ZoneEditor.EventTrigger.ClientFocusAction;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class InstanceZone
    {
        //-------------------------------------------------------------------------------------------------------

        #region 攻击
        //-------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 对单个单位攻击。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="attack"></param>
        /// <param name="target"></param>
        /// <param name="expectTarget">判断IsAttackable</param>
        /// <returns></returns>
        public bool UnitAttackSingle(InstanceUnit src, TAttackSource attack, InstanceUnit target, SkillTemplate.CastTarget expectTarget)
        {
            if (Formula.IsAttackable(src, target, expectTarget, AttackReason.Attack, attack.FromWeapon))
            {
                target.DoHitAttack(src, attack);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 某个单位对指定列表里的 IsAttackable 单位发起攻击。
        /// 此操作会修改List
        /// </summary>
        /// <param name="src"></param>
        /// <param name="attack"></param>
        /// <param name="list">调用完成后，列表中未命中单位会自动移除。</param>
        /// <param name="expectTarget">判断IsAttackable</param>
        /// <returns></returns>
        public int UnitAttack(InstanceUnit src, TAttackSource attack, List<InstanceUnit> list, SkillTemplate.CastTarget expectTarget)
        {
            int count = 0;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                InstanceUnit o = list[i];
                if (Formula.IsAttackable(src, o, expectTarget, AttackReason.Attack, attack.FromWeapon))
                {
                    if (o.DoHitAttack(src, attack))
                    {
                        count++;
                    }
                    else
                    {
                        list.RemoveAt(i);
                    }
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
            return count;
        }

        /// <summary>
        /// 直接对列表中的单位攻击，不做任何判断。
        /// </summary>
        /// <param name="src"></param>
        /// <param name="attack"></param>
        /// <param name="list"></param>
        public int UnitAttackDirect(InstanceUnit src, TAttackSource attack, List<InstanceUnit> list)
        {
            int count = 0;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                InstanceUnit o = list[i];
                if (o.DoHitAttack(src, attack))
                {
                    count++;
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
            return count;
        }


        /// <summary>
        /// 某个单位发起周身攻击
        /// </summary>
        /// <param name="src"></param>
        /// <param name="attack"></param>
        /// <param name="range"></param>
        /// <param name="expectTarget"></param>
        /// <returns></returns>
        public int UnitAttackRound(InstanceUnit src, TAttackSource attack, float range, SkillTemplate.CastTarget expectTarget)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                var shape = new Geometry.VoxelCylinder(src.Position, range, src.BodyHeight);
                GetObjectsInCylinder(this, static (InstanceZone state, InstanceZoneObject o, in VoxelCylinder shape) => Collider.Cylinder_Touch_HitBody(state, o, shape), shape, list);
                return UnitAttack(src, attack, list, expectTarget);
            }
        }

        /// <summary>
        /// 某个单位发起扇形范围攻击
        /// </summary>
        /// <param name="src"></param>
        /// <param name="attack"></param>
        /// <param name="direction"></param>
        /// <param name="range"></param>
        /// <param name="angle"></param>
        /// <param name="expectTarget"></param>
        /// <returns></returns>
        public int UnitAttackFan(InstanceUnit src, TAttackSource attack, float direction, float range, float angle, SkillTemplate.CastTarget expectTarget)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                var dr = angle / 2;
                var fan = new Geometry.VoxelFan(src.Position, range, src.BodyHeight, direction - dr, direction + dr);
                GetObjectsInFan(this, static (InstanceZone state, InstanceZoneObject o, in VoxelFan shape) => Collider.Fan_Touch_HitBody(state, o, shape), fan, list);
                return UnitAttack(src, attack, list, expectTarget);
            }
        }

        //         public int UnitAttackShape(InstanceUnit unit, AttackRangeHelper attack_range)
        //         {
        //                 attack_range.Shape = (AttackShape)current_action.OverrideAttackShape.AShape;
        //                 attack_range.Direction = unit.Direction;
        //                 attack_range.ExpectTarget = SkillData.ExpectTarget;
        //                 attack_range.BodySize = current_action.OverrideAttackShape.AttackRange;
        //                 attack_range.Distance = current_action.OverrideAttackShape.AttackRange;
        //                 attack_range.FanAngle = current_action.OverrideAttackShape.AttackAngle;
        //                 attack_range.StripWide = current_action.OverrideAttackShape.StripWide;
        //                 var dpos = unit.Position;
        //                 if (current_action.OverrideAttackShape.OffsetRadius != 0)
        //                 {
        //                     Geometry.VectorHelper.MovePolar(ref dpos, unit.Direction, current_action.OverrideAttackShape.OffsetRadius);
        //                 }
        //                 using (var list = unit.ObjectPool.AllocList<InstanceUnit>())
        //                 {
        //                     attack_range.GetShapeAttackable(list, AttackReason.Attack, SkillData, dpos);
        //                     if (list.Count > 0)
        //                     {
        //                         zone.UnitAttackDirect(unit, new AttackSource(skill, attack), list);
        //                     }
        //                 }
        //            
        //         }
        #endregion

        //-------------------------------------------------------------------------------------------------------
        #region 释放法术
        //-------------------------------------------------------------------------------------------------------
        public virtual int UnitLaunchSpell(
         InstanceUnit launcher,
         InstanceZoneObject sender,
         LaunchSpell launch,
         object from,
         Geometry.Vector3 startPos,
         InstanceUnit.EquipSkill fromeSkillTemplateID = null,
         InstanceUnit targetUnit = null,
         Geometry.Vector3? targetPos = null,
         float? faceDir = null,
         in SpellChainContext chain = null)
        {
            int spellCount = 0;
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                SpellTemplate spell = launcher.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(launcher, ref spell))
                {
                    var newchain = default(SpellChainContext);
                    try
                    {
                        var vchain = chain;
                        // 如果没有源链
                        if (vchain == null && launch.ChainLevel > 0)
                        {
                            newchain = vchain = SpellChainContext.Alloc(this, launch);
                        }
                        if (vchain != null)
                        {
                            if (!vchain.TryLaunch(launch.SpellID))
                            {
                                //vchain.Dispose();
                                // chain is end //
                                return spellCount;
                            }
                            if (launch.IgnoreSender)
                            {
                                if (sender is InstanceUnit iu)
                                {
                                    vchain.AddTarget(iu);
                                }
                            }
                        }
                        var add = new TAddSpell()
                        {
                            template = spell,
                            launch = launch,
                            sender = sender,
                            launcher = launcher,
                            target_obj_id = targetUnit != null ? targetUnit.ID : 0,
                            targetPos = targetPos,
                            startPos = startPos,
                            //direction = direction,
                            chain = vchain,
                            FromSkillTemplateID = fromeSkillTemplateID,
                            From = from,
                        };
                        //这里有个隐患，则如果存在延迟法术，则可能存在野指针
                        spellCount += this.StartLaunchSpellPosType(add, (this, startPos, faceDir),
                            static (st, add, out pos, out dir) =>
                            {
                                var launcher = add.launcher;
                                var startPos = st.startPos;
                                var direction = launcher.Direction;
                                if (st.faceDir != null)
                                {
                                    direction = st.faceDir.Value;
                                }
                                else if (add.targetPos != null)
                                {
                                    direction = MathVector.getDegree(
                                        startPos.X,
                                        startPos.Y,
                                        add.targetPos.Value.X,
                                        add.targetPos.Value.Y);
                                }
                                dir = direction;
                                pos = startPos;
                            },
                            static (st, add, pos, dir) =>
                            {
                                add.startPos = pos;
                                add.direction = dir;
                                st.Item1.AddSpell(add);
                            });
                    }
                    finally
                    {
                        //如果有Spell被释放出来，则这里会计数-1，如果没有Spell释放，则刚好销毁
                        newchain?.Release();
                    }
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        spellCount += UnitLaunchSpell(
                            launcher: launcher,
                            sender: sender,
                            launch: subSpell,
                            from: from,
                            startPos: startPos,
                            fromeSkillTemplateID: fromeSkillTemplateID,
                            targetUnit: targetUnit,
                            targetPos: targetPos,
                            faceDir: faceDir,
                            chain: chain);
                    }
                }
            }
            return spellCount;
        }
        public virtual int ObjectLaunchSpell(
         InstanceUnit launcher,
         InstanceZoneObject sender,
         LaunchSpell launch,
         object from,
         InstanceZoneObject startPos,
         InstanceUnit.EquipSkill fromeSkillTemplateID = null,
         InstanceUnit targetUnit = null,
         Geometry.Vector3? targetPos = null,
         in SpellChainContext chain = null)
        {
            int spellCount = 0;
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                SpellTemplate spell = launcher.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(launcher, ref spell))
                {
                    var newchain = default(SpellChainContext);
                    try
                    {
                        var vchain = chain;
                        // 如果没有源链
                        if (vchain == null && launch.ChainLevel > 0)
                        {
                            newchain = vchain = SpellChainContext.Alloc(this, launch);
                        }
                        if (vchain != null)
                        {
                            if (!vchain.TryLaunch(launch.SpellID))
                            {
                                //vchain.Dispose();
                                // chain is end //
                                return spellCount;
                            }
                            if (launch.IgnoreSender)
                            {
                                if (sender is InstanceUnit iu)
                                {
                                    vchain.AddTarget(iu);
                                }
                            }
                        }
                        var add = new TAddSpell()
                        {
                            template = spell,
                            launch = launch,
                            sender = sender,
                            launcher = launcher,
                            target_obj_id = targetUnit != null ? targetUnit.ID : 0,
                            targetPos = targetPos,
                            startPos = startPos.Position,
                            //direction = direction,
                            chain = vchain,
                            FromSkillTemplateID = fromeSkillTemplateID,
                            From = from,
                        };
                        //这里有个隐患，则如果存在延迟法术，则可能存在野指针
                        spellCount += this.StartLaunchSpellPosType(add, (this, startPos),
                            static (st, add, out pos, out dir) =>
                            {
                                var launcher = add.launcher;
                                var startPos = st.startPos.Position;
                                var direction = launcher.Direction;
                                if (add.targetPos != null)
                                {
                                    direction = MathVector.getDegree(
                                        startPos.X,
                                        startPos.Y,
                                        add.targetPos.Value.X,
                                        add.targetPos.Value.Y);
                                }
                                dir = direction;
                                pos = startPos;
                            },
                            static (st, add, pos, dir) =>
                            {
                                add.startPos = pos;
                                add.direction = dir;
                                st.Item1.AddSpell(add);
                            },
                            static (st, add, repeat) =>
                            {
                                st.startPos.Retain(repeat);
                            },
                            static (st, add, count) =>
                            {
                                st.startPos.Release();
                            });
                    }
                    finally
                    {
                        //如果有Spell被释放出来，则这里会计数-1，如果没有Spell释放，则刚好销毁
                        newchain?.Release();
                    }
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        spellCount += ObjectLaunchSpell(
                            launcher: launcher,
                            sender: sender,
                            launch: subSpell,
                            from: from,
                            startPos: startPos,
                            fromeSkillTemplateID: fromeSkillTemplateID,
                            targetUnit: targetUnit,
                            targetPos: targetPos,
                            chain: chain);
                    }
                }
            }
            return spellCount;
        }
        public virtual int SkillLaunchSpell(InstanceUnit.StateSkill skill, LaunchSpell launch)
        {
            var unit = skill.unit;
            //var param = skill.StartParam;
            //             var SkillData = skill.SkillData;
            //             float tx = unit.X;
            //             float ty = unit.Y;
            //             float dr = unit.Direction;
            //             var spellTargetPos = skill.SpellTargetPos;
            //             if (spellTargetPos != null && !spellTargetPos.Value.IsNaN)
            //             {
            //                 var pos = spellTargetPos.Value;
            //                 dr = MathVector.getDegree(unit.X, unit.Y, pos.Value.X, pos.Y);
            //                 float td = MathVector.getDistance(unit.X, unit.Y, pos.X, pos.Y);
            //                 var skillRange = unit.GetSkillAttackRange(SkillData.AttackRange);
            //                 // TargetPos超出技能范围 //
            //                 if (td > skillRange)
            //                 {
            //                     // 把TargetPos拉回 //
            //                     VectorHelper.MovePolar(ref pos, dr, skillRange - td);
            //                     spellTargetPos = pos;
            //                 }
            //                 // 设置法术出生点 (非自身坐标发射，比如Cannon) //
            //                 if (!SkillData.IsLaunchBody)
            //                 {
            //                     tx = pos.X;
            //                     ty = pos.Y;
            //                 }
            //             }
            //             else
            //             {
            //                 if (skill.IsFaceToTarget && skill.TargetUnit != null)
            //                 {
            //                     dr = MathVector.getDegree(unit.X, unit.Y, skill.TargetUnit.X, skill.TargetUnit.Y);
            //                 }
            //             }
            //             var startPos = new Vector3(tx, ty, unit.Z);
            //             var sender = unit;
            //             switch (launch.SenderUnit)
            //             {
            //                 case LaunchSpell.LaunchSpellSenderUnit.Target:
            //                     if (skill.TargetUnit != null)
            //                     {
            //                         sender = skill.TargetUnit;
            //                         startPos = sender.Position;
            //                     }
            //                     break;
            //             }
            //UnitLaunchSpell(unit, sender, launch, skill, startPos, skill.Skill, skill.TargetUnit, spellTargetPos, dr);
            int spellCount = 0;
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                var launcher = unit;
                var sender = unit;
                var targetUnit = skill.TargetUnit;
                //var targetPos = skill.SpellTargetPos;
                switch (launch.SenderUnit)
                {
                    case LaunchSpell.LaunchSpellSenderUnit.Target:
                        if (skill.TargetUnit != null)
                        {
                            sender = skill.TargetUnit;
                        }
                        break;
                }
                SpellTemplate spell = launcher.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(launcher, ref spell))
                {
                    var newchain = default(SpellChainContext);
                    try
                    {
                        var vchain = default(SpellChainContext);
                        // 如果没有源链
                        if (vchain == null && launch.ChainLevel > 0)
                        {
                            newchain = vchain = SpellChainContext.Alloc(this, launch);
                        }
                        if (vchain != null)
                        {
                            if (!vchain.TryLaunch(launch.SpellID))
                            {
                                //vchain.Dispose();
                                // chain is end //
                                return spellCount;
                            }
                            if (launch.IgnoreSender)
                            {
                                if (sender is InstanceUnit iu)
                                {
                                    vchain.AddTarget(iu);
                                }
                            }
                        }
                        var add = new TAddSpell()
                        {
                            template = spell,
                            launch = launch,
                            sender = sender,
                            launcher = launcher,
                            target_obj_id = targetUnit != null ? targetUnit.ID : 0,
                            //targetPos = null,
                            //startPos = startPos,
                            //direction = direction,
                            chain = vchain,
                            FromSkillTemplateID = skill.Skill,
                            From = skill,
                        };
                        //这里有个隐患，则如果存在延迟法术，则可能存在野指针
                        spellCount += this.StartLaunchSpellPosType(add, (this, skill, unit),
                            static (st, add, out _pos, out _dir) =>
                            {
                                var unit = st.unit;
                                var skill = st.skill;
                                var SkillData = skill.SkillData;
                                float tx = unit.X;
                                float ty = unit.Y;
                                float direction = unit.Direction;
                                var spellTargetPos = skill.SpellTargetPos;
                                if (spellTargetPos != null && !spellTargetPos.Value.IsNaN)
                                {
                                    var pos = spellTargetPos.Value;
                                    direction = MathVector.getDegree(unit.X, unit.Y, pos.X, pos.Y);
                                    float td = MathVector.getDistance(unit.X, unit.Y, pos.X, pos.Y);
                                    var skillRange = unit.GetSkillAttackRange(SkillData.AttackRange);
                                    // TargetPos超出技能范围 //
                                    if (td > skillRange)
                                    {
                                        // 把TargetPos拉回 //
                                        VectorHelper.MovePolar(ref pos, direction, skillRange - td);
                                        spellTargetPos = pos;
                                    }
                                    // 设置法术出生点 (非自身坐标发射，比如Cannon) //
                                    if (!SkillData.IsLaunchBody)
                                    {
                                        tx = pos.X;
                                        ty = pos.Y;
                                    }
                                }
                                else
                                {
                                    if (skill.IsFaceToTarget && skill.TargetUnit != null)
                                    {
                                        direction = MathVector.getDegree(unit.X, unit.Y, skill.TargetUnit.X, skill.TargetUnit.Y);
                                    }
                                }
                                var startPos = new Vector3(tx, ty, unit.Z);
                                switch (add.launch.SenderUnit)
                                {
                                    case LaunchSpell.LaunchSpellSenderUnit.Target:
                                        if (skill.TargetUnit != null)
                                        {
                                            startPos = skill.TargetUnit.Position;
                                        }
                                        break;
                                }
                                _dir = direction;
                                _pos = startPos;
                            },
                            static (st, add, _pos, _dir) =>
                            {
                                var skill = st.skill;
                                var unit = st.unit;
                                var SkillData = skill.SkillData;
                                if (!add.targetPos.HasValue)
                                {
                                    var spellTargetPos = skill.SpellTargetPos;
                                    if (spellTargetPos.HasValue && !spellTargetPos.Value.IsNaN)
                                    {
                                        var pos = spellTargetPos.Value;
                                        var direction = MathVector.getDegree(unit.X, unit.Y, pos.X, pos.Y);
                                        float td = MathVector.getDistance(unit.X, unit.Y, pos.X, pos.Y);
                                        var skillRange = unit.GetSkillAttackRange(SkillData.AttackRange);
                                        // TargetPos超出技能范围 //
                                        if (td > skillRange)
                                        {
                                            // 把TargetPos拉回 //
                                            VectorHelper.MovePolar(ref pos, direction, skillRange - td);
                                            spellTargetPos = pos;
                                        }
                                    }
                                    add.targetPos = spellTargetPos;
                                }
                                add.startPos = _pos;
                                add.direction = _dir;
                                st.Item1.AddSpell(add);
                            },
                            static (st, add, repeat) =>
                            {
                                st.skill.Retain(repeat);
                            },
                            static (st, add, count) =>
                            {
                                st.skill.Release();
                            });
                    }
                    finally
                    {
                        //如果有Spell被释放出来，则这里会计数-1，如果没有Spell释放，则刚好销毁
                        newchain?.Release();
                    }
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        spellCount += SkillLaunchSpell(skill, subSpell);
                    }
                }
            }
            return spellCount;
        }

        /*
        public virtual void SkillLaunchSpell(InstanceUnit.StateSkill skill, LaunchSpell launch)
        {
            var unit = skill.unit;
            //var param = skill.StartParam;
            var SkillData = skill.SkillData;
            float tx = unit.X;
            float ty = unit.Y;
            float dr = unit.Direction;
            var spellTargetPos = skill.SpellTargetPos;
            if (spellTargetPos != null && !spellTargetPos.Value.IsNaN)
            {
                var pos = spellTargetPos.Value;
                dr = MathVector.getDegree(unit.X, unit.Y, pos.Value.X, pos.Y);
                float td = MathVector.getDistance(unit.X, unit.Y, pos.X, pos.Y);
                var skillRange = unit.GetSkillAttackRange(SkillData.AttackRange);
                // TargetPos超出技能范围 //
                if (td > skillRange)
                {
                    // 把TargetPos拉回 //
                    VectorHelper.MovePolar(ref pos, dr, skillRange - td);
                    spellTargetPos = pos;
                }
                // 设置法术出生点 (非自身坐标发射，比如Cannon) //
                if (!SkillData.IsLaunchBody)
                {
                    tx = pos.X;
                    ty = pos.Y;
                }
            }
            else
            {
                if (skill.IsFaceToTarget && skill.TargetUnit != null)
                {
                    dr = MathVector.getDegree(unit.X, unit.Y, skill.TargetUnit.X, skill.TargetUnit.Y);
                }
            }
            var startPos = new Vector3(tx, ty, unit.Z);
            var sender = unit;
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpellSenderUnit.Target:
                    if (skill.TargetUnit != null)
                    {
                        sender = skill.TargetUnit;
                        startPos = sender.Position;
                    }
                    break;
            }
            UnitLaunchSpell(unit, sender, launch, skill, startPos, skill.Skill, skill.TargetUnit, spellTargetPos, dr);
        }
        */
        public virtual int SpellLaunchSpell(
            InstanceSpell sender,
            LaunchSpell launch,
            float reflectDirection,
            InstanceUnit targetUnit = null,
            Geometry.Vector3? targetPos = null)
        {
            int spellCount = 0;

            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                var chain = sender.ChainInfo;
                if (chain != null)
                {
                    if (!chain.TryLaunch(launch.SpellID))
                    {
                        if (launch.FinalChainSpell != null)
                        {
                            spellCount += SpellLaunchSpell(
                                sender,
                                launch.FinalChainSpell,
                                //reflectDirection,
                                reflectDirection,
                                targetUnit,
                                targetPos);
                        }
                        // chain is end //
                        //chain.Dispose();
                        return spellCount;
                    }
                }
                switch (launch.SenderUnit)
                {
                    case LaunchSpell.LaunchSpellSenderUnit.Launcher:
                        {
                            spellCount += this.UnitLaunchSpell(
                                sender.LauncherOwner,
                                sender.LauncherOwner,
                                launch,
                                sender,
                                sender.Position,
                                sender.FromSkillTemplateID, targetUnit, targetPos, null,
                                chain);
                            return spellCount;
                        }
                    case LaunchSpell.LaunchSpellSenderUnit.Target:
                        if (sender.Target != null)
                        {
                            spellCount += this.UnitLaunchSpell(
                                sender.LauncherOwner,
                                sender.Target,
                                launch,
                                sender,
                                sender.Position,
                                sender.FromSkillTemplateID, targetUnit, targetPos, null,
                                chain);
                            return spellCount;
                        }
                        break;
                }
                var spell = sender.LauncherOwner.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(sender.LauncherOwner, ref spell))
                {
                    var add = new TAddSpell()
                    {
                        template = spell,
                        launch = launch,
                        sender = sender,
                        launcher = sender.LauncherOwner,
                        target_obj_id = targetUnit != null ? targetUnit.ID : 0,
                        targetPos = targetPos,
                        //startPos = startPos,
                        //direction = direction,
                        chain = chain,
                        FromSkillTemplateID = sender.FromSkillTemplateID,
                        FromSpellUnit = sender,
                        From = sender,
                    };
                    spellCount += this.StartLaunchSpellPosType(add, (this, add, chain, sender, reflectDirection, targetUnit, targetPos),
                        static (st, add, out pos, out dir) =>
                        {
                            var sender = st.sender;
                            var launch = add.launch;
                            var direction = sender.Direction;
                            var startPos = sender.Position;
                            switch (launch.StartDirection)
                            {
                                case LaunchSpell.LaunchSpellStartDirection.ReflectDirection:
                                    direction = st.reflectDirection;
                                    break;

                                case LaunchSpell.LaunchSpellStartDirection.ReflectSender:
                                    direction = sender.Direction + CMath.RADIANS_180;
                                    break;
                                case LaunchSpell.LaunchSpellStartDirection.ReflectLauncher:
                                    direction = sender.LauncherOwner.Direction + CMath.RADIANS_180;
                                    break;
                                case LaunchSpell.LaunchSpellStartDirection.ReflectTarget:
                                    if (st.targetUnit != null)
                                    {
                                        direction = MathVector.getDegree(startPos.X, startPos.Y, st.targetUnit.X, st.targetUnit.Y) + CMath.RADIANS_180;
                                    }
                                    else if (st.targetPos != null)
                                    {
                                        direction = MathVector.getDegree(startPos.X, startPos.Y, st.targetPos.Value.X, st.targetPos.Value.Y) + CMath.RADIANS_180;
                                    }
                                    else
                                    {
                                        direction = sender.Direction + CMath.RADIANS_180;
                                    }
                                    break;

                                case LaunchSpell.LaunchSpellStartDirection.Sender:
                                    direction = sender.Direction;
                                    break;
                                case LaunchSpell.LaunchSpellStartDirection.Launcher:
                                    direction = sender.LauncherOwner.Direction;
                                    break;
                                case LaunchSpell.LaunchSpellStartDirection.FaceToTarget:
                                default:
                                    if (st.targetUnit != null)
                                    {
                                        direction = MathVector.getDegree(startPos.X, startPos.Y, st.targetUnit.X, st.targetUnit.Y);
                                    }
                                    else if (st.targetPos != null)
                                    {
                                        direction = MathVector.getDegree(startPos.X, startPos.Y, st.targetPos.Value.X, st.targetPos.Value.Y);
                                    }
                                    else
                                    {
                                        direction = sender.Direction;
                                    }
                                    break;
                            }
                            dir = direction;
                            pos = startPos;
                        },
                        static (st, add, pos, dir) =>
                        {
                            st.add.startPos = pos;
                            st.add.direction = dir;
                            st.sender.LauncherOwner.cb_unitSpellLaunchSpell(st.sender, st.add);
                            st.Item1.AddSpell(st.add);
                        });
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        spellCount += SpellLaunchSpell(
                            sender: sender,
                            launch: subSpell,
                            reflectDirection: reflectDirection,
                            targetUnit: targetUnit,
                            targetPos: targetPos);
                    }
                }
            }
            return spellCount;
        }

        public virtual int AttackLaunchSpell(
            InstanceUnit attacker,
            InstanceUnit damage,
            TAttackSource source,
            LaunchSpell launch,
            in SpellChainContext chain = null)
        {
            int spellCount = 0;
            if (launch == null) return spellCount;

            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                if (source.TryGetSrourceSkill(out var fromSkillTemplateID))
                {
                }
                InstanceZoneObject sender = attacker;
                if (source.FromSpellUnit != null)
                {
                    sender = source.FromSpellUnit;
                    fromSkillTemplateID = source.FromSpellUnit.FromSkillTemplateID;
                }
                switch (launch.SenderUnit)
                {
                    case LaunchSpell.LaunchSpellSenderUnit.Launcher:
                        sender = attacker;
                        break;
                    case LaunchSpell.LaunchSpellSenderUnit.Target:
                        sender = damage;
                        break;
                }
                SpellTemplate spell = attacker.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(attacker, ref spell))
                {
                    var vchain = chain ?? source.FromSpellUnit?.ChainInfo;
                    if (vchain != null && !vchain.TryLaunch(launch.SpellID))
                    {
                        //vchain.Dispose();
                        // chain is end //
                        return spellCount;
                    }
                    var add = new TAddSpell()
                    {
                        template = spell,
                        launch = launch,
                        sender = sender,
                        launcher = attacker,
                        target_obj_id = damage.ID,
                        targetPos = null,
                        //startPos = startPos,
                        direction = sender.Direction,
                        chain = vchain,
                        FromSkillTemplateID = fromSkillTemplateID,
                        FromSpellUnit = source.FromSpellUnit,
                        From = source,
                        damage = damage,
                    };
                    spellCount += this.StartLaunchSpellPosType(add, (this, add, vchain, damage),
                        static (st, add, out pos, out dir) =>
                        {
                            dir = add.sender.Direction;
                            pos = st.damage.Position;
                        },
                        static (st, add, pos, dir) =>
                        {
                            st.add.startPos = pos;
                            st.add.direction = dir;
                            st.Item1.AddSpell(st.add);
                        });
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        spellCount += AttackLaunchSpell(attacker, damage, source, subSpell, chain);
                    }
                }
            }
            return spellCount;
        }


        public virtual int BuffLaunchSpell(
            InstanceUnit launcher,
            InstanceUnit owner,
            InstanceUnit.EquipBuff buff,
            LaunchSpell launch,
            InstanceUnit targetUnit = null,
            Geometry.Vector3? targetPos = null)
        {
            int spellCount = 0;
            InstanceUnit sender = launcher;
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpellSenderUnit.Target:
                    sender = owner;
                    break;
                case LaunchSpell.LaunchSpellSenderUnit.Launcher:
                    sender = launcher;
                    break;
                case LaunchSpell.LaunchSpellSenderUnit.Sender:
                    //sender = sender;
                    break;
            }
            spellCount += ObjectLaunchSpell(
                launcher: launcher,
                sender: sender,
                launch: launch,
                from: buff,
                startPos: owner,
                fromeSkillTemplateID: buff.FromSkillID,
                targetUnit: targetUnit,
                targetPos: targetPos);

            return spellCount;
        }


        #endregion
        //-------------------------------------------------------------------------------------------------------
        #region BUFF
        #endregion
        //-------------------------------------------------------------------------------------------------------
        #region 召唤单位
        //-------------------------------------------------------------------------------------------------------
        public void SpellSummonUnit(InstanceSpell summoner, SummonUnit summon)
        {
            float x = summoner.X;
            float y = summoner.Y;
            float z = summoner.Z;
            float radius = summoner.BodyBlockSize;

            UnitInfo info = summoner.LauncherOwner.Cartridge.GetUnit(summon.UnitTemplateID);
            //UnitInfo un = (UnitInfo)info.Clone();
            //un.UType = UnitType.TYPE_SUMMON;
            string name = null;
            if (info != null && Formula.TrySummonUnit(summoner.LauncherOwner, summon, ref info, ref name))
            {
                for (int i = 0; i < summon.Count; i++)
                {
                    float dx, dy, dz, dr;

                    //float angle = (float)RandomN.NextDouble() * CMath.PI_MUL_2;
                    //float lengt = (float)RandomN.NextDouble() * radius;
                    //dx = x + (float)Math.Cos(angle) * lengt;
                    //dy = y + (float)Math.Sin(angle) * lengt;
                    //dr = (float)RandomN.NextDouble() * CMath.PI_MUL_2;

                    if (summon.IsRandom)
                    {
                        //召唤单位时判断当前位置能否行走，防止被召唤单位卡在地图里.Editor by Alex.
                        var p = new Geometry.Vector3(x, y, z);
                        var pos = FindNearRandomMoveableNode(ref p, radius);
                        if (pos == null)
                        {
                            dx = summoner.LauncherOwner.X;
                            dy = summoner.LauncherOwner.Y;
                            dz = summoner.LauncherOwner.Z;
                        }
                        else
                        {
                            var dpos = pos.UpwardCenterPos;
                            dx = dpos.X;
                            dy = dpos.Y;
                            dz = pos.Upward;
                        }
                    }
                    else
                    {
                        var pos = new Geometry.Vector3(x, y, z);
                        TryUpdatePos(null, ref pos, out var layer);
                        dx = pos.X;
                        dy = pos.Y;
                        dz = pos.Z;
                    }

                    dr = summoner.Direction;

                    //InstanceUnit unit = AddUnit(info, name, summoner.LauncherOwner.Force, summon.UnitLevel, dx, dy, dz, dr, );
                    InstanceUnit unit = AddUnit(new TAddUnit()
                    {
                        info = info,
                        editor_name = name,
                        player_uuid = name,
                        force = summoner.LauncherOwner.Force,
                        level = summon.UnitLevel,
                        pos = new Geometry.Vector3(dx, dy, dz),
                        direction = dr,
                        summoner = summoner.LauncherOwner,
                        //overrideType = UnitType.TYPE_SUMMON,
                    });
                    if (unit != null)
                    {
                        if (summon.Effect != null)
                        {
                            PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(summoner.ID, unit.Position, dr, summon.Effect));
                        }
                        if (info.LifeTimeMS > 0)
                        {
                            AddTimeDelayMS(info.LifeTimeMS, (task) =>
                            {
                                unit.Kill();
                            });
                        }
                    }
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------
        public InstanceUnit UnitSummonUnit(InstanceUnit summoner, SummonUnit summon)
        {
            float x = summoner.X;
            float y = summoner.Y;
            float z = summoner.Z;
            MathVector.movePolar(ref x, ref y, summoner.Direction, summoner.BodyBlockSize * 4);
            float radius = summoner.BodyBlockSize * 2;
            var pet = default(InstanceUnit);
            UnitInfo info = summoner.Cartridge.GetUnit(summon.UnitTemplateID);
            //UnitInfo un = (UnitInfo)info.Clone();
            //un.UType = UnitType.TYPE_SUMMON;
            string name = null;
            if (info != null && Formula.TrySummonUnit(summoner, summon, ref info, ref name))
            {
                for (int i = 0; i < summon.Count; i++)
                {
                    float angle = (float)RandomN.NextDouble() * CMath.PI_MUL_2;
                    float lengt = (float)RandomN.NextDouble() * radius;
                    float dx = x + (float)Math.Cos(angle) * lengt;
                    float dy = y + (float)Math.Sin(angle) * lengt;
                    float dr = (float)RandomN.NextDouble() * CMath.PI_MUL_2;
                    if (summon.Effect != null)
                    {
                        PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(summoner.ID, new Geometry.Vector3(dx, dy, z), dr, summon.Effect));
                    }
                    //InstanceUnit unit = AddUnit(info, name, summoner.Force, summon.UnitLevel, dx, dy, z, dr, summoner);
                    var evt = new TAddUnit();
                    {
                        evt.info = info;
                        evt.editor_name = name;
                        evt.player_uuid = name;
                        evt.force = summoner.Force;
                        evt.level = summon.UnitLevel;
                        evt.pos = new Geometry.Vector3(dx, dy, z);
                        evt.direction = dr;
                        evt.summoner = summoner;
                        //evt.overrideType = UnitType.TYPE_SUMMON;
                    }
                    InstanceUnit unit = AddUnit(evt);
                    if (unit != null)
                    {
                        pet = unit;
                        if (info.LifeTimeMS > 0)
                        {
                            AddTimeDelayMS(info.LifeTimeMS, (task) =>
                            {
                                unit.Kill();
                            });
                        }
                    }
                }
            }
            return pet;
        }

        public InstanceUnit UnitSummonUnit(InstanceUnit summoner, SummonUnit summon, Geometry.Vector3 pos, float dir, int overrideUnitTemplateID = 0)
        {
            var pet = default(InstanceUnit);
            var unitTemplateID = summon.UnitTemplateID;
            if (overrideUnitTemplateID > 0)
            {
                unitTemplateID = overrideUnitTemplateID;
            }
            var info = summoner.Cartridge.GetUnit(unitTemplateID);
            if (info != null)
            {
                string name = null;
                if (Formula.TrySummonUnit(summoner, summon, ref info, ref name))
                {
                    for (int i = 0; i < summon.Count; i++)
                    {
                        if (summon.Effect != null)
                        {
                            PostEvent(ObjectPool.Alloc<AddEffectEvent>().Init(summoner.ID, pos, dir, summon.Effect));
                        }
                        var unit = SummonUnit(summoner, info, name, summon.UnitLevel, pos, dir);
                        if (unit != null)
                        {
                            pet = unit;
                        }
                    }
                }
            }
            return pet;
        }

        public virtual InstanceUnit SummonUnit(InstanceUnit summoner, UnitInfo info, string name, int level, Geometry.Vector3 pos, float direction)
        {
            var evt = new TAddUnit();
            {
                evt.info = info;
                evt.editor_name = name;
                evt.player_uuid = name;
                evt.force = summoner.Force;
                evt.level = level;
                evt.pos = pos;
                evt.direction = direction;
                evt.summoner = summoner;
                //evt.overrideType = UnitType.TYPE_SUMMON;
            }
            InstanceUnit unit = AddUnit(evt);
            //InstanceUnit unit = AddUnit(info, name, summoner.Force, level, pos.X, pos.Y, pos.Z, direction, summoner);
            if (unit != null)
            {
                if (info.LifeTimeMS > 0)
                {
                    AddTimeDelayMS(info.LifeTimeMS, (task) =>
                    {
                        unit.Kill();
                    });
                }
            }
            return unit;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------
        #region 单位挂载
        public virtual InstanceUnit AttachUnit(InstanceUnit summoner, UnitAttachment attach)
        {
            var info = summoner.Templates.GetUnit(attach.UnitTemplateID);
            if (info != null)
            {
                var evt = new TAddUnit();
                {
                    evt.info = info;
                    evt.editor_name = "";
                    evt.player_uuid = "";
                    evt.force = summoner.Force;
                    evt.level = attach.UnitLevel;
                    evt.pos = summoner.Position;
                    evt.direction = summoner.Direction;
                    evt.summoner = summoner;
                }
                InstanceUnit unit = AddUnit(evt);
                //InstanceUnit unit = AddUnit(info, "", summoner.Force, attach.UnitLevel, summoner.X, summoner.Y, summoner.Z, summoner.Direction, summoner);
                if (unit != null)
                {
                    summoner.AddAttachment(unit, attach.ToDockingOffset());
                    if (info.LifeTimeMS > 0)
                    {
                        AddTimeDelayMS(info.LifeTimeMS, (task) =>
                        {
                            unit.Kill();
                        });
                    }
                }
                return unit;
            }
            return null;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------
        #region 搜寻目标


        //-------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 扫描所有可攻击对象
        /// </summary>
        /// <param name="src"></param>
        /// <param name="list"></param>
        /// <param name="expectTarget"></param>
        /// <param name="reason"></param>
        /// <param name="weapon"></param>
        public void GetAttackableUnits(InstanceUnit src, List<InstanceUnit> list,
            SkillTemplate.CastTarget expectTarget,
            AttackReason reason,
            TemplateData weapon)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                InstanceUnit o = list[i];
                if (!Formula.IsAttackable(src, o, expectTarget, reason, weapon))
                {
                    list.RemoveAt(i);
                }
            }
        }
        public void GetAttackableUnits(InstanceUnit src, List<InstanceUnit> list, EquipSkill skill, AttackReason reason)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                InstanceUnit o = list[i];
                if (!Formula.IsAttackableBySkill(src, o, skill, reason))
                {
                    list.RemoveAt(i);
                }
            }
        }
        public InstanceUnit SeekSkillAttackableUnit(InstanceUnit src, InstanceUnit.EquipSkill skill, FocusTarget focus, AttackReason reason, out Vector3? targetPos)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetObjectsInSphere(this, Collider.Sphere_Touch_HitBody, new BoundingSphere(src.Position, focus.SeekingTargetRange), list);
                GetAttackableUnits(src, list, skill, reason);
                if (list.Count > 0)
                {
                    this.Formula.SortSeekingTarget(RandomN, skill.Data, src.Position, focus.SeekingTargetExpect, list);
                    //                     switch (focus.SeekingTargetExpect)
                    //                     {
                    //                         case LaunchSkill.SeekingExpect.Random:
                    //                         //case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                    //                             CUtils.RandomList(RandomN, list);
                    //                             break;
                    //                         case LaunchSkill.SeekingExpect.Nearest:
                    //                         //case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                    //                             list.Sort(new ObjectSorterNearest<InstanceUnit>(src.Position));
                    //                             break;
                    //                         case LaunchSkill.SeekingExpect.Farthest:
                    //                         //case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                    //                             list.Sort(new ObjectSorterFarthest<InstanceUnit>(src.Position));
                    //                             break;
                    //                     }
                    if (list.Count > 0)
                    {
                        var index = focus.SeekingTargetIndex;
                        if (index >= 0 && index < list.Count)
                        {
                            var ret = list[index];
                            switch (focus.TargetAnchor)
                            {
                                case SeekingTargetAnchor.Foot: targetPos = ret.Position; break;
                                case SeekingTargetAnchor.Waist: targetPos = ret.WaistPosition; break;
                                case SeekingTargetAnchor.Head: targetPos = ret.HeadPosition; break;
                                default: targetPos = ret.WaistPosition; break;
                            }
                            return ret;
                        }
                    }
                }
            }
            targetPos = null;
            return null;
        }
        public int SeekSkillAttackableUnits<ST>(
            IList<(InstanceUnit, Vector3)> targets, ST st, BreakPredicate<ST, InstanceUnit> selector,
            InstanceUnit src,
            InstanceUnit.EquipSkill skill,
            FocusTarget focus,
            AttackReason reason)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetObjectsInSphere(this, Collider.Sphere_Touch_HitBody, new BoundingSphere(src.Position, focus.SeekingTargetRange), list);
                GetAttackableUnits(src, list, skill, reason);
                if (list.Count > 0)
                {
                    this.Formula.SortSeekingTarget(RandomN, skill.Data, src.Position, focus.SeekingTargetExpect, list);
                    //                     switch (focus.SeekingTargetExpect)
                    //                     {
                    //                         case SpellTemplate.SeekingExpect.Random:
                    //                         case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                    //                             CUtils.RandomList(RandomN, list);
                    //                             break;
                    //                         case SpellTemplate.SeekingExpect.Nearest:
                    //                         case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                    //                             list.Sort(new ObjectSorterNearest<InstanceUnit>(src.Position));
                    //                             break;
                    //                         case SpellTemplate.SeekingExpect.Farthest:
                    //                         case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                    //                             list.Sort(new ObjectSorterFarthest<InstanceUnit>(src.Position));
                    //                             break;
                    //                     }
                    if (list.Count > 0)
                    {
                        var index = focus.SeekingTargetIndex;
                        if (index >= 0 && index < list.Count)
                        {
                            var ret = list[index];
                            if (selector(st, ret))
                            {
                                var targetPos = ret.Position;
                                switch (focus.TargetAnchor)
                                {
                                    case SeekingTargetAnchor.Foot: targetPos = ret.Position; break;
                                    case SeekingTargetAnchor.Waist: targetPos = ret.WaistPosition; break;
                                    case SeekingTargetAnchor.Head: targetPos = ret.HeadPosition; break;
                                    default: targetPos = ret.WaistPosition; break;
                                }
                                targets.Add((ret, targetPos));
                            }
                        }
                    }
                }
                return list.Count;
            }
        }
        /// <summary>
        /// 锁定范围内指定目标
        /// </summary>
        /// <typeparam name="ST"></typeparam>
        /// <param name="targets"></param>
        /// <param name="st"></param>
        /// <param name="ignore"></param>
        /// <param name="launcher"></param>
        /// <param name="spell"></param>
        /// <param name="pos"></param>
        /// <param name="range"></param>
        /// <param name="expectTarget"></param>
        /// <param name="expectSeeking"></param>
        /// <param name="targetAnchor"></param>
        /// <returns></returns>
        public virtual int SeekSpellAttackable<ST>(
            IList<(InstanceUnit, Vector3)> targets,
            ST st,
            BreakPredicate<ST, InstanceUnit> ignore,
            InstanceUnit launcher,
            SpellTemplate spell,
            Geometry.Vector3? pos,
            float range,
            SkillTemplate.CastTarget expectTarget,
            LaunchSkill.SeekingExpect expectSeeking,
            SeekingTargetAnchor targetAnchor)
        {
            if (pos.HasValue == false)
            {
                return 0;
            }
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                GetObjectsInSphere(this, Collider.Sphere_Touch_HitBody, new Geometry.BoundingSphere(pos.Value, range), list);
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    InstanceUnit u = list[i];
                    if (ignore != null)
                    {
                        if (ignore(st, u))
                        {
                            list.RemoveAt(i);
                            continue;
                        }
                    }
                    if (!Formula.IsAttackable(launcher, u, expectTarget, AttackReason.Look, spell))
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                }
                this.Formula.SortSeekingTarget(RandomN, spell, pos.Value, expectSeeking, list);
                //                 switch (expectSeeking)
                //                 {
                //                     case SpellTemplate.SeekingExpect.Random:
                //                     //case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                //                         CUtils.RandomList(RandomN, list);
                //                         break;
                //                     case SpellTemplate.SeekingExpect.Nearest:
                //                     case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                //                         list.Sort(new ObjectSorterNearest<InstanceUnit>(pos.Value));
                //                         break;
                //                     case SpellTemplate.SeekingExpect.Farthest:
                //                     case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                //                         list.Sort(new ObjectSorterFarthest<InstanceUnit>(pos.Value));
                //                         break;
                //                 }
                if (list.Count > 0)
                {
                    for (int index = 0; index < list.Count; ++index)
                    {
                        var ret = list[index];
                        var targetPos = ret.Position;
                        switch (targetAnchor)
                        {
                            case SeekingTargetAnchor.Foot: targetPos = ret.Position; break;
                            case SeekingTargetAnchor.Waist: targetPos = ret.WaistPosition; break;
                            case SeekingTargetAnchor.Head: targetPos = ret.HeadPosition; break;
                            default: targetPos = ret.WaistPosition; break;
                        }
                        targets.Add((ret, targetPos));
                    }
                }
                return list.Count;
            }
        }

        public virtual (InstanceUnit, Vector3) SeekSpellAttackable(
          InstanceUnit launcher,
          SpellTemplate spell,
          Geometry.Vector3? pos,
          FocusTarget focus,
          SpellChainContext chain)
        {
            return SeekSpellAttackable(
                launcher,
                spell,
                pos,
                focus.SeekingTargetRange,
                spell.ExpectTarget,
                focus.SeekingTargetExpect,
                focus.SeekingIgnoreInChain,
                chain,
                focus.TargetAnchor,
                focus.SeekingTargetIndex);
        }


        /// <summary>
        /// 锁定范围内指定目标
        /// </summary>
        public virtual (InstanceUnit unit, Vector3 pos) SeekSpellAttackable<ST>(
            InstanceUnit launcher,
            SpellTemplate spell,
            Geometry.Vector3? pos,
            float range,
            SkillTemplate.CastTarget expectTarget,
            LaunchSkill.SeekingExpect expectSeeking,
            bool ignoreInChain,
            SpellChainContext chain,
            SeekingTargetAnchor targetAnchor,
            ST st = default,
            BreakPredicate<ST, InstanceUnit> ignore = null,
            int expectSeekingIndex = 0)
        {
            using (var result = ObjectPool.AllocList<(InstanceUnit, Vector3)>())
            {
                SeekSpellAttackable(result, (chain, expectSeeking, ignoreInChain, st, ignore), static (st, u) =>
                {
                    if (st.ignore != null)
                    {
                        if (st.ignore(st.st, u))
                        {
                            return true;
                        }
                    }
                    if (st.chain != null)
                    {
                        if (st.ignoreInChain)
                        {
                            if (st.chain.ContainsTarget(u))
                            {
                                return true; // ignore
                            }
                        }
                        else
                        {
                            if (st.chain.LastTarget == u)
                            {
                                return true; // ignore
                            }
                        }
                        //                         switch (st.expectSeeking)
                        //                         {
                        //                             case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                        //                             case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                        //                             case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                        //                                 if (st.chain.ContainsTarget(u))
                        //                                 {
                        //                                     return true;
                        //                                 }
                        //                                 break;
                        //                             default:
                        //                                 if (st.chain.LastTarget == u)
                        //                                 {
                        //                                     return true;
                        //                                 }
                        //                                 break;
                        //                         }
                    }
                    return false; // continue
                }, launcher, spell, pos, range, expectTarget, expectSeeking, targetAnchor);
                var index = expectSeekingIndex;
                if (index >= 0 && index < result.Count)
                {
                    var ret = result[index];
                    return ret;
                }
            }
            return (null, default);
        }

        public virtual InstanceUnit SeekUnitGuardTarget(InstanceUnit launcher, EquipSkill skill, UnitGuardAbility guard)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                var pos = launcher.Position;
                GetObjectsInSphere(this, Collider.Sphere_Touch_HitBody, new Geometry.BoundingSphere(launcher.Position, guard.GuardRange), list);
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    InstanceUnit u = list[i];
                    if (!Formula.IsAttackableBySkill(launcher, u, skill, AttackReason.Look))
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                }
                this.Formula.SortSeekingTarget(RandomN, skill.Data, pos, skill.LaunchSkill.AutoSeeking, list);
                //                 switch (skill.LaunchSkill.AutoSeeking)
                //                 {
                //                     case LaunchSkill.SeekingExpect.Random:
                //                         CUtils.RandomList(RandomN, list);
                //                         break;
                //                     case LaunchSkill.SeekingExpect.Nearest:
                //                         list.Sort(new ObjectSorterNearest<InstanceUnit>(pos));
                //                         break;
                //                     case LaunchSkill.SeekingExpect.Farthest:
                //                         list.Sort(new ObjectSorterFarthest<InstanceUnit>(pos));
                //                         break;
                //                 }
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            return null;
        }
        public virtual InstanceUnit SeekRangedTarget(InstanceUnit launcher, EquipSkill skill, float range)
        {
            using (var list = ObjectPool.AllocList<InstanceUnit>())
            {
                var pos = launcher.Position;
                GetObjectsInSphere(this, Collider.Sphere_Touch_HitBody, new Geometry.BoundingSphere(launcher.Position, range), list);
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    InstanceUnit u = list[i];
                    if (!Formula.IsAttackableBySkill(launcher, u, skill, AttackReason.Look))
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                }
                this.Formula.SortSeekingTarget(RandomN, skill.Data, pos, skill.LaunchSkill.AutoSeeking, list);
                //                 switch (skill.LaunchSkill.AutoSeeking)
                //                 {
                //                     case LaunchSkill.SeekingExpect.Random:
                //                         CUtils.RandomList(RandomN, list);
                //                         break;
                //                     case LaunchSkill.SeekingExpect.Nearest:
                //                         list.Sort(new ObjectSorterNearest<InstanceUnit>(pos));
                //                         break;
                //                     case LaunchSkill.SeekingExpect.Farthest:
                //                         list.Sort(new ObjectSorterFarthest<InstanceUnit>(pos));
                //                         break;
                //                 }
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            return null;
        }

        #endregion
        //-------------------------------------------------------------------------------------------------------

        // 
        //         [Obsolete("")]
        //         internal InstanceUnit AddUnit(UnitInfo info, string name, byte force, int level, float x, float y, float z, float direction, InstanceUnit summoner = null)
        //         {
        //             var evt = new TAddUnit();
        //             {
        //                 evt.info = info;
        //                 evt.editor_name = name;
        //                 evt.player_uuid = name;
        //                 evt.force = force;
        //                 evt.level = level;
        //                 evt.pos = new Geometry.Vector3(x, y, z);
        //                 evt.direction = direction;
        //                 evt.summoner = summoner;
        //             }
        //             var ret = AddUnit(evt);
        //             return ret;
        //         }
        // 
        //         [Obsolete("")]
        //         internal InstanceItem AddItem(ItemTemplate template, string name, in Geometry.Vector3 pos, float direction, byte force, InstanceUnit creater = null)
        //         {
        //             var evt = new TAddItem();
        //             {
        //                 evt.template = template;
        //                 evt.name = name;
        //                 evt.pos = pos;
        //                 evt.direction = direction;
        //                 evt.force = force;
        //                 evt.creater = creater;
        //             }
        //             var ret = AddItem(evt);
        //             //add = evt.out_event;
        //             return ret;
        //         }
        //        [Obsolete("")]
        //        internal InstanceSpell AddSpell(
        //            SpellTemplate template, LaunchSpell launch, InstanceZoneObject sender, InstanceUnit launcher, int fromSkillTemplateID,
        //            uint target_obj_id, Geometry.Vector3? targetPos, Geometry.Vector3 startPos, float direction, SpellChainLevelInfo chain = null)
        //        {
        //            var evt = new TAddSpell();
        //            {
        //                evt.template = template;
        //                evt.launch = launch;
        //                evt.sender = sender;
        //                evt.launcher = launcher;
        //                evt.target_obj_id = target_obj_id;
        //                evt.targetPos = targetPos;
        //                evt.startPos = startPos;
        //                evt.direction = direction;
        //                evt.chain = chain;
        //                evt.FromSkillTemplateID = fromSkillTemplateID;
        //            }
        //            return this.AddSpell(evt);
        //        }


    }
    //-------------------------------------------------------------------------------------------------------


    public static class SpellLauncher
    {
        public delegate void GetSpellPosition<ST>(ST st, TAddSpell add, out Vector3 pos, out float dir);
        public delegate void AddSpellAction<ST>(ST st, TAddSpell add, Vector3 startPos, float launchDir);
        public delegate void RetainAction<ST>(ST st, TAddSpell add, int repeatCount);
        public delegate void ReleaseAction<ST>(ST st, TAddSpell add, int count);


        public static int StartLaunchSpellPosType<ST>(this InstanceZone zone, TAddSpell add, ST st, GetSpellPosition<ST> getPos, AddSpellAction<ST> AddSpell, RetainAction<ST> retainAction = null, ReleaseAction<ST> releaseAction = null)
        {
            var launch = add.launch;
            getPos.Invoke(st, add, out var startPos, out var direction);
            var ret = do_launch<ST>(zone, add, startPos, direction, st, AddSpell);
            if (launch.RepeatCount > 0 && launch.RepeatIntervalMS > 0)
            {
                add.launcher?.Retain(launch.RepeatCount);
                add.sender?.Retain(launch.RepeatCount);
                add.chain?.Retain(launch.RepeatCount);
                add.FromSpellUnit?.Retain(launch.RepeatCount);
                add.FromSkillTemplateID?.Retain(launch.RepeatCount);
                retainAction?.Invoke(st, add, launch.RepeatCount);

                zone.AddTimeTask(launch.RepeatIntervalMS, launch.RepeatIntervalMS, launch.RepeatCount,
                    (zone, add, getPos, st, AddSpell, releaseAction),
                    static (st2, t) =>
                {
                    var add = st2.add;
                    st2.getPos.Invoke(st2.st, st2.add, out var startPos, out var direction);
                    var count = do_launch<ST>(st2.zone, st2.add, startPos, direction, st2.st, st2.AddSpell);

                    add.launcher?.Release();
                    add.sender?.Release();
                    add.chain?.Release();
                    add.FromSpellUnit?.Release();
                    add.FromSkillTemplateID?.Release();
                    st2.releaseAction?.Invoke(st2.st, st2.add, count);
                });
            }
            return ret;
        }

        private static int do_launch<ST>(this InstanceZone zone, TAddSpell add, Vector3 startPos, float direction, ST st, AddSpellAction<ST> AddSpell)
        {
            var launch = add.launch;
            var random = zone.RandomN;
            var spellCount = 0;
            switch (launch.PType)
            {
                case LaunchSpell.PosType.POS_TYPE_HORIZONTAL:
                    {
                        float d = launch.AdjustRandomAngle(random);
                        if (launch.Count > 1)
                        {
                            var angle = direction + launch.StartAngle + d;
                            var pos = startPos;
                            var width = launch.Step * launch.Count;
                            VectorHelper.MovePolar(ref pos, angle - CMath.RADIANS_90, width / 2f - launch.Step / 2f);
                            for (int i = 0; i < launch.Count; i++)
                            {
                                AddSpell(st, add, pos, angle);
                                spellCount++;
                                VectorHelper.MovePolar(ref pos, angle + CMath.RADIANS_90, launch.Step);
                            }
                        }
                        else
                        {
                            AddSpell(st, add, startPos, direction + launch.StartAngle + d);
                            spellCount++;
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_FAN:
                    {
                        if (launch.Count > 1)
                        {
                            float startAngle = direction - launch.Angle / 2f + launch.StartAngle;
                            float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                            for (int i = 0; i < launch.Count; i++)
                            {
                                float d = launch.AdjustRandomAngle(random);
                                AddSpell(st, add, startPos, startAngle + interAngle * i + d);
                                spellCount++;
                            }
                        }
                        else
                        {
                            float d = launch.AdjustRandomAngle(random);
                            AddSpell(st, add, startPos, direction + launch.StartAngle + d);
                            spellCount++;
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_CYCLE:
                    {
                        float startAngle = direction + launch.StartAngle;
                        float interAngle = CMath.PI_MUL_2 / launch.Count;
                        for (int i = 0; i < launch.Count; i++)
                        {
                            float d = launch.AdjustRandomAngle(random);
                            AddSpell(st, add, startPos, startAngle + interAngle * i + d);
                            spellCount++;
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                    if (add.FromSpellUnit != null)
                    {
                        for (int i = 0; i < launch.Count; i++)
                        {
                            //                             float r = (float)(random.NextFloat() * add.FromSpellUnit.BodySize);
                            //                             float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            //                             float x = (float)(startPos.X + Math.Cos(a) * r);
                            //                             float y = (float)(startPos.Y + Math.Sin(a) * r);
                            //                             float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            //                             AddSpell(st, add, new Vector3(x, y, startPos.Z), d + launch.AdjustRandomAngle(random));
                            var pos = add.FromSpellUnit.GetRandomPos();
                            float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            AddSpell(st, add, pos, d + launch.AdjustRandomAngle(random));
                            spellCount++;
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SENDER:
                    if (add.sender != null)
                    {
                        for (int i = 0; i < launch.Count; i++)
                        {
                            //                             float r = (float)(random.NextFloat() * add.FromSpellUnit.BodySize);
                            //                             float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            //                             float x = (float)(startPos.X + Math.Cos(a) * r);
                            //                             float y = (float)(startPos.Y + Math.Sin(a) * r);
                            //                             float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            //                             AddSpell(st, add, new Vector3(x, y, startPos.Z), d + launch.AdjustRandomAngle(random));
                            var pos = add.sender.GetRandomPos();
                            float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                            AddSpell(st, add, pos, d + launch.AdjustRandomAngle(random));
                            spellCount++;
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL_IN_CHAIN:
                    if (add.FromSpellUnit != null)
                    {
                        using (var pos_list = zone.ObjectPool.AllocList<Vector3>())
                        {
                            var count = launch.Count;
                            if (count > 1)
                            {
                                for (int i = -1; i < count; i++)
                                {
                                    pos_list.Add(add.FromSpellUnit.GetRandomPos());
                                }
                                for (int i = 0; i < count; i++)
                                {
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    var p1 = pos_list[i];
                                    var p2 = pos_list[i + 1];
                                    add.targetPos = p2;
                                    AddSpell(st, add, p1, d);
                                }
                                {
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    var p1 = pos_list[count];
                                    var p2 = pos_list[0];
                                    add.targetPos = p2;
                                    AddSpell(st, add, p1, d);
                                }
                            }
                            else
                            {
                                float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                AddSpell(st, add, add.FromSpellUnit.GetRandomPos(), d);
                            }
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SENDER_IN_CHAIN:
                    if (add.sender != null)
                    {
                        using (var pos_list = zone.ObjectPool.AllocList<Vector3>())
                        {
                            var count = launch.Count;
                            if (count > 1)
                            {
                                for (int i = -1; i < count; i++)
                                {
                                    pos_list.Add(add.sender.GetRandomPos());
                                }
                                for (int i = 0; i < count; i++)
                                {
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    var p1 = pos_list[i];
                                    var p2 = pos_list[i + 1];
                                    add.targetPos = p2;
                                    AddSpell(st, add, p1, d);
                                }
                                {
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    var p1 = pos_list[count];
                                    var p2 = pos_list[0];
                                    add.targetPos = p2;
                                    AddSpell(st, add, p1, d);
                                }
                            }
                            else
                            {
                                float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                AddSpell(st, add, add.sender.GetRandomPos(), d);
                            }
                        }
                    }
                    break;
                case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                default:
                    {
                        float d = launch.AdjustRandomAngle(random);
                        AddSpell(st, add, startPos, direction + launch.StartAngle + d);
                        spellCount++;
                    }
                    break;
            }
            return spellCount;
        }

    }

}

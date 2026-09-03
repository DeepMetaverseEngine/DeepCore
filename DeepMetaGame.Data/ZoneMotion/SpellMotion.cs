using DeepCore;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.EventTrigger.Data.IntegerValue;

namespace DeepMetaGame.Data.ZoneMotion
{
    public interface ISpellEntity { }


    public class SpellMotion
    {
    }
    public class SpellLauncher {
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
            var startPos = new Vector3(tx, ty, unit.Z);
            var sender = unit;
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpllSenderUnit.Target:
                    if (skill.TargetUnit != null)
                    {
                        sender = skill.TargetUnit;
                        startPos = sender.Position;
                    }
                    break;
            }
            UnitLaunchSpell(unit, sender, launch, skill, startPos, SkillData.ID, skill.TargetUnitID, spellTargetPos, dr);
        }

        public virtual void UnitLaunchSpell(
            InstanceUnit launcher,
            InstanceZoneObject sender,
            LaunchSpell launch,
            object from,
            Geometry.Vector3 startPos,
            int? fromeSkillTemplateID = null,
            uint targetUnitID = 0,
            Geometry.Vector3? targetPos = null,
            float? _direction = null)
        {
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                float direction = launcher.Direction;
                if (targetPos != null)
                {
                    direction = MathVector.getDegree(startPos.X, startPos.Y, targetPos.Value.X, targetPos.Value.Y);
                }
                if (_direction != null)
                {
                    direction = _direction.Value;
                }
                SpellTemplate spell = launcher.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(launcher, ref spell))
                {
                    SpellChainLevelInfo chain = null;
                    if (launch.ChainLevel > 0)
                    {
                        chain = new SpellChainLevelInfo(launch);
                        if (launch.IgnoreSender)
                        {
                            if (sender is InstanceUnit iu)
                            {
                                chain.AddTarget(iu);
                            }
                        }
                    }
                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                if (launch.Count > 1)
                                {
                                    float startAngle = direction - launch.Angle / 2f + launch.StartAngle;
                                    float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                    for (int i = 0; i < launch.Count; i++)
                                    {
                                        float d = launch.AdjustRandomAngle(random);
                                        AddSpell(new Instance.TAddSpell()
                                        {
                                            template = spell,
                                            launch = launch,
                                            sender = sender,
                                            launcher = launcher,
                                            target_obj_id = targetUnitID,
                                            startPos = startPos,
                                            targetPos = targetPos,
                                            direction = startAngle + interAngle * i + d,
                                            chain = chain,
                                            FromSkillTemplateID = fromeSkillTemplateID,
                                            From = from,
                                        });
                                    }
                                }
                                else
                                {
                                    float d = launch.AdjustRandomAngle(random);
                                    AddSpell(new Instance.TAddSpell()
                                    {
                                        template = spell,
                                        launch = launch,
                                        sender = sender,
                                        launcher = launcher,
                                        target_obj_id = targetUnitID,
                                        startPos = startPos,
                                        targetPos = targetPos,
                                        direction = direction + launch.StartAngle + d,
                                        chain = chain,
                                        FromSkillTemplateID = fromeSkillTemplateID,
                                        From = from,
                                    });
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
                                    AddSpell(new Instance.TAddSpell()
                                    {
                                        template = spell,
                                        launch = launch,
                                        sender = sender,
                                        launcher = launcher,
                                        target_obj_id = targetUnitID,
                                        startPos = startPos,
                                        targetPos = targetPos,
                                        direction = startAngle + interAngle * i + d,
                                        chain = chain,
                                        FromSkillTemplateID = fromeSkillTemplateID,
                                        From = from,
                                    });
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            {
                                log.Error(string.Format("Can not launch [POS_TYPE_RANDOM_FOR_SPELL] spell from unitLaunchSpell: {0} {1}", spell, launcher.Info));
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                float d = launch.AdjustRandomAngle(random);
                                AddSpell(new Instance.TAddSpell()
                                {
                                    template = spell,
                                    launch = launch,
                                    sender = sender,
                                    launcher = launcher,
                                    target_obj_id = targetUnitID,
                                    startPos = startPos,
                                    targetPos = targetPos,
                                    direction = direction + launch.StartAngle + d,
                                    chain = chain,
                                    FromSkillTemplateID = fromeSkillTemplateID,
                                    From = from,
                                });
                            }
                            break;
                    }

                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        UnitLaunchSpell(launcher, sender, subSpell, from, startPos, fromeSkillTemplateID, targetUnitID, targetPos, _direction);
                    }
                }
            }
        }

        // 单位释放法术
        public virtual void SpellLaunchSpell(
            InstanceSpell sender,
            LaunchSpell launch,
            Geometry.Vector3 startPos,
            uint targetUnitID = 0,
            Geometry.Vector3? targetPos = null)
        {
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpllSenderUnit.Launcher:
                    this.UnitLaunchSpell(
                        sender.LauncherOwner,
                        sender.LauncherOwner,
                        launch,
                        sender,
                        startPos,
                        sender.FromSkillTemplateID, targetUnitID, targetPos);
                    return;
                case LaunchSpell.LaunchSpllSenderUnit.Target:
                    if (sender.Target != null)
                    {
                        this.UnitLaunchSpell(
                            sender.LauncherOwner,
                            sender.Target,
                            launch,
                            sender,
                            startPos,
                            sender.FromSkillTemplateID, targetUnitID, targetPos);
                        return;
                    }
                    break;
            }
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                SpellChainLevelInfo chain = sender.ChainInfo;
                if (chain != null)
                {
                    if (!chain.TryLaunch(launch.SpellID))
                    {
                        // chain is end //
                        return;
                    }
                }
                SpellTemplate spell = sender.LauncherOwner.Cartridge.GetSpell(launch.SpellID);
                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(sender.LauncherOwner, ref spell))
                {
                    float direction = sender.Direction;
                    if (targetPos != null)
                    {
                        direction = MathVector.getDegree(startPos.X, startPos.Y, targetPos.Value.X, targetPos.Value.Y);
                    }

                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                float startAngle = direction - launch.Angle / 2f + launch.StartAngle;
                                float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    float d = launch.AdjustRandomAngle(random);
                                    AddSpell(new TAddSpell()
                                    {
                                        template = spell,
                                        launch = launch,
                                        sender = sender,
                                        launcher = sender.LauncherOwner,
                                        target_obj_id = targetUnitID,
                                        targetPos = targetPos,
                                        startPos = startPos,
                                        direction = startAngle + interAngle * i + d,
                                        chain = chain,
                                        FromSkillTemplateID = sender.FromSkillTemplateID,
                                        From = sender,
                                    });
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
                                    AddSpell(new TAddSpell()
                                    {
                                        template = spell,
                                        launch = launch,
                                        sender = sender,
                                        launcher = sender.LauncherOwner,
                                        target_obj_id = targetUnitID,
                                        targetPos = targetPos,
                                        startPos = startPos,
                                        direction = startAngle + interAngle * i + d,
                                        chain = chain,
                                        FromSkillTemplateID = sender.FromSkillTemplateID,
                                        From = sender,
                                    });
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            {
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    float r = (float)(random.NextFloat() * sender.BodyBlockSize);
                                    float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    float x = (float)(startPos.X + Math.Cos(a) * r);
                                    float y = (float)(startPos.Y + Math.Sin(a) * r);
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    AddSpell(new TAddSpell()
                                    {
                                        template = spell,
                                        launch = launch,
                                        sender = sender,
                                        launcher = sender.LauncherOwner,
                                        target_obj_id = targetUnitID,
                                        targetPos = targetPos,
                                        startPos = new Geometry.Vector3(x, y, startPos.Z),
                                        direction = d + launch.AdjustRandomAngle(random),
                                        chain = chain,
                                        FromSkillTemplateID = sender.FromSkillTemplateID,
                                        From = sender,
                                    });
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                AddSpell(new TAddSpell()
                                {
                                    template = spell,
                                    launch = launch,
                                    sender = sender,
                                    launcher = sender.LauncherOwner,
                                    target_obj_id = targetUnitID,
                                    targetPos = targetPos,
                                    startPos = startPos,
                                    direction = direction + launch.StartAngle + launch.AdjustRandomAngle(random),
                                    chain = chain,
                                    FromSkillTemplateID = sender.FromSkillTemplateID,
                                    From = sender,
                                });
                            }
                            break;
                    }

                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        SpellLaunchSpell(sender, subSpell, startPos, targetUnitID, targetPos);
                    }
                }
            }
        }

        public virtual void AttackLaunchSpell(
            InstanceUnit attacker,
            InstanceUnit damage,
            in TAttackSource source,
            LaunchSpell launch)
        {
            //             LaunchSpell launch = source.Attack.Spell;
            if (launch == null)
                return;

            int? fromSkillTemplateID = 0;
            if (source.FromSkill != null)
                fromSkillTemplateID = source.FromSkill.ID;

            InstanceZoneObject sender = attacker;
            if (source.FromSpellUnit != null)
            {
                sender = source.FromSpellUnit;
                fromSkillTemplateID = source.FromSpellUnit.FromSkillTemplateID;
            }
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpllSenderUnit.Launcher:
                    sender = attacker;
                    break;
                case LaunchSpell.LaunchSpllSenderUnit.Target:
                    sender = damage;
                    break;
            }
            if (CUtils.RandomPercent(RandomN, launch.LaunchPercent))
            {
                SpellTemplate spell = attacker.Cartridge.GetSpell(launch.SpellID);

                if (spell != null && launch.Count > 0 && Formula.TryLaunchSpell(attacker, ref spell))
                {
                    var startPos = damage.Position;
                    SpellChainLevelInfo chain = null;
                    if (source.FromSpellUnit != null && source.FromSpellUnit.ChainInfo != null)
                    {
                        chain = source.FromSpellUnit.ChainInfo;
                        if (!chain.TryLaunch(launch.SpellID))
                        {
                            // chain is end //
                            return;
                        }
                    }
                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                float startAngle = sender.Direction - launch.Angle / 2f + launch.StartAngle;
                                float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    var evt = new TAddSpell();
                                    {
                                        evt.template = spell;
                                        evt.launch = launch;
                                        evt.sender = sender;
                                        evt.launcher = attacker;
                                        evt.target_obj_id = damage.ID;
                                        evt.targetPos = null;
                                        evt.startPos = startPos;
                                        evt.direction = startAngle + interAngle * i + launch.AdjustRandomAngle(random);
                                        evt.chain = chain;
                                        evt.FromSkillTemplateID = fromSkillTemplateID;
                                        evt.From = source;
                                        evt.damage = damage;
                                    }
                                    AddSpell(evt);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_CYCLE:
                            {
                                float startAngle = sender.Direction + launch.StartAngle;
                                float interAngle = CMath.PI_MUL_2 / launch.Count;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    var evt = new TAddSpell();
                                    {
                                        evt.template = spell;
                                        evt.launch = launch;
                                        evt.sender = sender;
                                        evt.launcher = attacker;
                                        evt.target_obj_id = damage.ID;
                                        evt.targetPos = null;
                                        evt.startPos = startPos;
                                        evt.direction = startAngle + interAngle * i + launch.AdjustRandomAngle(random);
                                        evt.chain = chain;
                                        evt.FromSkillTemplateID = fromSkillTemplateID;
                                        evt.From = source;
                                        evt.damage = damage;
                                    }
                                    AddSpell(evt);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            if (source.FromSpellUnit != null)
                            {
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    float r = (float)(random.NextFloat() * source.FromSpellUnit.BodyBlockSize);
                                    float a = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    float x = (float)(startPos.X + Math.Cos(a) * r);
                                    float y = (float)(startPos.Y + Math.Sin(a) * r);
                                    float d = (float)(random.NextFloat() * CMath.PI_MUL_2);
                                    var evt = new TAddSpell();
                                    {
                                        evt.template = spell;
                                        evt.launch = launch;
                                        evt.sender = sender;
                                        evt.launcher = attacker;
                                        evt.target_obj_id = damage.ID;
                                        evt.targetPos = null;
                                        evt.startPos = new Geometry.Vector3(x, y, startPos.Z);
                                        evt.direction = d + launch.AdjustRandomAngle(random);
                                        evt.chain = chain;
                                        evt.FromSkillTemplateID = fromSkillTemplateID;
                                        evt.From = source;
                                        evt.damage = damage;
                                    }
                                    AddSpell(evt);
                                }
                            }
                            else
                            {
                                log.Error(string.Format("Can not launch [POS_TYPE_RANDOM_FOR_SPELL] spell from Unit Attack : {0} {1}", spell, source));
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                var evt = new TAddSpell();
                                {
                                    evt.template = spell;
                                    evt.launch = launch;
                                    evt.sender = sender;
                                    evt.launcher = attacker;
                                    evt.target_obj_id = damage.ID;
                                    evt.targetPos = null;
                                    evt.startPos = startPos;
                                    evt.direction = sender.Direction + launch.StartAngle + launch.AdjustRandomAngle(random);
                                    evt.chain = chain;
                                    evt.FromSkillTemplateID = fromSkillTemplateID;
                                    evt.From = source;
                                    evt.damage = damage;
                                }
                                AddSpell(evt);
                            }
                            break;
                    }

                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        AttackLaunchSpell(attacker, damage, in source, subSpell);
                    }
                }
            }

        }


        public virtual void BuffLaunchSpell(
            InstanceUnit launcher,
            InstanceUnit owner,
            InstanceUnit.EquipBuff buff,
            LaunchSpell launch,
            uint targetUnitID = 0,
            Geometry.Vector3? targetPos = null)
        {
            InstanceUnit sender = launcher;
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpllSenderUnit.Target:
                    sender = owner;
                    break;
                case LaunchSpell.LaunchSpllSenderUnit.Launcher:
                    sender = launcher;
                    break;
                case LaunchSpell.LaunchSpllSenderUnit.Sender:
                    //sender = sender;
                    break;
            }
            UnitLaunchSpell(launcher, sender, launch, buff, owner.Position, null, targetUnitID, targetPos);
        }

    }

}

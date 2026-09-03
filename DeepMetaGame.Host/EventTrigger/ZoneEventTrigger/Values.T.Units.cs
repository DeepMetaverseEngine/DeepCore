using DeepCore.GameData.EventTrigger;
using DeepCore.Game3D.Host.Instance;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepCore.Reflection;
using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor;
using System.Xml.Linq;
using System.Security.Cryptography;
using DeepCore.Geometry;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

    [Desc("场景-单位")]
    public abstract class UnitValue : ZoneAbstractValue<InstanceUnit>
    {
        [Desc("值 - 没有单位", "[游戏]/值")]
        public class NA : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("没有单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return null;
            }
        }
        [Desc("返回值", "[游戏]/值")]
        public class ReturnVALUE : UnitValue
        {
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args) => args.ReturnValue as InstanceUnit;
        }

        [Desc("（废弃）遍历迭代中的 - 单位", "[游戏]/循环迭代（废弃）")]
        public class PickedUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("遍历迭代中的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.IteratingObject as InstanceUnit;
            }
        }
        [Desc("遍历迭代中的 - 单位", "[游戏]/循环迭代")]
        public class PickingIteratingUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("遍历迭代中的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.IteratingObject as InstanceUnit;
            }
        }


        [Desc("编辑器 - 单位", "[游戏]/编辑器")]
        public class Editor : UnitValue
        {
            [Desc("场景中的名字")]
            [SceneObjectIDAttribute(typeof(UnitData))]
            public string EditorName;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位:<c color='" + sw.COLOR_CONST + "'>{0}</c>", EditorName);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetUnitByName(EditorName) as InstanceUnit;
            }
        }

        [Desc("功能 - 触发的单位", "[游戏]/功能")]
        public class Trigging : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingUnit;
            }
        }
        [Desc("功能 - 触发的对手单位", "[游戏]/功能")]
        public class TriggingTarget : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的对手单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingCounterPart;
            }
        }

        [Desc("指定ObjectID的单位", "[游戏]/功能")]
        public class UnitByObjID : UnitValue
        {

            [Desc("ObjectID")]
            public AbstractValue<double> ObjectID = new ZoneIntegerValue.VALUE(0);

            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位{0}", ObjectID);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetUnit((uint)ObjectID.GetValueAs(api, args));
            }
        }
        [Desc("功能 - 触发的BUFF施放者", "[游戏]/功能")]
        public class BuffSender : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("触发的BUFF施放者");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingBuffSender;
            }
        }

        [Desc("功能 - 触发的光环持有者", "[游戏]/功能")]
        public class TriggingAuraOwner : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("触发的光环持有者");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return args.TriggingAuraOwner;
            }
        }

        [Desc("功能 - 最后添加的单位", "[游戏]/功能")]
        public class LastAdded : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后添加的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastAddedUnit;
            }
        }
        [Desc("功能 - 最后的召唤者", "[游戏]/功能")]
        public class LastSummoner : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后的召唤者");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastSummoner;
            }
        }
        [Desc("功能 - 最后添加的玩家", "[游戏]/功能")]
        public class LastAddedPlayer : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后添加的玩家");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastAddedPlayer;
            }
        }
        [Desc("功能 - 最后激活的单位", "[游戏]/功能")]
        public class LastActivated : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后激活的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastActivatedUnit;
            }
        }
        [Desc("功能 - 最后复活的单位", "[游戏]/功能")]
        public class LastRebirth : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后复活的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastRebirthUnit;
            }
        }

        [Desc("功能 - 最后发动攻击的单位", "[游戏]/功能")]
        public class LastAttack : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后发动攻击的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastAttackUnit;
            }
        }
        [Desc("功能 - 最后被打的单位", "[游戏]/功能")]
        public class LastHitted : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后被打的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastHittedUnit;
            }
        }
        [Desc("功能 - 最后被杀死的单位", "[游戏]/功能")]
        public class LastKilled : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("最后被杀死的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastKilledUnit;
            }
        }

        [Desc("场景 - 最近的单位", "[游戏]/场景")]
        public class NearUnit : UnitValue
        {
            [Desc("参照位置")]
            public AbstractValue<Vector3?> SrcPosition = new PositionValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("离{0}最近的单位", SrcPosition);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var src = SrcPosition.GetValueAs(api, args);
                if (src == null) return null;
                return api.ZoneAPI.SelectNearUnit<InstanceUnit>(src.Value, static ( unit) =>
                {
                    return true;
                });
            }
        }

        [Desc("场景 - 随机单位", "[游戏]/场景")]
        public class RandomUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("随机单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.SelectRandomUnit<InstanceUnit>((InstanceUnit unit) =>
                {
                    return true;
                });
            }
        }

        [Desc("场景 - 随机阵营单位", "[游戏]/场景")]
        public class RandomForceUnit : UnitValue
        {
            [Desc("阵营")]
            public AbstractValue<double> Force = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("阵营{0}随机单位", Force);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                int force = (int)Force.GetValueAs(api, args);
                return api.ZoneAPI.SelectRandomUnit<InstanceUnit>((InstanceUnit unit) =>
                {
                    return unit.Force == force;
                });
            }
        }


        [Desc("场景 - 随机玩家", "[游戏]/场景")]
        public class RandomPlayer : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.Append("随机玩家");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.SelectRandomPlayer((InstancePlayer p) =>
                {
                    return true;
                });
            }
        }

        [Desc("场景 - 随机阵营玩家", "[游戏]/场景")]
        public class RandomForcePlayer : UnitValue
        {
            [Desc("阵营")]
            public AbstractValue<double> Force = new ZoneIntegerValue.VALUE(0);
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("阵营{0}随机玩家", Force);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                int force = (int)Force.GetValueAs(api, args);
                return api.ZoneAPI.SelectRandomPlayer<InstancePlayer>((InstancePlayer p) =>
                {
                    return p.Force == force;
                });
            }
        }


        [Desc("检取物品中的单位", "[游戏]/功能")]
        public class LastPickingItemUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("检取物品中的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastPickingItemUnit;
            }
        }

        [Desc("最后被点选的单位", "[游戏]/功能")]
        public class LastPickableUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后被点选的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastPickableUnit;
            }
        }


        [Desc("最后释放的技能的单位", "[游戏]/功能")]
        public class LastLaunchSkillUnit : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后释放的技能的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastLaunchSkillUnit;
            }
        }
        [Desc("最后释放的技能的单位的目标", "[游戏]/功能")]
        public class LastLaunchSkillTarget : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("最后释放的技能的单位的目标");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.LastLaunchSkillUnit?.LastLaunchSkillTargetUnit;
            }
        }

        [Desc("单位的目标", "[游戏]/功能")]
        public class UnitTarget : UnitValue
        {
            public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})的目标", Owner);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var owner = Owner?.GetValueAs(api, args);
                if (owner != null)
                {
                    return api.ZoneAPI.GetObject<InstanceUnit>(owner.CurrentTargetID);
                }
                return null;
            }
        }

        [Desc("单位的召唤者", "[游戏]/功能")]
        public class SummonerUnit : UnitValue
        {
            public AbstractValue<InstanceUnit> Owner = new UnitValue.Trigging();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("({0})的召唤者", Owner);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var owner = Owner?.GetValueAs(api, args);
                if (owner != null)
                {
                    return owner.Summoner;
                }
                return null;
            }
        }


        [Desc("指定名字的单位", "[游戏]/功能")]
        public class NamedUnit : UnitValue
        {
            [Desc("名字")]
            public AbstractValue<string> Name = new StringValue.VALUE();
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("名字为\"{0}\"的单位", Name);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetUnitByName(Name.GetValueAs(api, args));
            }
        }

        [Desc("指定UUID的玩家单位", "[游戏]/功能")]
        public class UnitWithPlayerUUID : UnitValue
        {
            [Desc("名字")]
            public AbstractValue<string> UUID = new StringValue.VALUE("ACTOR");
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("名字为\"{0}\"的单位", UUID);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetPlayerByUUID(UUID.GetValueAs(api, args));
            }
        }

        [Desc("ACTOR", "[游戏]/功能 - 测试")]
        public class TestACTOR : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("ACTOR");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.ZoneAPI.GetPlayerByUUID("ACTOR");
            }
        }


        [Desc("绑定的单位", "[游戏]/[单位触发器]")]
        public class UnitAPI : UnitValue
        {
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("单位触发器绑定的单位");
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                return api.UnitAPI;
            }
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    //     [Desc("基础-单位数组")]
    //     public abstract class UnitArrayValue : ZoneAbstractArrayValue<InstanceUnit>
    //     {
    //         [Desc("单位数组", "值")] public class VALUE : ArrayValue<AbstractValue<InstanceUnit>, InstanceUnit> { }
    //         [Desc("单位数组索引", "数组")] public class INDEX : ArrayIndexValue<InstanceUnit> { }
    //         [Desc("单位数组随机", "数组")] public class RANDOM : ArrayRandomValue<InstanceUnit> { }
    //         [Desc("迭代中的单位", "数组")] public class ITERATOR : ArrayIteratingValue<InstanceUnit> { }
    //     }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------



    public abstract class FindTarget : UnitValue
    {

        [Desc("范围内最近的可攻击目标", "[游戏]/单位-目标")]
        public class NearTargetInRange : FindTarget
        {
            [Desc("攻击者")]
            public AbstractValue<InstanceUnit> Origin = new UnitValue.UnitAPI();
            [Desc("范围")]
            public AbstractValue<double> Range = new ZoneRealValue.VALUE(6);
            [Desc("希望目标")]
            public SkillTemplate.CastTarget ExpectTarget = SkillTemplate.CastTarget.Enemy;
            [Desc("攻击原因")]
            public DeepMetaGame.Data.Misc.AttackReason Reason = DeepMetaGame.Data.Misc.AttackReason.Attack;
            protected override void GetText(EventStringBuilder sw)
            {
                sw.AppendFormat("{0}范围{1}内的可攻击目标", Origin, Range);
            }
            protected override InstanceUnit GetValue(IEventTriggerAdapter api, EventArguments args)
            {
                var range = (float)Range.GetValueAs(api, args);
                var origin = Origin.GetValueAs(api, args);
                if (origin == null)
                {
                    return null;
                }
                var pos = origin.Position;
                var mind = float.MaxValue;
                var min = default(InstanceUnit);
                api.ZoneAPI.ForEachObjectsInSphere<InstanceUnit>(new DeepCore.Geometry.BoundingSphere(pos, range), (u) =>
                {
                    if (u != origin && api.ZoneAPI.Formula.IsAttackable(origin, u, ExpectTarget, Reason, origin.Info))
                    {
                        if (min == null)
                        {
                            min = u;
                        }
                        else
                        {
                            var p = u.Position;
                            var d = Vector3.DistanceSquared(in pos, in p);
                            if (d < mind)
                            {
                                mind = d;
                                min = u;
                            }
                        }
                    }
                    return false;
                });
                return min;
            }

        }

    }


}

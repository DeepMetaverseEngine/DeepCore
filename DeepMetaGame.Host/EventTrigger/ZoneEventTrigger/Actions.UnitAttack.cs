using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.GameData.EventTrigger;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using static DeepCore.GUI.Cell.SpriteSet;
using static System.Net.Mime.MediaTypeNames;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    [Desc("设置自动释放技能", "[游戏]/单位/攻击")]
    public class UnitAutoLaunchSkillAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("开关")]
        public AbstractValue<bool> On = new ZoneBooleanValue.VALUE(true);
        [Desc("面朝目标")]
        public AbstractValue<bool> FaceToTarget = new ZoneBooleanValue.VALUE(true);
        [Desc("允许空放")]
        public AbstractValue<bool> LaunchAnyway = new ZoneBooleanValue.VALUE(false); 
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("{0}设置自动释放技能(开关:{1}, 面朝目标:{2} 允许空放:{3});", Unit, On, FaceToTarget, LaunchAnyway);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            if (unit != null)
            {
                if (On.GetValueAs(api, args))
                {
                    var comp = unit.Components.GetOrAddComponentAs<UnitAutoAttackComponent>();
                    comp.IsLaunchAnyway = this.LaunchAnyway.GetValueAs(api, args);
                    comp.IsFaceToTarget = this.FaceToTarget.GetValueAs(api, args);
                }
                else
                {
                    unit.Components.RemoveComponentAs<UnitAutoAttackComponent>();
                }

            }
            return null;

        }
    }

    [Desc("攻击/受击释放法术", "[游戏]/单位/攻击")]
    public class AttackLaunchSpell : ZoneAbstractAction
    {
        [Desc("攻击单位")]
        public AbstractValue<InstanceUnit> Attacker = new UnitValue.LastAttack();
        [Desc("受击单位")]
        public AbstractValue<InstanceUnit> Damage = new UnitValue.LastHitted();
        [Desc("释放法术")]
        public LaunchSpell LaunchSpell; 
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("发生攻击判定时：{0}对{1}释放法术{2};", Attacker, Damage, LaunchSpell);
        }
        protected override object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var attacker = Attacker.GetValueAs(api, args);
            var damage = Damage.GetValueAs(api, args);
            if (args.TriggingAttack!=null && attacker != null && damage != null)
            {
                var attack = args.TriggingAttack;
                api.ZoneAPI.AttackLaunchSpell(attacker, damage, attack, LaunchSpell);
            }
            return null;
        }
    }
}


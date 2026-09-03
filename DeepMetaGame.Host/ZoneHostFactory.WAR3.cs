using System;
using System.Security.Cryptography;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.IO;
using DeepMetaGame.Data;
using static DeepMetaGame.Data.War3DataFactory;

namespace DeepCore.Game3D.Host.War3
{

    public class War3ZoneHostFactory : ZoneHostFactory
    {
        public override InstanceZoneFormula CreateFormula(InstanceZone zone) { return WarZoneFormula.Alloc<WarZoneFormula>(zone); }
        public override InstanceUnitFormula CreateFormula(InstanceUnit unit) { return War3UnitFormula.Alloc<War3UnitFormula>(unit); }
        public override InstanceUnit CreateUnit(InstanceZone zone, TAddUnit add)
        {
            add.info = zone.CloneData(add.info);
            return base.CreateUnit(zone, add);
        }
        public class AttackDefenseTable
        {
            public float[,] table = new float[10, 10];
            public AttackDefenseTable()
            {
                CUtils.ForEach2D(10, 10, (a, d) => table[a, d] = 1f);

                this[AttackType.Normal, DefenseType.Ethereal] = 0;

                this[AttackType.Melee, DefenseType.Medium] = 1.50f;
                this[AttackType.Melee, DefenseType.Fortified] = 0.7f;
                this[AttackType.Melee, DefenseType.Divine] = 0.05f;
                this[AttackType.Melee, DefenseType.Ethereal] = 0;

                this[AttackType.Pierce, DefenseType.Light] = 2.0f;
                this[AttackType.Pierce, DefenseType.Medium] = 0.75f;
                this[AttackType.Pierce, DefenseType.None] = 1.5f;
                this[AttackType.Pierce, DefenseType.Fortified] = 0.35f;
                this[AttackType.Pierce, DefenseType.Hero] = 0.5f;
                this[AttackType.Pierce, DefenseType.Divine] = 0.05f;
                this[AttackType.Pierce, DefenseType.Ethereal] = 0;

                this[AttackType.Hero, DefenseType.Fortified] = 0.5f;
                this[AttackType.Hero, DefenseType.Divine] = 0.05f;
                this[AttackType.Hero, DefenseType.Ethereal] = 0;


                this[AttackType.Siege, DefenseType.Medium] = 0.5f;
                this[AttackType.Siege, DefenseType.None] = 1.5f;
                this[AttackType.Siege, DefenseType.Fortified] = 1.5f;
                this[AttackType.Pierce, DefenseType.Hero] = 0.5f;
                this[AttackType.Pierce, DefenseType.Divine] = 0.05f;
                this[AttackType.Siege, DefenseType.Ethereal] = 0;

                this[AttackType.Chaos, DefenseType.Ethereal] = 0;

                this[AttackType.Magic, DefenseType.Light] = 1.25f;
                this[AttackType.Magic, DefenseType.Medium] = 0.75f;
                this[AttackType.Magic, DefenseType.Large] = 2.00f;
                this[AttackType.Magic, DefenseType.Fortified] = 0.35f;
                this[AttackType.Magic, DefenseType.Hero] = 0.5f;
                this[AttackType.Magic, DefenseType.Divine] = 0.05f;
                this[AttackType.Magic, DefenseType.Ethereal] = 1.66f;
                this[AttackType.Magic, DefenseType.MagicImmune] = 0;


                this[AttackType.Spell, DefenseType.Hero] = 0.7f;
                this[AttackType.Spell, DefenseType.Divine] = 0.05f;
                this[AttackType.Spell, DefenseType.Ethereal] = 1.66f;
                this[AttackType.Spell, DefenseType.MagicImmune] = 0;

                this[AttackType.Heal, DefenseType.Ethereal] = 1.66f;

            }
            public float this[AttackType a, DefenseType d]
            {
                get
                {
                    return table[(int)a, (int)d];
                }
                private set
                {
                    table[(int)a, (int)d] = value;
                }
            }
        }
        public static AttackDefenseTable ADTable = new AttackDefenseTable();


        public class War3UnitFormula : InstanceUnitFormula
        {
            public float Armor;
            public float Attack;
            public War3DataFactory.War3UnitProperties War3 { get; private set; }
            public override InstanceUnitFormula Init(InstanceUnit owner)
            {
                War3 = owner.Properties as War3DataFactory.War3UnitProperties;
                return base.Init(owner);
            }
            protected override void Disposing()
            {
                Armor = 0;
                Attack = 0;
                base.Disposing();
            }
            // 1点力量 = 25点生命
            // 总生命值 = 100 + 25 * 力量 + 物品加成
            // 1点力量 = 0.05生命恢复 / 秒
            // 生命恢复速度 = 基础值 + 0.05 * 力量 + 物品加成 * 技能加成
            // 基础值：
            // 暗夜精灵族英雄：0.5（只在晚上）
            // 不死族英雄：2.0（只在荒芜上）
            // 其他英雄：0.25
            // 
            // 智力属性
            // 智力涉及到英雄控制魔法力的能力。
            // 
            // 1点智力 = 15魔法
            // 魔法值 = 15 * 智力 + 物品加成
            // 1点智力 = 0.05魔法恢复 / 秒
            // 魔法恢复速度 = 0.01 + 0.05 * 智力 + 技能加成 + 物品加成 * （0.01 + 0.05 * 智力）
            // 每个英雄的数据表里的魔法恢复速度为基础魔法恢复速度，即为0.01
            // 
            // 敏捷
            // 敏捷决定了英雄护甲值和攻击速度。
            // 
            // 1点敏捷 = 0.3点护甲
            // 总护甲 = -2 + 0.3 * 敏捷 + 0.3 * 敏捷加成 + 物品加成 + 技能加成
            // 注意：英雄护甲公式对于英雄的初始护甲，稍有偏差。
            // 1点敏捷 = 减少 2 % 攻击间隔
            // 攻击间隔 = 基础攻击间隔 / （1 + 0.02 * 敏捷）+物品加成 + 技能加成
            // 每个英雄的数据表里的攻击间隔为基础攻击间隔
            // 
            // 对战时，一般打野怪出现什么属性的书对应什么属性的英雄就吃什么书，但是力量型英雄吃敏捷可以加护甲和攻击速度,敏捷型英雄吃力量书可以提高生命上限和回血速度。
            // 
            // 这就要看自己的需要的。
            protected internal override void OnInit()
            {
                base.OnInit();
                this.Owner.Level = War3.LEVEL;
                Owner.MaxHP = (int)Math.Ceiling(Owner.Info.HealthPoint + War3.STR * 25);
                Owner.MaxMP = (int)Math.Ceiling(Owner.Info.ManaPoint + War3.INT * 15);
                if (Owner.ARecover)
                {
                    Owner.ARecover.HealthRecoveryPoint = (int)Math.Ceiling(1 + 0.05f * War3.STR);
                    Owner.ARecover.ManaRecoveryPoint = (int)Math.Ceiling(0.01 + 0.05f * War3.INT);
                }
                Owner.MulFastActionRate(1f + (War3.AGI * 0.02f));
                Armor = War3.DEF + (-2 + 0.3f * War3.AGI);
                Attack = War3.ATK;
            }

            public float MainPropValue
            {
                get
                {
                    switch (War3.H_TYPE)
                    {
                        case HeroAttribute.Agility: return War3.AGI;
                        case HeroAttribute.Strength: return War3.STR;
                        case HeroAttribute.Intelligence: return War3.INT;
                        default: return 0f;
                    }
                }
            }
        }

        public class WarZoneFormula : InstanceZoneFormula
        {
            public override long OnHit(InstanceUnit attacker, TAttackSource attack, ref TAttackResult result, InstanceUnit targget)
            {
                var cfg = attacker.Templates.DefaultExtConfig as War3CFG;
                var sa = attacker.Formula as War3UnitFormula;
                var da = targget.Formula as War3UnitFormula;
                var aa = attack.Attack.Properties as War3AttackProperties;

                // 最低攻击力：基本伤害 + 主要属性 * 1 + 骰子数
                // 最高攻击力：基本伤害 + 主要属性 * 1 + 骰子数 * 骰子面数
                // 
                // 攻击力期望值：1 / 2 * (最低攻击力 + 最低攻击力) = 基本伤害 + 主要属性 * 1 + 骰子数 * 1 / 2 * (1 + 骰子面数)
                // 附加攻击力：(攻击力期望值 + ∑主要属性 * 1 ) *( ∑技能或物品加成 % ) + ∑主要属性 * 1 + ∑技能或物品加成
                // 
                // 攻击力为整数(小数点四舍五入，但各技能的效果至少会加1或减1)，因此有时会与计算结果不同。
                // 攻击力期望值：(攻击力期望值 + 主要属性 * 1) * (1 + ∑技能或物品加成 % ) + ∑技能或物品加成
                // 
                // 随机取值(EX: 244 + 1d11)：244 + 1颗11面骰掷出的点数和
                // 随机取值(EX: 243 + 2d6)：243 + 2颗6面骰掷出的点数和
                var level = attacker.Level + 1;
                var mainProp = sa.MainPropValue;
                //                 var minAtk = sa.Attack + mainProp + attacker.Level;
                //                 var maxAtk = sa.Attack + mainProp + attacker.RandomN.Next(attacker.Level, attacker.Level * 6);
                var addAtk = attack.Attack.Attack;
                var rand = attacker.RandomN.Next(level, level * 6);
                var expAtk = (sa.Attack) + mainProp * cfg.HERO_MAIN_PROP_ATK_RATE + (level * 1f / 2f * (1f + rand)) + addAtk;

                //(输出伤害(物理) - 硬化外皮) * 伤害倍率(狂暴) % *(1 - Max抗性 %) * 装甲相克 * 防御力减成 * 幻影受伤倍率 %
                //Armor > 0：1/ ( 1 + Armor * 0.06 ) ≈ 1- 伤害减成%(游戏显示)
                //Armor < 0：2 - (1 - 0.06)[-Armor] ≈ 1 - 伤害减成 % (游戏显示)
                //Armor下限 - 20\
                var armor = Math.Max(-20, da.Armor);
                var defRat = (1f / (1f + armor * 0.06));

                var finalAtk = expAtk * defRat;

                var atkType = aa.ATK_TYPE == AttackType.Undefined ? sa.War3.ATK_TYPE : aa.ATK_TYPE;
                var defType = da.War3.DEF_TYPE;
                if (atkType != AttackType.Undefined && defType != DefenseType.Undefined)
                {
                    var rate = ADTable[atkType, defType];
                    finalAtk *= rate;
                }

                return (long)(finalAtk);
            }
        }
    }



}

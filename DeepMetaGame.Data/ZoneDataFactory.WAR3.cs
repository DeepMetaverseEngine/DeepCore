using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;

namespace DeepMetaGame.Data
{
    public class War3DataFactory : ZoneDataFactory
    {
        public const int MSG_START = 0x997000;
        public War3DataFactory()
        {
            base.RegistPropertiesTypes(GetType().GetNestedTypes());
        }
        //--------------------------------------------------------------------------------
        // protected override ZoneFuncDataAdapter InitLuaFactory() => null;
        public override ICommonConfig CreateCommonCFG() => new War3CFG();
        //--------------------------------------------------------------------------------
        [MessageType(MSG_START + 0x01)]
        public class War3CFG : IBaseFuncData, ICommonConfig
        {
            [Desc("英雄主属性攻击加成", "战斗")]
            public float HERO_MAIN_PROP_ATK_RATE = 1f;
        }
        [MessageType(MSG_START + 0xF100), Desc("War3单位类型")]
        public class War3UnitProperties : IBaseFuncData, IUnitProperties
        {
            [Desc("初始等级")] public int LEVEL = 0;


            [Desc("力量")] public float STR = 22;
            [Desc("敏捷")] public float AGI = 13;
            [Desc("智力")] public float INT = 17;

            [Desc("基础攻击")] public float ATK = 12;
            [Desc("基础护甲")] public float DEF = 4;

            [Desc("攻击类型")] public AttackType ATK_TYPE = AttackType.Melee;
            [Desc("防御类型")] public DefenseType DEF_TYPE = DefenseType.Light;
            [Desc("英雄类型")] public HeroAttribute H_TYPE = HeroAttribute.Strength;
        }
        [MessageType(MSG_START + 0xF101), Desc("War3攻击扩展")]
        public class War3AttackProperties : IBaseFuncData, IAttackProperties
        {
            [Desc("攻击类型")] public AttackType ATK_TYPE = AttackType.Undefined;
        }
        [MessageType(MSG_START + 0xF108), Desc("War3特效扩展")] public class War3EffectProperties : IBaseFuncData, IEffectProperties { }
        [MessageType(MSG_START + 0xF102), Desc("War3 Buff扩展")] public class War3BuffProperties : IBaseFuncData, IBuffProperties { }
        [MessageType(MSG_START + 0xF103), Desc("War3物品扩展")] public class War3ItemProperties : IBaseFuncData, IItemProperties { }
        [MessageType(MSG_START + 0xF104), Desc("War3技能扩展")] public class War3SkillProperties : IBaseFuncData, ISkillProperties { }
        [MessageType(MSG_START + 0xF105), Desc("War3法术扩展")] public class War3SpellProperties : IBaseFuncData, ISpellProperties { }
        [MessageType(MSG_START + 0xF107), Desc("War3光环扩展")] public class War3AuraProperties : IBaseFuncData, IAuraProperties { }
        [MessageType(MSG_START + 0xF109), Desc("War3词缀扩展")] public class War3CardProperties : IBaseFuncData, ICardProperties { }
        [MessageType(MSG_START + 0xF106), Desc("War3场景扩展")] public class War3SceneProperties : IBaseFuncData, ISceneProperties { }
        [MessageType(MSG_START + 0xF10A), Desc("War3KF扩展")] public class War3KeyFrameProperties : IBaseFuncData, IKeyFrameProperties { }

        //--------------------------------------------------------------------------------
        #region Enums

        public enum ArmorType
        {
            Undefined = -1,
            Flesh = 1,
            Metal = 2,
            Wood = 3,
            Ethereal = 4,
            Stone = 5,
        }
        public enum AttackType
        {
            Undefined = -1,
            Normal = 0,
            Melee = 1,
            Pierce = 2,
            Siege = 3,
            Magic = 4,
            Chaos = 5,
            Hero = 6,
            Spell = 7,
            Heal = 8,
        }
        public enum DefenseType
        {
            Undefined = -1,
            Light = 0,
            Medium = 1,
            Large = 2,
            Fortified = 3,
            Normal = 4,
            Hero = 5,
            Divine = 6,
            None = 7,
            Ethereal = 8,
            MagicImmune = 9,
        }
        public enum WeaponType
        {
            Undefined = 0,

            MetalLightChop = 1,
            MetalMediumChop = 2,
            MetalHeavyChop = 3,
            MetalLightSlice = 4,
            MetalMediumSlice = 5,
            MetalHeavySlice = 6,
            MetalMediumBash = 7,
            MetalHeavyBash = 8,
            MetalMediumStab = 9,
            MetalHeavyStab = 10,

            WoodLightSlice = 11,
            WoodMediumSlice = 12,
            WoodHeavySlice = 13,
            WoodLightBash = 14,
            WoodMediumBash = 15,
            WoodHeavyBash = 16,
            WoodLightStab = 17,
            WoodMediumStab = 18,

            ClawLightSlice = 19,
            ClawMediumSlice = 20,
            ClawHeavySlice = 21,

            AxeMediumChop = 22,

            RockHeavyBash = 23,
        }

        public enum TargetFlag
        {
            None = 1 << 0,
            Ground = 1 << 1,
            Air = 1 << 2,
            Structure = 1 << 3,
            Ward = 1 << 4,
            Item = 1 << 5,
            Tree = 1 << 6,
            Wall = 1 << 7,
            Debris = 1 << 8,
            Decoration = 1 << 9,
            Bridge = 1 << 10,
        }
        public enum HeroAttribute
        {
            Strength = 1,
            Intelligence = 2,
            Agility = 3,
            NA = 0,
        }
        public enum DamageType
        {
            Unknown = 0,
            Normal = 4,
            Enhanced = 5,
            Fire = 8,
            Cold = 9,
            Lightning = 10,
            Poison = 11,
            Disease = 12,
            Divine = 13,
            Magic = 14,
            Sonic = 15,
            Acid = 16,
            Force = 17,
            Death = 18,
            Mind = 19,
            Plant = 20,
            Defensive = 21,
            Demolition = 22,
            SlowPoison = 23,
            SpiritLink = 24,
            ShadowStrike = 25,
            Universal = 26,
        }
        public enum UnitCategory
        {

            Giant = 1 << 0,
            Undead = 1 << 1,
            Summoned = 1 << 2,
            Mechanical = 1 << 3,
            Peon = 1 << 4,
            Sapper = 1 << 5,
            Townhall = 1 << 6,
            Ancient = 1 << 7,
            Neutral = 1 << 8,
            Ward = 1 << 9,
            StandOn = 1 << 10,
            Tauren = 1 << 11,
        }
        public enum UnitType
        {
            Hero = 0,
            Dead = 1,
            Structure = 2,

            Flying = 3,
            Ground = 4,

            AttacksFlying = 5,
            AttacksGround = 6,

            MeleeAttacker = 7,
            RangedAttacker = 8,

            Giant = 9,
            Summoned = 10,
            Stunned = 11,
            Plagued = 12,
            Snared = 13,

            Undead = 14,
            Mechanical = 15,
            Peon = 16,
            Sapper = 17,
            Townhall = 18,
            Ancient = 19,

            Tauren = 20,
            Poisoned = 21,
            Polymorphed = 22,
            Sleeping = 23,
            Resistant = 24,
            Ethereal = 25,
            MagicImmune = 26,
        }
        #endregion
        //--------------------------------------------------------------------------------
    }


    //----------------------------------------------------------------------------------------------

}

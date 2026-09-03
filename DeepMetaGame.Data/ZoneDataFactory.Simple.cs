using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using System;

namespace DeepMetaGame.Data
{
    public class SimpleDataFactory : ZoneDataFactory
    {
        public const int SIMPLE_MSG_START = 0x999000;
        public SimpleDataFactory()
        {
            base.RegistPropertiesTypes(GetType().GetNestedTypes());
        }
        //--------------------------------------------------------------------------------
        //         protected override ZoneFuncDataAdapter InitLuaFactory()
        //         {
        //             return null;
        //         }
//         protected override IPropertiesData CreateProperties(IPropertiesOwner owner)
//         {
//             if (owner is SceneData scene) return new SimpleSceneProperties();
//             if (owner is UnitInfo unit) return new SimpleUnitProperties();
//             if (owner is ItemTemplate item) return new SimpleItemProperties();
//             if (owner is SkillTemplate skill) return new SimpleSkillProperties();
//             if (owner is SpellTemplate spell) return new SimpleSpellProperties();
//             if (owner is BuffTemplate buff) return new SimpleBuffProperties();
//             if (owner is AuraTemplate aura) return new SimpleAuraProperties();
//             if (owner is CardTemplate card) return new SimpleCardProperties();
//             if (owner is AttackProp attack) return new SimpleAttackProperties();
//             if (owner is LaunchEffect effect) return new SimpleEffectProperties();
//             if (owner is BaseKeyFrame kf) return new SimpleKeyFrameProperties(); 
//             //throw new NotImplementedException();
//             return null;
//         }
        //         public override IItemProperties CreateItemProperties()
        //         {
        //             return new SimpleItemProperties();
        //         }
        //         public override IUnitProperties CreateUnitProperties()
        //         {
        //             return new SimpleUnitProperties();
        //         }
        //         public override IAttackProperties CreateAttackProperties()
        //         {
        //             return new SimpleAttackProperties();
        //         }
        //         public override IEffectProperties CreateEffectProperties()
        //         {
        //             return new SimpleEffectProperties();
        //         }
        //         public override IBuffProperties CreateBuffProperties()
        //         {
        //             return new SimpleBuffProperties();
        //         }
        //         public override ISkillProperties CreateSkillProperties()
        //         {
        //             return new SimpleSkillProperties();
        //         }
        //         public override ISpellProperties CreateSpellProperties()
        //         {
        //             return new SimpleSpellProperties();
        //         }
        //         public override IAuraProperties CreateAuraProperties()
        //         {
        //             return new SimpleAuraProperties();
        //         }
        //         public override ISceneProperties CreateSceneProperties()
        //         {
        //             return new SimpleSceneProperties();
        //         }
        public override ICommonConfig CreateCommonCFG()
        {
            return new SimpleCFG();
        }

        //----------------------------------------------------------------------------------------------
        public class SimpleCFG : IBaseFuncData, ICommonConfig
        {
        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF100)]
        public class SimpleUnitProperties : IBaseFuncData, IUnitProperties
        {
            [Desc("力量")]
            public float STR;
            [Desc("敏捷")]
            public float AGI;
            [Desc("智力")]
            public float INT;
            public SimpleUnitProperties()
            {
            }
        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF101)]
        public class SimpleAttackProperties : IBaseFuncData, IAttackProperties
        {


        }
        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF108)]
        public class SimpleEffectProperties : IBaseFuncData, IEffectProperties
        {

        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF102)]
        public class SimpleBuffProperties : IBaseFuncData, IBuffProperties
        {

        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF103)]
        public class SimpleItemProperties : IBaseFuncData, IItemProperties
        {

        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF104)]
        public class SimpleSkillProperties : IBaseFuncData, ISkillProperties
        {

        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF105)]
        public class SimpleSpellProperties : IBaseFuncData, ISpellProperties
        {

        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF106)]
        public class SimpleSceneProperties : IBaseFuncData, ISceneProperties
        {


        }

        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF107)]
        public class SimpleAuraProperties : IBaseFuncData, IAuraProperties
        {

        }
        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF109)]
        public class SimpleCardProperties : IBaseFuncData, ICardProperties
        {

        }
        [MessageType(SimpleDataFactory.SIMPLE_MSG_START + 0xF10A)]
        public class SimpleKeyFrameProperties : IBaseFuncData, IKeyFrameProperties
        {

        }
    }




}

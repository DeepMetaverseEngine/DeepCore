using DeepCore;
using DeepCore.FuncData;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Template
{
    [MessageType(BattleConstants.CardTemplate)]
    [Desc("词缀功能")]
    public class CardTemplate : CustomEventTemplateData
    {
        //--------------------------------------------------------------------------------------------
        [Desc(Editable = false)]
        public List<CardField> Fields = new List<CardField>();
        //--------------------------------------------------------------------------------------------
        [Desc(Category = "1.词缀", Desc = "依赖的词缀")]
        public ArrayList<CardDependence> DependCards = new ArrayList<CardDependence>();
        //--------------------------------------------------------------------------------------------
        [Desc(Category = "2.弹药库", Desc = "自动学会技能")]
        public bool AutoLearnSkill = true;
        [Desc(Category = "2.弹药库", Desc = "用于编辑器数据填充")]
        public bool OnlyForSelfTemplate = false;
        //--------------------------------------------------------------------------------------------
        [Desc(Category = "9.扩展", Desc = "能力")]
        [NotNull]
        public ArrayList<ICardTemplateAbility> Abilities = new ArrayList<ICardTemplateAbility>();
        [Desc(Category = "9.扩展", Desc = "词缀用户自定义扩展属性"), Expandable, NotNull]
        public ICardProperties Properties;
        public override IPropertiesData PropertiesData => Properties;
        //--------------------------------------------------------------------------------------------
        public int LevelsCount
        {
            get
            {
                int max = 0;
                if (Fields != null && Fields.Count > 0)
                {
                    max = Math.Max(Fields[0].LevelsCount, max);
                }
                return max;
            }
        }

        public CardTemplate()
        {
            Properties = ZoneDataFactory.Factory.CreateProperties<ICardProperties>(this);
        }
        //--------------------------------------------------------------------------------------------
        [MessageType(BattleConstants.CardTemplateCardField)]
        public class CardField : ISerializable
        {
            public string ColumnName;
            [PrimitiveFuncType()]
            public Type FieldType = typeof(int);
            public string FieldDesc = string.Empty;
            public FieldOperation FieldOP = FieldOperation.SET;
            [Desc(Editable = false)]
            public CardFieldCell[] Levels;
            [Desc(Editable = false)]
            public CardReference UsedTemplates;
            public int LevelsCount { get => Levels != null ? Levels.Length : 0; }
            public bool ForEachUseTemplates<ST>(ST st, ForEachPredicate<ST, Type, int> action)
            {
                if (UsedTemplates?.References != null)
                {
                    foreach (var e in UsedTemplates.References)
                    {
                        var type = e.Key;
                        if (e.Value != null)
                        {
                            foreach (var tid in e.Value)
                            {
                                if (action(st, type, tid))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            public override string ToString()
            {
                return $"{ColumnName}";
            }
        }
        //--------------------------------------------------------------------------------------------
        [MessageType(BattleConstants.CardTemplateCardFieldCell)]
        public class CardFieldCell : ISerializable
        {
            public object FieldValue;
            public override string ToString()
            {
                return $"{FieldValue}";
            }
        }
        //--------------------------------------------------------------------------------------------
        [MessageType(BattleConstants.CardTemplateCardReference)]
        public class CardReference : ISerializable
        {
            [Desc(Editable = false)]
            public HashMap<Type, List<int>> References;

            public Tuple<Type, int> First
            {
                get
                {
                    if (References != null && References.Count > 0)
                    {
                        foreach (var used in References)
                        {
                            if (used.Value != null)
                            {
                                foreach (var tid in used.Value)
                                {
                                    return new Tuple<Type, int>(used.Key, tid);
                                }
                            }
                        }
                    }
                    return null;
                }
            }
            //      [Desc(Editable = false)]
            //      public Type OwnerTemplateType;
            //      [Desc(Editable = false)]
            //      [TemplateID(null)]
            //      public int OwnerTemplateID;
            //      public override string ToString()
            //      {
            //          return $"{OwnerTemplateID}@{OwnerTemplateType}";
            //      }
        }
        //--------------------------------------------------------------------------------------------
        [MessageType(BattleConstants.CardTemplateCardDependence)]
        public class CardDependence : ISerializable
        {
            [Desc("依赖的词缀")]
            [TemplateID(typeof(CardTemplate))]
            public int DependCardID;

            [Desc("依赖的词缀等级")]
            public int DependCardLevel;

            public override string ToString()
            {
                return $"{DependCardID}";
            }
        }
        //--------------------------------------------------------------------------------------------
    }


    public abstract class ICardTemplateAbility : IDataAbility
    {
    }

}

using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using static DeepMetaGame.Data.Misc.CardSlot;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{
    //----------------------------------------------------------------------------------------------------------------------

    [Desc("单位设置词缀", "[游戏]/词缀")]
    public class UnitPutCardAction : ZoneAbstractAction
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("词缀")]
        public CardTemplateValue Card = new CardTemplateValue.Template();
        [Desc("词缀操作")]
        public CardSlotOperation Op = CardSlotOperation.Upgrade;
        [Desc("词缀等级")]
        public AbstractValue<double> Level = new IntegerValue.VALUE(1);

        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("({0})设置词缀({1}){2};", Unit, Card, Op.ToEnumDesc(), Level);
        }
        override protected object Run(IEventTriggerAdapter api, EventArguments args)
        {
            var unit = Unit.GetValueAs(api, args);
            var card = Card.GetValueAs(api, args);
            if (unit != null && card != null)
            {
                var slog = new CardSlot()
                {
                    CardTemplateID = card.ID,
                    Op = Op,
                    Level = (int)Level.GetValueAs(api, args),
                };
                unit.Cartridge.PutCardSlot(slog);
            }
            return null;
        }
    }

    //----------------------------------------------------------------------------------------------------------------------
    #region Trigging

    [Desc("绑定单位词缀改变", "[游戏]/[词缀]")]
    public class BindingUnitCardsChanged : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var unit = api.UnitAPI;
                var handler = new InstanceUnit.CardsChangedHandler((u, h) =>
                {
                    args.TriggingUnit = u;
                    //args.TriggingCardTemplate = h.car;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardsChanged += handler,
                    static (unit, handler) => unit.OnCardsChanged -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }
    [Desc("指定单位词缀改变", "[游戏]/[词缀]")]
    public class SpecUnitCardsChanged : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api,args) is InstanceUnit unit)
            {
                var handler = new InstanceUnit.CardsChangedHandler((u, h) =>
                {
                    args.TriggingUnit = u;
                    //args.TriggingCardTemplate = h.car;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardsChanged += handler,
                    static (unit, handler) => unit.OnCardsChanged -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
    }


    [Desc("绑定单位增加词缀", "[游戏]/[词缀]")]
    public class BindingUnitAddCard : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var unit = api.UnitAPI;
                var handler = new InstanceUnit.CardAddHandler((u, h, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardAdd += handler,
                    static (unit, handler) => unit.OnCardAdd -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    }
    [Desc("指定单位增加词缀", "[游戏]/[词缀]")]
    public class SpecUnitAddCard : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api, args) is InstanceUnit unit)
            {
                var handler = new InstanceUnit.CardAddHandler((u, h, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardAdd += handler,
                    static (unit, handler) => unit.OnCardAdd -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    }


    [Desc("绑定单位移除词缀", "[游戏]/[词缀]")]
    public class BindingUnitRemoveCard : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var unit = api.UnitAPI;
                var handler = new InstanceUnit.CardRemoveHandler((u, h, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardRemove += handler,
                    static (unit, handler) => unit.OnCardRemove -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    }
    [Desc("指定单位移除词缀", "[游戏]/[词缀]")]
    public class SpecUnitRemoveCard : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api, args) is InstanceUnit unit)
            {
                var handler = new InstanceUnit.CardRemoveHandler((u, h, c) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardRemove += handler,
                    static (unit, handler) => unit.OnCardRemove -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
    }


    [Desc("绑定单位词缀等级变化", "[游戏]/[词缀]")]
    public class BindingUnitCardLevelChange : ZoneAbstractTrigger
    {
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (api.UnitAPI != null)
            {
                var unit = api.UnitAPI;
                var handler = new InstanceUnit.CardLevelChangeHandler((u, h, c, oldv) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    args.TriggingNumberValue = oldv;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardLevelChange += handler,
                    static (unit, handler) => unit.OnCardLevelChange -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
        [TriggingArg("CardLevel")] public double Level(EventArguments args) => args.TriggingNumberValue;
    }
    [Desc("指定单位词缀等级变化", "[游戏]/[词缀]")]
    public class SpecUnitCardLevelChange : ZoneAbstractTrigger
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        protected override void Listen(IEventTriggerAdapter api, EventArguments args)
        {
            if (Unit.GetValueAs(api, args) is InstanceUnit unit)
            {
                var handler = new InstanceUnit.CardLevelChangeHandler((u, h, c, oldv) =>
                {
                    args.TriggingUnit = u;
                    args.TriggingCardTemplate = c.Card;
                    args.TriggingNumberValue = oldv;
                    api.TestAndDoAction(args);
                });
                api.Listen(unit, handler,
                    static (unit, handler) => unit.OnCardLevelChange += handler,
                    static (unit, handler) => unit.OnCardLevelChange -= handler);
            }
        }
        [TriggingArg("触发的单位")] public InstanceUnit TriggingUnit(EventArguments args) => args.TriggingUnit;
        [TriggingArg("Card")] public CardTemplate Card(EventArguments args) => args.TriggingCardTemplate;
        [TriggingArg("CardLevel")] public double Level(EventArguments args) => args.TriggingNumberValue;
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------------------
    [Desc("单位是否拥有词缀", "[游戏]/[词缀]")]
    public class UnitCardExist : ZoneBooleanValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("词缀")]
        public ZoneAbstractValue<CardTemplate> CardID = new CardTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})是否拥有词缀({1})", Unit, CardID);
        }
        protected override bool GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && CardID.GetValueAs(api, args) is CardTemplate card && unit.Cartridge.TryGetCardSlot(card.ID, out var slot))
            {
                return true;
            }
            return false;
        }
    }

    [Desc("单位词缀等级", "[游戏]/[词缀]")]
    public class UnitCardLevel : ZoneIntegerValue
    {
        [Desc("单位")]
        public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
        [Desc("词缀")]
        public ZoneAbstractValue<CardTemplate> CardID = new CardTemplateValue.Template();
        protected override void GetText(EventStringBuilder sw)
        {
            sw.AppendFormat("单位({0})词缀({1})等级", Unit, CardID);
        }
        protected override double GetValue(IEventTriggerAdapter api, EventArguments args)
        {
            InstanceUnit unit = Unit.GetValueAs(api, args);
            if (unit != null && CardID.GetValueAs(api, args) is CardTemplate card && unit.Cartridge.TryGetCardSlot(card.ID, out var slot))
            {
                return slot.Level;
            }
            return -1;
        }
    }
    //----------------------------------------------------------------------------------------------------------------------
}

using DeepCore.FuncData;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.IO;
using DeepMetaGame.Data;
using DeepMetaGame.Data.FuncData;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DeepMetaGame.Data.Template.CardTemplate;

namespace DeepCore.Game3D.Host.FuncData
{
    //---------------------------------------------------------------------------------------------------
    public class UnitCartridgeMeta
    {
        public HashMap<int, UnitInfo> mUnits = new HashMap<int, UnitInfo>();
        public HashMap<int, SkillTemplate> mSkills = new HashMap<int, SkillTemplate>();
        public HashMap<int, SpellTemplate> mSpells = new HashMap<int, SpellTemplate>();
        public HashMap<int, BuffTemplate> mBuffs = new HashMap<int, BuffTemplate>();
        public HashMap<int, AuraTemplate> mAuras = new HashMap<int, AuraTemplate>();
        public HashMap<int, ItemTemplate> mItems = new HashMap<int, ItemTemplate>();
        public HashMap<int, UnitEventTemplate> mUnitEvents = new HashMap<int, UnitEventTemplate>();
        public HashMap<int, CardTemplate> mCards = new HashMap<int, CardTemplate>();
        /// <summary>
        /// 根据装备的词缀将所有模板装载进弹药库
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="templates"></param>
        /// <param name="ownerInfo"></param>
        /// <param name="ownerFuncs"></param>
        public void Update(InstanceUnit owner, ZoneCardRuntime templates, UnitInfo ownerInfo, HashMap<int, int> ownerFuncs)
        {
            using (var cartridges = owner.ObjectPool.AllocMap<Type, HashMap<int, TemplateData>>())
            {
                templates.FillTemplates(ownerInfo, ownerFuncs, cartridges);
                if (cartridges.TryGetValue(typeof(UnitInfo), out var units))
                {
                    mUnits.ConvertAddAll(units);
                }
                if (cartridges.TryGetValue(typeof(SkillTemplate), out var skills))
                {
                    mSkills.ConvertAddAll(skills);
                }
                if (cartridges.TryGetValue(typeof(SpellTemplate), out var spells))
                {
                    mSpells.ConvertAddAll(spells);
                }
                if (cartridges.TryGetValue(typeof(BuffTemplate), out var buffs))
                {
                    mBuffs.ConvertAddAll(buffs);
                }
                if (cartridges.TryGetValue(typeof(AuraTemplate), out var auras))
                {
                    mAuras.ConvertAddAll(auras);
                }
                if (cartridges.TryGetValue(typeof(ItemTemplate), out var items))
                {
                    mItems.ConvertAddAll(items);
                }
                if (cartridges.TryGetValue(typeof(UnitEventTemplate), out var uevents))
                {
                    mUnitEvents.ConvertAddAll(uevents);
                }
                if (cartridges.TryGetValue(typeof(CardTemplate), out var cards))
                {
                    mCards.ConvertAddAll(cards);
                }
            }
        }
        public void Clear()
        {
            mUnits.Clear();
            mSkills.Clear();
            mSpells.Clear();
            mBuffs.Clear();
            mAuras.Clear();
            mItems.Clear();
            mUnitEvents.Clear();
            mCards.Clear();
        }
        public bool TryGetUnit(int id, out UnitInfo value)
        {
            return mUnits.TryGetValue(id, out value);
        }
        public bool TryGetSkill(int id, out SkillTemplate value)
        {
            return mSkills.TryGetValue(id, out value);
        }
        public bool TryGetSpell(int id, out SpellTemplate value)
        {
            return mSpells.TryGetValue(id, out value);
        }
        public bool TryGetBuff(int id, out BuffTemplate value)
        {
            return mBuffs.TryGetValue(id, out value);
        }
        public bool TryGetAura(int id, out AuraTemplate value)
        {
            return mAuras.TryGetValue(id, out value);
        }
        public bool TryGetItem(int id, out ItemTemplate value)
        {
            return mItems.TryGetValue(id, out value);
        }
        public bool TryGetUnitEvent(int id, out UnitEventTemplate value)
        {
            return mUnitEvents.TryGetValue(id, out value);
        }
        public bool TryGetCard(int id, out CardTemplate value)
        {
            return mCards.TryGetValue(id, out value);
        }
    }

    //---------------------------------------------------------------------------------------------------

    /// <summary>
    /// 由天赋模板创建的弹药库
    /// </summary>
    public class UnitCartridge : Recyclable
    {
        private readonly List<(ChangeEvent, UnitCardSlot, int)> init_changed = new();
        protected readonly UnitCartridgeMeta meta = new UnitCartridgeMeta();
        protected readonly HashMap<int, UnitCardSlot> ownerFuncs = new HashMap<int, UnitCardSlot>();
        protected readonly HashMap<int, int> ownerFuncsID = new HashMap<int, int>();
        public TemplateManager Templates => Owner.Templates;
        public InstanceUnit Owner { get; private set; }
        public IReadOnlyDictionary<int, int> OwnerFuncs => ownerFuncsID;
        public IEnumerable<UnitCardSlot> OwnerCards => ownerFuncs.Values;
        public UnitCartridge()
        {

        }
        public static T Alloc<T>(in TAddUnit add, InstanceUnit owner) where T : UnitCartridge, new()
        {
            return owner.ObjectPool.Alloc<T>().Init(add, owner) as T;
        }
        public virtual UnitCartridge Init(in TAddUnit _add, InstanceUnit owner)
        {
            this.Owner = owner;
            this.init_changed.Clear();
            if (owner.Info.Abilities.TryGetComponentAs<UnitInventoryAbility>(out var inv) && inv.Cards != null)
            {
                foreach (var slot in inv.Cards)
                {
                    // 扫出所有初始化带入的 卡片数据
                    var evt = PutOwnerFuncs(slot, out var exist, out var oldlevel);
                    if (evt != ChangeEvent.NA)
                    {
                        init_changed.Add((evt, exist, oldlevel));
                    }
                }
            }
            var add = owner.Add;
            if (add.cards != null)
            {
                foreach (var slot in add.cards)
                {
                    // 扫出所有初始化带入的 卡片数据
                    var evt = PutOwnerFuncs(slot, out var exist, out var oldlevel);
                    if (evt != ChangeEvent.NA)
                    {
                        init_changed.Add((evt, exist, oldlevel));
                    }
                }
            }
            if (init_changed.Count > 0)
            {
                // 重新加载模板
                this.meta.Update(owner, new ZoneCardRuntime(owner.Zone), owner.Info, ownerFuncsID);
            }
            return this;
        }
        protected override void Disposing()
        {
            meta.Clear();
            init_changed.Clear();
            ownerFuncsID.Clear();
            ownerFuncs.Clear();
            OnCardAdded = null;
            OnCardRemoved = null;
            OnCardChanged = null;
            Owner = null;
        }
        //---------------------------------------------------------------------------------------------------
        public void InitMeta()
        {
            if (init_changed.Count > 0)
            {
                var owner = this.Owner;
                var templates = owner.Templates;
                using (var adding = owner.ObjectPool.AllocList<ValueTuple<CardTemplate, TemplateData>>())
                {
                    if (ownerFuncs.Count > 0)
                    {
                        // 扫出所有需要装配的技能
                        foreach (var cardID in ownerFuncs)
                        {
                            var card = this.GetCard(cardID.Key);
                            if (card != null && card.AutoLearnSkill)
                            {
                                foreach (var field in card.Fields)
                                {
                                    field.ForEachUseTemplates((templates, adding, card), static (st, OwnerTemplateType, OwnerTemplateID) =>
                                    {
                                        if (st.templates.TryGetTemplate(OwnerTemplateType, OwnerTemplateID, out var temp))
                                        {
                                            st.adding.Add((st.card, temp));
                                        }
                                        return false;
                                    });
                                }
                            }
                        }
                    }
                    if (adding.Count > 0)
                    {
                        // 学习装配的技能
                        foreach (var e in adding)
                        {
                            Learn(owner, e.Item1, e.Item2);
                        }
                    }
                }
                // 更新装配的技能数据
                RefreshMeta(owner);
                // 初始化时发布事件
                foreach (var change in init_changed)
                {
                    var exist = change.Item2;
                    var oldLevel = change.Item3;
                    switch (change.Item1)
                    {
                        case ChangeEvent.Add:
                            exist.Start();
                            OnCardAdded?.Invoke(this, exist);
                            Owner.cb_OnCardsAdded(this, exist);
                            break;
                        case ChangeEvent.Remove:
                            OnCardRemoved?.Invoke(this, exist);
                            Owner.cb_OnCardsRemove(this, exist);
                            exist.Stop();
                            break;
                        case ChangeEvent.LevelChange:
                            OnCardChanged?.Invoke(this, exist, oldLevel);
                            Owner.cb_OnCardsLevelChange(this, exist, oldLevel);
                            break;
                        default:
                            break;
                    }
                }
                Owner.cb_OnCardsChanged(this);
                init_changed.Clear();
            }
            if (OwnerFuncs.Count > 0)
            {
                Owner.PostEvent(Owner.ObjectPool.Alloc<PlayerSyncCardsEvent>().Init(Owner.ObjectID, OwnerFuncs));
            }
        }
        protected virtual void UpdateMetas(InstanceUnit owner, UnitCartridgeMeta meta, HashMap<int, int> ownerFuncs)
        {
            meta.Clear();
            var templates = owner.Templates;
            using (var adding = owner.ObjectPool.AllocList<ValueTuple<CardTemplate, CardField, TemplateData>>())
            {
                if (ownerFuncs.Count > 0)
                {
                    // 扫出所有需要装配的技能
                    foreach (var cardID in ownerFuncs)
                    {
                        var card = GetCard(cardID.Key);
                        if (card != null && card.AutoLearnSkill)
                        {
                            foreach (CardField field in card.Fields)
                            {
                                field.ForEachUseTemplates((templates, adding, card, field), static (st, OwnerTemplateType, OwnerTemplateID) =>
                                {
                                    if (st.templates.TryGetTemplate(OwnerTemplateType, OwnerTemplateID, out var temp))
                                    {
                                        st.adding.Add((st.card, st.field, temp));
                                    }
                                    return false;
                                });
                            }
                        }
                    }
                    // 重新加载模板
                    meta.Update(owner, new ZoneCardRuntime(owner.Zone), owner.Info, ownerFuncs);
                }
                if (adding.Count > 0)
                {
                    // 学习装配的技能
                    foreach (var e in adding)
                    {
                        Learn(owner, e.Item1, e.Item3);
                    }
                }
            }
            // 更新装配的技能数据
            RefreshMeta(owner);
        }
        /// <summary>
        /// 如果词缀里有该模板，那么给单位添加对应技能能力。
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="card"></param>
        /// <param name="temp"></param>
        protected virtual void Learn(InstanceUnit owner, CardTemplate card, TemplateData temp)
        {
            if (temp is SkillTemplate skill)
            {
                owner.LearnSkill(this, card, skill);
            }
            /*       else if (temp is BuffTemplate buff)
                   {
                       owner.LearnBuff(this, card, buff.ID);
                   }
                   else if (temp is AuraTemplate aura)
                   {
                       owner.LearnAura(this, card, aura.ID);
                   }*/
            else if (temp is UnitEventTemplate ue)
            {
                owner.LearnUnitEvent(this, card, ue);
            }
        }
        /// <summary>
        /// 更新装配的技能数据
        /// </summary>
        /// <param name="owner"></param>
        protected virtual void RefreshMeta(InstanceUnit owner)
        {
            owner.RefreshData(owner.Info);
            owner.ForEachSkills(meta, static (meta, equip) =>
            {
                if (meta.TryGetSkill(equip.ID, out var skill))
                {
                    equip.RefreshData(skill);
                }
            });
            foreach (var buff in meta.mBuffs)
            {
                owner.RefreshBuffData(buff.Value);
            }
            foreach (var aura in meta.mAuras)
            {
                var equip = owner.GetAura(aura.Key);
                if (equip != null)
                {
                    equip.RefreshData(aura.Value);
                }
            }
            foreach (var ue in meta.mUnitEvents)
            {
                owner.RefreshUnitEventData(ue.Value);
            }
            foreach (var card in this.ownerFuncs.Values)
            {
                card.Refresh();
            }
        }
        //---------------------------------------------------------------------------------------------------
        #region CardSlot
        protected virtual bool TryPutCard(InstanceUnit owner, CardSlot slot, CardTemplate card)
        {
            if (Owner.Zone.Formula.TryPutCard(Owner, slot, card))
            {
                return true;
            }
            return false;
        }
        public delegate void CardAddHander(UnitCartridge cartridge, UnitCardSlot level);
        public delegate void CardRemoveHander(UnitCartridge cartridge, UnitCardSlot level);
        public delegate void CardChangedHander(UnitCartridge cartridge, UnitCardSlot level, int oldLevel);
        public event CardAddHander OnCardAdded;
        public event CardRemoveHander OnCardRemoved;
        public event CardChangedHander OnCardChanged;
        public class UnitCardSlot
        {
            private InstanceUnit _owner;
            private int _level;
            private CustomUnitEventTriggerCollection _bindEvent;
            public CardTemplate Card { get; }
            public int Level
            {
                get => _level;
                internal set { _level = value; }
            }
            public UnitCardSlot(InstanceUnit owner, CardTemplate card)
            {
                this.Card = card;
                this._owner = owner;
            }
            internal void Start()
            {
                this._bindEvent = _owner.BindCustomUnitEvent(Card);
            }
            internal void Stop()
            {
                _owner.RemoveCustomEvent(_bindEvent);
                this._bindEvent = null;
            }
            internal void Refresh()
            {
                if (_bindEvent != null)
                {
                    this._bindEvent.RefreshData(Card);
                }
            }
            public object Tag { get; set; }
        }
        public enum ChangeEvent
        {
            NA,
            Add, Remove, LevelChange,
        }
        public bool TryGetCardSlot(int cardID, out UnitCardSlot slot)
        {
            return ownerFuncs.TryGetValue(cardID, out slot);
        }
        public UnitCardSlot FindCardSlot<ST>(ST st, TryGetPredicate<ST, UnitCardSlot> find)
        {
            foreach (var v in ownerFuncs.Values)
            {
                if (find(st, v))
                {
                    return v;
                }
            }
            return null;
        }
        //---------------------------------------------------------------------------------------------------
        public void PutOwnerFuncs(IReadOnlyDictionary<int, int> ownerFuncs)
        {
            if (ownerFuncs != null)
            {
                var cards = ownerFuncs.ConvertAll(func => new CardSlot() { CardTemplateID = func.Key, Op = CardSlot.CardSlotOperation.SetLevel, Level = func.Value });
                PutCardSlots(cards);
            }
        }
        private ChangeEvent PutOwnerFuncs(CardSlot slot, out UnitCardSlot exist, out int oldLevel)
        {
            if (Templates.GetCard(slot.CardTemplateID) is CardTemplate card && TryPutCard(Owner, slot, card))
            {
                switch (slot.Op)
                {
                    case CardSlot.CardSlotOperation.SetLevel:
                        if (ownerFuncs.TryGetValue(slot.CardTemplateID, out exist))
                        {
                            oldLevel = exist.Level;
                            if (exist.Level != slot.Level)
                            {
                                exist.Level = slot.Level;
                                ownerFuncsID.Put(slot.CardTemplateID, slot.Level);
                                return ChangeEvent.LevelChange;
                            }
                        }
                        else
                        {
                            oldLevel = 0;
                            exist = new UnitCardSlot(Owner, card) { Level = slot.Level };
                            ownerFuncs.Add(slot.CardTemplateID, exist);
                            ownerFuncsID.Put(slot.CardTemplateID, slot.Level);
                            return ChangeEvent.Add;
                        }
                        break;
                    case CardSlot.CardSlotOperation.Upgrade:
                        if (ownerFuncs.TryGetValue(slot.CardTemplateID, out exist))
                        {
                            oldLevel = exist.Level;
                            exist.Level += 1;
                            ownerFuncsID.Put(slot.CardTemplateID, exist.Level);
                            return ChangeEvent.LevelChange;
                        }
                        else
                        {
                            oldLevel = 0;
                            exist = new UnitCardSlot(Owner, card) { Level = 0 };
                            ownerFuncs.Add(slot.CardTemplateID, exist);
                            ownerFuncsID.Put(slot.CardTemplateID, exist.Level);
                            return ChangeEvent.Add;
                        }
                    case CardSlot.CardSlotOperation.Degrade:
                        if (ownerFuncs.TryGetValue(slot.CardTemplateID, out exist))
                        {
                            oldLevel = exist.Level;
                            if (exist.Level <= 0)
                            {
                                ownerFuncs.Remove(slot.CardTemplateID);
                                ownerFuncsID.Remove(slot.CardTemplateID);
                                return ChangeEvent.Remove;
                            }
                            else
                            {
                                exist.Level -= 1;
                                ownerFuncsID.Put(slot.CardTemplateID, exist.Level);
                                return ChangeEvent.LevelChange;
                            }
                        }
                        break;
                    case CardSlot.CardSlotOperation.Clear:
                        if (ownerFuncs.TryRemove(slot.CardTemplateID, out exist))
                        {
                            oldLevel = exist.Level;
                            ownerFuncsID.Remove(slot.CardTemplateID);
                            return ChangeEvent.Remove;
                        }
                        break;
                }
            }
            exist = null;
            oldLevel = 0;
            return ChangeEvent.NA;
        }

        public UnitCardSlot PutCardSlot(CardSlot slot)
        {
            var changed = PutOwnerFuncs(slot, out var exist, out var oldLevel);
            if (changed != ChangeEvent.NA)
            {
                UpdateMetas(Owner, meta, ownerFuncsID);
            }
            switch (changed)
            {
                case ChangeEvent.Add:
                    exist.Start();
                    OnCardAdded?.Invoke(this, exist);
                    Owner.cb_OnCardsAdded(this, exist);
                    break;
                case ChangeEvent.Remove:
                    OnCardRemoved?.Invoke(this, exist);
                    Owner.cb_OnCardsRemove(this, exist);
                    exist.Stop();
                    break;
                case ChangeEvent.LevelChange:
                    OnCardChanged?.Invoke(this, exist, oldLevel);
                    Owner.cb_OnCardsLevelChange(this, exist, oldLevel);
                    break;
                default:
                    break;
            }
            if (changed != ChangeEvent.NA)
            {
                Owner.cb_OnCardsChanged(this);
            }
            return exist;
        }
        public void PutCardSlots(IEnumerable<CardSlot> cards)
        {
            if (cards == null) return;
            using (var changelist = Owner.ObjectPool.AllocList<(ChangeEvent, UnitCardSlot, int)>())
            {
                foreach (var slot in cards)
                {
                    var changed = PutOwnerFuncs(slot, out var exist, out var oldLevel);
                    if (changed != ChangeEvent.NA)
                    {
                        changelist.Add((changed, exist, oldLevel));
                    }
                }
                if (changelist.Count > 0)
                {
                    UpdateMetas(Owner, meta, ownerFuncsID);
                    foreach (var change in changelist)
                    {
                        var exist = change.Item2;
                        var oldLevel = change.Item3;
                        switch (change.Item1)
                        {
                            case ChangeEvent.Add:
                                exist.Start();
                                OnCardAdded?.Invoke(this, exist);
                                Owner.cb_OnCardsAdded(this, exist);
                                break;
                            case ChangeEvent.Remove:
                                OnCardRemoved?.Invoke(this, exist);
                                Owner.cb_OnCardsRemove(this, exist);
                                exist.Stop();
                                break;
                            case ChangeEvent.LevelChange:
                                OnCardChanged?.Invoke(this, exist, oldLevel);
                                Owner.cb_OnCardsLevelChange(this, exist, oldLevel);
                                break;
                            default:
                                break;
                        }
                    }
                    Owner.cb_OnCardsChanged(this);
                }
            }
        }
        public void ClearCardSlots()
        {
            if (ownerFuncs.Count > 0)
            {
                ownerFuncsID.Clear();
                UpdateMetas(Owner, meta, ownerFuncsID);
                foreach (var exist in ownerFuncs.Values)
                {
                    OnCardRemoved?.Invoke(this, exist);
                    Owner.cb_OnCardsRemove(this, exist);
                    exist.Stop();
                }
                ownerFuncs.Clear();
                Owner.cb_OnCardsChanged(this);
            }
        }
        public bool RemoveCardSlot(int cardID)
        {
            var exist = this.PutCardSlot(new CardSlot() { CardTemplateID = cardID, Op = CardSlot.CardSlotOperation.Clear });
            return exist != null;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------
        #region Templates
        public virtual UnitInfo GetUnit(int id)
        {
            if (meta != null && meta.TryGetUnit(id, out var value))
            {
                return value;
            }
            return Templates.GetUnit(id);
        }
        public virtual SkillTemplate GetSkill(int id)
        {
            if (meta != null && meta.TryGetSkill(id, out var value))
            {
                return value;
            }
            return Templates.GetSkill(id);
        }
        public virtual SpellTemplate GetSpell(int id)
        {
            if (meta != null && meta.TryGetSpell(id, out var value))
            {
                return value;
            }
            return Templates.GetSpell(id);
        }
        public virtual BuffTemplate GetBuff(int id, int lv = 0)
        {
            if (meta != null && meta.TryGetBuff(id, out var value))
            {
                return value;
            }
            return Templates.GetBuff(id);
        }
        public virtual AuraTemplate GetAura(int id)
        {
            if (meta != null && meta.TryGetAura(id, out var value))
            {
                return value;
            }
            return Templates.GetAura(id);
        }
        public virtual ItemTemplate GetItem(int id)
        {
            if (meta != null && meta.TryGetItem(id, out var value))
            {
                return value;
            }
            return Templates.GetItem(id);
        }
        public virtual UnitEventTemplate GetUnitEvent(int id)
        {
            if (meta != null && meta.TryGetUnitEvent(id, out var value))
            {
                return value;
            }
            value = Templates.GetUnitEvent(id);
            return value;
        }
        public virtual CardTemplate GetCard(int id)
        {
            if (meta != null && meta.TryGetCard(id, out var value))
            {
                return value;
            }
            value = Templates.GetCard(id);
            return value;
        }
        #endregion
        //---------------------------------------------------------------------------------------------------
        /// <summary>
        /// 获取可升级列表，过滤掉满级或者未达成条件的
        /// </summary>
        /// <param name="cards"></param>
        public void FilterUpgradeableCards(List<CardTemplate> cards)
        {
            for (int i = cards.Count - 1; i >= 0; --i)
            {
                var card = cards[i];
                {
                    if (ownerFuncsID.TryGetValue(card.ID, out var currentLevel))
                    {
                        if (currentLevel >= card.LevelsCount - 1)
                        {
                            cards.RemoveAt(i);
                            continue;
                        }
                    }
                }
                //                 if (card.DependCards != null)
                //                 {
                //                     foreach (var dep in card.DependCards)
                //                     {
                //                         if (ownerFuncs.TryGetValue(dep.DependCardID, out var currentLevel))
                //                         {
                //                             if (currentLevel < dep.DependCardLevel)
                //                             {
                //                                 cards.RemoveAt(i);
                //                                 break;
                //                             }
                //                         }
                //                         else
                //                         {
                //                             cards.RemoveAt(i);
                //                             break;
                //                         }
                //                     }
                //                 }
            }
        }
        public void FilterDependOnCards(List<CardTemplate> cards)
        {
            for (int i = cards.Count - 1; i >= 0; --i)
            {
                var card = cards[i];
                //                 {
                //                     if (ownerFuncs.TryGetValue(card.ID, out var currentLevel))
                //                     {
                //                         if (currentLevel >= card.LevelsCount - 1)
                //                         {
                //                             cards.RemoveAt(i);
                //                             continue;
                //                         }
                //                     }
                //                 }
                if (card.DependCards != null)
                {
                    foreach (var dep in card.DependCards)
                    {
                        if (ownerFuncsID.TryGetValue(dep.DependCardID, out var currentLevel))
                        {
                            if (currentLevel < dep.DependCardLevel)
                            {
                                cards.RemoveAt(i);
                                break;
                            }
                        }
                        else
                        {
                            cards.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        //---------------------------------------------------------------------------------------------------
        public void GetCurrentCardStatus(IList<ClientStruct.UnitCardStatus> ret)
        {
            {
                foreach (var card in this.OwnerFuncs)
                {
                    ret.Add(new ClientStruct.UnitCardStatus()
                    {
                        CardID = card.Key,
                        Level = card.Value
                    });
                }
            }
        }
    }

    //---------------------------------------------------------------------------------------------------

}

using DeepCore.Components;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.FuncData;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Host.Instance.Triggers;
using DeepCore.GameData.Zone.ZoneEditor.EventTrigger;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 所有常态状态（Buff，技能，被动系）
    /// </summary>
    partial class InstanceUnit
    {
        //-----------------------------------------------------------------------------------------------------//

        //---------------------------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------------------------

        #region _背包和道具_


        public class InventorySlot : InstanceStatus
        {
            private int index;
            private InstanceUnit owner;
            private ItemTemplate item;
            private ItemInventory itemInv;
            private int count;
            private object tag;
            protected InventorySlot() { }
            public static InventorySlot Alloc(int index, InstanceUnit owner)
            {
                return owner.ObjectPool.AllocOrCreateAutoRelease<InventorySlot>(static s => new InventorySlot()).Init(index, owner);
            }
            protected virtual InventorySlot Init(int index, InstanceUnit owner)
            {
                this.index = index;
                this.owner = owner;
                return this;
            }
            protected override void Disposing()
            {
                this.index = default;
                this.owner = default;
                this.item = default;
                this.itemInv = default;
                this.count = default;
                this.tag = default;
            }


            public int Index { get => index; }
            public InstanceUnit Owner { get => owner; }
            public ItemTemplate Item { get => item; }
            public ItemInventory ItemInv { get => itemInv; }
            public int Count { get => count; }
            public object UserTag { get => tag; set => tag = value; }
            public bool IsEmpty
            {
                get { return (Item == null); }
            }
            public int ItemTemplateID
            {
                get { if (Item != null) return Item.ID; return 0; }
            }
            internal int AddItem(ItemTemplate item, int count)
            {
                if (item == null) return 0;
                if (count <= 0) return 0;
                if (item.Abilities.TryGetComponentAs<ItemInventory>(out var inv))
                {
                    if (this.IsEmpty)
                    {
                        // 背包为空 //
                        this.item = item;
                        this.itemInv = inv;
                        this.count = Math.Min(inv.MaxStackCount, count);
                        this.Owner.doGotInventoryItem(this, item, Index, count);
                        return this.Count;
                    }
                    else if (item.ID == this.ItemTemplateID)
                    {
                        int oldcount = this.Count;
                        // 道具类型一致, 堆叠 //
                        this.count = Math.Min(inv.MaxStackCount, this.Count + count);
                        if (oldcount != this.Count)
                        {
                            this.Owner.doGotInventoryItem(this, item, Index, count);
                            return this.Count - oldcount;
                        }
                        return 0;
                    }
                }
                return 0;
            }
            internal int TryUse(int count)
            {
                if (IsEmpty) return 0;
                if (!ItemInv.IsInventoryUseable) return 0;
                if (Owner.tryUseItem(Item, Owner, out var useable, out var usebase))
                {
                    return Math.Min(count, this.Count);
                }
                return 0;
            }
            internal int Use(int count)
            {
                if (IsEmpty) return 0;
                if (!ItemInv.IsInventoryUseable) return 0;
                if (Owner.tryUseItem(Item, Owner, out var useable, out var usebase))
                {
                    int used = 0;
                    count = Math.Min(count, this.Count);
                    for (int i = 0; i < count; i++)
                    {
                        Owner.UseItem(Item);
                        this.count--;
                        used++;
                        if (Count == 0)
                        {
                            break;
                        }
                    }
                    this.Owner.doLostInventoryItem(this, Item, Index, count);
                    if (Count == 0)
                    {
                        this.item = null;
                    }
                    return used;
                }
                return 0;
            }
            internal int Drop(int count)
            {
                if (IsEmpty) return 0;
                count = Math.Min(count, this.Count);
                this.count -= count;
                if (this.Count <= 0)
                {
                    this.count = 0;
                }
                this.Owner.doLostInventoryItem(this, Item, Index, count);
                if (this.count == 0)
                {
                    this.item = null;
                }
                return count;
            }
            internal int Clean()
            {
                return Drop(this.Count);
            }
        }
        public class InventoryBag
        {
            public InstanceZone Zone => unit.Parent;
            private readonly InstanceUnit unit;
            private List<InventorySlot> mInventorySlots = new List<InventorySlot>(1);
            private HashMap<int, TimeExpire<ItemTemplate>> mCoolDownItems = new HashMap<int, TimeExpire<ItemTemplate>>(1);
            public int SlotCount => mInventorySlots.Count;
            public IReadOnlyList<InventorySlot> Slots => mInventorySlots;
            public InventoryBag(InstanceUnit unit) { this.unit = unit; }

            public void AddSlot(InventorySlot slot)
            {
                mInventorySlots.Add(slot);
            }
            public void OnResetInventorySize(UnitInventoryAbility AInventory)
            {
                if (AInventory)
                {
                    if (unit.InventorySize < mInventorySlots.Count)
                    {
                        var count = mInventorySlots.Count - unit.InventorySize;
                        for (int i = 0; i < count; i++)
                        {
                            var index = mInventorySlots.Count - 1;
                            var slot = mInventorySlots[index];
                            try
                            {
                                unit.doLostInventoryItem(slot, slot.Item, index, slot.Count);
                                mInventorySlots.RemoveAt(index);
                            }
                            finally
                            {
                                slot.Dispose();
                            }
                        }
                    }
                    else if (unit.InventorySize > mInventorySlots.Count)
                    {
                        var count = unit.InventorySize - mInventorySlots.Count;
                        for (int i = 0; i < count; i++)
                        {
                            mInventorySlots.Add(Zone.CreateUnitInventorySlot(mInventorySlots.Count, unit));
                        }
                    }
                }
            }
            public void Clear()
            {
                foreach (var slot in mInventorySlots)
                {
                    slot.Clean();
                    slot.Dispose();
                }
                mInventorySlots.Clear();
                mCoolDownItems.Clear();
            }
            public void Dispose()
            {
                foreach (var slot in mInventorySlots)
                {
                    slot.Dispose();
                }
                mInventorySlots.Clear();
                mCoolDownItems.Clear();
            }

            public InventorySlot ForEachInventory<ST>(ST st, ForEachPredicate<ST, InventorySlot> action)
            {
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot != null && action(st, slot))
                    {
                        return slot;
                    }
                }
                return default;
            }
            public void ForEachInventory<ST>(ST st, ForEachAction<ST, InventorySlot> action)
            {
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot != null) action(st, slot);
                }
            }


            public TimeExpire<ItemTemplate> AddCoolDownItem(ItemTemplate item, int timeExpireMS)
            {
                var cooldown = new TimeExpire<ItemTemplate>().Init(timeExpireMS, item);
                mCoolDownItems.Put(item.ID, cooldown);
                return cooldown;
            }
            public bool IsItemCoolDown(int itemTemplateID)
            {
                if (mCoolDownItems.ContainsKey(itemTemplateID))
                {
                    return true;
                }
                return false;
            }
            //             public TimeExpire<ItemTemplate> RemoveCoolDownItem(int id)
            //             {
            //              return   mCoolDownItems.RemoveByKey(id);
            //             }
            public void Update()
            {
                if (mCoolDownItems.Count == 0) return;

                var intervalMS = unit.Parent.UpdateIntervalMS;
                using (var removed = unit.ObjectPool.AllocList<TimeExpire<ItemTemplate>>())
                {
                    foreach (TimeExpire<ItemTemplate> expire in mCoolDownItems.Values)
                    {
                        if (expire.Update(intervalMS))
                        {
                            removed.Add(expire);
                        }
                    }
                    if (removed.Count > 0)
                    {
                        foreach (TimeExpire<ItemTemplate> expire in removed)
                        {
                            if (expire.IsEnd)
                            {
                                mCoolDownItems.RemoveByKey(expire.Tag.ID);
                            }
                        }
                    }
                }
            }


            /// <summary>
            /// 添加道具到白空的背包
            /// </summary>
            /// <param name="item"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public int AddItemToEmptyInventory(ItemTemplate item, int count = 1)
            {
                if (item == null) return 0;
                if (count <= 0) return 0;
                if (item.Abilities.TryGetComponentAs<ItemInventory>(out var inv))
                {
                    if (!inv.IsDuplicateInventory || inv.HoldingLimit > 0)
                    {
                        int holding = GetItemCountInInventory(item.ID);
                        if (!inv.IsDuplicateInventory)
                        {
                            if (holding > 0)
                            {
                                return 0;
                            }
                            count = 1;
                        }
                        if (inv.HoldingLimit > 0)
                        {
                            if (holding >= inv.HoldingLimit)
                            {
                                return 0;
                            }
                            if (holding + count > inv.HoldingLimit)
                            {
                                count = inv.HoldingLimit - holding;
                            }
                            if (count <= 0) return 0;
                        }
                    }
                    int added = 0;
                    //优先堆叠//
                    for (int i = 0; i < mInventorySlots.Count; i++)
                    {
                        var slot = mInventorySlots[i];
                        if (slot.ItemTemplateID == item.ID)
                        {
                            added += slot.AddItem(item, count - added);
                            if (added >= count)
                            {
                                break;
                            }
                        }
                    }
                    //使用空闲格子//
                    for (int i = 0; i < mInventorySlots.Count; i++)
                    {
                        InventorySlot slot = mInventorySlots[i];
                        added += slot.AddItem(item, count - added);
                        if (added >= count)
                        {
                            break;
                        }
                    }
                    return added;
                }
                return 0;
            }

            /// <summary>
            /// 添加道具到指定背包
            /// </summary>
            /// <param name="item"></param>
            /// <param name="index"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public int AddItemToInventory(ItemTemplate item, int index, int count = 1)
            {
                if (item == null) return 0;
                if (count <= 0) return 0;
                if (item.Abilities.TryGetComponentAs<ItemInventory>(out var inv))
                {
                    if (!inv.IsDuplicateInventory || inv.HoldingLimit > 0)
                    {
                        int holding = GetItemCountInInventory(item.ID);
                        if (!inv.IsDuplicateInventory)
                        {
                            if (holding > 0)
                            {
                                return 0;
                            }
                            count = 1;
                        }
                        if (inv.HoldingLimit > 0)
                        {
                            if (holding >= inv.HoldingLimit)
                            {
                                return 0;
                            }
                            if (holding + count > inv.HoldingLimit)
                            {
                                count = inv.HoldingLimit - holding;
                            }
                            if (count <= 0) return 0;
                        }
                    }
                    if (index >= 0 && index < mInventorySlots.Count)
                    {
                        InventorySlot slot = mInventorySlots[index];
                        return slot.AddItem(item, count);
                    }
                }
                return 0;
            }
            /// <summary>
            /// 添加道具到指定背包
            /// </summary>
            /// <param name="itemTemplateID"></param>
            /// <param name="index"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public int AddItemToInventory(int itemTemplateID, int index, int count = 1)
            {
                if (count <= 0) return 0;
                return AddItemToInventory(unit.Cartridge.GetItem(itemTemplateID), index, count);
            }


            private int UseItemInternal(InventorySlot slot, int count)
            {
                if (slot.ItemInv != null)
                {
                    if (slot.ItemInv.UseInProgressTimeMS > 0)
                    {
                        count = slot.TryUse(count);
                        if (count > 0)
                        {
                            unit.StartPickProgressSelf(slot.ItemInv.UseInProgressTimeMS, (s, cancel, p, t) =>
                            {
                                if (!cancel)
                                {
                                    slot.Use(count);
                                }
                                return true;
                            });
                        }
                        return count;
                    }
                    else
                    {
                        return slot.Use(count);
                    }
                }
                return 0;
            }
            /// <summary>
            /// 使用背包的道具
            /// </summary>
            /// <param name="index"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public int UseInventoryItem(int index, int count = 1)
            {
                if (count <= 0) return 0;
                if (index >= 0 && index < mInventorySlots.Count)
                {
                    InventorySlot slot = mInventorySlots[index];
                    return UseItemInternal(slot, count);
                }
                return 0;
            }
            public int UseInventoryItemByType(int itemTemplateID, int count = 1)
            {
                if (count <= 0) return 0;
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot.ItemTemplateID == itemTemplateID)
                    {
                        return UseItemInternal(slot, count);
                    }
                }
                return 0;
            }


            public int DropInventoryItemByIndex(int index, int count = 1)
            {
                if (count <= 0) return 0;
                if (index >= 0 && index < mInventorySlots.Count)
                {
                    InventorySlot slot = mInventorySlots[index];
                    return slot.Drop(count);
                }
                return 0;
            }
            public int DropInventoryItemByType(int itemTemplateID, int count = 1)
            {
                if (count <= 0) return 0;
                int droped = 0;
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot.ItemTemplateID == itemTemplateID)
                    {
                        droped += slot.Drop(count);
                        if (droped >= count)
                        {
                            break;
                        }
                    }
                }
                return droped;
            }
            public int ClearInventoryItemByIndex(int index)
            {
                if (index >= 0 && index < mInventorySlots.Count)
                {
                    InventorySlot slot = mInventorySlots[index];
                    return slot.Clean();
                }
                return 0;
            }
            public int ClearInventoryItemByType(int itemTemplateID)
            {
                int droped = 0;
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot.ItemTemplateID == itemTemplateID)
                    {
                        droped += slot.Clean();
                    }
                }
                return droped;
            }

            /// <summary>
            /// 背包内是否有此道具
            /// </summary>
            /// <param name="itemTemplateID"></param>
            /// <returns></returns>
            public bool ContainsItemInInventory(int itemTemplateID)
            {
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot.ItemTemplateID == itemTemplateID)
                    {
                        return true;
                    }
                }
                return false;
            }
            public int GetItemCountInInventory(int itemTemplateID)
            {
                int ret = 0;
                for (int i = 0; i < mInventorySlots.Count; i++)
                {
                    InventorySlot slot = mInventorySlots[i];
                    if (slot.ItemTemplateID == itemTemplateID)
                    {
                        ret += slot.Count;
                    }
                }
                return ret;
            }
        }
        public InventoryBag Bag => mBag;
        protected readonly InventoryBag mBag;

        protected virtual void InitBagSlots()
        {
            if (AInventory)
            {
                InventorySize = AInventory.InventorySize;
                for (int i = 0; i < InventorySize; i++)
                {
                    mBag.AddSlot(Zone.CreateUnitInventorySlot(i, this));
                }
                if (AInventory.InventoryList != null)
                {
                    foreach (InventoryItem item in AInventory.InventoryList)
                    {
                        var temp = Cartridge.GetItem(item.ItemTemplateID);
                        if (temp != null)
                        {
                            Bag.AddItemToEmptyInventory(temp, item.Count);
                        }
                    }
                }
            }
        }
        protected virtual void ClearItemSlots()
        {
            mBag.Clear();
        }
        private void cleanItems()
        {
            mBag.Dispose();
        }

        public void GetCurrentItemStatus(IList<ClientStruct.UnitItemStatus> ret)
        {
            int i = 0;
            foreach (InventorySlot item in mBag.Slots)
            {
                ret.Add(new ClientStruct.UnitItemStatus()
                {
                    ItemTemplateID = item.ItemTemplateID,
                    Count = item.Count,
                });
                i++;
            }
        }
        private void OnResetInventorySize()
        {
            mBag.OnResetInventorySize(AInventory);
        }

        private void updateItems()
        {
            mBag.Update();
        }

        private bool tryUseItem(ItemTemplate item, InstanceUnit item_creater, out ItemUseable useable, out ItemUseValue usebase)
        {
            if (Parent.Formula.TryUseItem(this, item, item_creater))
            {
                useable = item.Abilities.GetComponentAs<ItemUseable>();
                usebase = item.Abilities.GetComponentAs<ItemUseValue>();
                if (useable)
                {
                    if (useable.UseCoolDownTimeMS > 0)
                    {
                        if (IsItemCoolDown(item.ID))
                        {
                            return false;
                        }
                        return true;
                    }
                }
                return true;
            }
            useable = null;
            usebase = null;
            return false;
        }
        private void beginUseItem(ItemTemplate item, ItemUseable usable)
        {
            if (usable.UseCoolDownTimeMS > 0)
            {
                mBag.AddCoolDownItem(item, usable.UseCoolDownTimeMS);
            }
        }

        /// <summary>
        /// 检测此道具是否在CD
        /// </summary>
        /// <param name="itemTemplateID"></param>
        /// <returns></returns>
        public bool IsItemCoolDown(int itemTemplateID)
        {
            return mBag.IsItemCoolDown(itemTemplateID);
        }


        public bool UseItem(int itemTemplateID, InstanceUnit item_creater = null)
        {
            ItemTemplate item = Cartridge.GetItem(itemTemplateID);
            if (item != null)
            {
                return UseItem(item, item_creater);
            }

            return false;
        }

        // 单位获取道具 
        protected virtual bool tryGotItem(InstanceItem item)
        {
            if (!Parent.Formula.IsVisibleAOI(this, item))
            {
                return false;
            }
            var ret = true;
            if (item.Info.DropMoneyMin > 0 && item.Info.DropMoneyMax > 0)
            {
                int min = Math.Min(item.Info.DropMoneyMin, item.Info.DropMoneyMax);
                int max = Math.Max(item.Info.DropMoneyMin, item.Info.DropMoneyMax);
                int money = RandomN.Next(min, max + 1);
                //this.CurrentMoney += money;
                this.AddMoney(money);
                ret |= true;
            }

            if (item.Info.GotOnUse)
            {
                // 获取后立即使用 //
                if (UseItem(item.Info, item.ItemCreater))
                {
                    ret |= true;
                    //return true;
                }
            }
            else
            {
                // 获取后进背包 //
                if (Bag.AddItemToEmptyInventory(item.Info) > 0)
                {
                    ret |= true;
                    //return true;
                }
            }

            return ret;
        }

        internal bool doGotInstanceItem(InstanceItem item)
        {
            if (tryGotItem(item) && Parent.cb_unitGotInstanceItemCallBack(this, item))
            {
                if (item.Info.GotEffect != null)
                {
                    PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(ID, item.Info.GotEffect));
                }
                return true;
            }
            return false;
        }

        internal void doGotInventoryItem(InventorySlot slot, ItemTemplate item, int index, int count)
        {
            Parent.Formula.OnGotInventoryItem(this, item);
            PostEvent(ObjectPool.Alloc<UnitSyncInventoryItemEvent>().Init(ID, item.ID, index, slot.Count));
            Parent.cb_unitGotInventoryItemCallBack(this, item, count);
            // 添加装备Buff //
            if (item.Abilities.TryGetComponentAs<ItemEquip>(out var equip))
            {
                foreach (LaunchBuff buff in equip.EquipBuffs)
                {
                    AddBuff(buff, this, null);
                }
            }
        }

        internal void doLostInventoryItem(InventorySlot slot, ItemTemplate item, int index, int count)
        {
            Parent.Formula.OnLostInventoryItem(this, item);
            PostEvent(ObjectPool.Alloc<UnitSyncInventoryItemEvent>().Init(ID, item.ID, index, slot.Count));
            Parent.cb_unitLostInventoryItemCallBack(this, item, count);
            // 移除装备Buff //
            if (item.Abilities.TryGetComponentAs<ItemEquip>(out var equip))
            {
                if (equip.EquipBuffs != null)
                {
                    foreach (LaunchBuff buff in equip.EquipBuffs)
                    {
                        RemoveBuff(buff.BuffID);
                    }
                }
            }
        }
        public bool UseItem(ItemTemplate item, InstanceUnit item_creater = null)
        {
            if (item_creater == null)
            {
                item_creater = this;
            }
            var launcher = item_creater ?? this;

            if (tryUseItem(item, item_creater, out var use, out var useValue))
            {
                if (use)
                {
                    beginUseItem(item, use);
                    // 如果关键帧绑定特效
                    if (use.UseEffect)
                    {
                        PostEvent(ObjectPool.Alloc<UnitEffectEvent>().Init(ID, use.UseEffect));
                    }
                    // 如果关键帧绑定释放法术
                    if (use.UseSpell != null)
                    {
                        Parent.UnitLaunchSpell(launcher, this, use.UseSpell, item, this.Position);
                    }
                    if (use.UseSummon != null)
                    {
                        Parent.UnitSummonUnit(item_creater, use.UseSummon);
                    }
                    // 如果关键帧绑定自己释放BUFF
                    if (use.UseBuffs != null)
                    {
                        foreach (LaunchBuff buff in use.UseBuffs)
                        {
                            this.AddBuff(buff, launcher);
                        }
                    }
                    if (use.UseCards != null)
                    {
                        this.Cartridge.PutCardSlots(use.UseCards);
                    }
                }
                if (useValue)
                {
                    this.AddHP(useValue.AddHP);
                    this.AddMP(useValue.AddMP);
                    this.AddSP(useValue.AddSP);
                    this.AddHP_Pct(useValue.AddHP_Pct, this);
                    this.AddMP_Pct(useValue.AddMP_Pct);
                    this.AddSP_Pct(useValue.AddSP_Pct);
                    this.AddExp(useValue.AddEXP);
                    this.AddMoney(useValue.AddMoney);
                }
                Parent.Formula.OnUseItem(this, item, item_creater);
                PostEvent(ObjectPool.Alloc<UnitUseItemEvent>().Init(ID, item.ID));
                Parent.cb_unitUseItemCallBack(this, item, item_creater);
                this.LogUseItem(item);
                return true;
            }
            return false;
        }


        #endregion

        //---------------------------------------------------------------------------------------------------------------

    }
}

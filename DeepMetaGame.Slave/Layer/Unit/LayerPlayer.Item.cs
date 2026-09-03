using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer
    {

        private HashMap<int, TimeExpire<ItemTemplate>> mCoolDownItems = new HashMap<int, TimeExpire<ItemTemplate>>();

        internal void SyncItems(IEnumerable<ClientStruct.UnitItemStatus> items)
        {
            if (items != null)
            {
                mItemSlots.Clear();
                foreach (var st in items)
                {
                    var slot = new ItemSlot(this);
                    if (st.Count > 0)
                    {
                        slot.Sync(st);
                    }
                    mItemSlots.Add(slot);
                }
            }
        }

        private void ResetItems()
        {
            mItemSlots.Clear();
            for (int i = 0; i < InventorySize; i++)
            {
                mItemSlots.Add(new ItemSlot(this));
            }
        }

        private void UpdateItems(float intervalMS)
        {
            if (mCoolDownItems.Count > 0)
            {
                using (var removed = ObjectPool.AllocList<TimeExpire<ItemTemplate>>())
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
        }

        protected virtual void DoPlayerSyncItem(UnitSyncInventoryItemEvent me)
        {
            ItemSlot slot = GetItem(me.Index);
            if (slot != null)
            {
                slot.Set(me);
            }
        }

        protected virtual void DoPlayerUseItem(UnitUseItemEvent me)
        {
            ItemTemplate item = Templates.GetItem(me.ItemTemplateID);
            if (item != null && item.Abilities.TryGetComponentAs<ItemUseable>(out var use))
            {
                if (use.UseCoolDownTimeMS > 0)
                {
                    mCoolDownItems.Put(item.ID, new TimeExpire<ItemTemplate>().Init(use.UseCoolDownTimeMS, item));
                }
            }
        }

        public TimeExpire<ItemTemplate> GetCoolDownItem(int itemTemplateID)
        {
            return mCoolDownItems.Get(itemTemplateID);
        }

        internal readonly List<ItemSlot> mItemSlots = new List<ItemSlot>();

        public class ItemSlot
        {
            public LayerUnit Owner { get; private set; }
            private ItemTemplate data;
            private int count;

            public bool IsEmpty { get { return count == 0 || data == null; } }
            public ItemTemplate Data { get { return data; } }
            public int Count { get { return count; } }

            public ItemSlot(LayerUnit unit)
            {
                this.Owner = unit;
            }

            internal void Sync(ClientStruct.UnitItemStatus syn)
            {
                ItemTemplate item = Owner.Templates.GetItem(syn.ItemTemplateID);
                if (item != null)
                {
                    this.data = item;
                    this.count = syn.Count;
                }
            }

            internal void Set(UnitSyncInventoryItemEvent me)
            {
                if (this.data != null && this.data.ID == me.ItemTemplateID)
                {
                    this.count = me.Count;
                    return;
                }
                ItemTemplate item = Owner.Templates.GetItem(me.ItemTemplateID);
                if (item != null)
                {
                    this.data = item;
                    this.count = me.Count;
                }
            }
        }

        public List<ItemSlot> GetItemSlots()
        {
            return new List<ItemSlot>(mItemSlots);
        }
        public void GetItemSlots(List<ItemSlot> ret)
        {
            ret.AddRange(mItemSlots);
        }

        public ItemSlot GetItem(int index)
        {
            if (index >= 0 && index < mItemSlots.Count)
            {
                return mItemSlots[index];
            }
            return null;
        }

        public ItemSlot GetItemByTemplateID(int itemTemplateID)
        {
            for (int i = mItemSlots.Count - 1; i >= 0; --i)
            {
                ItemSlot slot = mItemSlots[i];
                if (!slot.IsEmpty && slot.Data.ID == itemTemplateID)
                {
                    return slot;
                }
            }
            return null;
        }



    }
}




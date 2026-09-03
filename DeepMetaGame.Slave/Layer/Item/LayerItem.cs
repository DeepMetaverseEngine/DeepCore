using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;

namespace DeepCore.Game3D.Slave.Layer
{
    public class LayerItem : LayerZoneEntity, IZoneItem
    {
        //----------------------------------------------------------------------------------------------------------------------------
        private ItemTemplate _Info;
        private SyncItemInfo _SyncInfo;
        private ItemBuyable _ABuyable;
        private ItemInventory _AInventory;
        private ItemUseable _AUseable;
        private ItemEquip _AEquip;
        private ItemPickable _APickable;
        private ItemResource _AResource;
        private float _ExpireTimeMS;
        private float _TotalTimeMS;
        private object _EventSender;
        public static LayerItem Alloc(ItemTemplate info, SyncItemInfo syn, LayerZone parent, AddItemEvent add)
        {
            return parent.ObjectPool.AllocAutoRelease<LayerItem>().Init(info, syn, parent, add);
        }
        protected LayerItem Init(ItemTemplate temp, SyncItemInfo syn, LayerZone parent, AddItemEvent add)
        {
            var info = syn.template ?? temp;
            this._Info = info;
            base.Init(syn.ObjectID, parent);
            this._EventSender = add?.sender;
            this._SyncInfo = syn;
            this.mRemotePos.X = syn.pos.X;
            this.mRemotePos.Y = syn.pos.Y;
            this.mRemotePos.Z = syn.pos.Z;
            this.mDirection.ForceSync(syn.direction, syn.body_direction);
            {
                this._ABuyable = info.Abilities.GetComponentAs<ItemBuyable>();
                this._AInventory = info.Abilities.GetComponentAs<ItemInventory>();
                this._AUseable = info.Abilities.GetComponentAs<ItemUseable>();
                this._AEquip = info.Abilities.GetComponentAs<ItemEquip>();
                this._APickable = info.Abilities.GetComponentAs<ItemPickable>();
                this._AResource = info.Abilities.GetComponentAs<ItemResource>();
            }
            this._ExpireTimeMS = syn.ItemExpireTimeMS;
            this._TotalTimeMS = syn.ItemTotalTimeMS;
            return this;
        }
        protected override void Disposing()
        {
            base.Disposing();
            this._Info = default;
            this._SyncInfo = default;
            this._ABuyable = default;
            this._AInventory = default;
            this._AUseable = default;
            this._AEquip = default;
            this._APickable = default;
            this._AResource = default;
            this._ExpireTimeMS = default;
            this._TotalTimeMS = default;
            this._EventSender = default;
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public ItemTemplate Template => _Info;
        public ItemTemplate Info => _Info;
        public SyncItemInfo SyncInfo => _SyncInfo;
        public ItemBuyable ABuyable => _ABuyable;
        public ItemInventory AInventory => _AInventory;
        public ItemUseable AUseable => _AUseable;
        public ItemEquip AEquip => _AEquip;
        public ItemPickable APickable => _APickable;
        public ItemResource AResource => _AResource;
        public float ExpireTimeMS => _ExpireTimeMS;
        public float TotalTimeMS => _TotalTimeMS;
        public object EventSender => _EventSender;
        public float PassTimeMS { get { return TotalTimeMS - ExpireTimeMS; } }
        public byte Force { get => SyncInfo.Force; }
        public string Alias { get => SyncInfo.Alias; }

        public override int TemplateID => Info.ID;
        public override string Name { get { return SyncInfo.Name; } }
        public override string DisplayName { get { return Info.Name; } }
        public override float BodyBlockSize { get { return Info.BodySize; } }
        public override float BodyHeight { get { return Info.BodyHeight; } }
        public override bool IsStaticBlock { get { return false; } }
        public override IZoneShape ZoneShape { get => null; }
        public IZoneItem HostObject => _EventSender as IZoneItem;

        protected internal override void OnAdded()
        {
            base.OnAdded();
            //this.mPos.Gravity = Parent.Gravity;
            if (Info.DropEffect != null)
            {
                Parent.PreQueueEvent(ObjectPool.Alloc<AddEffectEvent>().Init(this.ObjectID, mRemotePos.ToGeometry3(), Direction, Info.DropEffect));
            }
            //             if (Parent.TerrainSrc.TryGetHeightByPos(mPos.X, mPos.Y, out var z))
            //             {
            //                 this.mPos.z = z;
            //             }
        }

        internal protected override void DoEvent(ObjectNotify e)
        {
            if (e is ObjectForceSyncPosEvent)
            {
                if (HostObject is IZoneItem hostItem)
                {
                    return;
                }
                var oe = e as ObjectForceSyncPosEvent;
                this.InternalSyncObject(oe);
            }
            else if (e is ObjectForceSyncFaceEvent)
            {
                if (HostObject is IZoneItem hostItem)
                {
                    return;
                }
                var oe = e as ObjectForceSyncFaceEvent;
                this.InternalSyncObject(oe);
            }
        }
        public override void SyncPos(UnitSyncPos pos)
        {
            if (HostObject is IZoneItem hostItem)
            {
                return;
            }
            base.SyncPos(pos);
        }
        protected override void UpdateAI()
        {

        }
        protected override void Update()
        {
            if (HostObject is IZoneItem hostItem)
            {
                this.mRemotePos.Value = hostItem.Position.Value;
                this.mDirection.ForceSync(hostItem.Direction, hostItem.BodyDirection);
            }
            if (ExpireTimeMS > 0)
            {
                this._ExpireTimeMS -= Parent.CurrentIntervalMS;
                if (this._ExpireTimeMS < 0)
                {
                    this._ExpireTimeMS = 0;
                }
            }
        }


    }

}

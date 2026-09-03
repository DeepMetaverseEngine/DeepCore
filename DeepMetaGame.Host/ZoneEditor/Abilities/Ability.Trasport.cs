using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepCore.Game3D.Host.Instance.Abilities
{
    //-------------------------------------------------------------------------------------------

    public class TransportUnitAbility : Ability
    {
        new public UnitTransportAbilityData Data { get => (UnitTransportAbilityData)base.Data; }
        public string NextPositionName { get; private set; }
        public UnitType AcceptUnitType { get; private set; }
        public bool AcceptUnitTypeForAll { get; private set; }
        public byte AcceptForce { get; private set; }
        public bool AcceptForceForAll { get; private set; }
        public LaunchEffect TransportEffect { get; private set; }

        public delegate bool SelectHandler(ZoneRegion region, InstanceUnit obj);
        private SelectHandler mOnSelect;
        public event SelectHandler OnSelect { add { mOnSelect += value; } remove { mOnSelect -= value; } }

        private InstanceFlag mNext;
        private ZoneRegion mOwner;

        public TransportUnitAbility(InstanceZone zone, UnitTransportAbilityData data)
            : base(zone, data)
        {
            this.NextPositionName = data.NextPosition;
            this.AcceptUnitType = data.AcceptUnitType;
            this.AcceptUnitTypeForAll = data.AcceptUnitTypeForAll;
            this.AcceptForce = data.AcceptForce;
            this.AcceptForceForAll = data.AcceptForceForAll;
            this.TransportEffect = data.TransportEffect;
        }
        protected override void OnStart(InstanceAttributes obj)
        {
            ZoneRegion region = obj as ZoneRegion;
            if (region != null)
            {
                InstanceZone zone = region.Parent;
                this.mNext = zone.GetFlag(this.NextPositionName);
                this.mOwner = region;
                if (mNext != null)
                {
                    region.OnUnitEnter += this.onUnitEnter;
                }
            }
        }
        private bool Select(InstanceUnit unit)
        {
            if (unit.AoiStatus == null || Data.AcceptAoiStatus)
            {
                if (!AcceptUnitTypeForAll && unit.UType != this.AcceptUnitType)
                {
                    return false;
                }
                if (!AcceptForceForAll && unit.Force != this.AcceptForce)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private void onUnitEnter(ZoneRegion region, InstanceUnit obj)
        {
            if (Select(obj) && (mOnSelect == null || mOnSelect.Invoke(region, obj)))
            {
                if (TransportEffect != null)
                {
                    mOwner.Parent.PostEvent(mOwner.ObjectPool.Alloc<AddEffectEvent>().Init (obj.ID, region.Position, region.Direction, TransportEffect));
                }
                obj.Transport(mNext.Position);
                obj.ResetAI();
                ZoneRegion rg = mNext as ZoneRegion;
                if (rg != null)
                {
                    rg.addInRegionViewed(obj);
                }
            }
        }

        protected override void Disposing()
        {
            mOnSelect = null;
            base.Disposing();
        }
    }

    //-------------------------------------------------------------------------------------------

    public class TransportSceneAbility : Ability
    {
        new public SceneTransportAbilityData Data { get => (SceneTransportAbilityData)base.Data; }
        public byte AcceptForce { get; private set; }
        public bool AcceptForceForAll { get; private set; }
        public int NextSceneID { get; private set; }
        public string NextScenePosition { get; private set; }
        public LaunchEffect TransportEffect { get; private set; }
        private ZoneRegion mOwner;

        public TransportSceneAbility(InstanceZone zone, SceneTransportAbilityData data)
            : base(zone, data)
        {
            this.AcceptForce = data.AcceptForce;
            this.AcceptForceForAll = data.AcceptForceForAll;
            this.NextSceneID = data.NextSceneID;
            this.NextScenePosition = data.NextScenePosition;
            this.TransportEffect = data.TransportEffect;
        }
        protected override void OnStart(InstanceAttributes obj)
        {
            ZoneRegion region = obj as ZoneRegion;
            if (region != null)
            {
                InstanceZone zone = region.Parent;
                this.mOwner = region;
                region.OnUnitEnter += this.onUnitEnter;
            }
        }
        private bool Select(InstanceUnit unit)
        {
            if (unit.AoiStatus == null || Data.AcceptAoiStatus)
            {
                if (unit is InstancePlayer)
                {
                    if (!AcceptForceForAll && unit.Force != this.AcceptForce)
                    {
                        return false;
                    }
                    var p = unit as InstancePlayer;
                    return p.IsReady;
                }
            }
            return false;
        }

        private void onUnitEnter(ZoneRegion region, InstanceUnit obj)
        {
            if (Select(obj))
            {
                if (TransportEffect != null)
                {
                    mOwner.Parent.PostEvent(mOwner.ObjectPool.Alloc<AddEffectEvent>().Init (obj.ID,  region.Position, region.Direction, TransportEffect));
                }
                Zone.cb_playerTransportScene(obj as InstancePlayer, region, this.NextSceneID, this.NextScenePosition);
            }
        }

    }
}

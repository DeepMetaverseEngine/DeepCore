using System;
using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance;

namespace DeepCore.Game3D.Host.Simple
{

    public class SimpleZoneHostFactory : ZoneHostFactory
    {
        public override IQuestAdapter CreateQuestAdapter(InstanceZone zone)
        {
            return new SimpleQuestAdapter(zone);
        }

        public override HateSystem CreateHateSystem(InstanceUnit owner)
        {
            return SimpleHateSystem.AllocSimple(owner);
        }

        public override ObjectAoiStatus CreateAOI(InstancePlayer player)
        {
            return null;
        }

        //----------------------------------------------------------------------------------------------




        public class SimpleQuestAdapter : IQuestAdapter
        {
            public SimpleQuestAdapter(InstanceZone zone)
                : base(zone)
            {

            }
            public override void DoAcceptQuest(InstancePlayer playerUUID, string quest, string args)
            {

            }
            public override void DoCommitQuest(InstancePlayer playerUUID, string quest, string args)
            {

            }
            public override void DoDropQuest(InstancePlayer playerUUID, string quest, string args)
            {

            }
            public override void DoUpdateQuestStatus(InstancePlayer playerUUID, string quest, string key, string value)
            {
            }
        }

        //----------------------------------------------------------------------------------------------



        public class SimpleHateSystem : HateSystem
        {
            protected SimpleHateSystem() { }
            public static SimpleHateSystem AllocSimple(InstanceUnit owner)
            {
                return AllocSimple(owner, owner.CFG.AI_HATE_SYSTEM_CAPACITY, owner.CFG.AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS);
            }
            public static SimpleHateSystem AllocSimple(InstanceUnit owner, int capacity, int updateIntervalMS)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<SimpleHateSystem>(static s => new SimpleHateSystem());
                ret.Init(owner, capacity, updateIntervalMS);
                return ret;
            }

        }

        //----------------------------------------------------------------------------------------------




        public class SimpleDistanceHateSystem : HateSystem
        {
            protected SimpleDistanceHateSystem() { }
            public static SimpleDistanceHateSystem AllocSimple(InstanceUnit owner)
            {
                return AllocSimple(owner, owner.CFG.AI_HATE_SYSTEM_CAPACITY, owner.CFG.AI_NPC_CHECK_IN_GUARD_LIMIT_TIME_MS);
            }
            public static SimpleDistanceHateSystem AllocSimple(InstanceUnit owner, int capacity, int updateIntervalMS)
            {
                var ret = owner.ObjectPool.AllocOrCreateAutoRelease<SimpleDistanceHateSystem>(static s => new SimpleDistanceHateSystem());
                ret.Init(owner, capacity, updateIntervalMS);
                return ret;
            }

            public override InstanceUnit GetHated()
            {
                base.Sort();
                return base.GetHated();
            }

            protected override int Compare(HateInfo x, HateInfo y)
            {
                float dx = CMath.GetDistanceSquare(x.Unit.X, x.Unit.Y, Owner.X, Owner.Y);
                float dy = CMath.GetDistanceSquare(y.Unit.X, y.Unit.Y, Owner.X, Owner.Y);
                return (int)(dx - dy);
            }
        }


        //----------------------------------------------------------------------------------------------




        //----------------------------------------------------------------------------------------------


    }




}

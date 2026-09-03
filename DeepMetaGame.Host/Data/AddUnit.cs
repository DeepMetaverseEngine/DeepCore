using DeepCore.Game3D.Host.Instance;
using DeepMetaGame.Data.Template;
using DeepCore.Geometry;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Helper;

namespace DeepCore.GameData.Data
{

    public struct AddUnitParam
    {
        /// <summary>模板</summary>
        public UnitInfo template;
        /// <summary>场景中名字(EditorName)</summary>
        public string name;
        /// <summary>玩家UUID</summary>
        public string player_uuid;
        public string displayName;
        /// <summary>坐标</summary>
        public Vector3 pos;
        /// <summary>方向</summary>
        public float direction;
        /// <summary>单位阵营</summary>
        public byte force;
        /// <summary>单位等级</summary>
        public int level;
        /// <summary>召唤者</summary>
        public InstanceUnit summoner;
        /// <summary> 任何自定义数据 </summary>
        public object arg;
        public string alias;

        /// <summary>输出事件，用于广播</summary>
        //public AddUnitEvent out_event;
    }

    public struct AddItemParam
    {
        /// <summary>模板</summary>
        public ItemTemplate template;
        /// <summary>场景中名字(EditorName)</summary>
        public string name;
        /// <summary>坐标</summary>
        public Vector3 pos;
        /// <summary>方向</summary>
        public float direction;
        /// <summary>单位阵营</summary>
        public byte force;
        /// <summary>道具创建者</summary>
        public InstanceUnit creater;
        /// <summary> 任何自定义数据 </summary>
        public object arg;

        /// <summary>输出事件，用于广播</summary>
        //public AddItemEvent out_event;
    }

    public struct AddSpellParam
    {
        /// <summary>模板</summary>
        public SpellTemplate template;
        /// <summary>启动数据</summary>
        public LaunchSpell launch;
        /// <summary>由谁传递过来</summary>
        public InstanceZoneObject sender;
        /// <summary>此法术的最初发起者</summary>
        public InstanceUnit launcher;
        /// <summary>目标单位ID</summary>
        public uint targetObjectID;
        /// <summary>起始坐标</summary>
        public Vector3 startPos;
        /// <summary>目标坐标，用于加农炮指向性类型</summary>
        public Vector3? targetPos;
        /// <summary>方向</summary>
        public float direction;
        /// <summary>法术链</summary>
        public SpellChainContext chain;
        /// <summary> 任何自定义数据 </summary>
        public object arg;
    }

    public struct AddBuffParam
    {
        /// <summary>模板</summary>
        public BuffTemplate template;
        /// <summary>启动数据</summary>
        public LaunchBuff launch;
        /// <summary>发起者</summary>
        public InstanceUnit sender;
        /// <summary>目标</summary>
        public InstanceUnit target;
        /// <summary>是否为被动</summary>
        public bool equip;

        /// <summary>时长</summary>
        public int lifeTimeMS;
        /// <summary>已经过时间</summary>
        public int passTimeMS;
        /// <summary>叠加层数</summary>
        public byte overLayLevel;

        /// <summary> 任何自定义数据 </summary>
        public object arg;
    }

    public struct LaunchSkillParam
    {
        public uint TargetUnitID;
        public bool AutoFocusNearTarget;
        public Vector3? SpellTargetPos;
    }

    public class SpellChainContext : BattleAutoRecycle
    {
        private LaunchSpell srcLaunch;
        private int mSpellID;
        private int mChainLevel;
        private readonly HashMap<uint, InstanceUnit> mChainList = new HashMap<uint, InstanceUnit>();
        private InstanceUnit mLastTarget;
        public int Level
        {
            get { return mChainLevel; }
        }
        public int MaxLevel
        {
            get { return srcLaunch.ChainLevel; }
        }
        public int SpellID
        {
            get { return mSpellID; }
        }
        public InstanceUnit LastTarget
        {
            get { return mLastTarget; }
        }
        public bool HasNextChain
        {
            get => srcLaunch != null && mChainLevel < srcLaunch.ChainLevel;
        }
        public static SpellChainContext Alloc(InstanceZone zone, LaunchSpell launchSpell)
        {
            return zone.ObjectPool.Alloc<SpellChainContext>().Init(launchSpell);
        }
        public SpellChainContext() { }
        public SpellChainContext Init(LaunchSpell launch)
        {
            srcLaunch = launch;
            mSpellID = launch.SpellID;
            mChainLevel = 0;
            return this;
        }
        protected override void Disposing()
        {
            foreach (var v in mChainList.Values)
            {
                v.Release();
            }
            this.mChainList.Clear();
            this.srcLaunch = null;
            this.mLastTarget = null;
            this.mSpellID = 0;
            this.mChainLevel = 0;
        }

        public bool TryLaunch(int spellID)
        {
            if (spellID == SpellID && srcLaunch != null)
            {
                if (mChainLevel < srcLaunch.ChainLevel)
                {
                    mChainLevel++;
                    return true;
                }
                return false;
            }
            //mSpellID = spellID;
            return true;
        }
        public void AddTarget(InstanceUnit target)
        {
            target.Retain();
            mChainList.Put(target.ID, target);
            mLastTarget = target;
        }
        public bool ContainsTarget(InstanceUnit target)
        {
            if (mChainList == null) return false;
            return mChainList.ContainsKey(target.ID);
        }
    }


}

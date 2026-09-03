using DeepCore.GameData.Data;
using DeepCore.IO;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    public struct TAddUnit
    {
        /// <summary>单位模板</summary>
        public UnitInfo info;
        /// <summary>场景中名字(EditorName)</summary>
        public string editor_name;
        /// <summary>玩家UUID</summary>
        public string player_uuid;
        /// <summary>显示名字</summary>
        public string displayName;
        /// <summary>显示名字</summary>
        public string alias;
        /// <summary>单位阵营</summary>
        public byte force;
        /// <summary>单位等级</summary>
        public int level;
        /// <summary>坐标</summary>
        public Geometry.Vector3? pos;
        /// <summary>方向</summary>
        public float direction;
        /// <summary>召唤者</summary>
        public InstanceUnit summoner;
        /// <summary>用于切换场景存储数据</summary>
        public ISerializable last_zone_save_data;

        public IEnumerable<CardSlot> cards;
        /// <summary>用于判断是否是克隆体</summary>
        public bool isDuplicate;
        public object arg;

        public UnitType? overrideType;
    }

    public struct TAddItem
    {
        public ItemTemplate template;
        public string name;
        public string alias;
        public Geometry.Vector3? pos;
        public float direction;
        public byte force;
        public InstanceUnit creater;       
        /// <summary>用于判断是否是克隆体</summary>
        public bool isDuplicate;

        public object arg;
    }

    public struct TAddSpell
    {
        public SpellTemplate template;
        public LaunchSpell launch;
        public InstanceZoneObject sender;
        /// <summary>此法术的最初发起者</summary>
        public InstanceUnit launcher;
        public uint target_obj_id;
        public Geometry.Vector3? startPos;
        public Geometry.Vector3? targetPos;
        public float direction;
        public SpellChainContext chain;
        /// <summary>
        /// 受击产生Spell的单位
        /// </summary>
        public InstanceUnit damage;
        public InstanceUnit.EquipSkill FromSkillTemplateID;
        public InstanceSpell FromSpellUnit;
        /// <summary>用于判断是否是克隆体</summary>
        public bool cloneTemplate;
        /// <summary>
        /// 技能或者Spell或者单位
        /// </summary>
        public object From;
        public object arg;
    }

    public struct TAddBuff
    {
        public BuffTemplate template;
        public InstanceUnit sender;
        public InstanceUnit unit;
        public int buffLevel;
        public float lifeTimeMS;
        public float passTimeMS;
        public int overLayLevel;
        public InstanceUnit.EquipSkill FromSkillID;
        public InstanceUnit.EquipBuff removed;
        public bool? isEquip;
        public object tag;
        /// <summary>用于判断是否是克隆体</summary>
        public bool isDuplicate;
       
    }
}

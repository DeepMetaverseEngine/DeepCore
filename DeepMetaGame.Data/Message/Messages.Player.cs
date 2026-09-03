using DeepCore;
using DeepCore.EventTrigger.Data;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Security.Cryptography;
using static DeepMetaGame.Data.Message.ClientStruct;

namespace DeepMetaGame.Data.Message
{
    [MessageType(BattleConstants.PlayerCDEvent)]
    public class PlayerCDEvent : PlayerNotify
    {
        [XmlSerializable]
        public bool is_all
        {
            get { return BitMask.BitGetMask(mask, 0); }
            set { BitMask.BitSetMask(ref mask, 0, value); }
        }
        [XmlSerializable]
        public bool is_decrease_time
        {
            get { return BitMask.BitGetMask(mask, 1); }
            set { BitMask.BitSetMask(ref mask, 1, value); }
        }
        [XmlSerializable]
        public bool is_decrease_pct
        {
            get { return BitMask.BitGetMask(mask, 2); }
            set { BitMask.BitSetMask(ref mask, 2, value); }
        }
        [XmlSerializable]
        public bool is_clear
        {
            get { return BitMask.BitGetMask(mask, 3); }
            set { BitMask.BitSetMask(ref mask, 3, value); }
        }

        private byte mask = 0;
        public float decrease_timeMS;
        public float decrease_pct;
        public int skill_template_id;
        protected override void OnDisposing(uint objID)
        {
            mask = 0;
            decrease_timeMS = 0;
            decrease_pct = 0;
            skill_template_id = 0;
        }
        public PlayerCDEvent() { }
        public PlayerCDEvent Init(uint unit_id) 
        {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(mask);
            if (is_all)
            {
                if (is_decrease_pct)
                    output.PutF32(decrease_pct);
                if (is_decrease_time)
                    output.PutF32(decrease_timeMS);
            }
            else
            {
                output.PutS32(skill_template_id);
                if (is_decrease_pct)
                    output.PutF32(decrease_pct);
                if (is_decrease_time)
                    output.PutF32(decrease_timeMS);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            mask = input.GetU8();
            if (is_all)
            {
                if (is_decrease_pct)
                    decrease_pct = input.GetF32();
                if (is_decrease_time)
                    decrease_timeMS = input.GetF32();
            }
            else
            {
                skill_template_id = input.GetS32();
                if (is_decrease_pct)
                    decrease_pct = input.GetF32();
                if (is_decrease_time)
                    decrease_timeMS = input.GetF32();
            }
        }
    }

    [MessageType(BattleConstants.PlayerSkillChangedEvent)]
    public class PlayerSkillChangedEvent : PlayerNotify
    {
        public SkillInit baseSkill;
        public readonly List<SkillInit> skills = new List<SkillInit>();
        protected override void OnDisposing(uint objID)
        {
            baseSkill = null;
            skills.Clear();
        }
        public PlayerSkillChangedEvent() { }
        public PlayerSkillChangedEvent Init(uint unit_id) {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutObj(baseSkill);
            output.PutList(skills, static (output, v) => output.PutObj(v));
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            baseSkill = input.GetObj<SkillInit>();
            input.GetList(static input => input.GetObj<SkillInit>(), skills);
        }
    }


    [MessageType(BattleConstants.UnitSyncEnvironmentVarEvent)]
    public class UnitSyncEnvironmentVarEvent : ObjectNotify //其它类型单位也同步
    {
        public ZoneEnvironmentVar Var;
        protected override void OnDisposing(uint objID)
        {
            Var = null;
        }
        public UnitSyncEnvironmentVarEvent() { }
        public UnitSyncEnvironmentVarEvent Init(uint unit_id, ZoneEnvironmentVar var)
        {
            base.object_id = unit_id;
            this.Var = var;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.EncodeExternalizable(Var);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Var = new ZoneEnvironmentVar();
            input.DecodeExternalizable(Var);
        }
    }
    [MessageType(BattleConstants.PlayerSyncEnvironmentVarEvent)]
    public class PlayerSyncEnvironmentVarEvent : PlayerNotify //其它类型单位也同步
    {
        public ZoneEnvironmentVar Var;
        protected override void OnDisposing(uint objID)
        {
            Var = null;
        }
        public PlayerSyncEnvironmentVarEvent() { }
        public PlayerSyncEnvironmentVarEvent Init(uint objid, ZoneEnvironmentVar value) 
        {
            base.object_id = objid;
            this.Var = value;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.EncodeExternalizable(Var);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Var = new ZoneEnvironmentVar();
            input.DecodeExternalizable(Var);
        }
    }

    [MessageType(BattleConstants.PlayerSkillStopEvent)]
    public class PlayerSkillStopEvent : PlayerNotify
    {
        public int SkillID;
        protected override void OnDisposing(uint objID)
        {
            SkillID = default;
        }
        public PlayerSkillStopEvent() { }
        public PlayerSkillStopEvent Init(uint unit_id, int skillID)
        {
            base.object_id = unit_id;
            SkillID = skillID;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(SkillID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            SkillID = input.GetS32();
        }
    }

    [MessageType(BattleConstants.PlayerSkillAddedEvent)]
    public class PlayerSkillAddedEvent : PlayerNotify
    {
        public SkillInit Skill;
        public bool IsDefault;
        protected override void OnDisposing(uint objID)
        {
            Skill = default;
            IsDefault = default;
        }
        public PlayerSkillAddedEvent() { }
        public PlayerSkillAddedEvent Init(uint unit_id, SkillInit sk, bool isDefault)
        {
            base.object_id = unit_id;
            Skill = sk;
            IsDefault = isDefault;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutObj(Skill);
            output.PutBool(IsDefault);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Skill = input.GetObj<SkillInit>();
            IsDefault = input.GetBool();
        }
    }
    [MessageType(BattleConstants.PlayerSkillRemovedEvent)]
    public class PlayerSkillRemovedEvent : PlayerNotify
    {
        public int SkillID;
        protected override void OnDisposing(uint objID)
        {
            SkillID = default;
        }
        public PlayerSkillRemovedEvent() { }
        public PlayerSkillRemovedEvent Init(uint unit_id, int skillID)
        {
            base.object_id = unit_id;
            SkillID = skillID;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(SkillID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            SkillID = input.GetS32();
        }
    }
    [MessageType(BattleConstants.PlayerSkillRefreshEvent)]
    public class PlayerSkillRefreshEvent : PlayerNotify
    {
        public SkillTemplate Skill;
        public int SkillLevel;
        public float PassTime;
        protected override void OnDisposing(uint objID)
        {
            Skill = default;
            SkillLevel = default;
            PassTime = default;
        }
        public PlayerSkillRefreshEvent() { }
        public PlayerSkillRefreshEvent Init(uint unit_id, SkillTemplate sk, int level, float passtime)
        {
            base.object_id = unit_id;
            this.Skill = sk;
            this.SkillLevel = level;
            this.PassTime = passtime;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutObj(Skill);
            output.PutS32(SkillLevel);
            output.PutF32(PassTime);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Skill = input.GetObj<SkillTemplate>();
            SkillLevel = input.GetS32();
            PassTime = input.GetF32();
        }
    }

    /// <summary>
    /// 脚本系统指令
    /// </summary>
    [MessageType(BattleConstants.PlayerScriptCommandEvent)]
    public class PlayerScriptCommandEvent : PlayerNotify
    {
        public string message;
        protected override void OnDisposing(uint objID)
        {
            message = default;
        }
        public PlayerScriptCommandEvent() { }
        public PlayerScriptCommandEvent Init(uint unit_id, string msg)
        {
            base.object_id = unit_id;
            message = msg;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(message);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            message = input.GetUTF();
        }
    }

    /// <summary>
    /// 技能可用性发生变化
    /// </summary>
    [MessageType(BattleConstants.PlayerSkillActiveChangedEvent)]
    public class PlayerSkillActiveChangedEvent : PlayerNotify
    {
        public struct State
        {
            public int SkillTemplateID;
            public SkillActiveState ST;
            public bool IsActive { get { return ST == SkillActiveState.Active; } }
            public bool IsPauseOnDeactive { get { return ST == SkillActiveState.DeactiveAndPause; } }
        }

        public readonly List<State> Skills = new();
        protected override void OnDisposing(uint objID)
        {
            Skills.Clear();
        }
        public PlayerSkillActiveChangedEvent() { }
        public PlayerSkillActiveChangedEvent Init(uint unit_id) 
        {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVS32(Skills.Count);
            for (int i = 0; i < Skills.Count; i++)
            {
                State sat = Skills[i];
                output.PutS32(sat.SkillTemplateID);
                output.PutEnum8(sat.ST);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            int count = input.GetVS32();
            Skills.Clear();
            Skills.Capacity = count;
            for (int i = 0; i < count; i++)
            {
                State sat = new State();
                sat.SkillTemplateID = input.GetS32();
                sat.ST = input.GetEnum8<SkillActiveState>();
                Skills.Add(sat);
            }
        }
    }

    [MessageType(BattleConstants.PlayerSkillTimeChangedEvent)]
    public class PlayerSkillTimeChangedEvent : PlayerNotify
    {
        public int SkillTemplateID;
        public float SkillPassTimeMS;
        public float SkillTotalTimeMS;
        public float SkillCastRate;
        protected override void OnDisposing(uint objID)
        {
            SkillTemplateID = default;
            SkillPassTimeMS = default;
            SkillTotalTimeMS = default;
            SkillCastRate = default;
        }
        public PlayerSkillTimeChangedEvent() { }
        public PlayerSkillTimeChangedEvent Init(uint unit_id, int skillID, float passTimeMS, float totalTimeMS, float skillCastRate)
        {
            base.object_id = unit_id;
            SkillTemplateID = skillID;
            SkillPassTimeMS = passTimeMS;
            SkillTotalTimeMS = totalTimeMS;
            SkillCastRate = skillCastRate;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(SkillTemplateID);
            output.PutF32(SkillPassTimeMS);
            output.PutF32(SkillTotalTimeMS);
            output.PutF32(SkillCastRate);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            SkillTemplateID = input.GetS32();
            SkillPassTimeMS = input.GetF32();
            SkillTotalTimeMS = input.GetF32();
            SkillCastRate = input.GetF32();
        }
    }

    /// <summary>
    /// 锁定目标
    /// </summary>
    [MessageType(BattleConstants.PlayerFocuseTargetEvent)]
    public class PlayerFocuseTargetEvent : PlayerNotify
    {
        public uint targetUnitID;
        public SkillTemplate.CastTarget expectTarget;
        protected override void OnDisposing(uint objID)
        {
            targetUnitID = default;
            expectTarget = default;
        }
        public PlayerFocuseTargetEvent() { }
        public PlayerFocuseTargetEvent Init(uint unit_id, uint target_id, SkillTemplate.CastTarget expect)
        {
            base.object_id = unit_id;
            targetUnitID = target_id;
            expectTarget = expect;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(targetUnitID);
            output.PutEnum(expectTarget);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            targetUnitID = input.GetVU32();
            expectTarget = input.GetEnum<SkillTemplate.CastTarget>();
        }
    }

    [MessageType(BattleConstants.PlayerSyncCardsEvent)]
    public class PlayerSyncCardsEvent : PlayerNotify //其它类型单位也同步
    {
        public readonly HashMap<int, int> ownerFunctions = new HashMap<int, int>();
        protected override void OnDisposing(uint objID)
        {
            ownerFunctions .Clear();
        }
        public PlayerSyncCardsEvent() { }
        public PlayerSyncCardsEvent Init(uint objid, IReadOnlyDictionary<int, int> funcs)
        {
            base.object_id = objid;
            this.ownerFunctions.PutAll(funcs);
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutMap(ownerFunctions,
                (o, v) => o.PutS32(v),
                (o, v) => o.PutS32(v));
        }


        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.GetMap(
                (i) => i.GetS32(),
                (i) => i.GetS32(),
                ownerFunctions);
        }
    }

}


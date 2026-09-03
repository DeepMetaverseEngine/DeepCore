using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Message
{
    public enum UnitFieldMask : uint
    {
        MASK_ALL/*            */= 0xFFFFFFFF,

        MASK_HP/*             */= 0x00000001,
        MASK_MP/*             */= 0x00000002,
        MASK_MAX_HP/*         */= 0x00000004,
        MASK_MAX_MP/*         */= 0x00000008,
        MASK_SPEED/*          */= 0x00000010,
        MASK_SP/*             */= 0x00000020,
        MASK_MAX_SP/*         */= 0x00000040,
        MASK_FCR/*            */= 0x00000080,

        MASK_MONEY/*          */= 0x00000100,
        MASK_LEVEL/*          */= 0x00000200,
        MASK_ZONE_SHAPE/*     */= 0x00000400,
        MASK_GRAVITY/*        */= 0x00000800,
        MASK_CURRENTTARGET/*  */= 0x00001000,
        MASK_FAR/*            */= 0x00002000,
        MASK_PICK_RANGE/*     */= 0x00004000,
        MASK_EXP/*            */= 0x00008000,

        MASK_FMR/*            */= 0x00010000,
        MASK_INVENTORY/*      */= 0x00020000,
        MASK_DISPLAY_NAME/*   */= 0x00040000,
        MASK_DOCKING_OBJ/*    */= 0x00080000,
        MASK_DOCKING_POS/*    */= 0x00100000,
        MASK_BODY_SCALE/*     */= 0x00200000,
        MASK_RES_SCALE/*      */= 0x00400000,
        MASK_PAUSED/*         */= 0x00800000,

        MASK_SKIN/*           */= 0x01000000,
        MASK_AVATAR/*         */= 0x02000000,

        MASK_DUMMY_0/*        */= 0x04000000,
        MASK_DUMMY_1/*        */= 0x08000000,
        MASK_DUMMY_2/*        */= 0x10000000,
        MASK_DUMMY_3/*        */= 0x20000000,
        MASK_DUMMY_4/*        */= 0x40000000,
        MASK_DUMMY_5/*        */= 0x80000000,
    }

    /// <summary>
    /// 同步单位数据，频繁需要改变的数据
    /// </summary>
    [MessageType(BattleConstants.UnitFieldChangedEvent)]
    public class UnitFieldChangedEvent : ObjectNotify
    {
        public UnitFieldMask mask = 0;
        public long currentHP;
        public long currentMP;
        public long maxHP;
        public long maxMP;
        public long currentSP;
        public long maxSP;
        public float currentSpeed;
        public float currentFMR;
        public float currentFCR;
        public float currentFAR;
        public long currentMoney;
        public int level;
        public long exp;
        public int inventorySize;
        public string displayName;
        public IZoneShape zoneShape;
        public float currentGravity;
        public uint currentTarget;
        public float pickRange;
        public uint dockingObj;
        public float bodyScale;
        public float resScale;
        public DockingOffset dockingOffset;
        public bool paused;
        public string skin;
        public string[] avatar;
        public int dummy_0;
        public int dummy_1;
        public int dummy_2;
        public int dummy_3;
        public int dummy_4;
        public int dummy_5;
        public void Clear()
        {
            base.object_id = 0;
            this.mask = 0;
            this.currentHP = default;
            this.currentMP = default;
            this.maxHP = default;
            this.maxMP = default;
            this.currentSP = default;
            this.maxSP = default;
            this.currentSpeed = default;
            this.currentFMR = default;
            this.currentFCR = default;
            this.currentFAR = default;
            this.currentMoney = default;
            this.level = default;
            this.exp = default;
            this.inventorySize = default;
            this.displayName = default;
            this.zoneShape = default;
            this.currentGravity = default;
            this.currentTarget = default;
            this.pickRange = default;
            this.dockingObj = default;
            this.bodyScale = default;
            this.resScale = default;
            this.dockingOffset = default;
            this.paused = default;
            this.skin = default;
            this.avatar = default;
            this.dummy_0 = default;
            this.dummy_1 = default;
            this.dummy_2 = default;
            this.dummy_3 = default;
            this.dummy_4 = default;
            this.dummy_5 = default;
        }
        protected override void OnDisposing(uint objID)
        {
            Clear();
        }
        public UnitFieldChangedEvent() { }
        public UnitFieldChangedEvent Init(uint unit_id, UnitFieldMask mask)
        {
            base.object_id = unit_id;
            this.mask = mask;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32((uint)mask);
            if ((mask & UnitFieldMask.MASK_HP) != 0) output.PutVS64(currentHP);
            if ((mask & UnitFieldMask.MASK_MP) != 0) output.PutVS64(currentMP);
            if ((mask & UnitFieldMask.MASK_MAX_HP) != 0) output.PutVS64(maxHP);
            if ((mask & UnitFieldMask.MASK_MAX_MP) != 0) output.PutVS64(maxMP);
            if ((mask & UnitFieldMask.MASK_SP) != 0) output.PutVS64(currentSP);
            if ((mask & UnitFieldMask.MASK_MAX_SP) != 0) output.PutVS64(maxSP);
            if ((mask & UnitFieldMask.MASK_SPEED) != 0) output.PutF32(currentSpeed);
            if ((mask & UnitFieldMask.MASK_FCR) != 0) output.PutF32(currentFCR);
            if ((mask & UnitFieldMask.MASK_FAR) != 0) output.PutF32(currentFAR);
            if ((mask & UnitFieldMask.MASK_FMR) != 0) output.PutF32(currentFMR);
            if ((mask & UnitFieldMask.MASK_MONEY) != 0) output.PutVS64(currentMoney);
            if ((mask & UnitFieldMask.MASK_LEVEL) != 0) output.PutS32(level);
            if ((mask & UnitFieldMask.MASK_ZONE_SHAPE) != 0) output.PutObj(zoneShape);
            if ((mask & UnitFieldMask.MASK_GRAVITY) != 0) output.PutF32(currentGravity);
            if ((mask & UnitFieldMask.MASK_CURRENTTARGET) != 0) output.PutU32(currentTarget);
            if ((mask & UnitFieldMask.MASK_PICK_RANGE) != 0) output.PutF32(pickRange);
            if ((mask & UnitFieldMask.MASK_EXP) != 0) output.PutVS64(exp);
            if ((mask & UnitFieldMask.MASK_INVENTORY) != 0) output.PutS32(inventorySize);
            if ((mask & UnitFieldMask.MASK_DISPLAY_NAME) != 0) output.PutUTF(displayName);
            if ((mask & UnitFieldMask.MASK_DOCKING_OBJ) != 0) output.PutU32(dockingObj);
            if ((mask & UnitFieldMask.MASK_DOCKING_POS) != 0) output.PutObj(dockingOffset);
            if ((mask & UnitFieldMask.MASK_BODY_SCALE) != 0) output.PutF32(bodyScale);
            if ((mask & UnitFieldMask.MASK_RES_SCALE) != 0) output.PutF32(resScale);
            if ((mask & UnitFieldMask.MASK_PAUSED) != 0) output.PutBool(paused);
            if ((mask & UnitFieldMask.MASK_SKIN) != 0) output.PutUTF(skin);
            if ((mask & UnitFieldMask.MASK_AVATAR) != 0) output.PutUTFArray(avatar);
            if ((mask & UnitFieldMask.MASK_DUMMY_0) != 0) output.PutS32(dummy_0);
            if ((mask & UnitFieldMask.MASK_DUMMY_1) != 0) output.PutS32(dummy_1);
            if ((mask & UnitFieldMask.MASK_DUMMY_2) != 0) output.PutS32(dummy_2);
            if ((mask & UnitFieldMask.MASK_DUMMY_3) != 0) output.PutS32(dummy_3);
            if ((mask & UnitFieldMask.MASK_DUMMY_4) != 0) output.PutS32(dummy_4);
            if ((mask & UnitFieldMask.MASK_DUMMY_5) != 0) output.PutS32(dummy_5);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            mask = (UnitFieldMask)input.GetVU32();
            if ((mask & UnitFieldMask.MASK_HP) != 0) currentHP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_MP) != 0) currentMP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_MAX_HP) != 0) maxHP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_MAX_MP) != 0) maxMP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_SP) != 0) currentSP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_MAX_SP) != 0) maxSP = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_SPEED) != 0) currentSpeed = input.GetF32();
            if ((mask & UnitFieldMask.MASK_FCR) != 0) currentFCR = input.GetF32();
            if ((mask & UnitFieldMask.MASK_FAR) != 0) currentFAR = input.GetF32();
            if ((mask & UnitFieldMask.MASK_FMR) != 0) currentFMR = input.GetF32();
            if ((mask & UnitFieldMask.MASK_MONEY) != 0) currentMoney = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_LEVEL) != 0) level = input.GetS32();
            if ((mask & UnitFieldMask.MASK_ZONE_SHAPE) != 0) zoneShape = input.GetObj<IZoneShape>();
            if ((mask & UnitFieldMask.MASK_GRAVITY) != 0) currentGravity = input.GetF32();
            if ((mask & UnitFieldMask.MASK_CURRENTTARGET) != 0) currentTarget = input.GetU32();
            if ((mask & UnitFieldMask.MASK_PICK_RANGE) != 0) pickRange = input.GetF32();
            if ((mask & UnitFieldMask.MASK_EXP) != 0) exp = input.GetVS64();
            if ((mask & UnitFieldMask.MASK_INVENTORY) != 0) inventorySize = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DISPLAY_NAME) != 0) displayName = input.GetUTF();
            if ((mask & UnitFieldMask.MASK_DOCKING_OBJ) != 0) dockingObj = input.GetU32();
            if ((mask & UnitFieldMask.MASK_DOCKING_POS) != 0) dockingOffset = input.GetObj<DockingOffset>();
            if ((mask & UnitFieldMask.MASK_BODY_SCALE) != 0) bodyScale = input.GetF32();
            if ((mask & UnitFieldMask.MASK_RES_SCALE) != 0) resScale = input.GetF32();
            if ((mask & UnitFieldMask.MASK_PAUSED) != 0) paused = input.GetBool();
            if ((mask & UnitFieldMask.MASK_SKIN) != 0) skin = input.GetUTF();
            if ((mask & UnitFieldMask.MASK_AVATAR) != 0) avatar = input.GetUTFArray();
            if ((mask & UnitFieldMask.MASK_DUMMY_0) != 0) dummy_0 = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DUMMY_1) != 0) dummy_1 = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DUMMY_2) != 0) dummy_2 = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DUMMY_3) != 0) dummy_3 = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DUMMY_4) != 0) dummy_4 = input.GetS32();
            if ((mask & UnitFieldMask.MASK_DUMMY_5) != 0) dummy_5 = input.GetS32();
        }
    }

    /// <summary>
    /// 单位进入吟唱状态
    /// </summary>
    [MessageType(BattleConstants.UnitChantSkillEvent)]
    public class UnitChantSkillEvent : ObjectNotify
    {
        public int skill_id;
        public float chant_ms;
        protected override void OnDisposing(uint objID)
        {
            skill_id = 0;
            chant_ms = 0;
        }
        public UnitChantSkillEvent() { }
        public UnitChantSkillEvent Init(uint unit_id, SkillTemplate sk)
        {
            base.object_id = unit_id;
            skill_id = sk.ID;
            chant_ms = sk.ChantTimeMS;
            return this;

        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(skill_id);
            output.PutF32(chant_ms);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            skill_id = input.GetS32();
            chant_ms = input.GetF32();
        }
    }


    /// <summary>
    /// 单位进入技能状态
    /// </summary>
    [MessageType(BattleConstants.UnitLaunchSkillEvent)]
    public class UnitLaunchSkillEvent : ObjectNotify
    {
        public int skill_id;
        public int skill_level;
        public Vector3 start_pos;
        public float start_dir;
        /// <summary>
        /// 如果是单一动作，则标识是哪一段攻击
        /// </summary>
        public byte action_index;
        /// <summary>
        /// 技能每段动作的时间，单一动作的话(SkillTemplate.IsSingleAction)，长度为1
        /// </summary>
        public readonly List<float> action_time_array = new();
        public float TotalCDTimeMS;
        public float fast_action_rate;
        public float SkillCastRate;
        public Vector3? spell_target_pos;
        public uint target_object_id;
        private BitSet8 bitMask = new BitSet8();
        protected override void OnDisposing(uint objID)
        {
            this.skill_id = default;
            this.skill_level = default;
            this.start_pos = default;
            this.start_dir = default;
            this.action_index = default;
            this.action_time_array.Clear();
            this.TotalCDTimeMS = default;
            this.fast_action_rate = default;
            this.SkillCastRate = default;
            this.spell_target_pos = default;
            this.target_object_id = default;
            this.bitMask.Clear();
        }

        [XmlSerializable]
        public bool IsSingleAction
        {
            get { return bitMask.Get(0); }
            private set { bitMask.Set(0, value); }
        }
        [XmlSerializable]
        public bool IsCastSpeedUP
        {
            get { return bitMask.Get(1); }
            private set { bitMask.Set(1, value); }
        }
        [XmlSerializable]
        public bool IsActionSpeedUP
        {
            get { return bitMask.Get(2); }
            private set { bitMask.Set(2, value); }
        }
        [XmlSerializable]
        public bool IsActionTimeChanged
        {
            get { return bitMask.Get(3); }
            private set { bitMask.Set(3, value); }
        }
        [XmlSerializable]
        public bool IsAutoFocusNearTarget
        {
            get { return bitMask.Get(4); }
            private set { bitMask.Set(4, value); }
        }
        [XmlSerializable]
        public bool IsSpellTargetPos
        {
            get { return bitMask.Get(5); }
            private set { bitMask.Set(5, value); }
        }
        [XmlSerializable]
        public bool IsTargetObject
        {
            get { return bitMask.Get(6); }
            private set { bitMask.Set(6, value); }
        }
        [XmlSerializable]
        public bool IsChangeTotalCDTime
        {
            get { return bitMask.Get(7); }
            private set { bitMask.Set(7, value); }
        }

        public float TotalActionTimeMS
        {
            get
            {
                float total = 0;
                if (action_time_array != null)
                {
                    foreach (float timeMS in action_time_array)
                    {
                        total += timeMS;
                    }
                }
                return total;
            }
        }

        public UnitLaunchSkillEvent() { }
        public UnitLaunchSkillEvent Init(
            uint unit_id,
            SkillTemplate sk,
            int skill_level,
            byte actionIndex,
            float far,
            float fcr,
            float totalCDTimeMS,
            bool isAutoFaceToTarget,
            Vector3? spellTargetPos,
            uint targetObjectID)
        {
            base.object_id = unit_id;
            skill_id = sk.ID;
            fast_action_rate = far;
            SkillCastRate = fcr;
            TotalCDTimeMS = totalCDTimeMS;
            spell_target_pos = spellTargetPos;
            target_object_id = targetObjectID;

            IsAutoFocusNearTarget = isAutoFaceToTarget;
            IsSingleAction = sk.IsSingleAction;
            IsActionSpeedUP = far != 1f;
            IsCastSpeedUP = fcr != 1f;
            IsTargetObject = targetObjectID != 0;
            IsSpellTargetPos = spellTargetPos != null && !spellTargetPos.Value.IsNaN;
            IsChangeTotalCDTime = totalCDTimeMS != sk.CoolDownMS;
            action_time_array.Clear();
            if (sk.IsSingleAction)
            {
                action_index = actionIndex;
                action_time_array.Add(sk.ActionQueue[actionIndex].TotalTimeMS);
            }
            else
            {
                action_index = 0;
                sk.ActionQueueTimeArray(action_time_array);
            }
            return this;
        }

        public override void BeforeWrite(TemplateManager templates)
        {
            base.BeforeWrite(templates);
            var orgin_temp = templates.GetSkill(skill_id);
            if (action_time_array == null)
            {
                IsActionTimeChanged = false;
            }
            else if (IsSingleAction)
            {
                IsActionTimeChanged = action_time_array[0] != orgin_temp.ActionQueue[action_index].TotalTimeMS;
            }
            else
            {
                IsActionTimeChanged = !CUtils.ListEqual(action_time_array, orgin_temp.ActionQueue, static (a, b) => a == b.TotalTimeMS);
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            base.EndRead(templates);
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(skill_id);
            output.PutU8(bitMask.Mask);
            output.WritePosAndDirection(start_pos, start_dir);

            if (IsSingleAction)
            {
                output.PutU8(action_index);
                if (IsActionTimeChanged) output.PutF32(action_time_array[0]);
            }
            else
            {
                if (IsActionTimeChanged) output.PutList(action_time_array, static (o, v) => o.PutF32(v));
            }
            if (IsActionSpeedUP)
            {
                output.PutF32(fast_action_rate);
            }
            if (IsCastSpeedUP)
            {
                output.PutF32(SkillCastRate);
            }
            if (IsChangeTotalCDTime)
            {
                output.PutF32(TotalCDTimeMS);
            }
            if (IsTargetObject)
            {
                output.PutVU32(target_object_id);
            }
            if (IsSpellTargetPos)
            {
                var pos = spell_target_pos.Value;
                output.WritePos(in pos);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            skill_id = input.GetS32();
            bitMask.Mask = input.GetU8();
            input.ReadPosAndDirection(out start_pos, out start_dir);

            if (IsSingleAction)
            {
                action_index = input.GetU8();
                if (IsActionTimeChanged)
                {
                    action_time_array.Add(input.GetF32());
                }
            }
            else
            {
                if (IsActionTimeChanged)
                {
                    input.GetList(static input => input.GetF32(), action_time_array);
                }
            }
            if (IsActionSpeedUP)
            {
                fast_action_rate = input.GetF32();
            }
            else
            {
                fast_action_rate = 1f;
            }
            if (IsCastSpeedUP)
            {
                SkillCastRate = input.GetF32();
            }
            else
            {
                SkillCastRate = 1f;
            }
            if (IsChangeTotalCDTime)
            {
                TotalCDTimeMS = input.GetF32();
            }
            if (IsTargetObject)
            {
                target_object_id = input.GetVU32();
            }
            if (IsSpellTargetPos)
            {
                input.ReadPos(out Vector3 pos);
                spell_target_pos = pos;
            }

        }
        public override string ToString()
        {
            return string.Format("LaunchSkill: {0}@{1}", skill_id, action_index);
        }
    }

    /// <summary>
    /// 单位动作中击中别的单位
    /// </summary>
    [MessageType(BattleConstants.UnitEffectEvent)]
    public class UnitEffectEvent : ObjectNotify
    {
        private uint effect_sn = 0;
        [XmlSerializable] public LaunchEffect effect;
        protected override void OnDisposing(uint objID)
        {
            effect_sn = 0;
            effect = null;
        }
        public UnitEffectEvent() { }
        public UnitEffectEvent Init(uint unit_id, LaunchEffect effect)
        {
            this.object_id = unit_id;
            this.effect = effect;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(effect_sn);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            effect_sn = input.GetVU32();
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            if (effect != null)
            {
                effect_sn = effect.SerialNumber;
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            effect = templates.GetSnData<LaunchEffect>(effect_sn);
        }
    }

    /// <summary>
    /// 单位仅做一个动作
    /// </summary>
    [MessageType(BattleConstants.UnitDoActionEvent)]
    public class UnitDoActionEvent : ObjectNotify
    {
        public UnitActionStatus Main;
        public string Sub;
        public string ActionName;
        protected override void OnDisposing(uint objID)
        {
            Main = default;
            Sub = default;
            ActionName = default;
        }
        public UnitDoActionEvent() { }
        public UnitDoActionEvent Init(uint unit_id, UnitActionStatus main, string sub, string actionName)
        {
            base.object_id = unit_id;
            Main = main;
            Sub = sub;
            ActionName = actionName;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutEnum8(Main);
            output.PutUTF(Sub);
            output.PutUTF(ActionName);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Main = input.GetEnum8<UnitActionStatus>();
            Sub = input.GetUTF();
            ActionName = input.GetUTF();
        }
    }

    public struct UnitHitArgs
    {
        public bool isDead;
        public bool isCritical;
        public bool HasEffect;
        public bool IsHitted;
        public bool HasSourceAttack;
        public bool HasExtendsResult;
        public LaunchEffect effect;
        public AttackProp SourceAttack;
        public ISerializable ExtendsResult;
        public uint AttackerID;
        public object Attacker;
        public long hp;
        public string client_state;
        public byte CustomData;//新增简化数据（用于处理 元素类型，闪避等效果）
    }
    /// <summary>
    /// 单位被击中
    /// </summary>
    [MessageType(BattleConstants.UnitHitEvent)]
    public class UnitHitEvent : ObjectNotify
    {
        [XmlSerializable]
        public bool isDead
        {
            get { return BitMask.BitGetMask(bitMask, 0); }
            set { BitMask.BitSetMask(ref bitMask, 0, value); }
        }
        [XmlSerializable]
        public bool isCritical
        {
            get { return BitMask.BitGetMask(bitMask, 2); }
            set { BitMask.BitSetMask(ref bitMask, 2, value); }
        }
        [XmlSerializable]
        public bool HasEffect
        {
            get { return BitMask.BitGetMask(bitMask, 3); }
            private set { BitMask.BitSetMask(ref bitMask, 3, value); }
        }

        /// <summary>
        /// 是否命中
        /// </summary>
        [XmlSerializable]
        public bool IsHitted
        {
            get { return BitMask.BitGetMask(bitMask, 4); }
            set { BitMask.BitSetMask(ref bitMask, 4, value); }
        }
        [XmlSerializable]
        public bool HasSourceAttack
        {
            get { return BitMask.BitGetMask(bitMask, 6); }
            private set { BitMask.BitSetMask(ref bitMask, 6, value); }
        }
        [XmlSerializable]
        public bool HasExtendsResult
        {
            get { return BitMask.BitGetMask(bitMask, 7); }
            private set { BitMask.BitSetMask(ref bitMask, 7, value); }
        }


        [XmlSerializable]
        public LaunchEffect effect
        {
            get { return biteffect; }
            set
            {
                biteffect = value;
                HasEffect = value != null;
            }
        }
        [XmlSerializable]
        public AttackProp SourceAttack
        {
            get { return sourceAttack; }
            set
            {
                sourceAttack = value;
                HasSourceAttack = value != null;
            }
        }
        [XmlSerializable]
        public ISerializable ExtendsResult
        {
            get { return extendsResult; }
            set
            {
                extendsResult = value;
                HasExtendsResult = value != null;
            }
        }
        [XmlSerializable]
        public uint AttackerID { get; private set; }
        public object Attacker { get; private set; }
        public long hp;
        public string client_state;
        public byte CustomData;//新增简化数据（用于处理 元素类型，闪避等效果）
        private byte bitMask = 0;
        private LaunchEffect biteffect;
        private uint biteffect_sn;
        private AttackProp sourceAttack;
        private uint sourceAttack_sn;
        private ISerializable extendsResult;

        public UnitHitArgs ToArgs()
        {
            return new UnitHitArgs()
            {
                isDead = this.isDead,
                isCritical = this.isCritical,
                HasEffect = this.HasEffect,
                IsHitted = this.IsHitted,
                HasSourceAttack = this.HasSourceAttack,
                HasExtendsResult = this.HasExtendsResult,
                effect = this.effect,
                SourceAttack = this.SourceAttack,
                ExtendsResult = this.ExtendsResult,
                AttackerID = this.AttackerID,
                Attacker = this.Attacker,
                hp = this.hp,
                client_state = this.client_state,
                CustomData = this.CustomData,
            };
        }

        protected override void OnDisposing(uint objID)
        {
            this.hp = default;
            this.client_state = default;
            this.CustomData = default;//新增简化数据（用于处理 元素类型，闪避等效果）
            this.bitMask = 0;
            this.biteffect = default;
            this.biteffect_sn = default;
            this.sourceAttack = default;
            this.sourceAttack_sn = default;
            this.extendsResult = default;
            this.AttackerID = 0;
            this.Attacker = default;
        }
        public UnitHitEvent() { }
        public UnitHitEvent Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
        public void SetAttacker(uint attackerID, object attacker)
        {
            AttackerID = attackerID;
            Attacker = attacker;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(AttackerID);
            output.PutVS64(hp);
            output.PutUTF(client_state);
            output.PutU8(bitMask);
            output.PutU8(CustomData);

            if (HasEffect)
            {
                output.PutVU32(biteffect_sn);
            }
            if (HasSourceAttack)
            {
                output.PutVU32(sourceAttack_sn);
            }
            if (HasExtendsResult)
            {
                output.PutObj(extendsResult);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            AttackerID = input.GetVU32();
            hp = input.GetVS64();
            client_state = input.GetUTF();
            bitMask = input.GetU8();
            CustomData = input.GetU8();
            if (HasEffect)
            {
                biteffect_sn = input.GetVU32();
            }
            if (HasSourceAttack)
            {
                sourceAttack_sn = input.GetVU32();
            }
            if (HasExtendsResult)
            {
                extendsResult = input.GetObjAny() as ISerializable;
            }
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            if (HasEffect)
            {
                biteffect_sn = biteffect.SerialNumber;
            }
            if (HasSourceAttack)
            {
                sourceAttack_sn = sourceAttack.SerialNumber;
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            if (HasEffect)
            {
                biteffect = templates.GetSnData<LaunchEffect>(biteffect_sn);
            }
            if (HasSourceAttack)
            {
                sourceAttack = templates.GetSnData<AttackProp>(sourceAttack_sn);
            }
        }
    }


    [MessageType(BattleConstants.UnitHitMoveEvent)]
    public class UnitHitMoveEvent : ObjectNotify
    {
        private byte bitMask = 0;

        private float direction;
        private float rotateSpeedSEC;
        private float expectlTimeMS;
        private float moveSpeedSEC;
        private float moveSpeedAdd;
        private float moveSpeedAcc;

        private float moveZSpeed;
        private float gravity;

        private uint targetID;
        private bool targetBodyBlock;
        private float bodyKeepRange;
        protected override void OnDisposing(uint objID)
        {
            bitMask = 0;
            direction = 0;
            rotateSpeedSEC = 0;
            expectlTimeMS = 0;
            moveSpeedSEC = 0;
            moveSpeedAdd = 0;
            moveSpeedAcc = 0;

            moveZSpeed = 0;
            gravity = 0;

            targetID = 0;
            targetBodyBlock = false;
            bodyKeepRange = 0;
        }
        //--------------------------------------------------------------------------------------------------------------------------
        public bool isRotate { get { return BitMask.BitGetMask(bitMask, 5); } set { BitMask.BitSetMask(ref bitMask, 5, value); } }
        public bool isSpeedAdd { get { return BitMask.BitGetMask(bitMask, 0); } set { BitMask.BitSetMask(ref bitMask, 0, value); } }
        public bool isSpeedAcc { get { return BitMask.BitGetMask(bitMask, 1); } set { BitMask.BitSetMask(ref bitMask, 1, value); } }
        public bool isNoneTouch { get { return BitMask.BitGetMask(bitMask, 2); } set { BitMask.BitSetMask(ref bitMask, 2, value); } }
        public bool hasFly { get { return BitMask.BitGetMask(bitMask, 3); } set { BitMask.BitSetMask(ref bitMask, 3, value); } }
        public bool hasTarget { get { return BitMask.BitGetMask(bitMask, 4); } set { BitMask.BitSetMask(ref bitMask, 4, value); } }
        //--------------------------------------------------------------------------------------------------------------------------
        public float Direction { get => direction; }
        public float RotateSpeedSEC { get => rotateSpeedSEC; }
        public float ExpectlTimeMS { get => expectlTimeMS; }
        public float MoveSpeedSEC { get => moveSpeedSEC; }
        public float MoveSpeedAdd { get => moveSpeedAdd; }
        public float MoveSpeedAcc { get => moveSpeedAcc; }
        //--------------------------------------------------------------------------------------------------------------------------
        public float MoveZSpeed { get => moveZSpeed; }
        public float Gravity { get => gravity; }
        //--------------------------------------------------------------------------------------------------------------------------
        public uint TargetID { get => targetID; }
        public bool TargetBodyBlock { get => targetBodyBlock; }
        public float TargetBodyKeepRange { get => bodyKeepRange; }
        //--------------------------------------------------------------------------------------------------------------------------
        public UnitHitMoveEvent() { }
        public UnitHitMoveEvent Init(
            uint launcherID,
            float direction,
            float rotateSpeedSEC,
            float expectlTimeMS,
            float moveSpeedSEC,
            float moveSpeedAdd,
            float moveSpeedAcc,
            bool isNoneTouch)
        {
            base.object_id = launcherID;
            this.direction = direction;
            this.rotateSpeedSEC = rotateSpeedSEC;
            this.expectlTimeMS = expectlTimeMS;
            this.moveSpeedSEC = moveSpeedSEC;
            this.moveSpeedAdd = moveSpeedAdd;
            this.moveSpeedAcc = moveSpeedAcc;

            this.isRotate = rotateSpeedSEC != 0;
            this.isSpeedAdd = moveSpeedAdd != 0;
            this.isSpeedAcc = moveSpeedAcc != 0;
            this.isNoneTouch = isNoneTouch;
            return this;
        }
        public void SetFly(float moveZSpeed, float gravity)
        {
            this.hasFly = true;
            this.moveZSpeed = moveZSpeed;
            this.gravity = gravity;
        }
        public void SetMoveTarget(uint targetID, bool targetBodyBlock, float bodyKeepRange = 0)
        {
            this.hasTarget = true;
            this.targetID = targetID;
            this.targetBodyBlock = targetBodyBlock;
            this.bodyKeepRange = bodyKeepRange;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(bitMask);
            output.PutF32(direction);
            if (isRotate) output.PutF32(rotateSpeedSEC);
            output.PutF32(expectlTimeMS);
            output.PutF32(moveSpeedSEC);
            if (isSpeedAdd) output.PutF32(moveSpeedAdd);
            if (isSpeedAcc) output.PutF32(moveSpeedAcc);
            if (hasFly)
            {
                output.PutF32(moveZSpeed);
                output.PutF32(gravity);
            }
            if (hasTarget)
            {
                output.PutU32(targetID);
                output.PutBool(targetBodyBlock);
                output.PutF32(bodyKeepRange);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.bitMask = input.GetU8();
            this.direction = input.GetF32();
            if (isRotate) this.rotateSpeedSEC = input.GetF32();
            this.expectlTimeMS = input.GetF32();
            this.moveSpeedSEC = input.GetF32();
            if (isSpeedAdd) this.moveSpeedAdd = input.GetF32();
            if (isSpeedAcc) this.moveSpeedAcc = input.GetF32();
            if (hasFly)
            {
                this.moveZSpeed = input.GetF32();
                this.gravity = input.GetF32();
            }
            if (hasTarget)
            {
                this.targetID = input.GetU32();
                this.targetBodyBlock = input.GetBool();
                this.bodyKeepRange = input.GetF32();
            }
        }
    }

    /// <summary>
    /// 单位被击中
    /// </summary>
    [MessageType(BattleConstants.UnitDeadEvent)]
    public class UnitDeadEvent : ObjectNotify
    {
        public bool Crushed;
        public uint attacker_id;
        public float DeadTimeMS;
        protected override void OnDisposing(uint objID)
        {
            Crushed = false;
            attacker_id = 0;
            DeadTimeMS = 0;
        }
        public UnitDeadEvent() { }
        public UnitDeadEvent Init(uint unit_id, uint attacker_id, bool crushed, float deadTimeMS)
        {
            base.object_id = unit_id;
            Crushed = crushed;
            this.attacker_id = attacker_id;
            DeadTimeMS = deadTimeMS;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutBool(Crushed);
            output.PutVU32(attacker_id);
            output.PutF32(DeadTimeMS);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Crushed = input.GetBool();
            attacker_id = input.GetVU32();
            DeadTimeMS = input.GetF32();
        }
    }

    /// <summary>
    /// 单位中BUFF
    /// </summary>
    [MessageType(BattleConstants.UnitLaunchBuffEvent)]
    public class UnitLaunchBuffEvent : ObjectNotify
    {
        [XmlSerializable]
        public bool IsEquip
        {
            get { return BitMask.BitGetMask(bitMask, 0); }
            private set { BitMask.BitSetMask(ref bitMask, 0, value); }
        }
        [XmlSerializable]
        public bool IsOverlayLevel
        {
            get { return BitMask.BitGetMask(bitMask, 1); }
            private set { BitMask.BitSetMask(ref bitMask, 1, value); }
        }
        [XmlSerializable]
        public bool IsLevel
        {
            get { return BitMask.BitGetMask(bitMask, 2); }
            private set { BitMask.BitSetMask(ref bitMask, 2, value); }
        }
        [XmlSerializable]
        public bool HasTemplate
        {
            get { return BitMask.BitGetMask(bitMask, 3); }
            private set { BitMask.BitSetMask(ref bitMask, 3, value); }
        }
        private byte bitMask = 0;
        public int buffTemplateID;
        public float buffTimeMS;
        public int buffLevel;
        public float passTimeMS;
        public int overlayLevel;
        public uint senderID;
        public BuffTemplate template;
        protected override void OnDisposing(uint objID)
        {
            bitMask = 0;
            buffTemplateID = 0;
            buffTimeMS = 0;
            buffLevel = 0;
            passTimeMS = 0;
            overlayLevel = 0;
            senderID = 0;
            template = null;
        }
        public UnitLaunchBuffEvent() { }
        public UnitLaunchBuffEvent Init(uint unit_id, int buffID, uint senderID, float timeMS, bool equip, int buffLevel, int overlayLevel, float passTimeMS)
        {
            base.object_id = unit_id;
            buffTemplateID = buffID;
            buffTimeMS = timeMS;
            this.buffLevel = buffLevel;
            this.passTimeMS = passTimeMS;
            this.senderID = senderID;
            IsEquip = equip;
            this.overlayLevel = overlayLevel;
            if (buffLevel != 0)
            {
                IsLevel = true;
            }
            if (overlayLevel > 0)
            {
                IsOverlayLevel = true;
            }
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            HasTemplate = template != null;
            base.WriteExternal(output);
            output.PutU8(bitMask);
            output.PutS32(buffTemplateID);
            output.PutVU32(senderID);
            if (IsLevel)
            {
                output.PutVS32(buffLevel);
            }
            if (!IsEquip)
            {
                output.PutF32(buffTimeMS);
                output.PutF32(passTimeMS);
            }
            if (IsOverlayLevel)
            {
                output.PutVS32(overlayLevel);
            }
            if (HasTemplate)
            {
                output.PutSer(template);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            bitMask = input.GetU8();
            buffTemplateID = input.GetS32();
            senderID = input.GetVU32();
            if (IsLevel)
            {
                buffLevel = input.GetVS32();
            }
            if (!IsEquip)
            {
                buffTimeMS = input.GetF32();
                passTimeMS = input.GetF32();
            }
            if (IsOverlayLevel)
            {
                overlayLevel = input.GetVS32();
            }
            if (HasTemplate)
            {
                template = input.GetObj<BuffTemplate>();
            }
        }
    }

    /// <summary>
    /// 单位停止BUFF
    /// </summary>
    [MessageType(BattleConstants.UnitStopBuffEvent)]
    public class UnitStopBuffEvent : ObjectNotify
    {

        public const byte EndResult_ByTimeUp = 1;//"time_up";
        public const byte EndResult_ByReplaced = 2;//"replaced";
        public const byte EndResult_ByClientRemoved = 3;// "client_removed";
        public const byte EndResult_ByCatgoryExclusive = 4;//"catgory_exclusive";
        public const byte EndResult_ByCode = 5;//"code";
        public const byte EndResult_ByDead = 6;//"dead";
        public const byte EndResult_ByReset = 7;//"reset"
        public const byte EndResult_OnlyRemove = 8;//"onlyRemove"
        public const byte EndResult_BySkill = 9;//"skillRemove"
        public const byte EndResult_OutAura = 10;//"aura";

        public int buffTemplateID;
        public uint senderID;
        public byte result;
        protected override void OnDisposing(uint objID)
        {
            buffTemplateID = 0;
            senderID = 0;
            result = 0;
        }
        public UnitStopBuffEvent() { }
        public UnitStopBuffEvent Init(uint unit_id, int buffID, uint senderID, byte result)
        {
            base.object_id = unit_id;
            this.buffTemplateID = buffID;
            this.senderID = senderID;
            this.result = result;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(buffTemplateID);
            output.PutVU32(senderID);
            output.PutU8(result);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            buffTemplateID = input.GetS32();
            senderID = input.GetVU32();
            result = input.GetU8();
        }
    }


    /// <summary>
    /// 锁定目标
    /// </summary>
    [MessageType(BattleConstants.UnitSyncBuffEvent)]
    public class UnitSyncBuffEvent : ObjectNotify
    {
        public ClientStruct.UnitBuffStatus sync;
        protected override void OnDisposing(uint objID)
        {
            sync = default;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            sync.WriteExternal(output);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            sync.ReadExternal(input);
        }
    }

    [MessageType(BattleConstants.UnitSyncInventoryItemEvent)]
    public class UnitSyncInventoryItemEvent : ObjectNotify, ActorMessage
    {
        public int ItemTemplateID;
        public int Index;
        public int Count;
        protected override void OnDisposing(uint objID)
        {
            ItemTemplateID = 0;
            Index = 0;
            Count = 0;
        }
        public UnitSyncInventoryItemEvent() { }
        public UnitSyncInventoryItemEvent Init(uint unit_id, int itemID, int index, int count)
        {
            base.object_id = unit_id;
            ItemTemplateID = itemID;
            Index = index;
            Count = count;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(ItemTemplateID);
            output.PutVS32(Index);
            output.PutVS32(Count);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ItemTemplateID = input.GetS32();
            Index = input.GetVS32();
            Count = input.GetVS32();
        }
    }


    [MessageType(BattleConstants.UnitUseItemEvent)]
    public class UnitUseItemEvent : ObjectNotify, ActorMessage
    {
        public int ItemTemplateID;
        protected override void OnDisposing(uint objID)
        {
            ItemTemplateID = 0;
        }
        public UnitUseItemEvent() { }
        public UnitUseItemEvent Init(uint unit_id, int itemID)
        {
            base.object_id = unit_id;
            ItemTemplateID = itemID;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(ItemTemplateID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ItemTemplateID = input.GetS32();
        }
    }

    [MessageType(BattleConstants.UnitSyncMultiTimeLine)]
    public class UnitSyncMultiTimeLine : ObjectNotify
    {
        public readonly List<bool> timelines = new();
        protected override void OnDisposing(uint objID)
        {
            timelines.Clear();
        }
        public UnitSyncMultiTimeLine() { }
        public UnitSyncMultiTimeLine Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
        public bool Update(List<MultiTimeLine> tlines)
        {
            bool ret = false;
            if (timelines.Count != tlines.Count)
            {
                CUtils.SetListSize(timelines, tlines.Count);
                ret = true;
            }
            for (int i = tlines.Count - 1; i >= 0; --i)
            {
                if (timelines[i] != tlines[i].Enable)
                {
                    timelines[i] = tlines[i].Enable;
                    ret = true;
                }
            }
            return ret;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutList(timelines, static (output, v) => output.PutBool(v));
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.GetList(static input => input.GetBool(), timelines);
        }
    }

    /// <summary>
    /// 单位复活
    /// </summary>
    [MessageType(BattleConstants.UnitRebirthEvent)]
    public class UnitRebirthEvent : ObjectNotify
    {
        protected override void OnDisposing(uint objID)
        {

        }
        public UnitRebirthEvent() { }
        public UnitRebirthEvent Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
    }
    public struct UnitDamageArgs
    {
        public bool HasDamageTime;
        public bool HasKnockDown;
        public bool HasMove;
        public AttackProp Source;
        public float DamageTimeMS;
        public UnitHitMoveEvent HitMove;

        public float TotalTimeMS { get { return DamageTimeMS; } }
        public string DamageActionName { get { return Source.DamageActionName; } }
        public bool HasDamageAction { get { return !string.IsNullOrEmpty(Source.DamageActionName); } }
    }
    /// <summary>
    /// 单位受攻击
    /// </summary>
    [MessageType(BattleConstants.UnitDamageEvent)]
    public class UnitDamageEvent : ObjectNotify
    {
        [XmlSerializable]
        public bool HasDamageTime
        {
            get { return mask.Get(0); }
            private set { mask.Set(0, value); }
        }
        [XmlSerializable]
        public bool HasKnockDown
        {
            get { return mask.Get(3); }
            private set { mask.Set(3, value); }
        }
        [XmlSerializable]
        public bool HasMove
        {
            get { return mask.Get(5); }
            set { mask.Set(5, value); }
        }

        [XmlSerializable]
        public AttackProp Source
        {
            get => source;
            set
            {
                if (value != null)
                {
                    source_sn = value.SerialNumber;
                }
                source = value;
            }
        }

        /// <summary>
        /// 总共受击时间（位移时间+受击时间）
        /// </summary>
        [XmlSerializable]
        public float TotalTimeMS
        {
            get { return DamageTimeMS; }
        }
        /// <summary>
        /// 特殊受击动作
        /// </summary>
        [XmlSerializable]
        public string DamageActionName
        {
            get { return Source.DamageActionName; }
        }
        /// <summary>
        /// 受击的特殊动作
        /// </summary>
        [XmlSerializable]
        public bool HasDamageAction
        {
            get { return !string.IsNullOrEmpty(Source.DamageActionName); }
        }
        private BitSet8 mask = new BitSet8();
        private AttackProp source;
        private uint source_sn = 0;
        public float DamageTimeMS = 0;
        public UnitHitMoveEvent HitMove;

        public UnitDamageArgs ToArgs()
        {
            return new UnitDamageArgs()
            {
                HasDamageTime = this.HasDamageTime,
                HasKnockDown = this.HasKnockDown,
                HasMove = this.HasMove,
                Source = this.Source,
                DamageTimeMS = this.DamageTimeMS,
                HitMove = this.HitMove,
            };
        }
        protected override void OnDisposing(uint objID)
        {
            mask.Clear();
            source = null;
            source_sn = 0;
            DamageTimeMS = 0;
            HitMove?.Dispose();
            HitMove = null;
        }
        public UnitDamageEvent() { }
        public UnitDamageEvent Init(uint unit_id, float damageTimeMS, bool knockDown, AttackProp source, UnitHitMoveEvent hitMove)
        {
            base.object_id = unit_id;
            this.Source = source;
            this.DamageTimeMS = damageTimeMS;
            this.HitMove = hitMove;
            if (hitMove != null)
            {
                this.DamageTimeMS += hitMove.ExpectlTimeMS;
            }
            HasDamageTime = DamageTimeMS != 0;
            HasMove = hitMove != null;
            HasKnockDown = knockDown;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(mask.Mask);
            output.PutVU32(source_sn);
            if (HasDamageTime)
            {
                output.PutF32(DamageTimeMS);
            }
            if (HasMove)
            {
                output.PutExt(HitMove);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            mask.Mask = input.GetU8();
            source_sn = input.GetVU32();
            if (HasDamageTime)
            {
                DamageTimeMS = input.GetF32();
            }
            if (HasMove)
            {
                HitMove = input.GetExt<UnitHitMoveEvent>();
            }
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            if (Source != null)
            {
                source_sn = Source.SerialNumber;
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            if (source_sn != 0)
            {
                Source = templates.GetSnData<AttackProp>(source_sn);
            }
        }
    }

    /// <summary>
    /// 技能动作序列被意外取消
    /// </summary>
    [MessageType(BattleConstants.UnitSkillActionChangeEvent)]
    public class UnitSkillActionChangeEvent : ObjectNotify
    {
        public byte ActionIndex;
        protected override void OnDisposing(uint objID)
        {
            ActionIndex = 0;
        }
        public UnitSkillActionChangeEvent() { }
        public UnitSkillActionChangeEvent Init(uint unit_id, byte index)
        {
            base.object_id = unit_id;
            ActionIndex = index;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(ActionIndex);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ActionIndex = input.GetU8();
        }
    }


    /// <summary>
    /// 单位开始检取道具读条
    /// </summary>
    [MessageType(BattleConstants.UnitStartPickObjectEvent)]
    public class UnitStartPickObjectEvent : ObjectNotify
    {
        public float PickTimeMS;
        public uint PickObjectID;
        public string PickStatus;
        protected override void OnDisposing(uint objID)
        {
            PickTimeMS = 0;
            PickObjectID = 0;
            PickStatus = default;
        }
        public UnitStartPickObjectEvent() { }
        public UnitStartPickObjectEvent Init(uint unit_id, float pickTimeMS, uint pickObjID, string status)
        {
            base.object_id = unit_id;
            PickTimeMS = pickTimeMS;
            PickObjectID = pickObjID;
            PickStatus = status;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(PickTimeMS);
            output.PutVU32(PickObjectID);
            output.PutUTF(PickStatus);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            PickTimeMS = input.GetF32();
            PickObjectID = input.GetVU32();
            PickStatus = input.GetUTF();
        }
    }

    [MessageType(BattleConstants.UnitStopPickObjectEvent)]
    public class UnitStopPickObjectEvent : ObjectNotify
    {
        public string StopReason;
        protected override void OnDisposing(uint objID)
        {
            StopReason = default;
        }
        public UnitStopPickObjectEvent() { }
        public UnitStopPickObjectEvent Init(uint unit_id, string reason)
        {
            base.object_id = unit_id;
            StopReason = reason;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(StopReason);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            StopReason = input.GetUTF();
        }
    }

    /// <summary>
    /// 单位从场景中获得道具
    /// </summary>
    [MessageType(BattleConstants.UnitGotZoneItemEvent)]
    public class UnitGotZoneItemEvent : ObjectNotify, ActorMessage
    {
        public uint ItemObjectID;
        protected override void OnDisposing(uint objID)
        {
            ItemObjectID = 0;
        }
        public UnitGotZoneItemEvent() { }
        public UnitGotZoneItemEvent Init(uint unit_id, uint pickObjID)
        {
            base.object_id = unit_id;
            ItemObjectID = pickObjID;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(ItemObjectID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ItemObjectID = input.GetVU32();
        }
    }

    /// <summary>
    /// 别的单位释放技能跳起
    /// </summary>
    [MessageType(BattleConstants.UnitJumpEvent)]
    public class UnitJumpEvent : ObjectNotify
    {
        public float ZSpeed;
        public float Gravity;
        protected override void OnDisposing(uint objID)
        {
            ZSpeed = 0;
            Gravity = 0;
        }
        public UnitJumpEvent() { }
        public UnitJumpEvent Init(uint unit_id, float zspeed, float gravity)
        {
            base.object_id = unit_id;
            ZSpeed = zspeed;
            Gravity = gravity;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(ZSpeed);
            output.PutF32(Gravity);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.ZSpeed = input.GetF32();
            this.Gravity = input.GetF32();
        }
    }

    /// <summary>
    /// 立即同步客户端坐标，比如单位传送之类，用于MoveByClient的模式
    /// </summary>
    [MessageType(BattleConstants.UnitForceSyncPosEvent)]
    public class UnitForceSyncPosEvent : ObjectNotify
    {
        public Vector3 Position;
        public float Direction;
        public float BodyDirection;
        public byte UnitMainState;
        public string UnitSubState;
        public float LayerUpward;
        protected override void OnDisposing(uint objID)
        {
            Position = Vector3.Zero;
            Direction = 0;
            BodyDirection = 0;
            UnitMainState = 0;
            UnitSubState = default;
            LayerUpward = 0;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WritePosAndDirection(in Position, Direction);
            output.WriteDirection(BodyDirection);
            output.PutU8(UnitMainState);
            output.PutUTF(UnitSubState);
            output.PutF32(LayerUpward);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.ReadPosAndDirection(out Position, out Direction);
            input.ReadDirection(out BodyDirection);
            UnitMainState = input.GetU8();
            UnitSubState = input.GetUTF();
            LayerUpward = input.GetF32();
        }
    }

    /// <summary>
    /// 立即同步客户端坐标，比如单位传送之类，用于MoveByClient的模式
    /// </summary>
    [MessageType(BattleConstants.ObjectForceSyncPosEvent)]
    public class ObjectForceSyncPosEvent : ObjectNotify
    {
        public Vector3 Pos;
        public float Direction;
        public float BodyDirection;
        protected override void OnDisposing(uint objID)
        {
            Pos = default;
            Direction = default;
            BodyDirection = default;
        }
        public ObjectForceSyncPosEvent() { }
        public ObjectForceSyncPosEvent Init(uint object_id, Vector3 pos, float d, float bd)
        {
            base.object_id = object_id;
            Pos = pos;
            Direction = d;
            BodyDirection = bd;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WritePosAndDirection(in Pos, Direction);
            output.WriteDirection(BodyDirection);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.ReadPosAndDirection(out Pos, out Direction);
            input.ReadDirection(out BodyDirection);
        }
    }

    /// <summary>
    /// 立即同步客户端坐标，比如单位传送之类，用于MoveByClient的模式
    /// </summary>
    [MessageType(BattleConstants.ObjectForceSyncFaceEvent)]
    public class ObjectForceSyncFaceEvent : ObjectNotify
    {
        public float Direction;
        public float BodyDirection;
        protected override void OnDisposing(uint objID)
        {
            Direction = 0;
            BodyDirection = 0;
        }
        public ObjectForceSyncFaceEvent() { }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WriteDirection(Direction);
            output.WriteDirection(BodyDirection);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Direction = input.ReadDirection();
            BodyDirection = input.ReadDirection();
        }
    }

    /// <summary>
    /// 立即同步客户端坐标，比如单位传送之类，用于MoveByClient的模式
    /// </summary>
    [MessageType(BattleConstants.UnitForceSyncStateEvent)]
    public class UnitForceSyncStateEvent : ObjectNotify
    {
        public byte UnitMainState;
        public string UnitSubState;
        protected override void OnDisposing(uint objID)
        {
            UnitMainState = 0;
            UnitSubState = default;
        }
        public UnitForceSyncStateEvent() { }
        public UnitForceSyncStateEvent Init(uint unit_id, byte state, string sub_state)
        {
            base.object_id = unit_id;
            UnitMainState = state;
            UnitSubState = sub_state;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(UnitMainState);
            output.PutUTF(UnitSubState);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            UnitMainState = input.GetU8();
            UnitSubState = input.GetUTF();
        }
    }


    /// <summary>
    /// Missile类法术，锁定单位时触发
    /// </summary>
    [MessageType(BattleConstants.SpellLockTargetEvent)]
    public class SpellLockTargetEvent : ObjectNotify, PositionMessage
    {
        public uint target_obj_id;
        public DeepCore.Geometry.Vector3 pos;
        protected override void OnDisposing(uint objID)
        {
            target_obj_id = 0;
            pos = default;
        }
        public SpellLockTargetEvent() { }
        public SpellLockTargetEvent Init(uint spell_id, uint target_obj_id, in DeepCore.Geometry.Vector3 pos)
        {
            base.object_id = spell_id;
            this.target_obj_id = target_obj_id;
            this.pos = pos;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(target_obj_id);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            target_obj_id = input.GetVU32();
        }
        public DeepCore.Geometry.Vector3 Position => pos;
    }
    /// <summary>
    /// Missile类法术，锁定单位时触发
    /// </summary>
    [MessageType(BattleConstants.SpellSyncEvent)]
    public class SpellSyncEvent : ObjectNotify, PositionMessage
    {
        public bool IsHit;
        public bool IsFin;
        public float dir;
        public DeepCore.Geometry.Vector3 pos;
        public double passTimeMS;
        public float speed;
        protected override void OnDisposing(uint objID)
        {
            IsHit = false;
            IsFin = false;
            pos = default(Vector3);
            dir = default(float);
        }
        public SpellSyncEvent() { }
        public SpellSyncEvent Init(uint spell_id, in DeepCore.Geometry.Vector3 pos, float dir, bool hit, bool fin, double passTimeMS, float speed)
        {
            base.object_id = spell_id;
            this.pos = pos;
            this.dir = dir;
            this.IsHit = hit;
            this.IsFin = fin;
            this.passTimeMS = passTimeMS;
            this.speed = speed;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutBool(IsHit);
            output.PutBool(IsFin);
            output.PutF64(passTimeMS);
            output.WritePosAndDirection(pos, dir);
            output.PutF32(speed);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            IsHit = input.GetBool();
            IsFin = input.GetBool();
            passTimeMS = input.GetF64();
            input.ReadPosAndDirection(out pos, out dir);
            speed = input.GetF32();
        }
        public DeepCore.Geometry.Vector3 Position => pos;
    }


    /// <summary>
    /// 单位中BUFF
    /// </summary>
    [MessageType(BattleConstants.UnitLaunchAuraEvent)]
    public class UnitLaunchAuraEvent : ObjectNotify
    {
        public int auraTemplateID;
        public float auraTimeMS;
        public float passTimeMS;
        public float range;
        protected override void OnDisposing(uint objID)
        {
            auraTemplateID = default;
            auraTimeMS = default;
            passTimeMS = default;
            range = default;
        }
        public UnitLaunchAuraEvent() { }
        public UnitLaunchAuraEvent Init(uint unit_id, int auraID, float timeMS, float passTimeMS, float range)
        {
            base.object_id = unit_id;
            auraTemplateID = auraID;
            auraTimeMS = timeMS;
            this.passTimeMS = passTimeMS;
            this.range = range;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(auraTemplateID);
            output.PutF32(auraTimeMS);
            output.PutF32(passTimeMS);
            output.PutF32(range);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            auraTemplateID = input.GetS32();
            auraTimeMS = input.GetF32();
            passTimeMS = input.GetF32();
            range = input.GetF32();
        }
    }

    /// <summary>
    /// 单位停止BUFF
    /// </summary>
    [MessageType(BattleConstants.UnitStopAuraEvent)]
    public class UnitStopAuraEvent : ObjectNotify
    {
        public int auraTemplateID;
        protected override void OnDisposing(uint objID)
        {
            auraTemplateID = default;
        }
        public UnitStopAuraEvent() { }
        public UnitStopAuraEvent Init(uint unit_id, int auraID)
        {
            base.object_id = unit_id;
            auraTemplateID = auraID;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(auraTemplateID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            auraTemplateID = input.GetS32();
        }
    }


    [MessageType(BattleConstants.UnitVisibleChangedEvent)]
    public class UnitVisibleChangedEvent : ObjectNotify
    {
        public IUnitVisibleData data;
        protected override void OnDisposing(uint objID)
        {
            data = null;
        }
        public UnitVisibleChangedEvent() { }
        public UnitVisibleChangedEvent Init(uint unit_id, IUnitVisibleData data)
        {
            base.object_id = unit_id;
            this.data = data;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutObj(data);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            data = input.GetObjAny() as IUnitVisibleData;
        }
    }

    [MessageType(BattleConstants.ComponentFieldChangeEvent)]
    public class ComponentFieldChangeEvent : ObjectNotify
    {
        public readonly BitSetFields Fields = new();
        public int ComponentTag;
        protected override void OnDisposing(uint objID)
        {
            Fields.Clear();
            ComponentTag = default;
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(ComponentTag);
            Fields.WriteExternal(output);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ComponentTag = input.GetS32();
            Fields.Clear();// = new BitSetFields();
            Fields.ReadExternal(input);
        }

    }


    /// <summary>
    /// 单位技能时间改变
    /// </summary>
    [MessageType(BattleConstants.ObjectSkillTimeChangedEvent)]
    public class ObjectSkillTimeChangedEvent : ObjectNotify
    {
        public int SkillTemplateID;
        public float SkillPassTimeMS;
        public float SkillTotalTimeMS;
        public float SkillCastRate;
        protected override void OnDisposing(uint objID)
        {
            SkillTemplateID = 0;
            SkillPassTimeMS = 0;
            SkillTotalTimeMS = 0;
            SkillCastRate = 0;
        }
        public ObjectSkillTimeChangedEvent() { }
        public ObjectSkillTimeChangedEvent Init(uint unit_id, int skillID, float passTimeMS, float totalTimeMS, float skillCastRate)
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
}
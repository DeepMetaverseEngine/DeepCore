using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneGeometry;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace DeepMetaGame.Data.Message
{
    /// <summary>
    /// 单位待机
    /// </summary>
    [MessageType(BattleConstants.UnitGuardAction)]
    public class UnitGuardAction : ObjectAction
    {
        private BitSet8 bits;
        [XmlSerializable] public bool guard { get { return bits.Get(0); } set { bits.Set(0, value); } }
        [XmlSerializable] public bool follow { get { return bits.Get(1); } set { bits.Set(1, value); } }
        public string reason = null;

        protected override void OnDisposing(uint objID)
        {
            bits.Clear();
            reason = null;
        }
        public UnitGuardAction() { }
        public UnitGuardAction Init(uint unit_id, bool guard)
        {
            base.object_id = unit_id;
            this.guard = guard;
            return this;
        }
        public UnitGuardAction Init(uint unit_id, bool guard, string reason)
        {
            base.object_id = unit_id;
            this.guard = guard;
            this.reason = reason;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(bits.Mask);
            output.PutUTF(reason);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            bits.Mask = input.GetU8();
            reason = input.GetUTF();
        }
    }

    /// <summary>
    /// A过去
    /// </summary>
    [MessageType(BattleConstants.UnitAttackToAction)]
    public class UnitAttackToAction : ObjectAction
    {
        public Vector3? target;
        public bool attack;
        public string name;
        protected override void OnDisposing(uint objID)
        {
            target = default;
            attack = false;
            name = default;
        }
        public UnitAttackToAction() { }
        public UnitAttackToAction Init(uint unit_id, Vector3? target, string name, bool attack)
        {
            base.object_id = unit_id;
            this.target = target;
            this.name = name;
            this.attack = attack;
            return this;
        }
        public override string ToString()
        {
            return $"AttackTo:{name}:{target}:{attack}";
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutNullable(target, static (o, t) => o.PutStruct(t));
            output.PutBool(attack);
            output.PutUTF(name);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            target = input.GetNullable(static (i) => i.GetStruct<Vector3>());
            attack = input.GetBool();
            name = input.GetUTF();
        }
    }
    /// <summary>
    /// 玩家选择朝向
    /// </summary>
    [MessageType(BattleConstants.UnitFaceToAction)]
    public class UnitFaceToAction : ObjectAction
    {
        public float Direction;
        protected override void OnDisposing(uint objID)
        {
            Direction = default;
        }
        public UnitFaceToAction() { }
        public UnitFaceToAction Init(uint unit_id, float d)
        {
            base.object_id = unit_id;
            Direction = d;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(Direction);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Direction = input.GetF32();
        }
    }

    /// <summary>
    /// 单位移动
    /// </summary>
    [MessageType(BattleConstants.UnitJumpAction)]
    public class UnitJumpAction : ObjectAction
    {
        public float Direction;
        public float MoveSpeed;
        public float? ZSpeed;
        protected override void OnDisposing(uint objID)
        {
            Direction = default;
            MoveSpeed = default;
            ZSpeed = default;
        }
        public UnitJumpAction() { }
        public UnitJumpAction Init(uint unit_id, float direction, float moveSpeed, float? zspeed)
        {
            base.object_id = unit_id;
            Direction = direction;
            MoveSpeed = moveSpeed;
            ZSpeed = zspeed;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(Direction);
            output.PutF32(MoveSpeed);
            output.PutNullable(ZSpeed, static (o, v) => o.PutF32(v));
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Direction = input.GetF32();
            MoveSpeed = input.GetF32();
            ZSpeed = input.GetNullable(static i => i.GetF32());
        }
    }

    /// <summary>
    /// 客户端手动控制同步包
    /// </summary>
    [MessageType(BattleConstants.UnitStopMoveAction)]
    public class UnitStopMoveAction : ObjectAction
    {
        protected override void OnDisposing(uint objID)
        {

        }
        public UnitStopMoveAction() { }
        public UnitStopMoveAction Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
        }
    }


    /// <summary>
    /// 锁定目标
    /// </summary>
    [MessageType(BattleConstants.UnitFocuseTargetAction)]
    public class UnitFocuseTargetAction : ObjectAction
    {
        public uint targetUnitID;
        protected override void OnDisposing(uint objID)
        {
            targetUnitID = default;
        }
        public UnitFocuseTargetAction() { }
        public UnitFocuseTargetAction Init(uint unit_id, uint target_id)
        {
            base.object_id = unit_id;
            targetUnitID = target_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU32(targetUnitID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            targetUnitID = input.GetU32();
        }
    }

    /// <summary>
    /// 单位技能动作
    /// </summary>
    [MessageType(BattleConstants.UnitLaunchSkillRequest)]
    public class UnitLaunchSkillRequest : ActorRequest
    {
        [XmlSerializable]
        public bool IsAutoFocusNearTarget
        {
            get { return mask.Get(0); }
            set { mask.Set(0, value); }
        }
        [XmlSerializable]
        private bool IsTargetID
        {
            get { return mask.Get(1); }
            set { mask.Set(1, value); }
        }
        [XmlSerializable]
        private bool IsTargetPos
        {
            get { return mask.Get(2); }
            set { mask.Set(2, value); }
        }
        [XmlSerializable]
        private bool IsSummonID
        {
            get { return mask.Get(3); }
            set { mask.Set(3, value); }
        }
        [XmlSerializable]
        private bool IsLaunchArgs
        {
            get { return mask.Get(4); }
            set { mask.Set(4, value); }
        }

        [XmlSerializable]
        private bool HasLaunchTime
        {
            get { return mask.Get(5); }
            set { mask.Set(5, value); }
        }

        [XmlSerializable]
        private bool HasRelatedPet
        {
            get { return mask.Get(6); }
            set { mask.Set(6, value); }
        }
        [XmlSerializable]
        private bool HasLaunchTag
        {
            get { return mask.Get(7); }
            set { mask.Set(7, value); }
        }
        private BitSet8 mask = new BitSet8();
        public int SkillID;
        public uint TargetObjID = 0;
        public Vector3? SpellTargetPos = null;
        public int SummonID = 0;
        public string LaunchArgs = null;
        public double LaunchTimeMS = 0;
        public uint RelatedPetId = 0;//关联宠物ID
        public ISerializable LaunchTag;
        protected override void OnDisposing(uint objID)
        {
            mask.Clear();
            SkillID = default;
            TargetObjID = default;
            SpellTargetPos = default;
            SummonID = default;
            LaunchArgs = null;
            LaunchTimeMS = 0;
            RelatedPetId = default;
            LaunchTag = null;
        }
        public UnitLaunchSkillRequest() { }
        public UnitLaunchSkillRequest(
            uint unit_id,
            int skill_id,
            bool autoFocuseTarget = false,
            uint targetObjID = 0,
            Vector3? spellTargetPos = null)
        {
            base.object_id = unit_id;
            SkillID = skill_id;
            TargetObjID = targetObjID;
            SpellTargetPos = spellTargetPos;
            IsAutoFocusNearTarget = autoFocuseTarget;
        }
        override public void WriteExternal(IOutputStream output)
        {
            IsTargetID = TargetObjID != 0;
            IsTargetPos = SpellTargetPos != null;
            IsSummonID = SummonID != 0;
            IsLaunchArgs = LaunchArgs != null;
            HasLaunchTime = (LaunchTimeMS != 0);
            HasRelatedPet = (RelatedPetId != 0);
            HasLaunchTag = (LaunchTag != null);

            base.WriteExternal(output);
            mask.WriteExternal(output);
            output.PutS32(SkillID);
            if (IsTargetID)
            {
                output.PutU32(TargetObjID);
            }
            if (IsTargetPos)
            {
                output.PutF32(SpellTargetPos.Value.X);
                output.PutF32(SpellTargetPos.Value.Y);
                output.PutF32(SpellTargetPos.Value.Z);
            }
            if (IsSummonID)
            {
                output.PutS32(SummonID);
            }
            if (IsLaunchArgs)
            {
                output.PutUTF(LaunchArgs);
            }
            if (HasLaunchTime)
            {
                output.PutF64(LaunchTimeMS);
            }

            if (HasRelatedPet)
            {
                output.PutU32(RelatedPetId);
            }
            if (HasLaunchTag)
            {
                output.PutObj(LaunchTag);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            mask.ReadExternal(input);
            SkillID = input.GetS32();

            if (IsTargetID)
            {
                TargetObjID = input.GetU32();
            }
            if (IsTargetPos)
            {
                SpellTargetPos = new Vector3(
                    input.GetF32(),
                    input.GetF32(),
                    input.GetF32());
            }
            if (IsSummonID)
            {
                SummonID = input.GetS32();
            }
            if (IsLaunchArgs)
            {
                LaunchArgs = input.GetUTF();
            }

            if (HasLaunchTime)
            {
                LaunchTimeMS = input.GetF64();
            }

            if (HasRelatedPet)
            {
                RelatedPetId = input.GetU32();
            }
            if (HasLaunchTag)
            {
                LaunchTag = input.GetObjAny() as ISerializable;
            }
        }
    }

    [MessageType(BattleConstants.UnitLaunchSkillResponse)]
    public class UnitLaunchSkillResponse : ActorResponse
    {
        public bool IsLaunched = true;
        protected override void OnDisposing(uint objID)
        {
            IsLaunched = true;
        }
        public UnitLaunchSkillResponse() { }
        public UnitLaunchSkillResponse(uint unit_id, bool isLaunched)
        {
            base.object_id = unit_id;
            IsLaunched = isLaunched;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutBool(IsLaunched);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            IsLaunched = input.GetBool();
        }
    }

    [MessageType(BattleConstants.UnitCancelSkillRequest)]
    public class UnitCancelSkillRequest : ActorRequest
    {
        public int SkillID;
        protected override void OnDisposing(uint objID)
        {
            SkillID = 0;
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
    /// <summary>
    /// 单位手动检取场景道具
    /// </summary>
    [MessageType(BattleConstants.UnitPickObjectAction)]
    public class UnitPickObjectAction : ObjectAction
    {
        public uint PickableObjectID;
        protected override void OnDisposing(uint objID)
        {
            PickableObjectID = 0;
        }
        public UnitPickObjectAction() { }
        public UnitPickObjectAction Init(uint unit_id, uint item_obj_id)
        {
            base.object_id = unit_id;
            PickableObjectID = item_obj_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVU32(PickableObjectID);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            PickableObjectID = input.GetVU32();
        }
    }
    /// <summary>
    /// 单位手动检取场景道具
    /// </summary>
    [MessageType(BattleConstants.UnitStopPickObjectAction)]
    public class UnitStopPickObjectAction : ObjectAction
    {
        public string reason;
        protected override void OnDisposing(uint objID)
        {
            reason = default;
        }
        public UnitStopPickObjectAction() { }
        public UnitStopPickObjectAction Init(uint unit_id, string reason)
        {
            base.object_id = unit_id;
            this.reason = reason;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(reason);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            reason = input.GetUTF();
        }
    }

    /// <summary>
    /// 单位使用道具
    /// </summary>
    [MessageType(BattleConstants.UnitUseItemAction)]
    public class UnitUseItemAction : ObjectAction
    {
        public int Index;
        public int Count;
        protected override void OnDisposing(uint objID)
        {
            Index = default;
            Count = default;
        }
        public UnitUseItemAction() { }
        public UnitUseItemAction Init(uint unit_id, int index, int count = 1)
        {
            base.object_id = unit_id;
            Index = index;
            Count = count;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVS32(Index);
            output.PutVS32(Count);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Index = input.GetVS32();
            Count = input.GetVS32();
        }
    }

    /// <summary>
    /// 客户端手动控制同步包
    /// </summary>
    [MessageType(BattleConstants.UnitAxisAction)]
    public class UnitAxisAction : ObjectAction
    {
        //public UnitActionStatus st = UnitActionStatus.Move;
        public float angle;
        public float distanceRate;
        public float faceto;
        public string subState;
        protected override void OnDisposing(uint objID)
        {
            angle = default;
            distanceRate = default;
            faceto = default;
            subState = default;
        }

        public UnitAxisAction() { }
        public UnitAxisAction Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WritePos(distanceRate);
            //output.PutEnum8(st);
            //if (st == UnitActionStatus.Move)
            if (distanceRate != 0)
            {
                output.WriteDirection(angle);
            }
            output.WriteDirection(faceto);
            output.PutUTF(subState);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.ReadPos(out distanceRate);
            //this.st = input.GetEnum8<UnitActionStatus>();
            //if (st == UnitActionStatus.Move)
            if (distanceRate != 0)
            {
                input.ReadDirection(out angle);
            }
            input.ReadDirection(out faceto);
            subState = input.GetUTF();
        }
    }

    /// <summary>
    /// 客户端手动控制同步包
    /// </summary>
    [MessageType(BattleConstants.UnitCustomAxisAction)]
    public class UnitCustomAxisAction : ObjectAction
    {
        //public UnitActionStatus st = UnitActionStatus.Move;
        public float angle;
        public float distanceRate;
        public float faceto;
        public string subState;
        protected override void OnDisposing(uint objID)
        {
            angle = default;
            distanceRate = default;
            faceto = default;
            subState = default;
        }
        public UnitCustomAxisAction() { }
        public UnitCustomAxisAction Init(uint unit_id)
        {
            base.object_id = unit_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WritePos(distanceRate);
            //output.PutEnum8(st);
            //if (st == UnitActionStatus.Move)
            if (distanceRate != 0)
            {
                output.WriteDirection(angle);
            }
            output.WriteDirection(faceto);
            output.PutUTF(subState);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.ReadPos(out distanceRate);
            if (distanceRate != 0)
            {
                input.ReadDirection(out angle);
            }
            input.ReadDirection(out faceto);
            subState = input.GetUTF();
        }
    }


    /// <summary>
    /// 单位移动
    /// </summary>
    [MessageType(BattleConstants.UnitUpdatePosAction)]
    public class UnitUpdatePosAction : ObjectAction
    {
        public Vector3? pos;
        public float? direction;
        public float? bodyDirection;
        public UnitActionStatus? mainState;
        public string subst;
        protected override void OnDisposing(uint objID)
        {
            pos = default;
            direction = default;
            bodyDirection = default;
            mainState = default;
            subst = default;
        }
        public UnitUpdatePosAction() { }
        public UnitUpdatePosAction Init(uint unit_id, Vector3? pos, float? d, float? bd, UnitActionStatus? st, string subst = null)
        {
            base.object_id = unit_id;
            this.pos = pos;
            this.direction = d;
            this.bodyDirection = bd;
            this.mainState = st;
            this.subst = subst;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutNullable(pos, static (output, v) => output.WritePos(v));
            output.PutNullable(direction, static (output, d) => output.WriteDirection(d));
            output.PutNullable(bodyDirection, static (output, v) => output.WriteDirection(v));
            output.PutNullable(mainState, static (output, v) => output.PutEnum8(v));
            //             output.WritePosAndDirection(in pos, d);
            //             output.WritePosAndDirection(in pos, d);
            //             output.WritePosAndDirection(in pos, d);
            output.PutUTF(subst);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            this.pos = input.GetNullable(static (input) => input.ReadPos3D());
            this.direction = input.GetNullable(static (input) => input.ReadDirection());
            this.bodyDirection = input.GetNullable(static (input) => input.ReadDirection());
            this.mainState = input.GetNullable(static (input) => input.GetEnum8<UnitActionStatus>());
            //           input.ReadPosAndDirection(out pos, out direction);
            //             input.ReadDirection(out bodyDirection);
            //mainState = input.GetEnum8<UnitActionStatus>();
            subst = input.GetUTF();
        }

        public bool DataEquals(UnitUpdatePosAction o)
        {
            if (o == null) return false;
            return o.pos == pos && o.direction == direction && o.bodyDirection == bodyDirection && o.mainState == mainState && o.subst == subst;
        }


        public static bool VectorEqual(in Vector3 a, in Vector3 b, float epsilon)
        {
            //float epsilon = MoveHelper.GetDistance(this.Parent.CurrentIntervalMS, mMinStepSEC);
            if (Math.Abs(a.X - b.X) > epsilon ||
               Math.Abs(a.Y - b.Y) > epsilon ||
               Math.Abs(a.Z - b.Z) > epsilon
               )
            {
                return false;
            }

            return true;
        }
        public static bool FloatEqual(float a, float b, float epsilon)
        {
            // float epsilon = MoveHelper.GetDistance(this.Parent.CurrentIntervalMS, mMinStepSEC);

            return Math.Abs(a - b) < epsilon;
        }

    }

    [MessageType(BattleConstants.UnitSetSyncModeAction)]
    public class UnitSetSyncModeAction : ObjectAction
    {
        public SyncMode Mode;
        protected override void OnDisposing(uint objID)
        {
            Mode = default;
        }
        public UnitSetSyncModeAction() { }
        public UnitSetSyncModeAction Init(uint unit_id, SyncMode mode)
        {
            base.object_id = unit_id;
            Mode = mode;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutEnum8(Mode);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Mode = input.GetEnum8<SyncMode>();
        }
    }

    [MessageType(BattleConstants.UnitCancelBuffAction)]
    public class UnitCancelBuffAction : ObjectAction
    {
        public int BuffID;
        protected override void OnDisposing(uint objID)
        {
            BuffID = default;
        }
        public UnitCancelBuffAction() { }
        public UnitCancelBuffAction Init(uint unit_id, int buffID)
        {
            base.object_id = unit_id;
            BuffID = buffID;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutS32(BuffID);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            BuffID = input.GetS32();
        }
    }




    [MessageType(BattleConstants.UnitGetStatisticRequest)]
    public class UnitGetStatisticRequest : ActorRequest
    {
        public readonly List<uint> RequestObjectsID = new List<uint>();
        protected override void OnDisposing(uint objID)
        {
            RequestObjectsID.Clear();
        }
        public UnitGetStatisticRequest() { }
        public UnitGetStatisticRequest Init(uint objID)
        {
            base.object_id = objID;
            return this;
        }

        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutList(RequestObjectsID,
                static (output, v) => output.PutVU32(v));
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.GetList(
                static input => input.GetVU32(),
                RequestObjectsID);
        }
    }

    [MessageType(BattleConstants.UnitGetStatisticResponse)]
    public class UnitGetStatisticResponse : ActorResponse
    {
        public readonly HashMap<uint, UnitStatisticData> Statistics = new HashMap<uint, UnitStatisticData>();
        protected override void OnDisposing(uint objID)
        {
            Statistics.Clear();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVS32(Statistics.Count);
            foreach (KeyValuePair<uint, UnitStatisticData> e in Statistics)
            {
                output.PutVU32(e.Key);
                output.PutExt(e.Value);
            }
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            Statistics.Clear();
            int count = input.GetVS32();
            for (int i = 0; i < count; i++)
            {
                var key = input.GetVU32();
                var data = input.GetExtAny() as UnitStatisticData;
                Statistics.Put(key, data);
            }
        }
    }

    /// <summary>
    /// 设置子动做
    /// </summary>
    [MessageType(BattleConstants.UnitSetSubStateAction)]
    public class UnitSetSubStateAction : ObjectAction
    {
        public string UnitSubState;
        protected override void OnDisposing(uint objID)
        {
            UnitSubState = default;
        }

        public UnitSetSubStateAction() { }
        public UnitSetSubStateAction Init(uint objID, string state)
        {
            base.object_id = objID;
            UnitSubState = state;
            return this;
        }

        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(UnitSubState);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            UnitSubState = input.GetUTF();
        }
    }

    /// <summary>
    /// 单位准备完毕
    /// </summary>
    [MessageType(BattleConstants.UnitReadyAction)]
    public class UnitReadyAction : ObjectAction
    {
        public string info;
        protected override void OnDisposing(uint objID)
        {
            info = default;
        }
        public UnitReadyAction() { }
        public UnitReadyAction Init(uint objID)
        {
            base.object_id = objID;
            return this;
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(info);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            info = input.GetUTF();
        }
    }

    /// <summary>
    /// 跟随目标
    /// </summary>
    [MessageType(BattleConstants.UnitFollowTargetAction)]
    public class UnitFollowTargetAction : ObjectAction
    {
        public uint targetUnitID;
        public bool autoAttack;
        public float minDistance;
        public float maxDistance;
        public float tpDistance;
        public int slotIndex;
        protected override void OnDisposing(uint objID)
        {
            targetUnitID = default;
            autoAttack = default;
            minDistance = default;
            maxDistance = default;
            tpDistance = default;
            slotIndex = default;
        }
        public UnitFollowTargetAction() { }
        public UnitFollowTargetAction Init(uint unit_id, uint target_id)
        {
            base.object_id = unit_id;
            targetUnitID = target_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU32(targetUnitID);
            output.PutBool(autoAttack);
            output.PutF32(minDistance);
            output.PutF32(maxDistance);
            output.PutF32(tpDistance);
            output.PutS32(slotIndex);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            targetUnitID = input.GetU32();
            autoAttack = input.GetBool();
            minDistance = input.GetF32();
            maxDistance = input.GetF32();
            tpDistance = input.GetF32();
            slotIndex = input.GetS32();
        }
    }

    /// <summary>
    /// 玩家自由移动，由StopMove终止
    /// </summary>
    [MessageType(BattleConstants.UnitClientCustomMoveAction)]
    public class UnitClientCustomMoveAction : ObjectAction
    {
        public string SubState;
        protected override void OnDisposing(uint objID)
        {
            SubState = default;
        }
        public UnitClientCustomMoveAction() { }
        public UnitClientCustomMoveAction Init(uint unit_id, string subState)
        {
            base.object_id = unit_id;
            SubState = subState;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(SubState);
        }
        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            SubState = input.GetUTF();
        }
    }

    /// <summary>
    /// 玩家开始攀爬，由StopMove终止
    /// </summary>
    [MessageType(BattleConstants.UnitClimbAction)]
    public class UnitClimbAction : ObjectAction
    {
        public Vector3 position;
        public float direction;
        public Quaternion rotation;
        protected override void OnDisposing(uint objID)
        {
            position = default;
            direction = default;
            rotation = default;
        }
        public UnitClimbAction() { }
        public UnitClimbAction Init(uint unit_id, Vector3 pos, float dir, Quaternion rot)
        {
            base.object_id = unit_id;
            position = pos;
            direction = dir;
            rotation = rot;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.WritePosAndDirection(in position, direction);
            output.WriteRotation(rotation);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.ReadPosAndDirection(out position, out direction);
            input.ReadRotation(out rotation);
        }
    }

    /// <summary>
    /// 单位统计数据
    /// </summary>
    [MessageType(BattleConstants.UnitStatisticData)]
    public class UnitStatisticData : Recyclable, IExternalizable
    {
        public uint ObjectID;
        /// <summary>
        /// 死亡次数
        /// </summary>
        public int DeadCount;
        /// <summary>
        /// 总共杀死单位数量
        /// </summary>
        public int KillUnitCount;
        /// <summary>
        /// 总共杀死玩家数量
        /// </summary>
        public int KillPlayerCount;

        /// <summary>
        /// 承受伤害
        /// </summary>
        public long SelfDamage;
        /// <summary>
        /// 对所有单位造成的总伤害
        /// </summary>
        public long TotalDamage;
        /// <summary>
        /// 对玩家造成的总伤害
        /// </summary>
        public long PlayerDamage;

        /// <summary>
        /// 对所有单位输出的总治疗量
        /// </summary>
        public long TotalHealing;
        /// <summary>
        /// 对玩家输出的总治疗量
        /// </summary>
        public long PlayerHealing;

        protected override void Disposing()
        {
            ObjectID = 0;
            DeadCount = 0;
            KillUnitCount = 0;
            KillPlayerCount = 0;
            SelfDamage = 0;
            TotalDamage = 0;
            PlayerDamage = 0;
            TotalHealing = 0;
            PlayerHealing = 0;
        }
        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutVU32(ObjectID);
            output.PutVS32(DeadCount);
            output.PutVS32(KillUnitCount);
            output.PutVS32(KillPlayerCount);
            output.PutVS64(SelfDamage);
            output.PutVS64(TotalDamage);
            output.PutVS64(PlayerDamage);
            output.PutVS64(TotalHealing);
            output.PutVS64(PlayerHealing);
        }

        public virtual void ReadExternal(IInputStream input)
        {
            ObjectID = input.GetVU32();
            DeadCount = input.GetVS32();
            KillUnitCount = input.GetVS32();
            KillPlayerCount = input.GetVS32();
            SelfDamage = input.GetVS64();
            TotalDamage = input.GetVS64();
            PlayerDamage = input.GetVS64();
            TotalHealing = input.GetVS64();
            PlayerHealing = input.GetVS64();
        }
    }

    [MessageType(BattleConstants.UnitAxis3DAction)]
    public class UnitAxis3DAction : UnitAxisAction
    {
        public float ZControlSpeed;
        public float XYControlSpeed;
        protected override void OnDisposing(uint objID)
        {
            ZControlSpeed = 0;
            XYControlSpeed = 0;
        }
        public UnitAxis3DAction()
        {
        }

        public UnitAxis3DAction Init3D(uint unit_id)
        {
            base.Init(unit_id);
            base.object_id = unit_id;
            return this;
        }

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            ZControlSpeed = input.GetF32();
            XYControlSpeed = input.GetF32();
        }

        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(ZControlSpeed);
            output.PutF32(XYControlSpeed);
        }
    }

    [MessageType(BattleConstants.ComponentFieldChangeAction)]
    public class ComponentFieldChangeAction : ObjectAction
    {
        public readonly BitSetFields Fields = new BitSetFields();
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
            Fields.Clear();
            Fields.ReadExternal(input);
        }

    }

    [MessageType(BattleConstants.PlayerGuardEvent)]
    public class PlayerGuardEvent : PlayerNotify
    {
        [XmlSerializable] public bool guard { get { return bits.Get(0); } set { bits.Set(0, value); } }
        [XmlSerializable] public bool follow { get { return bits.Get(1); } set { bits.Set(1, value); } }
        private BitSet8 bits;
        public string reason = null;
        protected override void OnDisposing(uint objID)
        {
            bits.Clear();
            reason = null;
        }
        public PlayerGuardEvent() { }
        public PlayerGuardEvent Init(uint unit_id, bool guard)
        {
            base.object_id = unit_id;
            this.guard = guard;
            return this;
        }

        public PlayerGuardEvent(uint unit_id, bool guard, string reason)
        {
            base.object_id = unit_id;
            this.guard = guard;
            this.reason = reason;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutU8(bits.Mask);
            output.PutUTF(reason);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            bits.Mask = input.GetU8();
            reason = input.GetUTF();
        }
    }
    /// <summary>
    /// 开始翻滚
    /// </summary>
    [MessageType(BattleConstants.UnitStartSomersaultAction)]
    public class UnitStartSomersaultAction : ObjectAction
    {
        public float Direction;
        protected override void OnDisposing(uint objID)
        {
            Direction = default;
        }
        public UnitStartSomersaultAction() { }

        public UnitStartSomersaultAction Init(uint obj_id, float direction)
        {
            base.object_id = obj_id;
            Direction = direction;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutF32(Direction);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            input.GetF32();
        }
    }

    /// <summary>
    /// 结束翻滚
    /// </summary>
    [MessageType(BattleConstants.UnitStopSomersaultAction)]
    public class UnitStopSomersaultAction : ObjectAction
    {
        protected override void OnDisposing(uint objID)
        {

        }
        public UnitStopSomersaultAction() { }

        public UnitStopSomersaultAction Init(uint obj_id)
        {
            base.object_id = obj_id;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
        }
    }


    /// <summary>
    /// 跳过cg
    /// </summary>
    [MessageType(BattleConstants.UnitSkipCGAction)]
    public class UnitSkipCGAction : ObjectAction
    {
        public string CGId;
        protected override void OnDisposing(uint objID)
        {
            CGId = default;
        }
        public UnitSkipCGAction() { }
        public UnitSkipCGAction Init(uint obj_id)
        {
            base.object_id = obj_id;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(CGId);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            CGId = input.GetUTF();

        }
    }

    [MessageType(BattleConstants.PlayerSetEnvVarAction)]
    public class PlayerSetEnvVarAction : ObjectAction
    {
        public string key;
        public object value;
        protected override void OnDisposing(uint objID)
        {
            key = default;
            value = default;
        }
        public PlayerSetEnvVarAction() { }
        public PlayerSetEnvVarAction Init(uint obj_id, string key, object val)
        {
            base.object_id = obj_id;
            this.key = key;
            this.value = val;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutUTF(key);
            output.PutRawData(value);
        }

        override public void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            key = input.GetUTF();
            value = input.GetRawData();
        }
    }
}

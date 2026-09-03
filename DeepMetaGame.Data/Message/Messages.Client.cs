using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Xml;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeepMetaGame.Data.Message
{
    /// <summary>
    /// 客户端同步模式
    /// </summary>
    public enum SyncMode : byte
    {
        /// <summary>
        /// 服务端强同步
        /// </summary>
        ForceByServer = 0,
        /// <summary>
        /// 移动直接在客户端处理，技能在客户端预处理，（伤害数字由服务端同步）
        /// </summary>
        MoveByClient_PreSkillByClient = 2,
    }

    //     /// <summary>
    //     /// 单位的客户端显示数据，通常存储横向功能，比如Avatar
    //     /// </summary>
    //     public interface IUnitVisibleData : IExternalizable
    //     {
    // 
    //     }

    public static class ClientStruct
    {
        public struct UnitAuraStatus : IReadExternalizable, IWriteExternalizable
        {
            public int AuraTemplateID;
            public float TotalTime;
            public float PassTime;
            public float Range;

            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(AuraTemplateID);
                output.PutF32(TotalTime);
                output.PutF32(PassTime);
                output.PutF32(Range);
            }
            public void ReadExternal(IInputStream input)
            {
                AuraTemplateID = input.GetS32();
                TotalTime = input.GetF32();
                PassTime = input.GetF32();
                Range = input.GetF32();
            }
        }
        public struct UnitBuffStatus : IReadExternalizable, IWriteExternalizable
        {
            public int BuffTemplateID;
            public uint SenderID;
            public bool IsEquip;
            public float TotalTime;
            public float PassTime;
            public int OverlayLevel;
            public int BuffLevel;
            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(BuffTemplateID);
                output.PutVU32(SenderID);
                output.PutBool(IsEquip);
                output.PutF32(TotalTime);
                output.PutF32(PassTime);
                output.PutVS32(OverlayLevel);
                output.PutVS32(BuffLevel);
            }
            public void ReadExternal(IInputStream input)
            {
                BuffTemplateID = input.GetS32();
                SenderID = input.GetVU32();
                IsEquip = input.GetBool();
                TotalTime = input.GetF32();
                PassTime = input.GetF32();
                OverlayLevel = input.GetVS32();
                BuffLevel = input.GetVS32();
            }
        }
        public struct UnitSkillStatus : IReadExternalizable, IWriteExternalizable
        {
            public int SkillTemplateID;
            public int SkillLevel;
            public float PassTime;
            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(SkillTemplateID);
                output.PutS32(SkillLevel);
                output.PutF32(PassTime);
            }
            public void ReadExternal(IInputStream input)
            {
                SkillTemplateID = input.GetS32();
                SkillLevel = input.GetS32();
                PassTime = input.GetF32();
            }
        }
        public struct UnitCardStatus : IReadExternalizable, IWriteExternalizable
        {
            public int CardID;
            public int Level;
            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(CardID);
                output.PutS32(Level);
            }
            public void ReadExternal(IInputStream input)
            {
                CardID = input.GetS32();
                Level = input.GetS32();
            }
        }
        public struct UnitItemStatus : IReadExternalizable, IWriteExternalizable
        {
            public int ItemTemplateID;
            public int Count;
            public void WriteExternal(IOutputStream output)
            {
                output.PutS32(ItemTemplateID);
                output.PutVS32(Count);
            }
            public void ReadExternal(IInputStream input)
            {
                ItemTemplateID = input.GetS32();
                Count = input.GetVS32();
            }
        }
        public class ZoneEnvironmentVar : IReadExternalizable, IWriteExternalizable
        {
            public string Key;
            public bool SyncToClient;
            public object Value;

            public void WriteExternal(IOutputStream output)
            {
                output.PutUTF(Key);
                output.PutBool(SyncToClient);
                output.PutRawData(Value);
            }
            public void ReadExternal(IInputStream input)
            {
                Key = input.GetUTF();
                SyncToClient = input.GetBool();
                Value = input.GetRawData();
            }
        }

    }

    abstract public class SyncObjectInfo : Recyclable, IExternalizable
    {
        public uint ObjectID;
        public int TemplateID;

        public Vector3 pos;
        public float direction;
        public float body_direction;

        /// <summary>
        /// 用于扩展属性
        /// </summary>
        public ISerializable ExtData;

        public abstract bool HasExtData { get; protected set; }
        sealed protected override void Disposing()
        {
            ObjectID = 0;
            TemplateID = 0;
            pos = Vector3.Zero;
            direction = 0;
            body_direction = 0;
            ExtData = null;
            HasExtData = false;
            OnDisposing();
        }
        protected abstract void OnDisposing();
        public virtual void ReadExternal(IInputStream input)
        {
            ObjectID = input.GetVU32();
            TemplateID = input.GetS32();
            input.ReadPosAndDirection(out pos, out direction);
            input.ReadDirection(out body_direction);
            if (HasExtData)
            {
                ExtData = input.GetObjAs<ISerializable>();
            }
        }
        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutVU32(ObjectID);
            output.PutS32(TemplateID);
            output.WritePosAndDirection(in pos, direction);
            output.WriteDirection(body_direction);
            if (HasExtData)
            {
                output.PutObj(ExtData);
            }
        }
    }

    /// <summary>
    /// 场景中同步单位数据
    /// </summary>
    [MessageType(BattleConstants.SyncUnitInfo)]
    public class SyncUnitInfo : SyncObjectInfo
    {
        public string Name;
        public string Alias;
        public byte Force;
        public UnitType UType;
        public int Level;
        public string PlayerUUID;
        public byte status;
        public string sub_status;
        public float speed_z;
        public readonly UnitFieldChangedEvent fields = new UnitFieldChangedEvent();
        public readonly List<ClientStruct.UnitBuffStatus> CurrentBuffStatus = new();
        public readonly List<ClientStruct.UnitAuraStatus> CurrentAuraStatus = new();
        public readonly List<ClientStruct.UnitCardStatus> CurrentCardStatus = new();
        public IUnitVisibleData VisibleInfo;
        public UnitInfo template;
        private BitSet16 mask = new BitSet16(0);
        protected override void OnDisposing()
        {
            this.Name = null;
            this.Alias = null;
            this.Force = default;
            this.UType = default;
            this.Level = default;
            this.PlayerUUID = null;
            this.status = default;
            this.sub_status = null;
            this.speed_z = default;
            this.fields.Clear();
            this.CurrentBuffStatus.Clear();
            this.CurrentAuraStatus.Clear();
            this.CurrentCardStatus.Clear();
            this.VisibleInfo = null;
            this.template = null;
            this.mask.Clear();
        }
        //---------------------------------------------------
        [XmlSerializable]
        public override bool HasExtData
        {
            get { return mask.Get(1); }
            protected set { mask.Set(1, value); }
        }
        [XmlSerializable]
        public bool HasName
        {
            get { return mask.Get(2); }
            private set { mask.Set(2, value); }
        }
        [XmlSerializable]
        public bool HasPlayerUUID
        {
            get { return mask.Get(3); }
            private set { mask.Set(3, value); }
        }
        [XmlSerializable]
        public bool HasAlias
        {
            get { return mask.Get(4); }
            private set { mask.Set(4, value); }
        }
        [XmlSerializable]
        public bool IsTouchObj
        {
            get { return mask.Get(5); }
            set { mask.Set(5, value); }
        }
        [XmlSerializable]
        public bool IsTouchMap
        {
            get { return mask.Get(6); }
            set { mask.Set(6, value); }
        }
        [XmlSerializable]
        public bool StaticBlockable
        {
            get { return mask.Get(7); }
            set { mask.Set(7, value); }
        }
        [XmlSerializable]
        public bool HasTemplate
        {
            get { return mask.Get(8); }
            set { mask.Set(8, value); }
        }
        //---------------------------------------------------
        public SyncUnitInfo() { }

        public override void ReadExternal(IInputStream input)
        {
            mask.Mask = input.GetS16();
            base.ReadExternal(input);
            Force = input.GetU8();
            UType = input.GetEnum8<UnitType>();
            Level = input.GetVS32();
            if (HasName) Name = input.GetUTF();
            if (HasPlayerUUID) PlayerUUID = input.GetUTF();
            if (HasAlias) Alias = input.GetUTF();
            // if (HasZoneShape) this.ZoneShape = input.GetExt<IZoneShape>();
            VisibleInfo = input.GetObjAny() as IUnitVisibleData;
            input.GetExtListNoHead<ClientStruct.UnitAuraStatus>(CurrentAuraStatus);
            input.GetExtListNoHead<ClientStruct.UnitBuffStatus>(CurrentBuffStatus);
            input.GetExtListNoHead<ClientStruct.UnitCardStatus>(CurrentCardStatus);
            status = input.GetU8();
            sub_status = input.GetUTF();
            speed_z = input.GetF32();
            fields.ReadExternal(input);
            if (HasTemplate) template = input.GetObj<UnitInfo>();
        }
        public override void WriteExternal(IOutputStream output)
        {
            HasName = !string.IsNullOrEmpty(Name);
            HasPlayerUUID = !string.IsNullOrEmpty(PlayerUUID);
            HasAlias = !string.IsNullOrEmpty(Alias);
            HasExtData = ExtData != null;
            HasTemplate = template != null;
            //   this.HasZoneShape = ZoneShape != null;

            output.PutS16(mask.Mask);
            base.WriteExternal(output);
            output.PutU8(Force);
            output.PutEnum8(UType);
            output.PutVS32(Level);
            if (HasName) output.PutUTF(Name);
            if (HasPlayerUUID) output.PutUTF(PlayerUUID);
            if (HasAlias) output.PutUTF(Alias);
            //if (HasZoneShape) output.PutExt(ZoneShape);
            output.PutObj(VisibleInfo);
            output.PutExtListNoHead(CurrentAuraStatus);
            output.PutExtListNoHead(CurrentBuffStatus);
            output.PutExtListNoHead(CurrentCardStatus);
            output.PutU8(status);
            output.PutUTF(sub_status);
            output.PutF32(speed_z);
            fields.WriteExternal(output);
            if (HasTemplate) output.PutSer(template);
        }
    }
    /// <summary>
    /// 场景中同步物品数据
    /// </summary>
    [MessageType(BattleConstants.SyncItemInfo)]
    public class SyncItemInfo : SyncObjectInfo
    {
        public string Name;
        public string Alias;
        public byte Force;
        public float ItemTotalTimeMS;
        public float ItemExpireTimeMS;
        public int PickTimes;
        public ItemTemplate template;
        private BitSet8 mask = new BitSet8(0);
        protected override void OnDisposing()
        {
            this.Name = null;
            this.Alias = null;
            this.Force = default;
            this.ItemTotalTimeMS = default;
            this.ItemExpireTimeMS = default;
            this.PickTimes = default;
            this.template = default;
            this.mask.Clear();
        }
        //---------------------------------------------------
        [XmlSerializable]
        public override bool HasExtData
        {
            get { return mask.Get(2); }
            protected set { mask.Set(2, value); }
        }
        [XmlSerializable]
        public bool HasName
        {
            get { return mask.Get(1); }
            private set { mask.Set(1, value); }
        }
        [XmlSerializable]
        public bool HasAlias
        {
            get { return mask.Get(3); }
            private set { mask.Set(3, value); }
        }
        [XmlSerializable]
        public bool HasTemplate
        {
            get { return mask.Get(4); }
            private set { mask.Set(4, value); }
        }
        //---------------------------------------------------
        public SyncItemInfo() { }

        public override void ReadExternal(IInputStream input)
        {
            mask.Mask = input.GetU8();
            base.ReadExternal(input);
            Force = input.GetU8();
            ItemExpireTimeMS = input.GetF32();
            ItemTotalTimeMS = input.GetF32();
            if (HasName) Name = input.GetUTF();
            if (HasAlias) Alias = input.GetUTF();
            PickTimes = input.GetVS32();
            if (HasTemplate) template = input.GetObj<ItemTemplate>();
        }
        public override void WriteExternal(IOutputStream output)
        {
            HasName = !string.IsNullOrEmpty(Name);
            HasAlias = !string.IsNullOrEmpty(Alias);
            HasExtData = ExtData != null;
            HasTemplate = template != null;

            output.PutU8(mask.Mask);
            base.WriteExternal(output);
            output.PutU8(Force);
            output.PutF32(ItemExpireTimeMS);
            output.PutF32(ItemTotalTimeMS);
            if (HasName) output.PutUTF(Name);
            if (HasAlias) output.PutUTF(Alias);
            output.PutVS32(PickTimes);
            if (HasTemplate) output.PutSer(template);
        }
    }
    /// <summary>
    /// 场景中同步法术数据
    /// </summary>
    [MessageType(BattleConstants.SyncSpellInfo)]
    public class SyncSpellInfo : SyncObjectInfo
    {
        public byte Force;
        public float CurSpeed;
        private BitSet8 mask = new BitSet8(0);
        protected override void OnDisposing()
        {
            this.Force = default;
            this.CurSpeed = default;
            this.mask.Clear();
        }
        //---------------------------------------------------

        [XmlSerializable]
        public override bool HasExtData
        {
            get { return mask.Get(4); }
            protected set { mask.Set(4, value); }
        }
        [XmlSerializable]
        public bool HasSpeed
        {
            get { return mask.Get(4); }
            set { mask.Set(4, value); }
        }
        //---------------------------------------------------
        public SyncSpellInfo() { }

        public override void ReadExternal(IInputStream input)
        {
            mask.Mask = input.GetU8();
            base.ReadExternal(input);
            Force = input.GetU8();
            if (HasSpeed)
            {
                CurSpeed = input.GetF32();
            }
        }
        public override void WriteExternal(IOutputStream output)
        {
            HasExtData = ExtData != null;
            output.PutU8(mask.Mask);
            base.WriteExternal(output);
            output.PutU8(Force);
            if (HasSpeed)
            {
                output.PutF32(CurSpeed);
            }
        }
    }

    /// <summary>
    /// 同步场景中所有单位
    /// </summary>
    [MessageType(BattleConstants.SyncObjectsEvent)]
    public class SyncObjectsEvent : ZoneNotify
    {
        public readonly List<SyncObjectInfo> Objects = new();
        public SyncObjectsEvent() { }
        protected override void OnDisposing()
        {
            Objects.Clear();
        }
        override public void WriteExternal(IOutputStream output)
        {
            int len = Objects.Count;
            output.PutVS32(len);
            for (int i = 0; i < len; i++)
            {
                output.PutExt(Objects[i]);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            int len = input.GetVS32();
            Objects.Clear();
            for (int i = 0; i < len; i++)
            {
                SyncObjectInfo add = input.GetExtAny() as SyncObjectInfo;
                Objects.Add(add);
            }
        }
    }

    /// <summary>
    /// 玩家登陆到服务器后，服务器分配对应的单位ID
    /// </summary>
    [MessageType(BattleConstants.LockActorEvent)]
    public class LockActorEvent : ZoneNotify
    {
        /// <summary>
        /// 当前房间单位信息
        /// </summary>
        public SyncUnitInfo UnitData;

        /// <summary>
        /// 服务端传过来的单位信息
        /// </summary>
        public IUnitProperties GameServerProp;
        /// <summary>
        /// 服务端传过来的场景信息
        /// </summary>
        public ISceneProperties SceneServerProp;

        /// <summary>
        /// 服务端更新速度/秒
        /// </summary>
        public float ServerUpdateInterval;
        /// <summary>
        /// 客户端同步范围
        /// </summary>
        public float ClientSyncObjectRange;
        /// <summary>
        /// 客户端同步范围
        /// </summary>
        public float ClientSyncObjectOutRange;
        public SyncMode ClientSyncMode;

        public PlayerSkillChangedEvent Skills;
        public readonly List<ClientStruct.UnitSkillStatus> CurrentSkillStatus = new();
        public readonly List<ClientStruct.UnitItemStatus> CurrentItemStatus = new();
        public readonly List<ClientStruct.ZoneEnvironmentVar> CurrentZoneVars = new();
        public readonly List<ClientStruct.ZoneEnvironmentVar> CurrentUnitVars = new();
        public readonly List<ClientStruct.ZoneEnvironmentVar> CurrentPlayerVars = new();
        protected override void OnDisposing()
        {
            this.UnitData?.Dispose(); this.UnitData = null;
            this.GameServerProp = default;
            this.SceneServerProp = default;
            this.ServerUpdateInterval = default;
            this.ClientSyncObjectRange = default;
            this.ClientSyncObjectOutRange = default;
            this.ClientSyncMode = default;
            this.Skills?.Dispose(); this.Skills = null;
            this.CurrentSkillStatus.Clear();
            this.CurrentItemStatus.Clear();
            this.CurrentZoneVars.Clear();
            this.CurrentUnitVars.Clear();
            this.CurrentPlayerVars.Clear();
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutExt(UnitData);
            output.PutObj(GameServerProp);
            output.PutObj(SceneServerProp);
            output.PutF32(ServerUpdateInterval);
            output.PutF32(ClientSyncObjectRange);
            output.PutF32(ClientSyncObjectOutRange);
            output.PutEnum8(ClientSyncMode);
            output.PutExt(Skills);
            output.PutExtListNoHead(CurrentSkillStatus);
            output.PutExtListNoHead(CurrentItemStatus);
            output.PutExtListNoHead(CurrentZoneVars);
            output.PutExtListNoHead(CurrentUnitVars);
            output.PutExtListNoHead(CurrentPlayerVars);

        }
        override public void ReadExternal(IInputStream input)
        {
            UnitData = input.GetExt<SyncUnitInfo>();
            GameServerProp = input.GetObjAny() as IUnitProperties;
            SceneServerProp = input.GetObjAny() as ISceneProperties;
            ServerUpdateInterval = input.GetF32();
            ClientSyncObjectRange = input.GetF32();
            ClientSyncObjectOutRange = input.GetF32();
            ClientSyncMode = input.GetEnum8<SyncMode>();
            Skills = input.GetExt<PlayerSkillChangedEvent>();
            input.GetExtListNoHead<ClientStruct.UnitSkillStatus>(CurrentSkillStatus);
            input.GetExtListNoHead<ClientStruct.UnitItemStatus>(CurrentItemStatus);
            input.GetExtListNoHead<ClientStruct.ZoneEnvironmentVar>(CurrentZoneVars);
            input.GetExtListNoHead<ClientStruct.ZoneEnvironmentVar>(CurrentUnitVars);
            input.GetExtListNoHead<ClientStruct.ZoneEnvironmentVar>(CurrentPlayerVars);
        }
    }

    /// <summary>
    /// 连接到代理服务器
    /// </summary>
    [MessageType(BattleConstants.ConnectToProxy)]
    public class ConnectToProxy : ZoneNotify
    {
        public string ConnectString;
        public ConnectToProxy() { }
        public ConnectToProxy Init(string connect_string)
        {
            ConnectString = connect_string;
            return this;
        }
        protected override void OnDisposing()
        {
            ConnectString = null;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(ConnectString);
        }
        override public void ReadExternal(IInputStream input)
        {
            ConnectString = input.GetUTF();
        }
    }
    /// <summary>
    /// 从代理服务器断开
    /// </summary>
    [MessageType(BattleConstants.DisconnectFromProxy)]
    public class DisconnectFromProxy : ZoneNotify
    {
        protected override void OnDisposing()
        {
        }
        override public void WriteExternal(IOutputStream output)
        {
        }
        override public void ReadExternal(IInputStream input)
        {
        }
    }


    [MessageType(BattleConstants.ClientEnterScene)]
    public class ClientEnterScene : ZoneNotify
    {
        public string zoneUUID;
        /// <summary>
        /// 场景ID
        /// </summary>
        public int sceneID;
        /// <summary>
        /// 分割大小
        /// </summary>
        public float spaceDivW;
        /// <summary>
        /// 重力
        /// </summary>
        public float gravity;
        /// <summary>
        /// 资源版本
        /// </summary>
        public string resVersion;
        /// <summary>
        /// 阶梯高度
        /// </summary>
        public float stepHeight;

        public ISerializable initData;

        protected override void OnDisposing()
        {
            this.zoneUUID = null;
            this.sceneID = default;
            this.spaceDivW = default;
            this.resVersion = default;
            this.gravity = default;
            this.stepHeight = default;
            this.initData = default;
        }
        public ClientEnterScene() { }
        public ClientEnterScene Init(string zoneUUID, int sceneID, float spaceDiv, float gravity, float stepHeight, string resVer, ISerializable initData)
        {
            this.zoneUUID = zoneUUID;
            this.sceneID = sceneID;
            this.spaceDivW = spaceDiv;
            this.resVersion = resVer;
            this.gravity = gravity;
            this.stepHeight = stepHeight;
            this.initData = initData;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(zoneUUID);
            output.PutS32(sceneID);
            output.PutF32(spaceDivW);
            output.PutF32(gravity);
            output.PutF32(stepHeight);
            output.PutUTF(resVersion);
            output.PutObj(initData);
        }
        override public void ReadExternal(IInputStream input)
        {
            zoneUUID = input.GetUTF();
            sceneID = input.GetS32();
            spaceDivW = input.GetF32();
            gravity = input.GetF32();
            stepHeight = input.GetF32();
            resVersion = input.GetUTF();
            initData = input.GetObjAny() as ISerializable;
        }
    }

    /// <summary>
    /// 玩家离开场景.
    /// </summary>
    [MessageType(BattleConstants.PlayerLeaveScene)]
    public class PlayerLeaveScene : PlayerNotify
    {
        protected override void OnDisposing(uint objID)
        {

        }
        public PlayerLeaveScene() { }
        public PlayerLeaveScene Init(uint oid)
        {
            base.object_id = oid;
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
    /// 玩家离开场景.
    /// </summary>
    [MessageType(BattleConstants.ClientFocusUnits)]
    public class ClientFocusUnits : ZoneNotify
    {
        public readonly List<uint> FocusUnitsID = new();
        protected override void OnDisposing()
        {
            FocusUnitsID.Clear();
        }
        public ClientFocusUnits() { }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutList(FocusUnitsID, static (s, o) => s.PutU32(o));
        }
        override public void ReadExternal(IInputStream input)
        {
            input.GetList(static (i) => i.GetU32(), FocusUnitsID);
        }
    }

    #region CLIENT_EVENTS_0x8300

    /// <summary>
    /// 显示箭头
    /// </summary>
    [MessageType(BattleConstants.LookAtEvent)]
    public class LookAtEvent : ClientNotify
    {
        public string target;
        public float x;
        public float y;
        protected override void OnDisposing()
        {
            this.target = default;
            this.x = default;
            this.y = default;
        }
        public LookAtEvent() { }
        public LookAtEvent Init(string target, float x, float y)
        {
            this.target = target;
            this.x = x;
            this.y = y; 
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(target);
            output.PutF32(x);
            output.PutF32(y);
        }
        override public void ReadExternal(IInputStream input)
        {
            target = input.GetUTF();
            x = input.GetF32();
            y = input.GetF32();
        }
    }
    /// <summary>
    /// 改变游戏运行速度
    /// </summary>
    [MessageType(BattleConstants.ChangeTimeScaleEvent)]
    public class ChangeTimeScaleEvent : ClientNotify
    {
        /// <summary>
        /// 时间尺度百分比
        /// </summary>
        public float TimeScalePct = 100f;
        protected override void OnDisposing()
        {
            this.TimeScalePct = 100f;
        }
        public ChangeTimeScaleEvent() { }
        public ChangeTimeScaleEvent Init(float pct)
        {
            TimeScalePct = pct;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(TimeScalePct);
        }
        override public void ReadExternal(IInputStream input)
        {
            TimeScalePct = input.GetF32();
        }
    }
    /// <summary>
    /// 暂停游戏
    /// </summary>
    [MessageType(BattleConstants.GamePauseEvent)]
    public class GamePauseEvent : ClientNotify
    {
        /// <summary>
        /// 暂停多少秒，如果为0，则无限暂停
        /// </summary>
        public float Seconds = 0f;
        protected override void OnDisposing()
        {
            Seconds = default;
        }
        public GamePauseEvent() { }
        public GamePauseEvent Init(float sec)
        {
            Seconds = sec;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(Seconds);
        }
        override public void ReadExternal(IInputStream input)
        {
            Seconds = input.GetF32();
        }
    }
    /// <summary>
    /// 继续游戏
    /// </summary>
    [MessageType(BattleConstants.GameResumeEvent)]
    public class GameResumeEvent : ClientNotify
    {
        protected override void OnDisposing()
        {

        }
        public GameResumeEvent() { }
        public override void WriteExternal(IOutputStream output)
        {
        }
        public override void ReadExternal(IInputStream input)
        {
        }
    }
    /// <summary>
    /// 移动镜头到
    /// </summary>
    [MessageType(BattleConstants.CameraMoveToEvent)]
    public class CameraMoveToEvent : ClientNotify
    {
        /// <summary>
        /// 镜头移动速度(每秒距离)
        /// </summary>
        public float MoveSpeedSec = 1f;
        /// <summary>
        /// 移动过去的总时间，如果为0，则按速度计算
        /// </summary>
        public int TimeMS = 0;
        public float x;
        public float y;
        protected override void OnDisposing()
        {
            MoveSpeedSec = 1f;
            TimeMS = 0;
            x = 0;
            y = 0;
        }
        public CameraMoveToEvent() { }
        public CameraMoveToEvent Init(float x, float y, float speed, int timeMS)
        {
            this.x = x;
            this.y = y;
            MoveSpeedSec = speed;
            TimeMS = timeMS;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(x);
            output.PutF32(y);
            output.PutF32(MoveSpeedSec);
            output.PutS32(TimeMS);
        }
        override public void ReadExternal(IInputStream input)
        {
            x = input.GetF32();
            y = input.GetF32();
            MoveSpeedSec = input.GetF32();
            TimeMS = input.GetS32();
        }
    }
    /// <summary>
    /// 锁定镜头到单位
    /// </summary>
    [MessageType(BattleConstants.CameraFocusUnitEvent)]
    public class CameraFocusUnitEvent : ClientNotify
    {
        public uint ObjectID;
        protected override void OnDisposing()
        {
            ObjectID = 0;
        }
        public CameraFocusUnitEvent() { }
        public CameraFocusUnitEvent Init(uint oid)
        {
            ObjectID = oid;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutU32(ObjectID);
        }
        override public void ReadExternal(IInputStream input)
        {
            ObjectID = input.GetU32();
        }
    }
    /// <summary>
    /// 拉近镜头
    /// </summary>
    [MessageType(BattleConstants.CameraZoomToEvent)]
    public class CameraZoomToEvent : ClientNotify
    {
        /// <summary>
        /// 镜头拉近距离
        /// </summary>
        public float ZoomDistance = 10f;
        /// <summary>
        /// 镜头拉近速度(每秒距离)
        /// </summary>
        public float ZoomSpeedSec = 1f;

        protected override void OnDisposing()
        {
            ZoomDistance = 10f;
            ZoomSpeedSec = 1f;
        }
        public CameraZoomToEvent() { }
        public CameraZoomToEvent Init(float distance, float speed)
        {
            ZoomDistance = distance;
            ZoomSpeedSec = speed;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(ZoomDistance);
            output.PutF32(ZoomSpeedSec);
        }
        override public void ReadExternal(IInputStream input)
        {
            ZoomDistance = input.GetF32();
            ZoomSpeedSec = input.GetF32();
        }
    }
    /// <summary>
    /// 旋转镜头
    /// </summary>
    [MessageType(BattleConstants.CameraRotateToEvent)]
    public class CameraRotateToEvent : ClientNotify
    {
        /// <summary>
        /// 镜头旋转角度（0～360）
        /// </summary>
        public float RotateAngle = 10f;
        /// <summary>
        /// 镜头旋转速度(每秒角度)
        /// </summary>
        public float RotateSpeedSec = 1f;

        protected override void OnDisposing()
        {
            RotateAngle = 10f;
            RotateSpeedSec = 1f;
        }
        public CameraRotateToEvent() { }
        public CameraRotateToEvent Init(float angle, float speed)
        {
            RotateAngle = angle;
            RotateSpeedSec = speed;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(RotateAngle);
            output.PutF32(RotateSpeedSec);
        }
        override public void ReadExternal(IInputStream input)
        {
            RotateAngle = input.GetF32();
            RotateSpeedSec = input.GetF32();
        }
    }
    /// <summary>
    /// 重置镜头
    /// </summary>
    [MessageType(BattleConstants.CameraResetEvent)]
    public class CameraResetEvent : ClientNotify
    {
        protected override void OnDisposing()
        {
        }
        public override void WriteExternal(IOutputStream output)
        {
        }
        public override void ReadExternal(IInputStream input)
        {
        }
    }

    /// <summary>
    /// 客户端动作序列
    /// </summary>
    [MessageType(BattleConstants.ClientEventQueue)]
    public class ClientEventQueue : ClientNotify
    {
        public readonly List<ClientNotify> EventQueue = new List<ClientNotify>();

        protected override void OnDisposing()
        {
            EventQueue.Clear();
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutU8((byte)EventQueue.Count);
            foreach (ClientNotify e in EventQueue)
            {
                output.PutExt(e);
            }
        }
        override public void ReadExternal(IInputStream input)
        {
            EventQueue.Clear();
            int count = input.GetU8();
            for (int i = 0; i < count; i++)
            {
                EventQueue.Add(input.GetExtAny() as ClientNotify);
            }
        }
    }

    /// <summary>
    /// 移动锁定一段时间
    /// </summary>
    [MessageType(BattleConstants.CameraHoldEvent)]
    public class CameraHoldEvent : ClientNotify
    {
        public float x;
        public float y;
        public float z;
        public int TimeMS;
        protected override void OnDisposing()
        {
            TimeMS = default;
            x = 0;
            y = 0;
            z = 0;
        }
        public CameraHoldEvent() { }
        public CameraHoldEvent Init(float x, float y, float z, int timeMS)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            TimeMS = timeMS;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutF32(x);
            output.PutF32(y);
            output.PutF32(z);
            output.PutS32(TimeMS);
        }
        override public void ReadExternal(IInputStream input)
        {
            x = input.GetF32();
            y = input.GetF32();
            z = input.GetF32();
            TimeMS = input.GetS32();
        }
    }

    [MessageType(BattleConstants.CameraControlEvent)]
    public class CameraControlEvent : ClientNotify
    {
        public string Name;
        protected override void OnDisposing()
        {
            Name = default;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Name);
        }
        override public void ReadExternal(IInputStream input)
        {
            Name = input.GetUTF();
        }
    }

    #endregion
    //--------------------------------------------------------------------------

}

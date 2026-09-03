using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Message
{
    //-----------------------------------------
    public enum UnitSyncModifer : byte
    {
        Posistion = 0x01,
        Direction = 0x02,
        MainState = 0x04,
        SubState = 0x08,
        LayerUpward = 0x10,
        Rotation = 0x20,
        BodyRotation = 0x40,
        All = 0xFF,
    }
    public class UnitSyncPos
    {
        private uint m_ID;
        private byte modifer = 0xFF;
        private Vector3 m_Pos;
        private float m_Direction;
        private float m_BodyDirection;
        private float m_LayerUpward;
        private byte m_UnitMainState;
        private string m_UnitSubState;
        public void Clean()
        {
            this.m_ID = default;
            this.modifer = 0xFF;
            this.m_Pos = default;
            this.m_Direction = default;
            this.m_BodyDirection = default;
            this.m_LayerUpward = default;
            this.m_UnitMainState = default;
            this.m_UnitSubState = default;
        }

        public uint ID { get { return m_ID; } }
        public float X { get { return m_Pos.X; } }
        public float Y { get { return m_Pos.Y; } }
        public float Z { get { return m_Pos.Z; } }
        [XmlSerializable]
        public Vector3 Position
        {
            get { return m_Pos; }
            set
            {
                if (m_Pos != value)
                {
                    m_Pos = value;
                    AddModifer(UnitSyncModifer.Posistion);
                }
            }
        }
        [XmlSerializable]
        public float Direction
        {
            get { return m_Direction; }
            set
            {
                if (m_Direction != value)
                {
                    m_Direction = value;
                    AddModifer(UnitSyncModifer.Direction);
                }
            }
        }
        [XmlSerializable]
        public float BodyDirection
        {
            get { return m_BodyDirection; }
            set
            {
                if (m_BodyDirection != value)
                {
                    m_BodyDirection = value;
                    AddModifer(UnitSyncModifer.BodyRotation);
                }
            }
        }

        [XmlSerializable]
        public UnitActionStatus UnitMainState
        {
            get { return (UnitActionStatus)m_UnitMainState; }
            set
            {
                if (m_UnitMainState != (byte)value)
                {
                    m_UnitMainState = (byte)value;
                    AddModifer(UnitSyncModifer.MainState);
                }
            }
        }
        [XmlSerializable]
        public string UnitSubState
        {
            get { return m_UnitSubState; }
            set
            {
                if (m_UnitSubState != value)
                {
                    m_UnitSubState = value;
                    AddModifer(UnitSyncModifer.SubState);
                }
            }
        }

        [XmlSerializable]
        public float LayerUpward
        {
            get => m_LayerUpward;
            set
            {
                if (m_LayerUpward != value)
                {
                    m_LayerUpward = value;
                    AddModifer(UnitSyncModifer.LayerUpward);
                }
            }
        }
        public bool IsDirty { get => modifer != 0; }
        public bool HasModifer(UnitSyncModifer modifer)
        {
            return (this.modifer & (byte)modifer) != 0;
        }
        public void Begin(uint objID)
        {
            modifer = 0;
            m_ID = objID;
        }
        public void AddModifer(UnitSyncModifer modifer)
        {
            this.modifer |= (byte)modifer;
        }
        public void Sync(UnitSyncPos sync)
        {
            m_ID = sync.m_ID;
            if (sync.HasModifer(UnitSyncModifer.Posistion)) this.Position = sync.Position;
            if (sync.HasModifer(UnitSyncModifer.Direction)) this.Direction = sync.Direction;
            if (sync.HasModifer(UnitSyncModifer.BodyRotation)) this.BodyDirection = sync.BodyDirection;
            if (sync.HasModifer(UnitSyncModifer.MainState)) this.UnitMainState = sync.UnitMainState;
            if (sync.HasModifer(UnitSyncModifer.SubState)) this.UnitSubState = sync.UnitSubState;
            if (sync.HasModifer(UnitSyncModifer.LayerUpward)) this.LayerUpward = sync.LayerUpward;
        }

        public void Write(IOutputStream output)
        {
            output.PutVU32(m_ID);
            output.PutU8(modifer);
            if (HasModifer(UnitSyncModifer.Posistion)) output.WritePos(in m_Pos);
            if (HasModifer(UnitSyncModifer.Direction)) output.WriteDirection(m_Direction);
            if (HasModifer(UnitSyncModifer.BodyRotation)) output.WriteDirection(m_BodyDirection);
            if (HasModifer(UnitSyncModifer.MainState)) output.PutU8(m_UnitMainState);
            if (HasModifer(UnitSyncModifer.SubState)) output.PutUTF(m_UnitSubState);
            if (HasModifer(UnitSyncModifer.LayerUpward)) output.PutF32(m_LayerUpward);
        }
        public void Read(IInputStream input)
        {
            m_ID = input.GetVU32();
            modifer = input.GetU8();
            if (HasModifer(UnitSyncModifer.Posistion)) input.ReadPos(out m_Pos);
            if (HasModifer(UnitSyncModifer.Direction)) input.ReadDirection(out m_Direction);
            if (HasModifer(UnitSyncModifer.BodyRotation)) input.ReadDirection(out m_BodyDirection);
            if (HasModifer(UnitSyncModifer.MainState)) m_UnitMainState = input.GetU8();
            if (HasModifer(UnitSyncModifer.SubState)) m_UnitSubState = input.GetUTF();
            if (HasModifer(UnitSyncModifer.LayerUpward)) m_LayerUpward = input.GetF32();
        }

    }
    /// <summary>
    /// 同步场景中移动单位的坐标
    /// </summary>
    [MessageType(BattleConstants.SyncPosEvent)]
    public class SyncPosEvent : ZoneNotify
    {
        //-----------------------------------------
        private BitSet8 mask = new BitSet8();
        private double pass_time_ms;
        private readonly List<UnitSyncPos> readed_units_pos = new List<UnitSyncPos>();
        protected override void OnDisposing()
        {
            mask.Clear();
            pass_time_ms = 0;
            readed_units_pos.Clear();
        }
        //-----------------------------------------
        public bool IsEmpty
        {
            get { return mask.Get(2); }
            private set { mask.Set(2, value); }
        }
        public double PassTimeMS
        {
            get { return pass_time_ms; }
        }
        public IReadOnlyList<UnitSyncPos> ReadUnitPosList
        {
            get { return readed_units_pos; }
        }
        //-----------------------------------------
        public SyncPosEvent() { }

        public bool Init(int unit_count, double passtime)
        {
            IsEmpty = unit_count <= 0;
            pass_time_ms = passtime;
            return IsEmpty == false;
        }
        public void SetUnitList(List<UnitSyncPos> units_pos)
        {
            readed_units_pos.Clear();
            if (units_pos != null)
            {
                for (int i = 0; i < units_pos.Count; i++)
                {
                    readed_units_pos.Add(units_pos[i]);
                }
            }
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutU8(mask.Mask);
            output.PutF64(pass_time_ms);
            var count = readed_units_pos == null ? 0 : readed_units_pos.Count;
            output.PutVS32(count);
            if (count > 0)
            {
                foreach (var add in readed_units_pos)
                {
                    add.Write(output);
                }
            }
        }

        override public void ReadExternal(IInputStream input)
        {
            mask.Mask = input.GetU8();
            pass_time_ms = input.GetF64();
            var count = input.GetVS32();
            if (count > 0)
            {
                readed_units_pos.Clear();
                for (int i = 0; i < count; i++)
                {
                    var add = new UnitSyncPos();
                    add.Read(input);
                    readed_units_pos.Add(add);
                }
            }
        }

    }

}

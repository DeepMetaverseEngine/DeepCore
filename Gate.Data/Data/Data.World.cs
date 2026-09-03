using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Meta.Channel.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gate.Data
{
    [MessageType(Constants.WORLD_DATA_START + 0x300)]
    public class WorldChannelInfo : ISerializable
    {

    }


    [MessageType(Constants.WORLD_DATA_START + 0x310)]
    public class ObjectSyncState : IExternalizable, IChannelS2C, IChannelC2S, IObjectMessage
    {
        public uint ObjectID { get => objectID; }
        private uint objectID;
        private BitSet8 mask = new BitSet8(0);
        private Vector3 position;
        private float direction;
        private float moveSpeedSEC;
        private int state;
        private string stateAction;

        public Vector3 Position
        {
            get => position;
            set
            {
                position = value;
                mask.Set(0, true);
            }
        }
        public float Direction
        {
            get => direction;
            set
            {
                direction = value;
                mask.Set(1, true);
            }
        }
        public float MoveSpeedSEC
        {
            get => moveSpeedSEC;
            set
            {
                moveSpeedSEC = value;
                mask.Set(2, true);
            }
        }
        public int State
        {
            get => state;
            set
            {
                state = value;
                mask.Set(3, true);
            }
        }
        public string StateAction
        {
            get => stateAction;
            set
            {
                stateAction = value;
                mask.Set(4, true);
            }
        }

        public bool HasPosition { get => mask.Get(0); }
        public bool HasDirection { get => mask.Get(1); }
        public bool HasMoveSpeedSEC { get => mask.Get(2); }
        public bool HasState { get => mask.Get(3); }
        public bool HasStateAction { get => mask.Get(4); }
        public void SetMask(byte m)
        {
            mask.Mask = m;
        }
        public byte TrySync(ref Vector3 _position, ref float _direction, ref float _moveSpeedSEC, ref int _state, ref string _stateAction)
        {
            if (mask.Get(0)) { _position = position; }
            if (mask.Get(1)) { _direction = direction; }
            if (mask.Get(2)) { _moveSpeedSEC = moveSpeedSEC; }
            if (mask.Get(3)) { _state = state; }
            if (mask.Get(4)) { _stateAction = stateAction; }
            return mask.Mask;
        }
        public byte TryUpdate(uint objectID, Vector3 _position, float _direction, float _moveSpeedSEC, int _state, string _stateAction)
        {
            this.objectID = objectID;
            mask.Mask = 0;
            if (Position != _position)
            {
                Position = _position;
            }
            if (Direction != _direction)
            {
                Direction = _direction;
            }
            if (MoveSpeedSEC != _moveSpeedSEC)
            {
                MoveSpeedSEC = _moveSpeedSEC;
            }
            if (State != _state)
            {
                State = _state;
            }
            if (StateAction != _stateAction)
            {
                StateAction = _stateAction;
            }
            return mask.Mask;
        }
        public void ReadExternal(IInputStream input)
        {
            objectID = input.GetU32();
            mask.ReadExternal(input);
            if (mask.Get(0)) { position = input.GetStruct<Vector3>(); }
            if (mask.Get(1)) { direction = input.GetF32(); }
            if (mask.Get(2)) { moveSpeedSEC = input.GetF32(); }
            if (mask.Get(3)) { state = input.GetS32(); }
            if (mask.Get(4)) { stateAction = input.GetUTF(); }
        }

        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(objectID);
            mask.WriteExternal(output);
            if (mask.Get(0)) { output.PutStruct(position); }
            if (mask.Get(1)) { output.PutF32(direction); }
            if (mask.Get(2)) { output.PutF32(moveSpeedSEC); }
            if (mask.Get(3)) { output.PutS32(state); }
            if (mask.Get(4)) { output.PutUTF(stateAction); }
        }
    }


    public class MapChunk
    {
        public readonly int ChunkID;
        public readonly Location3D ChunkLocation;
        public readonly Size3D ChunkSize;
        public readonly BoundingBox AABB;
        public List<MapChunk> Nexts = new List<MapChunk>();
        public MapChunk(int chunkID, Location3D chunkLocation, Size3D chunkSize)
        {
            this.ChunkID = chunkID;
            this.ChunkLocation = chunkLocation;
            this.ChunkSize = chunkSize;
            this.AABB = new BoundingBox(chunkLocation, chunkSize);
        }
    }
}

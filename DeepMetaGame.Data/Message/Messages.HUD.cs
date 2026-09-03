using DeepCore;
using DeepCore.Geometry;
using DeepCore.GUI;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;
using System.Text;
using DeepCore.GUI.Input;

namespace DeepMetaGame.Data.Message.UI
{
    //-----------------------------------------------------------------------------------------
    public abstract class UIInteractiveAction : BattleAction
    {
        public uint SenderObjectID;
    }
    //-----------------------------------------------------------------------------------------
    public abstract class MouseInputAction : UIInteractiveAction
    {
        public string ComponentName;
        public MouseButton Button;
        public Vector2 ScreenPoint;
        public int Clicks;
        public int Delta;
        public Raycast raycast;
        sealed protected override void OnDisposing()
        {
            OnDisposing(Button);
            this.SenderObjectID = default;
            this.ComponentName = default;
            this.Button = default;
            this.ScreenPoint = default;
            this.Clicks = default;
            this.Delta = default;
            this.raycast?.Dispose();
            this.raycast = default;
        }
        protected abstract void OnDisposing(MouseButton btn);
        public override void WriteExternal(IOutputStream output)
        {
            output.PutU32(SenderObjectID);
            output.PutUTF(ComponentName);
            output.PutEnum(Button);
            output.PutStruct(ScreenPoint);
            output.PutS32(Clicks);
            output.PutS32(Delta);
        }
        public override void ReadExternal(IInputStream input)
        {
            this.SenderObjectID = input.GetU32();
            this.ComponentName = input.GetUTF();
            this.Button = input.GetEnum<MouseButton>();
            this.ScreenPoint = input.GetStruct<Vector2>();
            this.Clicks = input.GetS32();
            this.Delta = input.GetS32();
        }
    }
    public abstract class KeyInputAction : UIInteractiveAction
    {
        public KeyCode Key;
        public KeyCode Modifiers;
        sealed protected override void OnDisposing()
        {
            OnDisposing(Key);
            this.SenderObjectID = default;
            this.Key = default;
            this.Modifiers = default;
        }
        protected abstract void OnDisposing(KeyCode key);
        public bool IsShift { get => (Modifiers | KeyCode.ModifierShift) != 0; }
        public bool IsControl { get => (Modifiers | KeyCode.ModifierControl) != 0; }
        public bool IsAlt { get => (Modifiers | KeyCode.ModifierAlt) != 0; }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutU32(SenderObjectID);
            output.PutEnum(Key);
            output.PutEnum(Modifiers);
        }
        public override void ReadExternal(IInputStream input)
        {
            this.SenderObjectID = input.GetU32();
            this.Key = input.GetEnum<KeyCode>();
            this.Modifiers = input.GetEnum<KeyCode>();
        }
    }
    //-----------------------------------------------------------------------------------------
    [MessageType(BattleConstants.Raycast)]
    public class Raycast : Recyclable, IExternalizable
    {
        public Vector2? screen;
        public Vector3 origin;
        public Vector3 normal;


        public bool IsHitTerrain;
        public Vector3 HitTerrainPosition;
        public uint HitObjectID;
        public Vector3 HitObjectPosition;

        public string HitFlagName;
        /// <summary>
        /// 单位和地面射线交点的平面坐标
        /// </summary>
        //public Vector3 HitObjectPlanePosition;
        protected override void Disposing()
        {
            screen = default;
            origin = default;
            normal = default;
            IsHitTerrain = false;
            HitTerrainPosition = default;
            HitObjectID = default;
            HitFlagName = default;
            HitObjectPosition = default;
            //HitObjectPlanePosition = default;
        }
        public bool IsHitObject => (HitObjectID != 0);
        public bool IsHitFlag => (!string.IsNullOrEmpty(HitFlagName));

        public void WriteExternal(IOutputStream output)
        {
            output.PutNullable(screen, static (o, t) => o.PutStruct(t));
            output.PutStruct(origin);
            output.PutStruct(normal);

            output.PutBool(IsHitTerrain);
            if (IsHitTerrain)
            {
                output.PutStruct(HitTerrainPosition);
            }
            output.PutU32(HitObjectID);
            if (IsHitObject)
            {
                output.PutStruct(HitObjectPosition);
            }
            output.PutUTF(HitFlagName);
        }
        public void ReadExternal(IInputStream input)
        {
            this.screen = input.GetNullable<Vector2>(static (i) => i.GetStruct<Vector2>());
            this.origin = input.GetStruct<Vector3>();
            this.normal = input.GetStruct<Vector3>();

            this.IsHitTerrain = input.GetBool();
            if (IsHitTerrain)
            {
                this.HitTerrainPosition = input.GetStruct<Vector3>();
            }
            this.HitObjectID = input.GetU32();
            if (IsHitObject)
            {
                this.HitObjectPosition = input.GetStruct<Vector3>();
            }
            this.HitFlagName = input.GetUTF();
        }
    }
    //-----------------------------------------------------------------------------------------
    [MessageType(BattleConstants.MouseDownAction)]
    public class MouseDownAction : MouseInputAction
    {
        protected override void OnDisposing(MouseButton btn) { }
    }
    [MessageType(BattleConstants.MouseUpAction)]
    public class MouseUpAction : MouseInputAction
    {
        protected override void OnDisposing(MouseButton btn) { }
    }
    [MessageType(BattleConstants.MouseMoveAction)]
    public class MouseMoveAction : MouseInputAction
    {
        protected override void OnDisposing(MouseButton btn) { }
    }
    [MessageType(BattleConstants.MouseClickAction)]
    public class MouseClickAction : MouseInputAction
    {
        protected override void OnDisposing(MouseButton btn) { }
    }
    //-----------------------------------------------------------------------------------------
    [MessageType(BattleConstants.KeyDownAction)]
    public class KeyDownAction : KeyInputAction
    {
        protected override void OnDisposing(KeyCode key) { }
    }
    [MessageType(BattleConstants.KeyUpAction)]
    public class KeyUpAction : KeyInputAction
    {
        protected override void OnDisposing(KeyCode key) { }
    }
    //-----------------------------------------------------------------------------------------

    [MessageType(BattleConstants.CameraOffset)]
    public class CameraOffset : BattleNotify, SystemMessage
    {
        private BitSet8 bitset = new BitSet8(0);
        public float OffsetZ;
        public float Angle;
        public float Radius;
        protected override void OnDisposing()
        {
            bitset.Clear();
            OffsetZ = 0;
            Angle = 0;
            Radius = 0;
        }
        public bool LockYaw { get => bitset.Get(0); set => bitset.Set(0, value); }
        public bool LockPitch { get => bitset.Get(1); set => bitset.Set(1, value); }

        public override void WriteExternal(IOutputStream output)
        {
            output.PutU8(bitset.Mask);
            output.PutF32(OffsetZ);
            output.PutF32(Angle);
            output.PutF32(Radius);
        }
        public override void ReadExternal(IInputStream input)
        {
            this.bitset.Mask = input.GetU8();
            this.OffsetZ = input.GetF32();
            this.Angle = input.GetF32();
            this.Radius = input.GetF32();
        }
    }
    //-----------------------------------------------------------------------------------------
    [MessageType(BattleConstants.MouseSelectObjectAction)]
    public class MouseSelectObjectAction : BattleAction
    {
        public uint HitObjectID;
        protected override void OnDisposing()
        {
            HitObjectID = 0;
        }
        public override void ReadExternal(IInputStream input)
        {
            this.HitObjectID = input.GetU32();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutU32(HitObjectID);
        }
    }
}

using DeepCore.Components;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using System;

namespace DeepCore.Game3D.Slave.Layer
{
//     public partial class LayerUnit
//     {
//         public bool IsZeroGravityFly => ZeroGravityFly?.IsStarted ?? false;
// 
//         private ZeroGravityFlyComponent mZeroGravityFly;
// 
//         public ZeroGravityFlyComponent ZeroGravityFly
//         {
//             get
//             {
//                 if (mZeroGravityFly != null && !mZeroGravityFly.IsDisposed)
//                 {
//                     return mZeroGravityFly;
//                 }
// 
//                 Components.TryGetComponentAs(out mZeroGravityFly, true);
// 
//                 return mZeroGravityFly;
//             }
//         }
// 
// 
//         [ComponentTag(0x1, "无重力飞行")]
//         public class ZeroGravityFlyComponent : LayerObjectComponent<LayerUnit>
//         {
//             public bool IsStarted => SyncableFields.GetField<bool>(0);
//             public float LandOffset => SyncableFields.GetField<float>(1);
//             public float MinPosZ => SyncableFields.GetField<float>(2);
//             public float MaxPosZ => SyncableFields.GetField<float>(3);
// 
// 
//             private bool mLastStarted;
// 
//             protected override void OnUpdate(int intervalMS)
//             {
//                 base.OnUpdate(intervalMS);
//                 if (mLastStarted != IsStarted)
//                 {
//                     if (IsStarted)
//                     {
//                         Owner.mLocalPos.SpeedZ = 0;
//                         ZMove(0, 0);
//                     }
//                 }
// 
//                 if (IsStarted)
//                 {
//                     if (TryAutoPause())
//                     {
//                         return;
//                     }
// 
//                     Owner.mHitFlyState?.End();
//                     Owner.mLocalPos.SpeedZ = 0;
//                     
//                 }
// 
//                 mLastStarted = IsStarted;
//             }
//             
//             public void ZMove(float speed, int intervalMS)
//             {
//                 var distance = MoveHelper.GetDistance(intervalMS, speed);
//                 FixDistance(ref distance);
//                 if (Math.Abs(distance) > 0.01f)
//                 {
//                     Owner.mLocalPos.Fly(distance);
//                 }
//             }
// 
//             public void FixDistance(ref float distance)
//             {
//                 var max = MaxPosZ - Owner.Z;
//                 var min = MinPosZ - Owner.Z;
//                 Owner.Parent.Terrain3D.TryGetVoxelTopRange(Owner.mLocalPos.Position, out var top);
//                 top -= Owner.BodyHeight;
//                 Owner.Parent.Terrain3D.TryGetVoxelUpRange(Owner.mLocalPos.Position, out var up);
//   
//                 var landDistance = up + LandOffset - Owner.Z;
//                 
//                 top = top - Owner.Z;
//                 up = up - Owner.Z;
//                 distance = Math.Min(distance, max);
//                 distance = Math.Max(distance, min);
//                 distance = Math.Max(distance, landDistance);
//                 distance = Math.Min(distance, top);
//                 distance = Math.Max(distance, up);
//             }
// 
//             private bool TryAutoPause()
//             {
//                 if (Owner.CurrentState.NotControllable())
//                 {
//                     return true;
//                 }
// 
//                 if (Owner.mHitFlyState != null && Owner.mLocalPos.SpeedZ > 0 && !Owner.mHitFlyState.IsEnd)
//                 {
//                     return true;
//                 }
// 
//                 return false;
//             }
//         }
//     }
// 
//     public partial class LayerPlayer
//     {
//         protected virtual bool PreAxisZeroGravity(UnitAxisAction axis, int intervalMS)
//         {
//             if (IsZeroGravityFly && axis is UnitAxis3DAction axis3D)
//             {
//                 var absSpeed = Math.Min(Math.Abs(axis3D.ZControlSpeed), MoveSpeedSEC);
//                 var zSpeed = CMath.GetDirect(axis3D.ZControlSpeed) * absSpeed;
//                 ZeroGravityFly.ZMove(zSpeed, intervalMS);
//                 if (axis3D.distance != 0)
//                 {
//                     var xySpeed = axis3D.XYControlSpeed == 0 ? MoveSpeedSEC : axis3D.XYControlSpeed;
//                     xySpeed =  Math.Min(xySpeed, MoveSpeedSEC);
//                     float ispeed = MoveHelper.GetDistance(intervalMS, xySpeed);
//                     float direction = axis.angle;
//                     float addX = (float) (Math.Cos(direction) * ispeed);
//                     float addY = (float) (Math.Sin(direction) * ispeed);
//                     this.PreBlockMove(addX, addY);
//                 }
//                 return true;
//             }
// 
//             return false;
//         }
//     }
}
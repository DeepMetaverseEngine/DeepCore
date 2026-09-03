using DeepCore;
using DeepCore.Geometry;
using DeepCore.Unity;
using DeepCore.Unity.Expose;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepCore.Unity3D.Voxel;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity.BattleView;
using System.Security.Cryptography;
using UnityEngine;
using static DeepCore.EventTrigger.Data.AI.LLMAgentValue;

namespace DeepMetaGame.Unity.Preview.Preview
{

    public abstract partial class PreviewObject : DisplayObject, IZoneObject
    {
        IZone IZoneObject.Zone => Proxy;
        bool IZoneObject.Enable => true;

        public static PreviewProxy Proxy { get => PreviewProxy.Proxy; }
        public static TemplateManager Templates { get => PreviewProxy.Templates.Templates; }
        public static System.Random RandomN { get => PreviewProxy.RandomN; }
        //--------------------------------------------------------------------------------------
        public object UserTag { get; set; }
        private bool isInitDone = false;
        public bool IsInitDone => isInitDone;
        protected void Init(object data)
        {
            try
            {
                DoInit(data);
            }
            finally
            {
                isInitDone = true;
            }
            if (Disposed)
            {
                CleanResource();
                return;
            }
            Replay();
        }
        protected virtual void DoInit(object data) { }

        //--------------------------------------------------------------------------------------

        protected virtual void Awake()
        {
            interval = new UnityInterval();
        }
        private void Start()
        {
            try
            {
                ResetTime();
                DoStart();
                childGizmos = InitGizmos();
                childCollider = InitEditCollider();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void Update()
        {
            try
            {
                DoUpdate();
                UpdateMotion();
                UpdateTime();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void LateUpdate()
        {
            try
            {
                UpdateResource();
                UpdateGizmos(childGizmos);
                UpdateCollider(childCollider);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        protected override void OnDisposing()
        {
            try
            {
                DoDestory();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            CleanResource();
        }
        void OnDestroy()
        {
        }
        //--------------------------------------------------------------------------------------


        protected override void OnGUI()
        {
            try
            {
                if (isInitDone)
                {
                    base.OnGUI();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        //--------------------------------------------------------------------------------------
        protected virtual void DoStart() { }
        protected virtual void DoReplay() { }
        protected virtual void DoUpdate() { }
        protected virtual void DoDestory() { }
        //--------------------------------------------------------------------------------------
        #region Time

        public double PassTimeMS { get => interval.PassTimeMS; }
        public float IntervalMS { get => interval.IntervalMS; }

        private UnityInterval interval;

        public void ResetTime()
        {
            interval.ResetTime();
        }
        private void UpdateTime()
        {
            interval.UpdateTime();
        }
        public void Replay()
        {
            try
            {
                ResetTime();
                ClearMotion();
                DoReplay();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        #endregion
        //--------------------------------------------------------------------------------------
        #region Gizmos
        public float BodyHeight = 0;
        public float BodySize = 0;
        public Color BodyColor = Color.white.SetAlpha(0.5f);
        float IZoneObject.BodySize => this.BodySize;
        float IZoneObject.BodyHeight => this.BodyHeight;
        public VoxelCylinder Body
        {
            get
            {
                var body = LocalBody;
                body.Center += Position;
                return body;
            }
        }
        public virtual VoxelCylinder LocalBody => new VoxelCylinder(DeepCore.Geometry.Vector3.Zero, BodySize, BodyHeight);

        private GameObject childGizmos;
        private Collider childCollider;

        protected virtual GameObject InitGizmos()
        {
            var childGizmos = VoxelGizmos.CreateVoxelCylinder(1, 1);
            if (childGizmos)
            {
                childGizmos.transform.SetParent(transform, false);
                if (childGizmos.TryGetComponent<MeshRenderer>(out var srender) && RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    srender.material = drender.material;
                    srender.material.color = BodyColor;
                }
                var body = LocalBody;
                childGizmos.transform.localScale = new UnityEngine.Vector3(body.Radius, body.Height, body.Radius);
                childGizmos.transform.localPosition = new UnityEngine.Vector3(body.Center.X, body.Center.Z, -body.Center.Y);
                return childGizmos;
            }
            return null;
        }
        protected virtual void UpdateGizmos(GameObject childGizmos)
        {
            if (childGizmos != null)
            {
                var body = LocalBody;
                childGizmos.transform.localScale = new UnityEngine.Vector3(body.Radius, body.Height, body.Radius);
                childGizmos.transform.localPosition = new UnityEngine.Vector3(body.Center.X, body.Center.Z, -body.Center.Y);
                if (childGizmos.activeSelf != PreviewConfig.IsShowGizmos)
                {
                    childGizmos.SetActive(PreviewConfig.IsShowGizmos);
                }
            }
        }
        protected virtual Collider InitEditCollider()
        {
            var collider = gameObject.AddComponent<CapsuleCollider>();
            if (collider)
            {
                var body = LocalBody;
                collider.radius = body.Radius;
                collider.height = body.Height;
                collider.center = new UnityEngine.Vector3(body.Center.X, body.Center.Z + body.Height / 2f, -body.Center.Y);
                return collider;
            }
            return null;
        }
        protected virtual void UpdateCollider(Collider collider)
        {
            if (collider is CapsuleCollider c)
            {
                var body = LocalBody;
                c.radius = body.Radius;
                c.height = body.Height;
                c.center = new UnityEngine.Vector3(body.Center.X, body.Center.Z + body.Height / 2f, -body.Center.Y);
            }
        }
        public static bool TouchInRange(DeepCore.Geometry.Vector3 a, DeepCore.Geometry.Vector3 b, float range)
        {
            return DeepCore.Geometry.Vector3.DistanceSquared(a, b) <= range * range;
        }
        public static bool TouchBody(PreviewObject a, PreviewObject b)
        {
            return a.Body.Intersects(b.Body);
        }
        public static bool TouchBodyRange(PreviewObject a, PreviewObject b, float range)
        {
            var ab = a.Body;
            var bb = b.Body;
            ab.Radius += range;
            return ab.Intersects(bb);
        }
        public GameObject AppendGizmos(float bodySize, float bodyHeight, Color bodyColor)
        {
            var gizmos = VoxelGizmos.CreateVoxelCylinder(1, 1);
            {
                gizmos.transform.SetParent(transform, false);
                if (gizmos.TryGetComponent<MeshRenderer>(out var srender) && RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    srender.material = drender.material;
                    srender.material.color = bodyColor;
                }
                gizmos.transform.localScale = new UnityEngine.Vector3(bodySize, bodyHeight, bodySize);
                gizmos.transform.localPosition = UnityEngine.Vector3.zero;
            }
            return gizmos;
        }
        #endregion
        //--------------------------------------------------------------------------------------
        #region Res
        public IViewResource MainRes { get; set; }
        private List<IViewResource> resources = new List<IViewResource>();
        private Transform mainResTransform;
        private void CleanResource()
        {
            foreach (var res in resources)
            {
                res.Dispose();
            }
            resources.Clear();
        }
        public void PlaySound(string resName, ResourceType resType)
        {
            try
            {
                Proxy.PlaySound(resName, resType, this);
            }
            catch (Exception e)
            {
                UnityIPC.PLog(e);
            }
        }
        public IViewResource LoadRes(string resName, ResourceType resType)
        {
            try
            {
                var res = Proxy.LoadRes(resName, resType, this);
                if (res != null)
                {
                    if (MainRes == null)
                    {
                        MainRes = res;
                        mainResTransform = res.transform;
                    }
                    //UnityIPC.RTG.AddEditorObject(res.go);
                    resources.Add(res);
                }
                return res;
            }
            catch (Exception e)
            {
                UnityIPC.PLog(e);
            }
            return null;
        }
        public IViewResource LoadEffect(LaunchEffect effect)
        {
            if (effect != null)
            {
                var res = LoadRes(effect.Name, ResourceType.Effect);
                if (res != null)
                {
                    //                     switch (effect.BodyVoxelAnchor)
                    //                     {
                    //                         case VoxelAnchor.Floating:
                    //                             res.transform.localPosition = new UnityEngine.Vector3(0, this.BodyHeight / 2f, 0);
                    //                             break;
                    //                         case VoxelAnchor.Flooring:
                    //                             break;
                    //                         case VoxelAnchor.Ceiling:
                    //                             res.transform.localPosition = new UnityEngine.Vector3(0, this.BodyHeight, 0);
                    //                             break;
                    //                     }
                    if (effect.BindBody)
                    {
                        res.BindBody(this.MainRes, effect.BindPartName);
                    }
                    BindEffectOffset(res, this, effect);
                    res.gameObject.SetParticleEmission(true);
                    res.gameObject.PlayParticle();
                }
                if (effect.SubEffects != null)
                {
                    foreach (var sub in effect.SubEffects)
                    {
                        LoadEffect(sub);
                    }
                }
                return res;
            }
            return null;
        }
        public void ShowEffect(string resName)
        {
            //PLog($"ShowEffect : {this} : {resName}");
            var e = Proxy.CreateDisplay<EffectPlayer>($"{resName}");
            e.Position = Position;
            e.Direction = Direction;
            e.Init(new ResInfo() { ResName = resName, ResData = this });
        }
        public void ShowEffect(LaunchEffect effect)
        {
            if (effect != null)
            {
                //PLog($"ShowEffect : {this} : {effect}");
                var e = Proxy.CreateDisplay<EffectDisplay>(effect.ToString());
                e.Position = Position;
                e.Direction = Direction;
                if (e.MainRes != null)
                {
                    BindEffectOffset(e.MainRes, this, effect);
                }
                //                 switch (effect.BodyVoxelAnchor)
                //                 {
                //                     case VoxelAnchor.Floating:
                //                         e.Position += new DeepCore.Geometry.Vector3(0, 0, this.BodyHeight / 2f);
                //                         break;
                //                     case VoxelAnchor.Flooring:
                //                         break;
                //                     case VoxelAnchor.Ceiling:
                //                         e.Position += new DeepCore.Geometry.Vector3(0, 0, this.BodyHeight);
                //                         break;
                //                 }
                e.Init(new(effect, MainRes));
                if (effect.SubEffects != null)
                {
                    foreach (var eff in effect.SubEffects)
                    {
                        ShowEffect(eff);
                    }
                }
            }
        }
        public static void BindEffectOffset(IViewResource res, PreviewObject owner, LaunchEffect effect)
        {
            if (res != null)
            {
                if (effect != null && owner != null)
                {
                    var offset = TransHelper.BattleToUnityVoxelAnchorOffset(owner.BodyHeight, effect.BodyVoxelAnchor);
                    if (effect.BindingOffsetDistance != 0)
                    {
                        DeepCore.Geometry.Vector3 offset2 = DeepCore.Geometry.VectorHelper.Polar(
                            CMath.ToPI(effect.BindingOffsetAngle360),
                            effect.BindingOffsetDistance);
                        offset2.Z = effect.BindingOffsetZ;
                        offset += TransHelper.BattleToUnityOffset(offset2);
                    }
                    else
                    {
                        DeepCore.Geometry.Vector3 offset2 = new DeepCore.Geometry.Vector3(0, 0, effect.BindingOffsetZ);
                        offset += TransHelper.BattleToUnityOffset(offset2);
                    }
                    {
                        //                     var bindPart = effect.BindPartName;
                        //                     var bindBody = effect.BindBody;
                        //                     if (bindBody)
                        //                     {
                        //                         res.BindBody(binding, bindPart);
                        //                     }
                        if (effect.ScaleToBodySize != 0)
                        {
                            res.transform.localScale *= effect.ScaleToBodySize;
                        }
                    }
                    res.transform.localPosition += (offset);
                }
            }
        }

        private void UpdateResource()
        {
//             if (MainRes != null)
//             {
//                 MainRes.UpdateResource(this.gameObject);
//             }
            foreach (var res in resources)
            {
                res.UpdateResource(this.gameObject);
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------
        #region Transform
        private float? m_Direction;
        [ExposeProperty]
        public float Direction
        {
            get
            {
                if (m_Direction.HasValue == false)
                {
                    m_Direction = TransHelper.UnityToBattleRotation(transform.localRotation);
                }
                return m_Direction.Value;
            }
            set
            {
                m_Direction = value;
                transform.localRotation = TransHelper.BattleToUnityRotation(value);
            }
        }
        public void Turn(float add)
        {
            Direction += add;
        }
        [ExposeProperty]
        public DeepCore.Geometry.Vector3 Position
        {
            get { return TransHelper.UnityToBattleOffset(transform.position); }
            set { transform.position = TransHelper.BattleToUnityOffset(value); }
        }
        public DeepCore.Geometry.Vector3 WaistPosition
        {
            get => Position + new DeepCore.Geometry.Vector3(0, 0, BodyHeight / 2f);
        }
        public DeepCore.Geometry.Vector3 HeadPosition
        {
            get => Position + new DeepCore.Geometry.Vector3(0, 0, BodyHeight);
        }
        public float TopZ { get => Position.Z + BodyHeight; }
        public float WaistZ { get => Position.Z + BodyHeight / 2f; }
        public float BodyDirection => this.Direction;

        public void LookAt(Transform target)
        {
            if (target == null) return;
            var p1 = transform.position;
            var p2 = target.position;
            p2.y = p1.y;
            transform.LookAt(p2, UnityEngine.Vector3.up);
            m_Direction = TransHelper.UnityToBattleRotation(transform.localRotation);
        }
        public void LookAt(UnityEngine.Vector3 p2)
        {
            var p1 = transform.position;
            p2.y = p1.y;
            transform.LookAt(p2, UnityEngine.Vector3.up);
            m_Direction = TransHelper.UnityToBattleRotation(transform.localRotation);
        }
        public void LookAt(DeepCore.Geometry.Vector3 targetPos)
        {
            var p1 = transform.position;
            var p2 = ToUnityPosition(targetPos);
            p2.y = p1.y;
            transform.LookAt(p2, UnityEngine.Vector3.up);
            m_Direction = TransHelper.UnityToBattleRotation(transform.localRotation);
        }
        public bool MoveToTarget(DeepCore.Geometry.Vector3 target, float speedSEC, float intervalMS)
        {
            var pos = Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float ddx = target.X - pos.X;
            float ddy = target.Y - pos.Y;
            float direction = MathVector.getDegree(ddx, ddy);
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            if (Math.Abs(ddx) < distance && Math.Abs(ddy) < distance)
            {
                dx = ddx;
                dy = ddy;
                Position = new DeepCore.Geometry.Vector3(target.X, target.Y, pos.Z);
                return true;
            }
            else
            {
                Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
                return false;
            }
        }
        public void MoveTo(float direction, float speedSEC, float intervalMS)
        {
            var pos = Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
        }
        public void MoveLerp(float direction, float distance)
        {
            var pos = Position;
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
        }
        public bool MoveBlink(BlinkMove blink, PreviewObject targetUnit)
        {
            switch (blink.MType)
            {
                case BlinkMove.BlinkMoveType.MoveToForward:
                    MoveLerp(Direction + blink.DirectionOffset, blink.Distance);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToBackward:
                    MoveLerp(Direction + blink.DirectionOffset, -blink.Distance);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToTargetPos:
                    {
                        var pos = Position;
                        var tp = targetUnit.Position;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(tp, pos) < blink.Distance * blink.Distance)
                        {
                            float angle = MathVector.getDegree(pos.X, pos.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(pos.X, pos.Y, tp.X, tp.Y), blink.Distance);
                            MoveLerp(angle + blink.DirectionOffset, distance);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitFace:
                    {
                        var pos = Position;
                        var r = blink.Distance + targetUnit.BodySize;
                        var tp = targetUnit.Position;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(targetUnit.Position, pos) < r * r)
                        {
                            float angle = MathVector.getDegree(pos.X, pos.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(pos.X, pos.Y, tp.X, tp.Y), blink.Distance) - (targetUnit.BodySize + BodySize);
                            MoveLerp(angle + blink.DirectionOffset, distance);
                            LookAt(targetUnit.Position);
                            //                             Position = targetUnit.Position;
                            //                             Direction = targetUnit.Direction + CMath.PI_F + blink.DirectionOffset;
                            //                             MoveLerp(targetUnit.Direction, targetUnit.BodySize + BodySize);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitBack:
                    if (targetUnit != null)
                    {
                        var pos = Position;
                        var r = blink.Distance + targetUnit.BodySize;
                        var tp = targetUnit.Position;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(targetUnit.Position, pos) < r * r)
                        {
                            float angle = MathVector.getDegree(pos.X, pos.Y, tp.X, tp.Y);
                            float distance = Math.Min(MathVector.getDistance(pos.X, pos.Y, tp.X, tp.Y), blink.Distance) + (targetUnit.BodySize + BodySize);
                            MoveLerp(angle + blink.DirectionOffset, distance);
                            LookAt(targetUnit.Position);
                            //                             Position = targetUnit.Position;
                            //                             Direction = targetUnit.Direction + blink.DirectionOffset;
                            //                             MoveLerp(targetUnit.Direction, -(targetUnit.BodySize + BodySize));
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public bool ElasticOtherObject(PreviewObject o)
        {
            var srcp = Position;
            var dstp = o.Position;
            float ddr = MathVector.getDistance(srcp.X, srcp.Y, dstp.X, dstp.Y);
            if (ddr > 0)
            {
                var dir = MathVector.getDegree(srcp.X, srcp.Y, dstp.X, dstp.Y);
                float bdr = BodySize + o.BodySize;
                float d = bdr - ddr;
                if (d > 0)
                {
                    MoveLerp(dir, -d);
                    return true;
                }
            }
            return false;
        }

        public static UnityEngine.Vector3 ToUnityPosition(DeepCore.Geometry.Vector3 p)
        {
            return new UnityEngine.Vector3(p.X, p.Z, -p.Y);
        }
        public static UnityEngine.Vector3 ToUnityPosition(DeepCore.Geometry.Vector3 p, float bodyHeight, LaunchEffect effect)
        {
            switch (effect.BodyVoxelAnchor)
            {
                case VoxelAnchor.Floating:
                    p.Z += bodyHeight / 2;
                    break;
                case VoxelAnchor.Ceiling:
                    p.Z += bodyHeight;
                    break;
                case VoxelAnchor.Flooring:
                default:
                    break;
            }
            return new UnityEngine.Vector3(p.X, p.Z, -p.Y);
        }


        #endregion
        //--------------------------------------------------------------------------------------
        #region Motion
        private StartMoveAction mCurrentStartMove;
        private FallingDownAction mCurrentFallingDown;
        public bool CheckFallDown()
        {
            var pos = Position;
            if (pos.Z > 0 && mCurrentFallingDown == null)
            {
                StartJump(0);
                return true;
            }
            return false;
        }
        private void ClearMotion()
        {
            if (mCurrentFallingDown != null)
            {

            }
            if (mCurrentStartMove != null)
            {
                mCurrentStartMove.Stop();
                mCurrentStartMove = null;
            }
        }
        private void UpdateMotion()
        {
            if (mCurrentFallingDown != null && mCurrentFallingDown.Update(IntervalMS))
            {
                mCurrentFallingDown = null;
            }
            if (mCurrentStartMove != null && mCurrentStartMove.Update(IntervalMS))
            {
                mCurrentStartMove = null;
            }

        }
        public StartMoveAction StartHitMove(StartMove start_move)
        {
            //PLog($"StartHitMove : {this} : {start_move}");
            if (mCurrentStartMove != null)
            {
                mCurrentStartMove.Stop();
            }
            var move = new StartMoveAction(this, start_move);
            mCurrentStartMove = move;
            return move;
        }
        public StartMoveAction StartHitMove(
                float direction,
                float rotateSpeedSEC,
                int expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAccPct)
        {
            //PLog($"StartHitMove : {this} : {expectlTimeMS}");
            if (mCurrentStartMove != null)
            {
                mCurrentStartMove.Stop();
            }
            var move = new StartMoveAction(this,
                 direction,
                 rotateSpeedSEC,
                 expectlTimeMS,
                 moveSpeedSEC,
                 moveSpeedAdd,
                 moveSpeedAccPct);
            mCurrentStartMove = move;
            return move;
        }
        public FallingDownAction StartJump(float speedZ, float gravity = 0)
        {
            //PLog($"StartJump : {this} : {speedZ} : {gravity}");
            mCurrentFallingDown = new FallingDownAction(this, speedZ, gravity);
            return mCurrentFallingDown;
        }
        /// <summary>
        /// 关键帧开始移动
        /// </summary>
        public class StartMoveAction
        {
            private readonly PreviewObject mOwner;
            private readonly float mStartDirection;
            private readonly int mTotalTimeMS;
            private readonly float mMoveSpeedAdd;
            private readonly float mMoveSpeedAcc;
            private readonly float mRotateSpeedSEC;

            private float mMoveSpeedSEC;

            private TimeExpire<int> hitMoveTime;
            private FallingDownAction hasFly;
            private PreviewObject moveTarget;
            private bool moveTargetBody;
            private float moveTargetKeepRange;

            private PreviewObject blockTarget;
            private float blockTargetKeepRange;

            public DeepCore.Geometry.Vector3 PrevPos { get; private set; }
            public bool IsFly { get { return hasFly != null; } }
            public int TotalTimeMS { get => mTotalTimeMS; }
            public bool IsEnd { get; private set; }

            public StartMoveAction(
                PreviewObject owner,
                float direction,
                float rotateSpeedSEC,
                int expectlTimeMS,
                float moveSpeedSEC,
                float moveSpeedAdd,
                float moveSpeedAccPct)
            {
                mOwner = owner;
                mStartDirection = direction;

                mMoveSpeedSEC = moveSpeedSEC;
                mMoveSpeedAdd = moveSpeedAdd;
                mMoveSpeedAcc = moveSpeedAccPct / 100f;
                mRotateSpeedSEC = rotateSpeedSEC;
                mTotalTimeMS = expectlTimeMS;
                hitMoveTime = new TimeExpire<int>(mTotalTimeMS);

                IsEnd = false;

                PrevPos = owner.Position;

            }

            public StartMoveAction(PreviewObject owner, StartMove action_move)
            {
                mOwner = owner;
                mStartDirection = owner.Direction + action_move.Direction;
                if (action_move.Direction != 0)
                {
                    owner.Direction = mStartDirection;
                }
                mMoveSpeedSEC = action_move.SpeedSEC;
                mMoveSpeedAdd = action_move.SpeedAdd;
                mMoveSpeedAcc = action_move.SpeedAcc / 100f;
                mRotateSpeedSEC = action_move.RotateSpeedSEC;
                mTotalTimeMS = action_move.KeepTimeMS;
                hitMoveTime = new TimeExpire<int>(mTotalTimeMS);

                if (action_move.ZSpeedSEC != 0)
                {
                    SetFly(
                          action_move.ZSpeedSEC,
                          action_move.OverrideGravity);
                }

                IsEnd = false;
                PrevPos = owner.Position;
            }

            public void SetFly(float moveZSpeed, float gravity = 0)
            {
                if (moveZSpeed != 0)
                {
                    hasFly = mOwner.StartJump(moveZSpeed, gravity);
                }
            }
            public void SetBlockTarget(PreviewObject target, float bodyKeepRange = 0)
            {
                blockTarget = target;
                blockTargetKeepRange = bodyKeepRange;
            }
            public void SetMoveTarget(PreviewObject target, bool targetBodyBlock, float bodyKeepRange = 0)
            {
                moveTarget = target;
                moveTargetBody = targetBodyBlock;
                moveTargetKeepRange = bodyKeepRange;
            }

            public void Stop()
            {
                IsEnd = true;
            }

            public bool Update(float intervalMS)
            {
                if (mRotateSpeedSEC != 0)
                {
                    mOwner.Turn(MoveHelper.GetDistance(intervalMS, mRotateSpeedSEC));
                }
                PrevPos = mOwner.Position;
                if (!testBlock(intervalMS))
                {
                    // 移动 //
                    if (moveTarget != null)
                    {
                        move(intervalMS, moveTarget);
                    }
                    else
                    {
                        move(intervalMS, mStartDirection);
                    }
                }
                // 递增 //
                {
                    //每秒递减速度绝对值//
                    mMoveSpeedSEC = MoveHelper.UpdateSpeed(intervalMS, mMoveSpeedSEC, mMoveSpeedAdd, mMoveSpeedAcc);
                }

                if (hitMoveTime.Update(intervalMS))
                {
                    if (hasFly != null)
                    {
                        IsEnd = hasFly.IsEnd;
                    }
                    else
                    {
                        IsEnd = true;
                    }
                }
                return IsEnd;
            }
            private bool testBlock(float intervalMS)
            {
                if (blockTarget != null)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, mMoveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (TouchBodyRange(mOwner, blockTarget, distance + blockTargetKeepRange))
                    {
                        return true;
                    }
                }
                return false;
            }
            private void move(float intervalMS, PreviewObject target)
            {
                if (moveTargetBody)
                {
                    float distance = MoveHelper.GetDistance(intervalMS, mMoveSpeedSEC);
                    //if (CMath.includeRoundPoint(mOwner.X, mOwner.Y, target.RadiusSize + mOwner.RadiusSize + distance, target.X, target.Y))
                    if (TouchBodyRange(mOwner, target, distance + moveTargetKeepRange))
                    {
                        return;
                    }
                }
                mOwner.MoveToTarget(target.Position, mMoveSpeedSEC, intervalMS);
            }
            private void move(float intervalMS, float direction)
            {
                mOwner.MoveTo(direction, mMoveSpeedSEC, intervalMS);
            }


        }

        /// <summary>
        /// 落体运动
        /// </summary>
        public class FallingDownAction
        {
            private readonly PreviewObject unit;
            private readonly float startZ;

            private float zspeed;
            private float gravity;

            public PreviewObject Unit { get => unit; }
            public bool IsEnd { get; private set; }
            public float StartZ { get => startZ; }
            public float Gravity { get => gravity; }
            public FallingDownAction(PreviewObject unit, float zspeed, float zgravity)
            {
                this.unit = unit;
                gravity = zgravity == 0 ? UnityIPC.Templates.Templates.DefaultConfig.GLOBAL_GRAVITY : zgravity;
                this.zspeed = zspeed;
                startZ = unit.Position.Z;
                IsEnd = false;
            }

            public void End()
            {
                if (IsEnd) { return; }
                IsEnd = true;
                OnFallDown?.Invoke(this);
                OnFallDown = null;
            }

            public bool Update(float intervalMS)
            {
                if (ProcessGravity(intervalMS))
                {
                    End();
                    return true;
                }
                return IsEnd;
            }
            bool ProcessGravity(float intervalMS)
            {
                var currentPos = unit.Position;
                try
                {
                    currentPos.Z += CMath.GetSpeedDistance(intervalMS, zspeed);
                    zspeed -= CMath.GetSpeedDistance(intervalMS, gravity);
                    if (zspeed > 0)
                    {

                    }
                    else if (currentPos.Z <= 0f)
                    {
                        zspeed = 0;
                        currentPos.Z = 0;
                        return true;
                    }
                }
                finally
                {
                    unit.Position = currentPos;
                }
                return false;
            }
            public event Action<FallingDownAction> OnFallDown;
        }

        #endregion
        //--------------------------------------------------------------------------------------

        //         public static T[] GetAllTargets<T>() where T : PreviewObject
        //         {
        //             return Proxy.GetComponentsInChildren<T>();
        //         }
        public static List<T> GetRangeTargets<T>(DeepCore.Geometry.Vector3 pos, float range) where T : PreviewObject
        {
            var ret = new List<T>();
            foreach (var target in Proxy.GetComponentsInChildren<PreviewObject>())
            {
                if (target is T preview)
                {
                    if (DeepCore.Geometry.Vector3.DistanceSquared(pos, target.Position) <= range * range)
                    {
                        ret.Add(preview);
                    }
                }
            }
            return ret;
        }

        public static List<T> GetShapeTargets<T>(UnitAttackRangeHelper attack_range) where T : PreviewObject
        {
            var ret = new List<T>();
            foreach (var target in Proxy.GetComponentsInChildren<PreviewObject>())
            {
                if (target is T preview)
                {
                    if (attack_range.Touch(target))
                    {
                        ret.Add(preview);
                    }
                }
            }
            return ret;
        }
        public static List<T> GetShapeTargets<T>(PreviewObject launcher, float range) where T : PreviewObject
        {
            var ret = new List<T>();
            foreach (var target in Proxy.GetComponentsInChildren<PreviewObject>())
            {
                if (target is T preview)
                {
                    if (DeepCore.Geometry.Vector3.Distance(launcher.Position, target.Position) <= range)
                    {
                        ret.Add(preview);
                    }
                }
            }
            return ret;
        }
        public static UnitInfo RandomTargetInfo()
        {
            var units = UnityIPC.Templates.Templates.AllUnits.ToArray();
            return RandomN.GetRandomInArray(units);
        }

    }

    //-----------------------------------------------------------------------------------------------------------------------------------
    public abstract class PreviewObject<T> : PreviewObject
    {
        public T Data { get; private set; }

        public void Init(T data)
        {
            Data = data;
            base.Init(data);
        }
        sealed protected override void DoInit(object data)
        {
            Data = (T)data;
            DoInit((T)data);
        }
        protected virtual void DoInit(T data) { }
    }
    //-----------------------------------------------------------------------------------------------------------------------------------


    public abstract class PreviewWindow : GUIWindow
    {
        public PreviewProxy Proxy => PreviewProxy.Proxy;
        public UnityRTG RTG => UnityRTG.RTG;
    }
}

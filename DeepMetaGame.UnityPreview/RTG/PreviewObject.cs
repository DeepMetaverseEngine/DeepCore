using Cysharp.Threading.Tasks;
using DeepCore;
using DeepCore.Geometry;
using DeepCore.Unity.Expose;
using DeepCore.Unity3D;
using DeepCore.Unity3D.Voxel;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.ZoneEditor;
using UnityEngine;

namespace DeepGameEditor3D.Unity3D.Ipc.RTG
{

    public abstract class DisplayObject : MonoBehaviour, IDisposable
    {
        public static SingleThreadCollectionPool ObjectPool => UnityIPC.ObjectPool;
        //public static PreviewProxy Proxy { get => PreviewProxy.Proxy; }
        //public static EditorTemplatesData Templates { get => PreviewProxy.Templates; }
        public static UnityRTG RTG { get => UnityRTG.RTG; }
        //public static System.Random RandomN { get => PreviewProxy.RandomN; }
        //--------------------------------------------------------------------------------------
        protected async UniTask InitAsync(object data)
        {
            await DoInitAsync(data);
            Replay();
        }
        protected virtual UniTask DoInitAsync(object data) { return UniTask.CompletedTask; }

        //--------------------------------------------------------------------------------------
        public static void PLog(object message)
        {
            UnityIPC.PLog(message);
        }
        protected virtual void Awake()
        {
        }
        void Start()
        {
            try
            {
                ResetTime();
                DoStart();
                this.childGizmos = InitGizmos();
                this.childCollider = InitEditCollider();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        void Update()
        {
            try
            {
                DoUpdate();
                UpdateGizmos(this.childGizmos);
                UpdateCollider(this.childCollider);
                UpdateMotion();
                UpdateTime();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        void OnDestroy()
        {
            try
            {
                DoDestory();
                CleanResource();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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
        public void Dispose()
        {
            GameObject.Destroy(gameObject);
        }
        //--------------------------------------------------------------------------------------
        protected virtual void DoStart() { }
        protected virtual void DoReplay() { }
        protected virtual void DoUpdate() { }
        protected virtual void DoDestory() { }
        //--------------------------------------------------------------------------------------
        #region Time
        public int PassTimeMS { get => (int)(passTime); }
        public int IntervalMS { get => interval; }

        private int startTime;
        private int lastTime;
        private int passTime;
        private int interval = 0;

        public void ResetTime()
        {
            this.startTime = (int)(Time.timeSinceLevelLoad * 1000);
            this.interval = 0;
            this.passTime = 0;
            this.lastTime = 0;
        }
        private void UpdateTime()
        {
            this.lastTime = passTime;
            this.passTime = (int)(Time.timeSinceLevelLoad * 1000 - startTime);
            this.interval = (int)(passTime - lastTime);
        }
        #endregion
        //--------------------------------------------------------------------------------------
        #region Gizmos

        public float BodyHeight = 0;
        public float BodySize = 0;
        public Color BodyColor = Color.white.SetAlpha(0.5f);

        public VoxelCylinder Body
        {
            get
            {
                var body = LocalBody;
                body.Center += this.Position;
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
                childGizmos.transform.SetParent(this.transform, false);
                if (childGizmos.TryGetComponent<MeshRenderer>(out var srender) && Proxy.TempGizmoz && Proxy.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    srender.material = drender.material;
                    srender.material.color = BodyColor;
                }
                var body = this.LocalBody;
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
                var body = this.LocalBody;
                childGizmos.transform.localScale = new UnityEngine.Vector3(body.Radius, body.Height, body.Radius);
                childGizmos.transform.localPosition = new UnityEngine.Vector3(body.Center.X, body.Center.Z, -body.Center.Y);
                if (childGizmos.activeSelf != PreviewProxy.IsShowGizmos)
                {
                    childGizmos.SetActive(PreviewProxy.IsShowGizmos);
                }
            }
        }
        protected virtual Collider InitEditCollider()
        {
            var collider = gameObject.AddComponent<CapsuleCollider>();
            if (collider)
            {
                var body = this.LocalBody;
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
                var body = this.LocalBody;
                c.radius = body.Radius;
                c.height = body.Height;
                c.center = new UnityEngine.Vector3(body.Center.X, body.Center.Z + body.Height / 2f, -body.Center.Y);
            }
        }
        public static bool TouchInRange(DeepCore.Geometry.Vector3 a, DeepCore.Geometry.Vector3 b, float range)
        {
            return DeepCore.Geometry.Vector3.DistanceSquared(a, b) <= (range * range);
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
                gizmos.transform.SetParent(this.transform, false);
                if (gizmos.TryGetComponent<MeshRenderer>(out var srender) && Proxy.TempGizmoz && Proxy.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
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
        public IRes MainRes { get; set; }
        private List<IRes> resources = new List<IRes>();
//         [SerializeField]
//         private Transform mainResTransform;
        private void CleanResource()
        {
            foreach (var res in resources)
            {
                res.Dispose();
            }
            resources.Clear();
        }
        public async UniTask<IRes> LoadRes(int resID, string resName)
        {
            try
            {
                var res = await Proxy.LoadRes(resID, resName, this.gameObject);
                if (res != null)
                {
                    if (MainRes == null)
                    {
                        MainRes = res;
                        //mainResTransform = res.transform;
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
        public async UniTask<IRes> LoadEffect(LaunchEffect effect)
        {
            if (effect != null)
            {
                var res = await LoadRes(effect.ResId, effect.Name);
                return res;
            }
            return null;
        }
        public void ShowEffect(int resID, string resName)
        {
            var e = Proxy.CreateDisplay<EffectPlayer>($"{resName}({resID})"); 
            e.Position = this.Position;
            e.Direction = this.Direction;
            e.InitAsync(new ResInfo() { ResID = resID, ResName = resName, ResData = this }).ContinueWith(() => { });
        }
        public void ShowEffect(LaunchEffect effect)
        {
            if (effect != null)
            {
                var e = Proxy.CreateDisplay<EffectDisplay>(effect.ToString());
                e.Position = this.Position + effect.Offset;
                e.Direction = this.Direction;
                if (effect.SubEffects != null)
                {
                    foreach (var eff in effect.SubEffects)
                    {
                        ShowEffect(eff);
                    }
                }
                e.InitAsync(new(effect, MainRes)).ContinueWith(() => { }); ;

            }
        }

        #endregion
        //--------------------------------------------------------------------------------------
        #region Transform
        [ExposeProperty]
        public float Direction
        {
            get
            {
                var ry = this.transform.localRotation.eulerAngles.y;
                return (ry - 90) * Mathf.Deg2Rad;
            }
            set
            {
                this.transform.localRotation = UnityEngine.Quaternion.AngleAxis(value * Mathf.Rad2Deg + 90, UnityEngine.Vector3.up);
            }
        }
        [ExposeProperty]
        public DeepCore.Geometry.Vector3 Position
        {
            get
            {
                var p = this.transform.position;
                return new DeepCore.Geometry.Vector3(p.x, -p.z, p.y);
            }
            set
            {
                this.transform.position = new UnityEngine.Vector3(value.X, value.Z, -value.Y);
            }
        }
        public DeepCore.Geometry.Vector3 WaistPosition
        {
            get => this.Position + new DeepCore.Geometry.Vector3(0, 0, BodyHeight / 2f);
        }
        public float TopZ { get => Position.Z + BodyHeight; }
        public float WaistZ { get => Position.Z + BodyHeight / 2f; }

        public void LookAt(UnityEngine.Transform target)
        {
            var p1 = this.transform.position;
            var p2 = target.position;
            p2.y = p1.y;
            this.transform.LookAt(p2, UnityEngine.Vector3.up);
        }
        public void LookAt(UnityEngine.Vector3 p2)
        {
            var p1 = this.transform.position;
            p2.y = p1.y;
            this.transform.LookAt(p2, UnityEngine.Vector3.up);
        }
        public void LookAt(DeepCore.Geometry.Vector3 targetPos)
        {
            var p1 = this.transform.position;
            var p2 = ToUnityPosition(targetPos);
            p2.y = p1.y;
            this.transform.LookAt(p2, UnityEngine.Vector3.up);
        }
        public void Turn(float add)
        {
            this.Direction += add;
        }
        public bool MoveToTarget(DeepCore.Geometry.Vector3 target, float speedSEC, int intervalMS)
        {
            var pos = this.Position;
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
                this.Position = new DeepCore.Geometry.Vector3(target.X, target.Y, pos.Z);
                return true;
            }
            else
            {
                this.Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
                return false;
            }
        }
        public void MoveTo(float direction, float speedSEC, int intervalMS)
        {
            var pos = this.Position;
            float distance = MoveHelper.GetDistance(intervalMS, speedSEC);
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            this.Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
        }
        public void MoveLerp(float direction, float distance)
        {
            var pos = this.Position;
            float dx = (float)(Math.Cos(direction) * distance);
            float dy = (float)(Math.Sin(direction) * distance);
            this.Position = new DeepCore.Geometry.Vector3(pos.X + dx, pos.Y + dy, pos.Z);
        }
        public bool MoveBlink(BlinkMove blink, PreviewObject targetUnit)
        {
            switch (blink.MType)
            {
                case BlinkMove.BlinkMoveType.MoveToForward:
                    MoveLerp(this.Direction + blink.DirectionOffset, blink.Distance);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToBackward:
                    MoveLerp(this.Direction + blink.DirectionOffset, -blink.Distance);
                    return true;
                case BlinkMove.BlinkMoveType.MoveToTargetPos:
                    {
                        var pos = this.Position;
                        var tp = targetUnit.Position;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(tp, pos) < blink.Distance * blink.Distance)
                        {
                            float angle = MathVector.getDegree(pos.X, pos.Y, tp.X, tp.Y);
                            float distance = Math.Max(MathVector.getDistance(pos.X, pos.Y, tp.X, tp.Y), blink.Distance);
                            MoveLerp(angle + blink.DirectionOffset, distance);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitFace:
                    {
                        var pos = this.Position;
                        var r = blink.Distance + targetUnit.BodySize;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(targetUnit.Position, pos) < r * r)
                        {
                            this.Position = (targetUnit.Position);
                            this.Direction = (targetUnit.Direction + CMath.PI_F + blink.DirectionOffset);
                            MoveLerp(targetUnit.Direction, targetUnit.BodySize + this.BodySize);
                            return true;
                        }
                    }
                    break;
                case BlinkMove.BlinkMoveType.MoveToTargetUnitBack:
                    if (targetUnit != null)
                    {
                        var pos = this.Position;
                        var r = blink.Distance + targetUnit.BodySize;
                        if (DeepCore.Geometry.Vector3.DistanceSquared(targetUnit.Position, pos) < r * r)
                        {
                            this.Position = (targetUnit.Position);
                            this.Direction = (targetUnit.Direction + blink.DirectionOffset);
                            MoveLerp(targetUnit.Direction, -(targetUnit.BodySize + this.BodySize));
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public bool ElasticOtherObject(PreviewObject o)
        {
            var srcp = this.Position;
            var dstp = o.Position;
            float ddr = MathVector.getDistance(srcp.X, srcp.Y, dstp.X, dstp.Y);
            if (ddr > 0)
            {
                var dir = MathVector.getDegree(srcp.X, srcp.Y, dstp.X, dstp.Y);
                float bdr = (this.BodySize + o.BodySize);
                float d = (bdr - ddr);
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
                case LaunchEffect.VoxelAnchor.Floating:
                    p.Z += bodyHeight / 2;
                    break;
                case LaunchEffect.VoxelAnchor.Ceiling:
                    p.Z += bodyHeight;
                    break;
                case LaunchEffect.VoxelAnchor.Flooring:
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
            var pos = this.Position;
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
                this.mOwner = owner;
                this.mStartDirection = direction;

                this.mMoveSpeedSEC = moveSpeedSEC;
                this.mMoveSpeedAdd = moveSpeedAdd;
                this.mMoveSpeedAcc = moveSpeedAccPct / 100f;
                this.mRotateSpeedSEC = rotateSpeedSEC;
                this.mTotalTimeMS = expectlTimeMS;
                this.hitMoveTime = new TimeExpire<int>(mTotalTimeMS);

                this.IsEnd = false;

                this.PrevPos = owner.Position;

            }

            public StartMoveAction(PreviewObject owner, StartMove action_move)
            {
                this.mOwner = owner;
                this.mStartDirection = owner.Direction + action_move.Direction;
                if (action_move.Direction != 0)
                {
                    owner.Direction = (mStartDirection);
                }
                this.mMoveSpeedSEC = action_move.SpeedSEC;
                this.mMoveSpeedAdd = action_move.SpeedAdd;
                this.mMoveSpeedAcc = action_move.SpeedAcc / 100f;
                this.mRotateSpeedSEC = action_move.RotateSpeedSEC;
                this.mTotalTimeMS = action_move.KeepTimeMS;
                this.hitMoveTime = new TimeExpire<int>(mTotalTimeMS);

                if (action_move.ZSpeedSEC != 0)
                {
                    this.SetFly(
                          action_move.ZSpeedSEC,
                          action_move.OverrideGravity);
                }

                this.IsEnd = false;
                this.PrevPos = owner.Position;
            }

            public void SetFly(float moveZSpeed, float gravity = 0)
            {
                if (moveZSpeed != 0)
                {
                    this.hasFly = mOwner.StartJump(moveZSpeed, gravity);
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
                this.IsEnd = true;
            }

            public bool Update(int intervalMS)
            {
                if (mRotateSpeedSEC != 0)
                {
                    mOwner.Turn(MoveHelper.GetDistance(intervalMS, mRotateSpeedSEC));
                }
                this.PrevPos = mOwner.Position;
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
                    MoveHelper.UpdateSpeed(intervalMS, ref mMoveSpeedSEC, mMoveSpeedAdd, mMoveSpeedAcc);
                }

                if (hitMoveTime.Update(intervalMS))
                {
                    if (hasFly != null)
                    {
                        this.IsEnd = hasFly.IsEnd;
                    }
                    else
                    {
                        IsEnd = true;
                    }
                }
                return IsEnd;
            }
            private bool testBlock(int intervalMS)
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
            private void move(int intervalMS, PreviewObject target)
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
            private void move(int intervalMS, float direction)
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
                this.gravity = zgravity == 0 ? Templates.DefaultConfig.GLOBAL_GRAVITY : zgravity;
                this.zspeed = zspeed;
                this.startZ = unit.Position.Z;
                this.IsEnd = false;
            }

            public void End()
            {
                if (IsEnd) { return; }
                IsEnd = true;
                OnFallDown?.Invoke(this);
                OnFallDown = null;
            }

            public bool Update(int intervalMS)
            {
                if (ProcessGravity(intervalMS))
                {
                    End();
                    return true;
                }
                return IsEnd;
            }
            bool ProcessGravity(int intervalMS)
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
    }

    //-----------------------------------------------------------------------------------------------------------------------------------
    public abstract class PreviewObject<T> : PreviewObject
    {
        public T Data { get; private set; }

        public UniTask InitAsync(T data)
        {
            this.Data = data;
            return base.InitAsync(data);
        }
        sealed protected override UniTask DoInitAsync(object data)
        {
            this.Data = (T)data;
            return this.DoInitAsync((T)data);
        }
        protected virtual UniTask DoInitAsync(T data) { return UniTask.CompletedTask; }

    }

}

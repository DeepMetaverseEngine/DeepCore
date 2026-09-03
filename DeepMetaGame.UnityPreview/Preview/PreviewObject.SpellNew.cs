using DeepCore;
using DeepCore.Geometry;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using UnityEngine;
using Vector3 = DeepCore.Geometry.Vector3;

namespace DeepMetaGame.Unity.Preview.Preview
{

    partial class PreviewObject
    {

        //--------------------------------------------------------------------------------------
        public class SpellNode : PreviewObject<SpellNode.AddSpell>, IZoneSpell
        {
            public class SpellChainLevelInfo
            {
                private readonly LaunchSpell srcLaunch;
                private int mSpellID;
                private HashSet<PreviewUnit> mChainList;
                private int mChainLevel;
                private PreviewUnit mLastTarget;

                public int Level
                {
                    get { return mChainLevel; }
                }
                public int SpellID
                {
                    get { return mSpellID; }
                }

                public PreviewUnit LastTarget
                {
                    get { return mLastTarget; }
                }

                public bool IsNextChain
                {
                    get => mChainLevel < srcLaunch.ChainLevel;
                }

                public SpellChainLevelInfo(LaunchSpell launch)
                {
                    srcLaunch = launch;
                    mSpellID = launch.SpellID;
                    mChainLevel = launch.ChainLevel;
                }
                public bool TryLaunch(int spellID)
                {
                    if (spellID == mSpellID)
                    {
                        mChainLevel--;
                        return (mChainLevel >= 0);
                    }
                    //mSpellID = spellID;
                    return true;
                }
                public void AddTarget(PreviewUnit target)
                {
                    if (mChainList == null) mChainList = new HashSet<PreviewUnit>();
                    mChainList.Add(target);
                    mLastTarget = target;
                }
                public bool ContainsTarget(PreviewUnit target)
                {
                    if (mChainList == null) return false;
                    return mChainList.Contains(target);
                }
            }
            public bool IsFromSpellMagnitude
            {
                get
                {
                    if (this.Data.launch.FromSpellMagnitude)
                    {
                        if (this.sender is IZoneSpell senderSpell && senderSpell.StartNormal.HasValue)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            //----------------------------------------------------------------------------------------------
            #region IZoneSpell
            ISpellMotion IZoneSpell.Motion => this.motion;
            public bool IsFinish => isEnd;
            public bool IsHitted { get; private set; }
            SpellTemplate IZoneSpell.Template => this.spell;
            LaunchSpell IZoneSpell.LaunchData => this.Data.launch;
            IZoneObject IZoneSpell.Sender => this.sender;
            IZoneUnit IZoneSpell.LauncherUnit => this.launcher;
            bool IZoneSpell.IsNextChain => (mChainInfo != null && mChainInfo.IsNextChain);
            double IZoneSpell.PassTimeMS => this.PassTimeMS;
            bool IZoneSpell.IsForceSync => true;
            float IZoneSpell.StartSpeed { get => spell.MSpeedSEC; set { } }
            Vector3 IZoneSpell.RemotePosition => this.Position;
            Vector3 IZoneSpell.PrevPos => this.mPrvePos;

            //---------------------------------------------------------------------
            public Vector3? StartNormal { get; set; }
            public Vector3? RayTouchPoint { get; set; }
            float IZoneSpell.SpellDistance { get => mDistance; set => mDistance = value; }
            float IZoneSpell.SpellSize { get => mBaseSize; set => mBaseSize = value; }
            float IZoneSpell.SpellDisplayDistance { get => mDistance; set => mDistance = value; }
            float IZoneSpell.SpellDisplaySize { get => mBaseSize; set => mBaseSize = value; }

            Vector3? IZoneSpell.TargetPos { get => this.targetPos; set => this.targetPos = value.Value; }
            IZoneUnit IZoneSpell.TargetUnit { get => this.target as IZoneUnit; set => this.target = value as PreviewUnit; }
            void IZoneSpell.FaceTo(float dir) => this.Direction = (dir);
            void IZoneSpell.FaceTo(Vector3 dir) => this.LookAt(dir);
            void IZoneSpell.Turn(float dir) => this.Turn(dir);
            void IZoneSpell.SetPosition(Vector3 position) => this.Position = (position);
            //---------------------------------------------------------------------
            bool IZoneSpell.TrySeekAttackable(float range, bool postEvent, out IZoneUnit target)
            {
                target = seekAttackable(range);
                return target != null;
            }
            bool IZoneSpell.TryRayCastTouchEndUnit(VoxelStripe ray, out IZoneUnit target)
            {
                target = null;
                return false;
            }
            bool IZoneSpell.CheckBinding(IZoneObject target) => true;
            bool IZoneSpell.CheckRemoveOnBindingSkillOver(IZoneUnit target)
            {
                return false;
            }
            public void Finish(bool destoryImmediately)
            {
                if (destoryImmediately)
                {
                    isEnd = true;
                }
                else
                {
                    if (spell.DestoryTimeMS > 0)
                    {
                        finishExpire = new TimeExpire(spell.DestoryTimeMS);
                    }
                    isEnd = true;
                }
            }
            #endregion
            //----------------------------------------------------------------------------------------------
            public class AddSpell
            {
                public SpellTemplate spell;
                public PreviewObject sender;
                public PreviewUnit launcher;
                public LaunchSpell launch;
                public PreviewUnit target;
                public DeepCore.Geometry.Vector3 targetPos;
                public DeepCore.Geometry.Vector3 startPos;
                public float startDirection;
                public SpellChainLevelInfo chain;
            }
            public SpellTemplate spell => Data.spell;
            public PreviewObject sender => Data.sender;
            public PreviewUnit launcher => Data.launcher;
            public LaunchSpell launch => Data.launch;
            public SpellChainLevelInfo ChainInfo => mChainInfo;
            public PreviewUnit target { get; private set; }
            public Vector3? targetPos { get; private set; }

            private SpellChainLevelInfo mChainInfo;
            private PopupKeyFrames<SpellTemplate.KeyFrame> current_frames = new PopupKeyFrames<SpellTemplate.KeyFrame>();
            private DeepCore.Geometry.Vector3 startPos;
            //private DeepCore.Geometry.Vector3? startNormal;
            // private float startDirection;
            private TimeInterval<SpellTemplate.KeyFrame> mHitIntervalTicker;
            private HashMap<PreviewObject, double> hitted = new();
            private float mBaseSize;
            private float mDistance;
            //             private float mSpeed;
            //             private float mSpeedZ;
            //             private float mDistanceSpeed;
            //             private float mRotateSpeed;
            private int affectCount;
            private bool isEnd = false;
            private ISpellMotion motion;
            private TimeExpire finishExpire;
            private DeepCore.Geometry.Vector3 mPrvePos;
            public float Distance => mDistance;
            public Transform Bone1 { get; private set; }
            public Transform Bone2 { get; private set; }
            public override string ToString()
            {
                return $"Spell:{spell?.Name}";
            }
            protected override void DoInit(AddSpell add)
            {
                this.target = add.target;
                this.targetPos = add.targetPos;
                mChainInfo = add.chain;
                this.Position = add.startPos;
                this.startPos = add.startPos;
                //                 startDirection = add.startDirection;
                base.BodySize = spell.BodySize;
                base.BodyHeight = spell.BodyHeight;
                BodyColor = Color.yellow.SetAlpha(0.5f);
                mBaseSize = spell.BodySize;
                mDistance = spell.Distance;
                //                 mSpeed = spell.MSpeedSEC;
                //                 mRotateSpeed = spell.RotateSpeedSEC;
                //                 mDistanceSpeed = 0;
                mHitIntervalTicker = new TimeInterval<SpellTemplate.KeyFrame>(spell.HitIntervalMS);
                mHitIntervalTicker.Tag = spell.HitIntervalKeyFrame;
                mPrvePos = Position;
                this.motion = ZoneDataFactory.Factory.CreateSpellMotion(this);
                this.motion.Init(this);
                OnAdd();
                var res = LoadRes(spell.FileName, DeepMetaGame.Data.ResourceType.Effect);
                if (res != null)
                {
                    Proxy.PlayEffect(res, spell.LifeTimeMS);
                    if (res.transform.TryGetComponentsInChildren<ParticleSystem>(out var pss))
                    {
                        foreach (var p in pss)
                        {
                            p.scalingMode = ParticleSystemScalingMode.Hierarchy;
                            p.Simulate(0, true, true);
                            p.Play();
                        }
                    }
                    if (!string.IsNullOrEmpty(spell.BonesBegin))
                    {
                        Bone1 = res.gameObject.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == spell.BonesBegin));
                    }
                    if (!string.IsNullOrEmpty(spell.BonesEnd))
                    {
                        Bone2 = res.gameObject.FindDeep(new Func<Transform, bool>((t) => t.gameObject.name == spell.BonesEnd));
                    }
                }
                if (spell.BindingEffect != null)
                {
                    LoadEffect(spell.BindingEffect);
                }
                PlaySound(spell.SoundName, DeepMetaGame.Data.ResourceType.Sound_Effect);
                ShowEffect(spell.FileNameSpawn);
                ShowEffect(spell.SpawnEffect);
                updateResource();
            }
            protected override void DoDestory()
            {
                current_frames.Clear();
                ShowEffect(spell.FileNameDestory);
                ShowEffect(spell.DestoryEffect);
                this.motion?.Dispose();
            }
            private void OnAdd()
            {
                this.motion.OnAdded();
                this.startPos = this.Position;

                if (target != null)
                {
                    if (spell.TargetEffect != null)
                    {
                        target.ShowEffect(spell.TargetEffect);
                    }
                }
            }
            //--------------------------------------------------------------------------------------------------------------
            public override VoxelCylinder LocalBody
            {
                get
                {
                    var height = BodyHeight;
                    var pos = spell.AdjustVoxelAnchor(DeepCore.Geometry.Vector3.Zero, ref height);
                    return new VoxelCylinder(pos, BodySize, height);
                }
            }
            protected override GameObject InitGizmos()
            {
                var height = spell.BodyHeight;
                var pos = spell.AdjustVoxelAnchor(DeepCore.Geometry.Vector3.Zero, ref height);
                var shapeObject = AttackShapeGizmos.CreateAttackShape(
                      spell.AsBodyShape,
                      spell.BodySize,
                      height,
                      spell.Distance,
                      spell.FanAngle,
                      spell.RectWide,
                      target?.transform,
                      sender.transform,
                      launcher.transform);
                if (shapeObject != null)
                {
                    shapeObject.transform.SetParent(transform, false);
                    shapeObject.transform.localPosition = new UnityEngine.Vector3(pos.X, pos.Z, pos.Y);
                    shapeObject.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
                    if (RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                    {
                        AttackShapeGizmos.SetMaterial(
                            shapeObject,
                            drender.material,
                            BodyColor);
                    }
                    return shapeObject;
                }
                return null;
            }
            protected override void UpdateGizmos(GameObject childGizmos)
            {
                if (childGizmos != null)
                {
                    var aoeFactor = 1f;
                    switch (spell.BodyShape)
                    {
                        case SpellTemplate.Shape.LineToTargetPos:
                        case SpellTemplate.Shape.LineToTarget:
                        case SpellTemplate.Shape.LineToStart:
                        case SpellTemplate.Shape.LineToSender:
                            break;
                        case SpellTemplate.Shape.Strip:
                        case SpellTemplate.Shape.StripRay:
                        case SpellTemplate.Shape.StripRayTouchEnd:
                        case SpellTemplate.Shape.RectStrip:
                        case SpellTemplate.Shape.RectStripRay:
                        case SpellTemplate.Shape.WideStrip:
                            aoeFactor = mDistance / spell.Distance;
                            childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, 1f);
                            break;
                        default:
                            aoeFactor = mBaseSize / spell.BodySize;
                            childGizmos.transform.localScale = new UnityEngine.Vector3(aoeFactor, 1f, aoeFactor);
                            break;
                    }
                    if (childGizmos.activeSelf != PreviewConfig.IsShowGizmos)
                    {
                        childGizmos.SetActive(PreviewConfig.IsShowGizmos);
                    }
                }
            }
            public bool TouchSpellRange(PreviewObject target)
            {
                var height = spell.BodyHeight;
                var pos = spell.AdjustVoxelAnchor(Position, ref height);
                var attack_range = new UnitAttackRangeHelper(this)
                {
                    Shape = spell.AsBodyShape,
                    AttackRange = spell.BodySize,
                    Distance = spell.Distance,
                    Height = height,
                    Position = pos,

                    Direction = Direction,
                    FanAngle = spell.FanAngle,
                    StripWide = spell.RectWide,
                    OffsetRadius = 0,
                };
                return attack_range.Touch(target);
            }
            public List<PreviewUnit> GetShapeAttackable(SkillTemplate.CastTarget expectTarget)
            {
                var height = spell.BodyHeight;
                var pos = spell.AdjustVoxelAnchor(Position, ref height);
                var attack_range = new UnitAttackRangeHelper(this)
                {
                    Shape = spell.AsBodyShape,
                    AttackRange = mBaseSize,
                    Distance = mDistance,
                    Height = height,
                    Position = pos,

                    Direction = Direction,
                    FanAngle = spell.FanAngle,
                    StripWide = spell.RectWide,
                    OffsetRadius = 0,
                };
                var list = GetShapeTargets<PreviewUnit>(attack_range);
                foreach (var target in list.ToArray())
                {
                    if (expectTarget == SkillTemplate.CastTarget.EveryOne)
                    {

                    }
                    else if (expectTarget == SkillTemplate.CastTarget.Self)
                    {
                        if (target != launcher)
                        {
                            list.Remove(target);
                        }
                    }
                    else
                    {
                        if (target == launcher)
                        {
                            list.Remove(target);
                        }
                    }
                }
                return list;
            }

            //--------------------------------------------------------------------------------------------------------------
            protected override void DoReplay()
            {
                current_frames.Clear();
                current_frames.AddRange(spell.KeyFrames);
            }
            protected override void DoUpdate()
            {
                mPrvePos = this.Position;
                this.BodySize = mBaseSize;
                updateMotion();
                updateResource();
                if (isEnd)
                {
                    switch (spell.MType)
                    {
                        case SpellTemplate.MotionType.Cannon:
                            if (!IsHitted)
                            {
                                var list = GetShapeAttackable(spell.ExpectTarget);
                                affectToMulti(spell.HitOnExplosionKeyFrame, list);
                                isEnd = true;
                            }
                            break;
                    }
                    if (finishExpire != null)
                    {
                        if (finishExpire.Update(IntervalMS))
                        {
                            affectToDummy(spell.LastKeyFrame);
                            Dispose();
                        }
                    }
                    else
                    {
                        affectToDummy(spell.LastKeyFrame);
                        Dispose();
                    }
                    return;
                }
                else
                {
                    updateKeyFrames();
                    if (hitted.Count > 0 && spell.CleanHitIntervalMS > 0)
                    {
                        var curTime = PassTimeMS;
                        foreach (var kv in hitted.ToArray())
                        {
                            if (kv.Value + spell.CleanHitIntervalMS < curTime)
                            {
                                hitted.Remove(kv.Key);
                            }
                        }
                    }
                    if (spell.MaxHitCount > 0 && affectCount >= spell.MaxHitCount)
                    {
                        Finish(false);
                    }
                    else if (spell.LifeTimeMS <= 0)
                    {
                        if (sender == null || !sender.enabled)
                        {
                            Finish(false);
                        }
                    }
                    else if (PassTimeMS > spell.LifeTimeMS)
                    {
                        Finish(false);
                    }
                }
            }
            private void updateMotion()
            {
                this.motion.UpdateMotion(IntervalMS);
            }

            private void updateKeyFrames()
            {
                switch (spell.MType)
                {
                    case SpellTemplate.MotionType.Missile:
                    case SpellTemplate.MotionType.SeekerMissile:
                        //【战斗】Missile类技能，在目标消失后失效//
                        if (target != null)
                        {
                            if (TouchSpellRange(target))
                            {
                                affectToSingle(spell.HitOnExplosionKeyFrame, target);
                                isEnd = true;
                            }
                            else
                            {
                                updateKeyFramesToDummy();
                            }
                        }
                        else
                        {
                            updateKeyFramesToDummy();
                        }
                        break;
                    case SpellTemplate.MotionType.Cannon:
                        if (isEnd)
                        {
                            var list = GetShapeAttackable(spell.ExpectTarget);
                            affectToMulti(spell.HitOnExplosionKeyFrame, list);
                            isEnd = true;
                        }
                        else
                        {
                            updateKeyFramesToDummy();
                        }
                        break;
                    case SpellTemplate.MotionType.Chain:
                        if (target != null)
                        {
                            if (TouchInRange(Position, target.WaistPosition, spell.Distance))
                            {
                                updateKeyFrameSingleTarget(target, true);
                            }
                            else
                            {
                                isEnd = true;
                            }
                        }
                        break;
                    default:
                        if (spell.BodyShape == SpellTemplate.Shape.LineToTargetPos) {
                            if (targetPos != null)
                            {
                                updateKeyFrameRanged();
                            }
                        } else
                        if (spell.BodyShape == SpellTemplate.Shape.LineToTarget ||
                               spell.BodyShape == SpellTemplate.Shape.LineToStart ||
                               spell.BodyShape == SpellTemplate.Shape.LineToSender)
                        {
                            if (target != null)
                            {
                                if (TouchInRange(Position, target.WaistPosition, spell.Distance))
                                {
                                    updateKeyFrameSingleTarget(target, true);
                                }
                                else
                                {
                                    updateKeyFrameSingleTarget(target, false);
                                }
                            }
                            else if (targetPos != null)
                            {
                                updateKeyFrameRanged();
                            }
                        }
                        else
                        {
                            updateKeyFrameRanged();
                        }
                        break;
                }
            }
            public float ResourceScale
            {
                get
                {
                    switch (spell.BodyShape)
                    {
                        case SpellTemplate.Shape.LineToTargetPos:
                        case SpellTemplate.Shape.LineToTarget:
                        case SpellTemplate.Shape.LineToStart:
                        case SpellTemplate.Shape.LineToSender:
                            return 1f;
                        case SpellTemplate.Shape.Strip:
                        case SpellTemplate.Shape.StripRay:
                        case SpellTemplate.Shape.StripRayTouchEnd:
                        case SpellTemplate.Shape.RectStrip:
                        case SpellTemplate.Shape.RectStripRay:
                        case SpellTemplate.Shape.WideStrip:
                            return mDistance / spell.Distance;
                        default:
                            return mBaseSize / spell.BodySize;
                    }
                }
            }
            //             public float ResourceFitSize
            //             {
            //                 get
            //                 {
            //                     switch (spell.BodyShape)
            //                     {
            //                         case SpellTemplate.Shape.LineToTarget:
            //                         case SpellTemplate.Shape.LineToStart:
            //                         case SpellTemplate.Shape.LineToSender:
            //                             return 1f;
            //                         case SpellTemplate.Shape.Strip:
            //                         case SpellTemplate.Shape.StripRay:
            //                         case SpellTemplate.Shape.StripRayTouchEnd:
            //                         case SpellTemplate.Shape.RectStrip:
            //                         case SpellTemplate.Shape.RectStripRay:
            //                         case SpellTemplate.Shape.WideStrip:
            //                             return mDistance;
            //                         default:
            //                             return mBaseSize * 2;
            //                     }
            //                 }
            //             }
            private UnityEngine.Vector3? oldPos;
            protected virtual void updateResource()
            {
                if (MainRes != null && MainRes.gameObject != null)
                {
                    //                     if (spell.FitOwnerScale)
                    //                     {
                    //                         MainRes.transform.localScale = new UnityEngine.Vector3(ResourceFitSize, BodyHeight, ResourceFitSize);
                    //                     }
                    //                     if (spell.FileBodyScale != 1f && spell.FileBodyScale != 0)
                    //                     {
                    //                         MainRes.transform.localScale = UnityEngine.Vector3.one * spell.FileBodyScale;
                    //                     }
                    var res = MainRes;
                    var scale = UnityEngine.Vector3.one * ResourceScale;
                    if (spell.FileBodyScale != 1f && spell.FileBodyScale != 0)
                    {
                        scale *= spell.FileBodyScale;
                    }
                    res.transform.localScale = scale;
                }
                switch (spell.MType)
                {
                    case SpellTemplate.MotionType.Chain:
                        if (sender != null && target != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(target.WaistPosition);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                }
                switch (spell.BodyShape)
                {
                    case SpellTemplate.Shape.LineToStart:
                        if (startPos != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(startPos);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                    case SpellTemplate.Shape.LineToTarget:
                        if (target != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(target.WaistPosition);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                    case SpellTemplate.Shape.LineToTargetPos:
                        if (targetPos != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(targetPos.Value);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                    case SpellTemplate.Shape.LineToSender:
                        if (sender != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(sender.WaistPosition);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                    case SpellTemplate.Shape.RectStrip:
                    case SpellTemplate.Shape.RectStripRay:
                    case SpellTemplate.Shape.Strip:
                    case SpellTemplate.Shape.StripRay:
                    case SpellTemplate.Shape.WideStrip:
                        break;
                    case SpellTemplate.Shape.StripRayTouchEnd:
                        if (target != null)
                        {
                            var p1 = this.transform.position;
                            var p2 = TransHelper.BattleToUnityOffset(target.WaistPosition);
                            if (Bone1) { Bone1.position = p1; }
                            if (Bone2) { Bone2.position = p2; }
                        }
                        break;
                }
                if (oldPos.HasValue)
                {
                    if (spell.IsProjectile && spell.ResFaceToMotion)
                    {
                        this.transform.LookAt(this.transform.position + (this.transform.position - oldPos.Value));
                    }
                }
                oldPos = this.transform.position;
            }
            private void affectToDummy(SpellTemplate.KeyFrame kf)
            {
                if (kf == null) return;
                if (kf.Effect != null)
                {
                    ShowEffect(kf.Effect);
                }
                if (kf.Spell != null)
                {
                    SpellLaunchSpell(this, launcher, kf.Spell, target);
                }
                IsHitted = true;
                affectCount++;
            }
            private void affectToSingle(SpellTemplate.KeyFrame kf, PreviewUnit target)
            {
                if (kf == null) return;
                if (kf.Effect != null)
                {
                    ShowEffect(kf.Effect);
                }
                // 法术造成伤害
                if (kf.Attack != null)
                {
                    LaunchAttack(this, launcher, kf.Attack, target);
                }
                if (kf.Spell != null)
                {
                    SpellLaunchSpell(this, launcher, kf.Spell, target);
                }
                IsHitted = true;
                affectCount++;
            }
            private void affectToMulti(SpellTemplate.KeyFrame kf, List<PreviewUnit> list)
            {
                if (kf == null) return;

                if (kf.Effect != null)
                {
                    ShowEffect(kf.Effect);
                }
                foreach (var tgt in list)
                {
                    // 法术造成伤害
                    if (kf.Attack != null)
                    {
                        LaunchAttack(this, launcher, kf.Attack, tgt);
                    }
                    affectCount++;
                    IsHitted = true;
                }
                // 法术产生法术
                if (kf.Spell != null)
                {
                    SpellLaunchSpell(this, launcher, kf.Spell, target);
                }
            }


            private void updateKeyFrameRanged()
            {
                using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
                {
                    int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                    bool is_interval_test = mHitIntervalTicker.Update(IntervalMS);
                    if (kfs_count > 0 || is_interval_test || spell.HitIntervalMS == 0)
                    {
                        var list = GetShapeAttackable(spell.ExpectTarget);
                        {
                            if (kfs_count > 0)
                            {
                                for (int i = 0; i < kfs.Count; i++)
                                {
                                    affectToMulti(kfs[i], list);
                                }
                            }
                            if (spell.HitOnExplosion)
                            {
                                if (list.Count > 0)
                                {
                                    // 击中后爆炸
                                    affectToMulti(spell.HitOnExplosionKeyFrame, list);
                                    isEnd = true;
                                }
                            }
                            else if (spell.HitIntervalMS == 0)
                            {
                                if (list.Count > 0)
                                {
                                    // 只在接触后第一次产生效果
                                    foreach (var e in list.ToArray())
                                    {
                                        if (hitted.ContainsKey(e)) { list.Remove(e); }
                                        else { hitted.Add(e, PassTimeMS); }
                                    }
                                }
                                if (list.Count > 0)
                                {
                                    affectToMulti(spell.HitIntervalKeyFrame, list);
                                }
                            }
                            else if (is_interval_test)
                            {
                                // 间隔产生效果
                                affectToMulti(spell.HitIntervalKeyFrame, list);
                            }
                        }
                    }
                }
            }
            private void updateKeyFrameSingleTarget(PreviewUnit target, bool affect)
            {
                using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
                {
                    int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                    bool is_interval_test = mHitIntervalTicker.Update(IntervalMS);
                    if (affect)
                    {
                        if (kfs_count > 0 || is_interval_test || spell.HitIntervalMS == 0)
                        {
                            {
                                if (kfs_count > 0)
                                {
                                    for (int i = 0; i < kfs.Count; i++)
                                    {
                                        affectToSingle(kfs[i], target);
                                    }
                                }
                                if (spell.HitOnExplosion)
                                {
                                    // 击中后爆炸
                                    affectToSingle(spell.HitOnExplosionKeyFrame, target);
                                    isEnd = true;
                                }
                                else if (spell.HitIntervalKeyFrame != null)
                                {
                                    if (spell.HitIntervalMS == 0)
                                    {
                                        if (!hitted.ContainsKey(target))
                                        {
                                            hitted.Add(target, PassTimeMS);
                                            // 只在接触后第一次产生效果
                                            affectToSingle(spell.HitIntervalKeyFrame, target);
                                        }
                                    }
                                    else if (is_interval_test)
                                    {
                                        // 间隔产生效果
                                        affectToSingle(spell.HitIntervalKeyFrame, target);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (kfs_count > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                affectToDummy(kfs[i]);
                            }
                        }
                    }
                }
            }
            private void updateKeyFramesToDummy()
            {
                using (var kfs = ObjectPool.AllocList<SpellTemplate.KeyFrame>())
                {
                    int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                    {
                        if (kfs_count > 0)
                        {
                            for (int i = 0; i < kfs.Count; i++)
                            {
                                affectToDummy(kfs[i]);
                            }
                        }
                    }
                }
            }
            public PreviewUnit seekAttackable(float range)
            {
                var list = GetRangeTargets<PreviewUnit>(this.Position, range);
                if (list.Count > 0)
                {
                    switch (spell.SeekingExpectTarget)
                    {
                        case LaunchSkill.SeekingExpect.Random:
                            //case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                            CUtils.RandomList(RandomN, list);
                            break;
                        case LaunchSkill.SeekingExpect.Nearest:
                            //case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                            list.Sort(new ObjectSorterNearest(this.Position));
                            break;
                        case LaunchSkill.SeekingExpect.Farthest:
                            //case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                            list.Sort(new ObjectSorterFarthest(this.Position));
                            break;
                    }
                    return list[0];
                }
                return null;
            }

            //------------------------------------------------------------------------------------------------------
            #region __Motion__
            //             public void faceTo(PreviewObject target)
            //             {
            //                 LookAt(target.Position);
            //             }
            //             public void faceTo(DeepCore.Geometry.Vector3 targetPos)
            //             {
            //                 LookAt(targetPos);
            //             }
            //
            // 
            //             public void moveTo(float direction, float intervalMS)
            //             {
            //                 var pos = Position;
            //                 float distance = MoveHelper.GetDistance(intervalMS, mSpeed);
            //                 VectorHelper.MovePolar(ref pos, direction, distance);
            //                 if (spell.BodyVoxelAnchor == VoxelAnchor.Flooring)
            //                 {
            //                     pos.Z = 0f;
            //                 }
            //                 Position = pos;
            //             }
            //             public void moveLerp(DeepCore.Geometry.Vector3 normal, float intervalMS)
            //             {
            //                 var pos = Position;
            //                 float distance = MoveHelper.GetDistance(intervalMS, mSpeed);
            //                 pos = VectorHelper.MoveLerp(pos, normal, distance);
            //                 Position = pos;
            //             }
            //             public bool traceToTarget(DeepCore.Geometry.Vector3 targetPos, float intervalMS)
            //             {
            //                 var pos = Position;
            //                 float distance = MoveHelper.GetDistance(intervalMS, mSpeed);
            //                 var ret = VectorHelper.MoveTo3D(ref pos, in targetPos, distance);
            //                 Position = pos;
            //                 return ret;
            //             }
            //             public void traceToTargetTunning(DeepCore.Geometry.Vector3 targetPos, float tunningSpeedSEC, float intervalMS)
            //             {
            //                 var pos = Position;
            //                 var dir = Direction;
            //                 MoveHelper.MoveToTargetTunning(ref pos, ref dir, targetPos, mSpeed, tunningSpeedSEC, intervalMS);
            //                 Position = pos;
            //                 Direction = dir;
            //             }
            //             public bool projectileToTarget(DeepCore.Geometry.Vector3 targetPos, float intervalMS)
            //             {
            //                 var pos = Position;
            //                 if (mSpeedZ < 0 && pos.Z <= targetPos.Z) return true;
            //                 var distance = MotionHelper.GetDistance(intervalMS, mSpeed);
            //                 {
            //                     var totalDistanceQ = VectorHelper.GetDistanceSquare(startPos.X, startPos.Y, targetPos.X, targetPos.Y);
            //                     var targetDistanceQ = VectorHelper.GetDistanceSquare(startPos.X, startPos.Y, pos.X, pos.Y);
            //                     if (targetDistanceQ >= totalDistanceQ)
            //                     {
            //                         distance = 0;
            //                     }
            //                 }
            //                 var offsetZ = MotionHelper.GetDistance(intervalMS, mSpeedZ);
            //                 var gravity = spell.MCannonGravitySEC > 0 ? spell.MCannonGravitySEC : Templates.DefaultConfig.GLOBAL_GRAVITY;
            //                 //var dir = VectorHelper.GetDegree(pos, targetPos);
            //                 mSpeedZ -= MotionHelper.GetDistance(intervalMS, gravity);
            //                 if (distance != 0)
            //                 {
            //                     VectorHelper.MovePolar(ref pos, startDirection, distance);
            //                 }
            //                 pos.Z += offsetZ;
            //                 var newPos = ToUnityPosition(pos);
            //                 transform.LookAt(newPos);
            //                 if (mSpeedZ < 0 && pos.Z < targetPos.Z)
            //                 {
            //                     pos.Z = targetPos.Z;
            //                     Position = pos;
            //                     return true;
            //                 }
            //                 else
            //                 {
            //                     Position = pos;
            //                     return false;
            //                 }
            //             }
            //             private DeepCore.Geometry.Vector3 updateBinding(PreviewObject target)
            //             {
            //                 if (spell.IsBinding)
            //                 {
            //                     if (spell.IsBindingDirection)
            //                     {
            //                         Direction = target.Direction;
            //                     }
            //                     Position = GetBindingPos(target);
            //                     //                     var bindingP = target.WaistPosition;
            //                     //                     switch (spell.BodyVoxelAnchor)
            //                     //                     {
            //                     //                         case VoxelAnchor.Ceiling:
            //                     //                             bindingP.Z = target.TopZ + spell.BindingOffsetZ;
            //                     //                             break;
            //                     //                         case VoxelAnchor.Floating:
            //                     //                             bindingP.Z = target.WaistZ + spell.BindingOffsetZ;
            //                     //                             break;
            //                     //                         case VoxelAnchor.Flooring:
            //                     //                             bindingP.Z = target.Position.Z + spell.BindingOffsetZ;
            //                     //                             break;
            //                     //                     }
            //                     //                     if (spell.IsBindingOrbit)
            //                     //                     {
            //                     //                         if (spell.OrbitDistance != 0)
            //                     //                         {
            //                     //                             float dadd = spell.OrbitDistance;
            //                     //                             float ox = (float)Math.Cos(Direction) * dadd;
            //                     //                             float oy = (float)Math.Sin(Direction) * dadd;
            //                     //                             bindingP.X += ox;
            //                     //                             bindingP.Y += oy;
            //                     //                         }
            //                     //                     }
            //                     //                     Position = bindingP;
            //                     //                     return bindingP;
            //                 }
            //                 return Position;
            //             }
            //             public DeepCore.Geometry.Vector3 GetBindingPos(PreviewObject target)
            //             {
            //                 var bindingP = target.WaistPosition;
            //                 switch (spell.BodyVoxelAnchor)
            //                 {
            //                     case VoxelAnchor.Ceiling:
            //                         bindingP.Z = target.TopZ + spell.BindingOffsetZ;
            //                         break;
            //                     case VoxelAnchor.Floating:
            //                         bindingP.Z = target.WaistZ + spell.BindingOffsetZ;
            //                         break;
            //                     case VoxelAnchor.Flooring:
            //                         bindingP.Z = target.Position.Z + spell.BindingOffsetZ;
            //                         break;
            //                 }
            //                 if (spell.IsBindingOrbit)
            //                 {
            //                     if (spell.OrbitDistance != 0 || mDistanceSpeed != 0)
            //                     {
            //                         float dadd = spell.OrbitDistance + mDistanceSpeed;
            //                         float ox = (float)Math.Cos(Direction) * dadd;
            //                         float oy = (float)Math.Sin(Direction) * dadd;
            //                         bindingP.X += ox;
            //                         bindingP.Y += oy;
            //                     }
            //                 }
            //                 //             if (mBindingOffset != null)
            //                 //             {
            //                 //                 if (mBindingOffset.distance != 0)
            //                 //                 {
            //                 //                     float dadd = mBindingOffset.distance;
            //                 //                     float ox = (float)Math.Cos(target.Direction + mBindingOffset.direction) * dadd;
            //                 //                     float oy = (float)Math.Sin(target.Direction + mBindingOffset.direction) * dadd;
            //                 //                     bindingP.X += ox;
            //                 //                     bindingP.Y += oy;
            //                 //                 }
            //                 //                 bindingP.Z += mBindingOffset.height;
            //                 //             }
            //                 return bindingP;
            //             }
            //             private void updateAOE()
            //             {
            //                 switch (spell.BodyShape)
            //                 {
            //                     case SpellTemplate.Shape.LineToTarget:
            //                     case SpellTemplate.Shape.LineToStart:
            //                     case SpellTemplate.Shape.LineToSender:
            //                         break;
            //                     case SpellTemplate.Shape.Strip:
            //                     case SpellTemplate.Shape.StripRay:
            //                     case SpellTemplate.Shape.StripRayTouchEnd:
            //                     case SpellTemplate.Shape.RectStrip:
            //                     case SpellTemplate.Shape.RectStripRay:
            //                     case SpellTemplate.Shape.WideStrip:
            //                         updateAoeMotion(spell.Distance, ref mDistance);
            //                         break;
            //                     default:
            //                         updateAoeMotion(spell.BodySize, ref mBaseSize);
            //                         break;
            //                 }
            //             }
            // 
            //             private void updateAoeMotion(float base_value, ref float value)
            //             {
            //                 switch (spell.AOEMType)
            //                 {
            //                     case SpellTemplate.AoeMotionType.Sine:
            //                         value = (float)Math.Sin(CMath.PI_F * PassTimeMS / spell.LifeTimeMS) * base_value;
            //                         break;
            //                     case SpellTemplate.AoeMotionType.Linear:
            //                     default:
            //                         value += MoveHelper.GetDistance(IntervalMS, mSpeed);
            //                         break;
            //                 }
            //             }


            #endregion

        }
        //----------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------

        public class BuffNode : PreviewObject<BuffNode.AddBuff>
        {
            public class AddBuff
            {
                public PreviewUnit launcher;
                public PreviewUnit binding;
                public BuffTemplate buff;
            }
            public PreviewUnit launcher => Data.launcher;
            public PreviewUnit binding => Data.binding;
            public BuffTemplate buff => Data.buff;

            private PopupKeyFrames<BuffTemplate.KeyFrame> current_frames = new PopupKeyFrames<BuffTemplate.KeyFrame>();
            private TimeInterval<BuffTemplate.KeyFrame> mHitIntervalTicker;
            public override string ToString()
            {
                return $"Buff:{buff?.Name}";
            }
            protected override void DoInit(AddBuff add)
            {
                var buff = add.buff;
                this.BodyHeight = launcher.BodyHeight;
                mHitIntervalTicker = new TimeInterval<BuffTemplate.KeyFrame>(buff.HitIntervalMS);
                mHitIntervalTicker.Tag = buff.HitKeyFrame;

                if (buff.Abilities.TryGetComponentAs<BuffEffectAbility>(out var effects))
                {
                    LoadEffect(effects.BindingEffect);
                    if (effects.BindingEffectList != null)
                    {
                        foreach (var e in effects.BindingEffectList)
                        {
                            LoadEffect(e);
                        }
                    }
                }
            }
            protected override GameObject InitGizmos()
            {
                return null;
            }
            protected override Collider InitEditCollider()
            {
                return null;
            }
            protected override void DoReplay()
            {
                current_frames.Clear();
                current_frames.AddRange(buff.KeyFrames);
            }
            protected override void DoUpdate()
            {
                Position = binding.Position;

                if (mHitIntervalTicker.Update(IntervalMS))
                {
                    DoKeyFrame(buff.HitKeyFrame);
                }

                using (var kfs = ObjectPool.AllocList<BuffTemplate.KeyFrame>())
                {
                    int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                    foreach (var kf in kfs)
                    {
                        DoKeyFrame(kf);
                    }
                }
                if (!buff.IsEquip)
                {
                    if (PassTimeMS > buff.LifeTimeMS)
                    {
                        DoKeyFrame(buff.EndKeyFrame);
                        Dispose();
                    }
                }
            }
            protected override void DoDestory()
            {
                current_frames.Clear();
            }
            private void DoKeyFrame(BuffTemplate.KeyFrame kf)
            {
                if (kf != null)
                {
                    if (kf.Effect != null)
                    {
                        ShowEffect(kf.Effect);
                    }
                    if (kf.Spell != null)
                    {
                        BuffLaunchSpell(this, kf.Spell);
                    }
                    if (kf.Attack != null)
                    {
                        LaunchAttack(this, launcher, kf.Attack, binding);
                    }
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------
        public class TargetNode : PreviewUnit<UnitInfo>, IPreviewUnit
        {
            public UnitInfo UnitData => Data;
            public PreviewObject Preview => this;
            public IViewResource Res { get; private set; }
            public override UnitInfo Template => UnitData;
            public override UnitSkillAbility ASkill { get => askill; }

            private UnitSkillAbility askill;
            private UnitActionMap actionMap = new UnitActionMap();
            public override string ToString()
            {
                return $"Target:{Data?.Name}";
            }
            protected override void Awake()
            {
                base.Awake();
                RTG.AddEditorObject(gameObject);
            }
            protected override void DoInit(UnitInfo unit)
            {
                askill = unit.Abilities.GetComponentAs<UnitSkillAbility>();
                BodySize = unit.BodySize;
                BodyHeight = unit.BodyHeight;
                actionMap.Append(Templates.DefaultUnitActionDefinition);
                if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
                {
                    actionMap.Append(u_res.OverrideActionMap);
                    Res = LoadRes(u_res.FileName, DeepMetaGame.Data.ResourceType.Object);
                }
            }
            protected override void DoReplay()
            {
                base.DoReplay();
            }
            protected override void DoUpdate()
            {
                base.DoUpdate();
                UpdateDamage();
                CheckFallDown();
            }

            //----------------------------------------------------------------------------------------------------------------

            #region Damage

            private PreviewObject attacker;
            private float startDirection;
            private float rotateSpeedSEC;
            private bool isEnd = false;
            private TimeExpire<object> damageExpire;
            private StartMoveAction hitMoveSpeed;

            public void DoDamage(PreviewUnit attacker, PreviewObject sender, AttackProp atk)
            {
                //Debug.Log("DoDamage + " + DateTime.Now);
                var udt = actionMap;
                if (udt != null && udt.TryGetAction(UnitActionStatus.Damage, null, out var damage))
                {
                    foreach (var action in damage.ActionQueue)
                    {
                        if (MainRes != null)
                            MainRes.PlayAction(UnitActionStatus.Damage, action);
                        break;
                    }
                }

                var mtype = atk.HitMoveMType;
                var moveSource = attacker as PreviewObject;
                if (sender is SpellNode spell)
                {
                    if (atk.HitMoveBySpellLauncher)
                    {
                        moveSource = spell.launcher;
                    }
                    else
                    {
                        moveSource = spell;
                    }
                }
                // 计算受击时间 //
                int damageTime = Data.DamageTimeMS;
                if (atk.KnockOutTimeMS > 0)
                {
                    damageTime = atk.KnockOutTimeMS;
                }
                else if (Data.DamageTimeMS > 0)
                {
                    damageTime = Data.DamageTimeMS;
                }
                else
                {
                    damageTime = Templates.DefaultConfig.OBJECT_DAMAGE_TIME_MS;
                }
                damageExpire = new TimeExpire<object>(damageTime);

                startDirection = MoveHelper.CalculateHitMoveDirection(
                   Position,
                   Direction,
                   moveSource.Position,
                   moveSource.Direction,
                   mtype);
                if (atk.HitMove)
                {
                    startDirection += atk.HitMove.Direction;
                    rotateSpeedSEC = atk.HitMove.RotateSpeedSEC * (RandomN.Next() % 2 == 0 ? -1 : 1);
                    hitMoveSpeed = StartHitMove(
                        startDirection,
                        atk.HitMove.RotateSpeedSEC,
                        atk.HitMove.KeepTimeMS,
                        atk.HitMove.SpeedSEC,
                        atk.HitMove.SpeedAdd,
                        atk.HitMove.SpeedAcc);
                    if (atk.HitMove.HasFly)
                    {
                        hitMoveSpeed.SetFly(
                            atk.HitMove.ZSpeedSEC,
                            atk.HitMove.OverrideGravity);
                    }
                }
                else if (atk.IsHitFly)
                {
                    var startMove = new StartMove()
                    {
                        Direction = startDirection,
                        RotateSpeedSEC = rotateSpeedSEC,
                        KeepTimeMS = 0,
                        SpeedSEC = Templates.DefaultConfig.OBJECT_DAMAGE_FLY_SPEED_SEC,
                        ZSpeedSEC = Templates.DefaultConfig.OBJECT_DAMAGE_FLY_ZSPEED_SEC,
                        SpeedAdd = Templates.DefaultConfig.OBJECT_DAMAGE_FLY_SPEED_ADD,
                        SpeedAcc = Templates.DefaultConfig.OBJECT_DAMAGE_FLY_SPEED_ACC,
                    };
                    hitMoveSpeed = StartHitMove(startMove);
                }
                if (hitMoveSpeed != null)
                {
                    if (atk.HitMoveMType == AttackProp.HitMoveType.ToSenderCenter)
                    {
                        hitMoveSpeed.SetMoveTarget(moveSource, false, 0);
                    }
                    else if (atk.HitMoveMType == AttackProp.HitMoveType.ToSenderBodySize)
                    {
                        hitMoveSpeed.SetMoveTarget(moveSource, true, 0);
                    }
                }
            }
            private void EndDamage()
            {
                attacker = null;
                startDirection = 0;
                rotateSpeedSEC = 0;
                damageExpire = null;
                hitMoveSpeed = null;
            }
            private void UpdateDamage()
            {
                if (hitMoveSpeed != null)
                {
                    if (hitMoveSpeed.IsEnd)
                    {
                        if (hitMoveSpeed.IsFly && damageExpire.TotalTimeMS == 0)
                        {
                            EndDamage();
                        }
                        hitMoveSpeed = null;
                    }
                }
                else if (damageExpire != null)
                {
                    if (damageExpire.Update(IntervalMS))
                    {
                        EndDamage();
                    }
                }
                else
                {
                    EndDamage();
                }
            }
            #endregion
        }
        //----------------------------------------------------------------------------------------------------------------
        #region Launch


        static public PreviewUnit SeekSpellAttackable(
            PreviewObject sender,
            PreviewUnit launcher,
            SpellTemplate spell,
            DeepCore.Geometry.Vector3? pos,
            float range,
            SkillTemplate.CastTarget expectTarget,
            LaunchSkill.SeekingExpect expectSeeking,
            bool seekingIgnoreInChain,
            int expectSeekingIndex,
            SpellNode.SpellChainLevelInfo chain,
            SeekingTargetAnchor targetAnchor,
            out DeepCore.Geometry.Vector3? targetPos)
        {
            if (pos.HasValue == false)
            {
                targetPos = null;
                return null;
            }
            {
                var list = GetShapeTargets<PreviewUnit>(sender, range);
                foreach (var target in list.ToArray())
                {
                    if (expectTarget == SkillTemplate.CastTarget.EveryOne)
                    {

                    }
                    else if (expectTarget == SkillTemplate.CastTarget.Self)
                    {
                        if (target != launcher)
                        {
                            list.Remove(target);
                        }
                    }
                    else
                    {
                        if (target == launcher)
                        {
                            list.Remove(target);
                        }
                    }
                }
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    var u = list[i];
                    if (chain != null)
                    {
                        if (seekingIgnoreInChain)
                        {
                            if (chain.ContainsTarget(u))
                            {
                                list.RemoveAt(i);
                                continue;
                            }
                        }
                        else
                        {
                            if (chain.LastTarget == u)
                            {
                                list.RemoveAt(i);
                                continue;
                            }
                        }
                        //                         switch (expectSeeking)
                        //                         {
                        //                             case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                        //                             case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                        //                             case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                        //                                 if (chain.ContainsTarget(u))
                        //                                 {
                        //                                     list.RemoveAt(i);
                        //                                     continue;
                        //                                 }
                        //                                 break;
                        //                             default:
                        //                                 if (chain.LastTarget == u)
                        //                                 {
                        //                                     list.RemoveAt(i);
                        //                                     continue;
                        //                                 }
                        //                                 break;
                        //                         }
                    }
                }
                switch (expectSeeking)
                {
                    case LaunchSkill.SeekingExpect.Random:
                        //case SpellTemplate.SeekingExpect.RandomIgnoreInChain:
                        CUtils.RandomList(RandomN, list);
                        break;
                    case LaunchSkill.SeekingExpect.Nearest:
                        //case SpellTemplate.SeekingExpect.NearestIgnoreInChain:
                        list.Sort(new ObjectSorterNearest(pos.Value));
                        break;
                    case LaunchSkill.SeekingExpect.Farthest:
                        //case SpellTemplate.SeekingExpect.FarthestIgnoreInChain:
                        list.Sort(new ObjectSorterFarthest(pos.Value));
                        break;
                }
                if (list.Count > 0)
                {
                    var index = expectSeekingIndex;
                    if (index >= 0 && index < list.Count)
                    {
                        var ret = list[index];
                        switch (targetAnchor)
                        {
                            case SeekingTargetAnchor.Foot: targetPos = ret.Position; break;
                            case SeekingTargetAnchor.Waist: targetPos = ret.WaistPosition; break;
                            case SeekingTargetAnchor.Head: targetPos = ret.HeadPosition; break;
                            default: targetPos = ret.WaistPosition; break;
                        }
                        return ret;
                    }
                }
            }
            targetPos = null;
            return null;
        }

        static public void AddSpellNode(
            PreviewObject sender,
            PreviewUnit launcher,
            LaunchSpell launch,
            DeepCore.Geometry.Vector3 startPos,
            float startDirection,
            PreviewUnit target,
            SpellNode.SpellChainLevelInfo chain)
        {
            var targetPos = target.WaistPosition;
            //PLog($"AddSpellNode : {this} : {sender} -> {target} : {launch}");
            if (Templates.TryGetTemplate<SpellTemplate>(launch.SpellID, out var spell))
            {
                switch (spell.MType)
                {
                    case SpellTemplate.MotionType.BindingTarget:
                        startPos = target.Position;
                        break;
                    case SpellTemplate.MotionType.SelectTarget:
                        startPos = target.Position;
                        break;
                    case SpellTemplate.MotionType.SeekerMissile:
                    case SpellTemplate.MotionType.SeekerSelectTarget:
                        if (spell.SeekingCooldownMS > 0)
                        {
                            target = null;
                        }
                        break;
                    case SpellTemplate.MotionType.Chain:
                        break;
                }
                if (launch.IsAutoSeekingTarget)
                {
                    //需要目标的spell提前扫描周围如果没有可攻击的目标则不再生成spell.//
                    var redirect = SeekSpellAttackable(
                        sender,
                        launcher,
                        spell,
                        startPos,
                        launch.SeekingTargetRange,
                        spell.ExpectTarget,
                        launch.SeekingTargetExpect,
                        launch.SeekingIgnoreInChain,
                        0,
                        chain,
                        launch.SeekingAnchor,
                        out var _targetPos);
                    if (redirect != null)
                    {
                        target = redirect;
                        startDirection = VectorHelper.GetDegree(startPos, redirect.Position);
                        if (launch.SenderFaceToTarget)
                        {
                            if (sender is PreviewUnit senderUnit)
                            {
                                senderUnit.LookAt(target.Position);
                            }
                            else if (sender is SpellNode senderSpell)
                            {
                                senderSpell.LookAt(target.Position);
                            }
                        }
                    }
                    if (_targetPos.HasValue)
                    {
                        targetPos = _targetPos.Value;
                        startDirection = VectorHelper.GetDegree(startPos, targetPos);
                    }
                }
                if (spell.IsNeedTarget)
                {
                    if (target == null)
                    {
                        return;
                    }
                }
                var display = Proxy.CreateDisplay<SpellNode>(spell.Name);
                display.Position = startPos;
                display.Direction = startDirection;
                display.Init(new SpellNode.AddSpell()
                {
                    launcher = launcher,
                    sender = sender,
                    spell = spell,
                    launch = launch,
                    startPos = startPos,
                    startDirection = startDirection,
                    target = target,
                    targetPos = targetPos,
                    chain = chain,
                }); ;
            }
        }
        static public void AddBuffNode(LaunchBuff launch, PreviewUnit launcher, PreviewUnit target)
        {
            //PLog($"AddBuffNode : {this} : {launch} -> {target} : {launch}");
            if (Templates.TryGetTemplate<BuffTemplate>(launch.BuffID, out var buff))
            {
                var display = Proxy.CreateDisplay<BuffNode>(buff.Name);
                display.transform.SetParent(target.transform);
                display.Init(new BuffNode.AddBuff()
                {
                    launcher = launcher,
                    binding = target,
                    buff = buff,
                }); ;
            }
        }

        static public void SkillLaunchSpell(LaunchSpell launch, PreviewUnit launcher, SkillTemplate SkillData, PreviewUnit current_target)
        {
            var startPos = launcher.Position;
            var targetPos = startPos;
            var dr = launcher.Direction;
            if (current_target)
            {
                targetPos = current_target.WaistPosition;
                dr = MathVector.getDegree(startPos.X, startPos.Y, targetPos.X, targetPos.Y);
                var td = MathVector.getDistance(startPos.X, startPos.Y, targetPos.X, targetPos.Y);
                // TargetPos超出技能范围 //
                if (td > SkillData.AttackRange)
                {
                    // 把TargetPos拉回 //
                    MathVector.movePolar(ref targetPos, dr, SkillData.AttackRange - td);
                }
                // 设置法术出生点 (非自身坐标发射，比如Cannon) //
                if (!SkillData.IsLaunchBody)
                {
                    startPos = targetPos;
                }
            }
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpellSenderUnit.Target:
                    startPos = targetPos;
                    break;
            }
            UnitLaunchSpell(launcher, launcher, startPos, launch, current_target, dr);
        }
        static public void UnitLaunchSpell(PreviewObject sender, PreviewUnit launcher, DeepCore.Geometry.Vector3 startPos, LaunchSpell launch, PreviewUnit target, float? _direction)
        {
            //PLog($"UnitLaunchSpell : {this} : {sender} -> {target} : {launch}");
            if (RandomN.RandomPercent(launch.LaunchPercent))
            {
                if (launch.Count > 0)
                {
                    PreviewObject.SpellNode.SpellChainLevelInfo chain = null;
                    if (launch.ChainLevel > 0)
                    {
                        chain = new(launch);
                    }
                    var targetPos = target.WaistPosition;
                    var direction = MathVector.getDegree(startPos.X, startPos.Y, targetPos.X, targetPos.Y);
                    if (_direction.HasValue)
                    {
                        direction = _direction.Value;
                    }
//                     Proxy.StartLaunchSpellPosType(launch, startPos, sender.Direction, (launcher), (st, startPos, dir) =>
//                     {
//                         AddSpellNode(sender, launcher, launch, startPos, dir, target, chain);
//                     }, (st, repeat) => { }, (st, count) => { });
                    /*
                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                if (launch.Count > 1)
                                {
                                    float startAngle = direction - launch.Angle / 2f + launch.StartAngle;
                                    float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                    for (int i = 0; i < launch.Count; i++)
                                    {
                                        AddSpellNode(sender, launch, startPos, startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), target, chain);
                                    }
                                }
                                else
                                {
                                    AddSpellNode(sender, launch, startPos, direction + launch.StartAngle + launch.AdjustRandomAngle(RandomN), target, chain);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_CYCLE:
                            {
                                float startAngle = direction + launch.StartAngle;
                                float interAngle = CMath.PI_MUL_2 / launch.Count;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    AddSpellNode(sender, launch, startPos, startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), target, chain);
                                }
                            }
                            break;
                        //                         case LaunchSpell.PosType.POS_TYPE_RANDOM_DIRECTION:
                        //                             {
                        //                                 for (int i = 0; i < launch.Count; i++)
                        //                                 {
                        //                                     float d = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                        //                                     AddSpellNode(sender, launch, startPos, d, target);
                        //                                 }
                        //                             }
                        //                             break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            {
                                Debug.LogError(string.Format("Can not launch [POS_TYPE_RANDOM_FOR_SPELL] spell from unitLaunchSpell: {0} {1}", launch, name));
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                AddSpellNode(sender, launch, startPos, direction + launch.StartAngle + launch.AdjustRandomAngle(RandomN), target, chain);
                            }
                            break;
                    }
                    */
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        UnitLaunchSpell(sender, launcher, startPos, subSpell, target, _direction);
                    }
                }
            }
        }

        public static void SpellLaunchSpell(SpellNode sender, PreviewUnit launcher, LaunchSpell launch, PreviewUnit target)
        {
            //             switch (launch.SenderUnit)
            //             {
            //                 case LaunchSpell.LaunchSpllSenderUnit.Launcher:
            //                     this.UnitLaunchSpell(
            //                         sender.launcher,
            //                         sender.Position,
            //                         launch, sender.FromSkillTemplateID, startPos, targetUnitID, targetPos);
            //                     return;
            //                 case LaunchSpell.LaunchSpllSenderUnit.Target:
            //                     if (sender.target != null)
            //                     {
            //                         this.UnitLaunchSpell(
            //                             sender.target,
            //                             sender.target.Position,
            //                             launch, sender.FromSkillTemplateID, startPos, targetUnitID, targetPos);
            //                         return;
            //                     }
            //                     break;
            //             }
            //PLog($"SpellLaunchSpell : {this} : {sender} -> {target} : {launch}");
            if (RandomN.RandomPercent(launch.LaunchPercent))
            {
                if (launch.Count > 0)
                {
                    SpellNode.SpellChainLevelInfo chain = sender.ChainInfo;
                    if (chain != null)
                    {
                        if (!chain.TryLaunch(launch.SpellID))
                        {
                            // chain is end //
                            return;
                        }
                    }
                    var startPos = sender.Position;
                    switch (launch.SenderUnit)
                    {
                        case LaunchSpell.LaunchSpellSenderUnit.Launcher:
                            UnitLaunchSpell(sender.launcher, launcher, startPos, launch, target, null);
                            return;
                        case LaunchSpell.LaunchSpellSenderUnit.Target:
                            UnitLaunchSpell(sender.target, launcher, startPos, launch, target, null);
                            return;
                    }
                    var targetPos = target.Position;
                    var direction = MathVector.getDegree(startPos.X, startPos.Y, targetPos.X, targetPos.Y);
//                     Proxy.StartLaunchSpellPosType(launch, sender, RandomN, startPos, sender.Direction, (sender), (st, startP, startD) =>
//                     {
//                         AddSpellNode(sender, launcher, launch, startP, startD, target, chain);
//                     }, (st, repeat) => { }, (st, count) => { });
                    /*
                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                float startAngle = direction - launch.Angle / 2f + launch.StartAngle;
                                float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    AddSpellNode(sender, launch, startPos, startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), target, chain);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_CYCLE:
                            {
                                float startAngle = direction + launch.StartAngle;
                                float interAngle = CMath.PI_MUL_2 / launch.Count;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    AddSpellNode(sender, launch, startPos, startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), target, chain);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            {
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    float r = (float)(RandomN.NextDouble() * sender.BodySize);
                                    float a = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                                    float x = (float)(startPos.X + Math.Cos(a) * r);
                                    float y = (float)(startPos.Y + Math.Sin(a) * r);
                                    float d = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                                    AddSpellNode(sender, launch, new DeepCore.Geometry.Vector3(x, y, startPos.Z), d + launch.AdjustRandomAngle(RandomN), target, chain);
                                }
                            }
                            break;
                        //                         case LaunchSpell.PosType.POS_TYPE_RANDOM_DIRECTION:
                        //                             {
                        //                                 for (int i = 0; i < launch.Count; i++)
                        //                                 {
                        //                                     float d = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                        //                                     AddSpellNode(sender, launch, startPos, d, target);
                        //                                 }
                        //                             }
                        //                             break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                AddSpellNode(sender, launch, startPos, direction + launch.StartAngle + launch.AdjustRandomAngle(RandomN), target, chain);
                            }
                            break;
                    }*/

                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        SpellLaunchSpell(sender, launcher, subSpell, target);
                    }
                }
            }
        }

        static public void AttackLaunchSpell(PreviewObject source, PreviewUnit launcher, AttackProp attack, PreviewUnit damage, LaunchSpell launch)
        {
            //PLog($"AttackLaunchSpell : {this} : {source} -> {damage} : {attack}");
            //LaunchSpell launch = attack.Spell;
            if (launch == null)
                return;

            if (RandomN.RandomPercent(launch.LaunchPercent))
            {
                if (launch.Count > 0)
                {
                    SpellNode.SpellChainLevelInfo chain = null;
                    if (source is SpellNode src_spell && src_spell.ChainInfo != null)
                    {
                        chain = src_spell.ChainInfo;
                        if (!chain.TryLaunch(launch.SpellID))
                        {
                            // chain is end //
                            return;
                        }
                    }
                    var sender = source;
                    switch (launch.SenderUnit)
                    {
                        case LaunchSpell.LaunchSpellSenderUnit.Launcher:
                            sender = launcher;
                            break;
                        case LaunchSpell.LaunchSpellSenderUnit.Target:
                            sender = damage as PreviewUnit;
                            break;
                    }
                    var startPos = damage.Position;
//                     Proxy.StartLaunchSpellPosType(launch, RandomN, startPos, sender.Direction, (source), (st, startPos, dir) =>
//                     {
//                         AddSpellNode(sender, launcher, launch, startPos, dir, damage, chain);
//                     }, (st, repeat) => { }, (st, count) => { });
                    /*
                    switch (launch.PType)
                    {
                        case LaunchSpell.PosType.POS_TYPE_FAN:
                            {
                                float startAngle = sender.Direction - launch.Angle / 2f + launch.StartAngle;
                                float interAngle = launch.Count > 0 ? launch.Angle / (launch.Count - 1) : 0;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    AddSpellNode(sender, launch, startPos,
                                        startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), damage, chain);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_CYCLE:
                            {
                                float startAngle = sender.Direction + launch.StartAngle;
                                float interAngle = CMath.PI_MUL_2 / launch.Count;
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    AddSpellNode(sender, launch, startPos,
                                        startAngle + interAngle * i + launch.AdjustRandomAngle(RandomN), damage, chain);
                                }
                            }
                            break;
                        case LaunchSpell.PosType.POS_TYPE_RANDOM_FOR_SPELL:
                            if (source is SpellNode)
                            {
                                for (int i = 0; i < launch.Count; i++)
                                {
                                    float r = (float)(RandomN.NextDouble() * sender.BodySize);
                                    float a = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                                    float x = (float)(startPos.X + Math.Cos(a) * r);
                                    float y = (float)(startPos.Y + Math.Sin(a) * r);
                                    float d = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                                    AddSpellNode(sender, launch, new DeepCore.Geometry.Vector3(x, y, startPos.Z), d + launch.AdjustRandomAngle(RandomN), damage, chain);
                                }
                            }
                            else
                            {
                                Debug.LogError(string.Format("Can not launch [POS_TYPE_RANDOM_FOR_SPELL] spell from Unit Attack : {0} {1}", launch, source));
                            }
                            break;
                        //                         case LaunchSpell.PosType.POS_TYPE_RANDOM_DIRECTION:
                        //                             {
                        //                                 for (int i = 0; i < launch.Count; i++)
                        //                                 {
                        //                                     float d = (float)(RandomN.NextDouble() * CMath.PI_MUL_2);
                        //                                     AddSpellNode(sender, launch, startPos, d, damage);
                        //                                 }
                        //                             }
                        //                             break;
                        case LaunchSpell.PosType.POS_TYPE_DEFAULT_SINGLE:
                        default:
                            {
                                AddSpellNode(sender, launch, startPos, sender.Direction + launch.StartAngle + launch.AdjustRandomAngle(RandomN), damage, chain);
                            }
                            break;
                    }
                    */
                }

                if (launch.SubSpells != null)
                {
                    foreach (var subSpell in launch.SubSpells)
                    {
                        AttackLaunchSpell(source, launcher, attack, damage, subSpell);
                    }
                }
            }

        }

        static public void BuffLaunchSpell(BuffNode buff, LaunchSpell launch)
        {
            //PLog($"BuffLaunchSpell : {this} : {buff} : {launch}");
            var sender = buff.launcher;
            switch (launch.SenderUnit)
            {
                case LaunchSpell.LaunchSpellSenderUnit.DamagedUnit:
                    sender = buff.binding;
                    break;
            }
            UnitLaunchSpell(sender, buff.launcher, buff.binding.Position, launch, sender, null);
        }
        static public void LaunchAttack(PreviewObject sender, PreviewUnit launcher, AttackProp atk, PreviewUnit target)
        {
            //PLog($"LaunchAttack : {this} : {sender} -> {target} : {atk}");
            if (sender is SpellNode src_spell && src_spell.ChainInfo != null)
            {
                src_spell.ChainInfo.AddTarget(target);
            }
            if (atk.Spell != null)
            {
                AttackLaunchSpell(sender, launcher, atk, target, atk.Spell);
            }
            if (atk.Buff != null)
            {
                AddBuffNode(atk.Buff, launcher, target);
            }
            if (atk.Effect != null)
            {
                target.ShowEffect(atk.Effect);
            }
            if (atk.CrushEffect != null)
            {
                target.ShowEffect(atk.CrushEffect);
            }
            //if (atk.MaskDamage)
            {
                if (target is TargetNode targetNode)
                {
                    targetNode.DoDamage(launcher, sender, atk);
                }
            }
        }

        #endregion


    }

}

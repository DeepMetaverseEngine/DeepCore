using DeepCore;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Geometry;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Display.GUI;
using System.Security.Cryptography;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.Preview
{
    //---------------------------------------------------------------------------------------------------------------------------------

    public class UnitSkillDisplay : PreviewUnit<ValueTuple<UnitInfo, SkillTemplate>>, IPreviewUnit
    {
        public PreviewObject Preview => this;
        public UnitInfo UnitData { get => Data.Item1; }
        public SkillTemplate SkillData { get => Data.Item2; }
        public override UnitSkillAbility ASkill { get => askill; }
        public LaunchSkill Launch { get; private set; }

        private IViewResource unit_res;

        private UnitSkillAbility askill;
        private TimeExpire<int> chantExpire;
        private Queue<UnitActionData> action_queue = new Queue<UnitActionData>();
        private PopupKeyFrames<UnitActionData.KeyFrame> current_frames = new PopupKeyFrames<UnitActionData.KeyFrame>();
        private UnitActionData current_action;
        private TargetNode current_target;
        private bool initOver = false;
        //---------------------------------------------------------------------------------------
        public override UnitInfo Template => this.UnitData;
        //---------------------------------------------------------------------------------------
        public override string ToString()
        {
            return $"Player:{UnitData?.Name}";
        }
        protected override void Awake()
        {
            base.Awake();
            //OnInitGUI();
            RTG.OnTargetPropertyChanged += RTG_OnTargetPropertyChanged;
            RTG.AddEditorObject(gameObject);
        }

        protected override void DoDestory()
        {
            RTG.OnTargetPropertyChanged -= RTG_OnTargetPropertyChanged;
            current_frames.Clear();
        }
        protected override void DoInit(ValueTuple<UnitInfo, SkillTemplate> tuple)
        {
            var unit = UnitData;
            gameObject.name = $"ACTOR:{unit.Name}";
            BodySize = unit.BodySize;
            BodyHeight = unit.BodyHeight;
            BodyColor = Color.green.SetAlpha(0.5f);
            if (unit.Abilities.TryGetComponentAs<UnitSkillAbility>(out var u_skill))
            {
                askill = u_skill;
                Launch = u_skill.GetSkillByID(SkillData.ID);
            }
            if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
            {
                unit_res = LoadRes(u_res.FileName, DeepMetaGame.Data.ResourceType.Object);
                if (UnityBattleFactory.Resource.TryGetSpine(unit_res.gameObject, out var spine))
                {
                    spine.initialSkinName = u_res.SkinName;
                    spine.SetAvatar(u_res.SkinAvatar);
                }
            }
            {
                var range = SkillData.AttackRange;
                range = Math.Max(range, unit.BodySize * 2.4f);
                AddTargetAsync(new DeepCore.Geometry.Vector3(+0.8f * range, 0, 0), true);
                AddTargetAsync(new DeepCore.Geometry.Vector3(-0.8f * range, 0, 0));
                AddTargetAsync(new DeepCore.Geometry.Vector3(0, +0.8f * range, 0));
                AddTargetAsync(new DeepCore.Geometry.Vector3(0, -0.8f * range, 0));
            }
            if (this.Disposed)
                return;
            LoadTargetPos();
            initOver = true;
        }
        protected override void DoReplay()
        {
            var unit = UnitData;
            if (PreviewConfig.IsResetPos)
            {
                ResetTargetPos();
                Position = DeepCore.Geometry.Vector3.Zero;
            }
            //PLog($"------------------------------------ Play Skill : {SkillData} ------------------------------------");
            InitAttackShape(SkillData.AttackShape);
            if (SkillData.ChantTimeMS > 0)
            {
                chantExpire = new TimeExpire<int>(SkillData.ChantTimeMS);
            }
            else
            {
                StartAction();
            }
        }
        private void SkillEnd()
        {
            this.MainRes?.IsVisible = true;
            InitAttackShape(null);
            //PLog($"------------------------------------ End Skill : {SkillData} ------------------------------------");
        }
        private void StartAction()
        {
            LookAt(current_target?.transform);
            current_action = null;
            current_frames.Clear();
            action_queue.Clear();
            action_queue.EnqueueRange(SkillData.ActionQueue);
            NextAction();
        }
        private bool NextAction()
        {
            move_to_target_path = null;
            jump_to_target_pos = null;
            ResetTime();
            current_frames.Clear();
            if (action_queue.TryDequeue(out current_action))
            {
                this.MainRes?.IsVisible = !current_action.IsInvisible;
                current_frames.AddRange(current_action.KeyFrames);
                if (current_action.IsMoveToTarget)
                {
                    //先寻路//
                    startMoveToTarget(current_action);
                }
                else if (current_action.IsJumpToTarget)
                {
                    //计算跳跃距离//
                    startJumpToTarget(current_action);
                }
                if (current_action.OverrideAttackShape != null)
                {
                    InitAttackShape(current_action.OverrideAttackShape);
                }
                if (unit_res != null)
                {
                    unit_res.PlayAction(UnitActionStatus.Skill, current_action.Action);
                }
                if (!string.IsNullOrEmpty(current_action.ActionEffectFileName))
                {
                    ShowEffect(current_action.ActionEffectFileName);
                    //                     Owner.parent.PlayObjectEffect(Owner,
                    //                         this.skillAction.CurrentAction.ActionEffectFileName,
                    //                         null, 1f,
                    //                         this.skillAction.CurrentAction.TotalTimeMS);
                }
                return true;
            }
            else
            {
                SkillEnd();
            }
            return false;
        }
        protected override void DoUpdate()
        {
            if (!IsInitDone) return;
            if (chantExpire != null)
            {
                if (chantExpire.Update(IntervalMS))
                {
                    chantExpire = null;
                    ResetTime();
                    StartAction();
                }
                else
                {
                    return;
                }
            }
            if (current_action != null)
            {
                using (var kfs = ObjectPool.AllocList<UnitActionData.KeyFrame>())
                {
                    int kfs_count = current_frames.PopKeyFrames(PassTimeMS, kfs);
                    foreach (var kf in kfs)
                    {
                        doKeyFrame(kf);
                    }
                }
                if (current_action.IsFaceToTarget)
                {
                    LookAt(current_target.transform);
                }
                if (current_action.IsMoveToTarget)
                {
                    doMoveToTarget(current_action);
                }
                else if (current_action.IsJumpToTarget)
                {
                    doJumpToTarget(current_action);
                }
                else
                {
                    if (start_move != null)
                    {
                        doMove();
                    }
                    // 身体攻击 //
                    if (current_action.BodyHit != null)
                    {
                        doBodyHit(current_action);
                    }
                    // 防止技能位移导致单位重合 //
                    if (current_action.BodyBlockOnAttackRange)
                    {
                        doBodyBlock(current_action);
                    }
                }
                if (PassTimeMS >= current_action.TotalTimeMS)
                {
                    NextAction();
                }
            }
            else if (SkillData.ActionQueue != null && SkillData.ActionQueue.Count > 0)
            {
                if (PassTimeMS >= Math.Min(SkillData.CoolDownMS, SkillData.TotalActionQueueTimeMS + 1000))
                {
                    if (PreviewConfig.IsAutoReplay)
                    {
                        Replay();
                    }
                }
            }
            UpdateAttackShape();
        }
        //----------------------------------------------------------------------------------------------------------------
        #region AttackShape
        private AttackShapeGizmos attackShape;
        private void InitAttackShape(UnitActionData.AttackShape shape)
        {
            if (attackShape != null) { Destroy(attackShape.gameObject); attackShape = null; }
            if (shape != null)
            {
                var shapeObject = new GameObject(shape.AShape.ToString());
                shapeObject.transform.SetParent(transform, false);
                shapeObject.transform.localRotation = UnityEngine.Quaternion.AngleAxis(-90, UnityEngine.Vector3.up);
                attackShape = shapeObject.AddComponent<AttackShapeGizmos>().InitGizmos(
                      shape.AsShape,
                      BodySize + shape.AttackRange,
                      BodyHeight,
                      BodySize + shape.AttackRange,
                      shape.AttackAngle,
                      shape.StripWide,
                      current_target?.transform,
                      shape.OffsetRadius);
                if (RTG.TempGizmoz && RTG.TempGizmoz.TryGetComponent<MeshRenderer>(out var drender))
                {
                    attackShape.SetMaterial(
                        drender.material,
                        Color.red.SetAlpha(0.5f));
                }
            }
        }
        private void UpdateAttackShape()
        {
            if (attackShape != null)
            {
                if (attackShape.gameObject.activeSelf != PreviewConfig.IsShowGizmos)
                {
                    attackShape.gameObject.SetActive(PreviewConfig.IsShowGizmos);
                }
            }
        }
        //----------------------------------------------------------------------------------------------------------------
        public List<T> GetShapeAttackable<T>(SkillTemplate.CastTarget expectTarget) where T : PreviewUnit
        {
            var shape = SkillData.AttackShape;
            if (current_action?.OverrideAttackShape != null)
            {
                shape = current_action.OverrideAttackShape;
            }
            var range = shape;
            var attack_range = new UnitAttackRangeHelper(this)
            {
                Shape = range.AsShape,
                AttackRange = BodySize + range.AttackRange,
                Distance = BodySize + range.AttackRange,
                Height = BodyHeight,
                Position = Position,

                Direction = Direction,
                FanAngle = range.AttackAngle,
                StripWide = range.StripWide,
                OffsetRadius = range.OffsetRadius,
            };
            var list = GetShapeTargets<T>(attack_range);
            foreach (var target in list.ToArray())
            {
                if (expectTarget == SkillTemplate.CastTarget.EveryOne)
                {

                }
                else if (expectTarget == SkillTemplate.CastTarget.Self)
                {
                    if (target != this)
                    {
                        list.Remove(target);
                    }
                }
                else
                {
                    if (target == this)
                    {
                        list.Remove(target);
                    }
                }
            }
            return list;
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------
        #region KeyFrame

        private StartMoveAction start_move;
        private DeepCore.Geometry.Vector3? move_to_target_path;
        private DeepCore.Geometry.Vector3? jump_to_target_pos;
        private void doKeyFrame(UnitActionData.KeyFrame kf)
        {
            if (kf != null)
            {
                if (kf.ChangeStatus != null)
                {
                    this.MainRes?.IsVisible = !kf.ChangeStatus.IsInvisible;
                }
                if (kf.Effect != null)
                {
                    ShowEffect(kf.Effect);
                }
                if (kf.Spell != null)
                {
                    SkillLaunchSpell(kf.Spell, this, SkillData, current_target);
                }
                if (kf.SelfBuff != null)
                {
                    AddBuffNode(kf.SelfBuff, this, this);
                }
                if (kf.Attack != null)
                {
                    doHitAttack(kf.Attack);
                }
                if (kf.Move != null)
                {
                    startMove(kf.Move);
                }
                if (kf.Blink != null)
                {
                    startBlink(kf.Blink);
                }
                if (kf.CustomAction != null)
                {
                    customAction(kf.CustomAction);
                }
            }
        }

        private void startMove(StartMove startMove)
        {
            start_move = StartHitMove(startMove);
            if (!startMove.IsNoneTouch)
            {
                start_move.SetBlockTarget(current_target);
            }
            //             if (this.SkillData.AttackBodyTouchRange > 0)
            //             {
            //                 this.start_move.SetMoveTarget(this.target,
            //                     true,
            //                     this.SkillData.AttackBodyTouchRange);
            //             }
        }
        private void startBlink(BlinkMove blink)
        {
            if (blink.BeginEffect != null)
            {
                ShowEffect(blink.BeginEffect);
            }
            MoveBlink(blink, current_target);
            if (blink.TargetEffect != null)
            {
                ShowEffect(blink.TargetEffect);
            }
        }

        /// <summary>
        /// 检测冲锋距离
        /// </summary>
        /// <returns></returns>
        private void startMoveToTarget(UnitActionData action)
        {
            if (action.IsMoveToTarget)
            {
                if (getTargetPos(action, out var tpos))
                {
                    move_to_target_path = tpos;
                    float max = MoveHelper.GetDistance(action.TotalTimeMS, action.MoveToTargetSpeedSEC);
                    float total = DeepCore.Geometry.Vector3.Distance(move_to_target_path.Value, Position);
                    if (total > max)
                    {
                        move_to_target_path = null;
                    }
                }
            }
        }

        private void customAction(IKeyFrameProperties customAction)
        {
            UnityBattleFactory.Resource.PlayKeyFrame(this.transform, customAction, this);
            //               if (customAction != null)
            //               {
            //                   if (customAction.CustomActionType == KeyFrameCustomAction.ActionType.PlaySound)
            //                   {
            //                       PlaySound(customAction.StringParameter, DeepMetaGame.Data.ResourceType.Sound);
            //                   }
            //               }
        }

        /// <summary>
        /// 检测跳跃速度
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        private void startJumpToTarget(UnitActionData action)
        {
            if (action.IsJumpToTarget)
            {
                if (getTargetPos(action, out var tpos))
                {
                    if (action.IsJumpLockTarget)
                    {
                        //自动冲向目标
                        if (current_target == null)
                        {
                            return;
                        }

                        float rg = GetSkillAttackRange(SkillData);
                        if (TouchInRange(Position, current_target.Position, rg))
                        {
                            return;
                        }

                        if (!TouchInRange(Position, current_target.Position, action.JumpLockMaxRange))
                        {
                            return;
                        }

                        float distance = DeepCore.Geometry.Vector2.Distance(Position, tpos);
                        jump_to_target_pos = tpos;

                        if (action.JumpLockTimeMS == 0)
                        {
                            action.JumpLockTimeMS = 1;
                        }

                        var fall = StartJump(action.JumpToTargetSpeedZ);
                        if (fall != null)
                        {
                            fall.OnFallDown += (f) =>
                            {
                                if (action.JumpFallenDownKeyFrame != null)
                                {
                                    doKeyFrame(action.JumpFallenDownKeyFrame);
                                }
                                if (action.IsMoveToTargetStopAction && NextAction())
                                {
                                }
                            };
                        }
                    }
                    else
                    {
                        float distance = DeepCore.Geometry.Vector2.Distance(Position, tpos);
                        jump_to_target_pos = tpos;
                        var fall = StartJump(action.JumpToTargetSpeedZ);
                        if (fall != null)
                        {
                            fall.OnFallDown += (f) =>
                            {
                                if (action.JumpFallenDownKeyFrame != null)
                                {
                                    doKeyFrame(action.JumpFallenDownKeyFrame);
                                }
                                if (action.IsMoveToTargetStopAction && NextAction())
                                {

                                }
                            };
                        }
                    }
                }
            }
        }
        private void doHitAttack(AttackProp attack)
        {
            var shape = SkillData.AttackShape;
            if (current_action?.OverrideAttackShape != null)
            {
                shape = current_action.OverrideAttackShape;
            }

            if (shape.IsSingle)
            {
                var range = shape;
                var attack_range = new UnitAttackRangeHelper(this)
                {
                    Shape = range.AsShape,
                    AttackRange = BodySize + range.AttackRange,
                    Distance = BodySize + range.AttackRange,
                    Height = BodyHeight,
                    Position = Position,

                    Direction = Direction,
                    FanAngle = range.AttackAngle,
                    StripWide = range.StripWide,
                    OffsetRadius = range.OffsetRadius,
                };
                if (attack_range.Touch(current_target))
                {
                    LaunchAttack(this, this, attack, current_target);
                }
            }
            else
            {
                var list = GetShapeAttackable<PreviewUnit>(SkillData.ExpectTarget);
                if (list.Count > 0)
                {
                    foreach (var tg in list)
                    {
                        LaunchAttack(this, this, attack, tg);
                    }
                }
            }
        }

        /// <summary>
        /// 身体攻击
        /// </summary>
        private void doBodyHit(UnitActionData current_action)
        {
            var list = GetShapeAttackable<PreviewUnit>(SkillData.ExpectTarget);
            if (list.Count > 0)
            {
                foreach (var tg in list)
                {
                    LaunchAttack(this, this, current_action.BodyHit, tg);
                }
                if (current_action.BodyHitNextAction)
                {
                    if (NextAction())
                    {

                    }
                }
            }
        }
        private void doMove()
        {
            if (start_move.IsEnd)
            {
                start_move = null;
            }
        }

        /// <summary>
        /// 防止技能位移导致单位重合
        /// </summary>
        private void doBodyBlock(UnitActionData current_action)
        {
            if (start_move != null)
            {
                if (ElasticOtherObject(current_target))
                {
                }
            }
        }

        /// <summary>
        /// 移动到目标面前
        /// </summary>
        private void doMoveToTarget(UnitActionData action)
        {
            if (move_to_target_path == null)
            {
                if (action.IsMoveToTargetStopAction && NextAction())
                {

                }
            }
            else if (TouchBody(this, current_target))
            {
                move_to_target_path = null;
                if (action.IsMoveToTargetStopAction && NextAction())
                {
                }
            }
            else
            {
                var tpos = move_to_target_path.Value;
                LookAt(tpos);
                if (MoveToTarget(tpos, action.MoveToTargetSpeedSEC, IntervalMS))
                {
                    move_to_target_path = null;
                }
            }
        }
        private void doJumpToTarget(UnitActionData action)
        {
            if (jump_to_target_pos.HasValue)
            {
                MoveToTarget(jump_to_target_pos.Value, action.MoveToTargetSpeedSEC, IntervalMS);
                if (TouchBody(this, current_target))
                {
                    jump_to_target_pos = null;
                    if (action.IsMoveToTargetStopAction && NextAction())
                    {
                    }
                }
            }
        }

        private bool getTargetPos(UnitActionData action, out DeepCore.Geometry.Vector3 targetPos)
        {
            var unitPos = targetPos = Position;
            UnitActionData.TargetPosEnum pos = UnitActionData.TargetPosEnum.Body;
            float offset = 0;
            if (action != null)
            {
                pos = action.TargetPos;
                offset = action.TargetOffset;
            }
            if (current_target != null)
            {
                targetPos = current_target.Position;
                switch (pos)
                {
                    case UnitActionData.TargetPosEnum.Body:
                        break;
                    case UnitActionData.TargetPosEnum.Face:
                        var angle = MathVector.getDegree(unitPos.X, unitPos.Y, targetPos.X, targetPos.Y);
                        var len = -(BodySize + current_target.BodySize + offset);
                        VectorHelper.MovePolar(ref targetPos, angle, len);
                        break;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public float GetSkillAttackRange(SkillTemplate skill)
        {
            return BodySize + skill.AttackRange;
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------
        #region Targets

        private List<TargetNode> targets = new List<TargetNode>();
        public TargetNode AddTargetAsync(DeepCore.Geometry.Vector3 offset, bool focus = false)
        {
            if (Disposed)
                return null;

            var unit = UnitData;
            if (PreviewConfig.IsRandomTarget)
            {
                unit = RandomTargetInfo();
            }

            var target = Proxy.CreateDisplay<TargetNode>("T:" + UnitData.Name);
            target.Position = Position + offset;
            target.LookAt(transform);
            if (focus)
            {
                FocusTarget(target);
            }
            target.Init(unit);
            targets.Add(target);
            return target;
        }
        public void FocusTarget(TargetNode target)
        {
            if (target != null)
            {
                current_target = target;
                LookAt(target.transform);
            }
        }
        public void FocusRandomTarget()
        {
            //var targets = GetAllTargets<TargetNode>();
            FocusTarget(RandomN.GetRandomInList(targets));
        }

        private void RTG_OnTargetPropertyChanged(GameObject obj)
        {
            SaveTargetPos();
        }

        private static List<DeepCore.Geometry.Vector3> saveTargetPos = new List<DeepCore.Geometry.Vector3>();
        private void SaveTargetPos()
        {
            saveTargetPos.Clear();
            foreach (var tgt in targets)
            {
                saveTargetPos.Add(tgt.Position);
            }
        }
        private void LoadTargetPos()
        {
            if (!PreviewConfig.IsResetPos)
            {
                //var targets = GetAllTargets<TargetNode>();
                for (var i = 0; i < saveTargetPos.Count && i < targets.Count; i++)
                {
                    var tgt = targets[i];
                    var tgp = saveTargetPos[i];
                    //if (tgt != current_target)
                    {
                        tgt.Position = tgp;
                    }
                }
            }
        }
        public void ResetTargetPos()
        {
            //var targets = GetAllTargets<TargetNode>();
            var tpos = new DeepCore.Geometry.Vector3[] {
                new DeepCore.Geometry.Vector3(+0.8f * SkillData.AttackRange, 0, 0),
                new DeepCore.Geometry.Vector3(-0.8f * SkillData.AttackRange, 0, 0),
                new DeepCore.Geometry.Vector3(0, +0.8f * SkillData.AttackRange, 0),
                new DeepCore.Geometry.Vector3(0, -0.8f * SkillData.AttackRange, 0),
                };
            for (var i = 0; i < targets.Count && i < tpos.Length; i++)
            {
                var tgt = targets[i];
                var tp = tpos[i];
                tgt.Position = tp;
            }
        }
        public static void ClearTargetPos()
        {
            saveTargetPos.Clear();
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------------

        private Texture2D tex_track;
        private Texture2D tex_frame;
        private Texture2D tex_frame_hi;
        private Texture2D tex_cursor;
        private Texture2D tex_black;
        private Texture2D tex_gray;
        protected override void OnInitGUI(GUICanvas canvas)
        {
            tex_track = Proxy.Textures.MakeTexture(GetType(), "tex_track", 64, 64, Color.gray.SetAlpha(0.3f));
            tex_frame = Proxy.Textures.MakeTexture(GetType(), "tex_frame", 64, 64, Color.red.SetAlpha(0.5f));
            tex_frame_hi = Proxy.Textures.MakeTexture(GetType(), "tex_frame_hi", 64, 64, Color.red.SetAlpha(0.8f));
            tex_cursor = Proxy.Textures.MakeTexture(GetType(), "tex_cursor", 64, 64, Color.white.SetAlpha(0.8f));
            tex_black = Proxy.Textures.MakeTexture(GetType(), "tex_black", 64, 64, Color.black.SetAlpha(0.5f));
            tex_gray = Proxy.Textures.MakeTexture(GetType(), "tex_gray", 64, 64, Color.black.SetAlpha(0.1f));
        }
        protected override void OnDrawGUI()
        {
            try
            {

                if (SkillData == null || SkillData.ActionQueue == null || SkillData.ActionQueue.Count == 0) { return; }
                var max_ms = SkillData.ActionQueue.Max(act => act == null ? 0 : act.TotalTimeMS);
                if (max_ms == 0) { return; }
                var fps = 60;
                if (Templates?.DefaultConfig != null) { fps = Templates.DefaultConfig.SYSTEM_FPS; }
                var interval = 1000 / fps;
                var frameW = 18;
                var frameH = 28;
                var totalW = Screen.width - 20;
                var totalH = SkillData.ActionQueue.Count * frameH + frameH;

                using (var gui = new GUIGraphics())
                {
                    gui.CurrentStype.fontSize = 9;
                    using (var gAnim = gui.BeginGroup(new Rect(10, Screen.height - totalH - frameH, totalW, totalH)))
                    {
                        for (int a = 0; a < SkillData.ActionQueue.Count; a++)
                        {
                            var act = SkillData.ActionQueue[a];
                            var ay = a * frameH + 1;
                            var ah = frameH - 2;
                            var tw = 100;
                            var totalRate = (float)act.TotalTimeMS / max_ms;
                            // draw name
                            GUI.Box(
                                new Rect(0, ay, tw - 4, ah),
                                new GUIContent() { text = $"ACTION: {act.Action.ActionName}({act.Action.ActionResId}) " },
                                new GUIStyle() { alignment = TextAnchor.MiddleRight });
                            GUI.Box(
                                new Rect(totalW - tw, ay, tw - 4, ah),
                                new GUIContent() { text = $" TIME: {act.TotalTimeMS}" },
                                new GUIStyle() { alignment = TextAnchor.MiddleLeft });

                            // draw frames
                            using (var gframes = gui.BeginGroup(new Rect(tw, ay, (totalW - tw - tw) * totalRate, ah)))
                            {
                                var grect = gframes.rect;
                                if (act.KeyFrames != null)
                                {
                                    GUI.DrawTexture(new Rect(0, 8, grect.width, grect.height - 8), tex_track);
                                    for (int t = 0; t < act.TotalTimeMS; t += 1000)
                                    {
                                        GUI.DrawTexture(new Rect(grect.width * t / act.TotalTimeMS, 0, 2, grect.height), tex_black);
                                    }
                                    for (int t = 0; t < act.TotalTimeMS; t += interval)
                                    {
                                        GUI.DrawTexture(new Rect(grect.width * t / act.TotalTimeMS, 8, 1, grect.height - 8), tex_gray);
                                    }
                                    for (int f = 0; f < act.KeyFrames.Count; f++)
                                    {
                                        var frame = act.KeyFrames[f];
                                        var frect = new Rect(grect.width * frame.FrameMS / act.TotalTimeMS, 8, frameW, grect.height - 8);
                                        if (frect.Contains(Event.current.mousePosition))
                                        {
                                            gui.CurrentStype.normal.background = tex_frame_hi;
                                        }
                                        else
                                        {
                                            gui.CurrentStype.normal.background = tex_frame;
                                        }
                                        GUI.Box(frect, new GUIContent()
                                        {
                                            text = frame.ToShortText(),
                                            tooltip = frame.ToToolText(),
                                        }, gui.CurrentStype);
                                    }
                                }
                                if (current_action == act)
                                {
                                    var crect = new Rect((float)(grect.width * PassTimeMS / act.TotalTimeMS), 8, frameW, grect.height - 8);
                                    gui.CurrentStype.normal.background = tex_cursor;
                                    GUI.DrawTexture(crect, tex_cursor);
                                }
                            }
                        }
                        GUI.Box(
                            new Rect(0, totalH - frameH, totalW, frameH),
                            new GUIContent() { text = $"SKILL:{SkillData}  TIME:{(long)(PassTimeMS)}" },
                            new GUIStyle() { alignment = TextAnchor.MiddleLeft });
                    }
                    {
                        GUIUtils.AutoTooltips();
                        //                     var mp = Event.current.mousePosition;
                        //                     var stype = new GUIStyle()
                        //                     {
                        //                         alignment = TextAnchor.LowerCenter,
                        //                     };
                        //                     stype.normal.textColor = Color.white;
                        //                     GUI.Label(new Rect(mp.x - 100, mp.y - 420, 200, 400), GUI.tooltip, stype);
                    }
                }
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        //----------------------------------------------------------------------------------------------------------------
    }

    //---------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// 从小到大排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectSorterNearest : IComparer<IZoneUnit>
    {
        private DeepCore.Geometry.Vector3 pos;
        public ObjectSorterNearest(DeepCore.Geometry.Vector3 pos)
        {
            this.pos = pos;
        }
        public int Compare(IZoneUnit x, IZoneUnit y)
        {
            float d0 = DeepCore.Geometry.Vector3.DistanceSquared(pos, x.Position);//MathVector.getDistanceSquare(x.X, x.Y, this.X, this.Y);
            float d1 = DeepCore.Geometry.Vector3.DistanceSquared(pos, y.Position);//MathVector.getDistanceSquare(y.X, y.Y, this.X, this.Y);
            if (d0 < d1)
                return -1;
            if (d0 > d1)
                return 1;
            return 0;
        }
    }

    /// <summary>
    /// 从大到小排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ObjectSorterFarthest : IComparer<IZoneUnit>
    {
        private DeepCore.Geometry.Vector3 pos;
        public ObjectSorterFarthest(DeepCore.Geometry.Vector3 pos)
        {
            this.pos = pos;
        }
        public int Compare(IZoneUnit x, IZoneUnit y)
        {
            float d0 = DeepCore.Geometry.Vector3.DistanceSquared(pos, x.Position);//MathVector.getDistanceSquare(x.X, x.Y, this.X, this.Y);
            float d1 = DeepCore.Geometry.Vector3.DistanceSquared(pos, y.Position);//MathVector.getDistanceSquare(y.X, y.Y, this.X, this.Y);
            if (d0 < d1)
                return 1;
            if (d0 > d1)
                return -1;
            return 0;
        }
    }

}

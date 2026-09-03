using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZonePreview;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.IO;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.OnGUI;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SocialPlatforms;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class BattleDisplay : PreviewBehavior
    {
        public static UnityRTG RTG => UnityRTG.RTG;
        public static PreviewProxy Proxy => PreviewProxy.Proxy;
        public static TemplateManager Templates => UnityIPC.Templates.Templates;
        //----------------------------------------------------------------------------------------------
        protected UnityZone battle;
        protected LocalBattle runtime;
        private SceneData lastSceneData;
        private bool firstFocus = false;
        public EditorScene Zone => runtime?.Zone;
        public UnityZone Battle => battle;
        void Start()
        {
            StartBattle();
            ZonePreviewComponent.log = new UnityIPCLogger("Preview");
            RTG.OnTargetTransformChanged += Battle_RTG_OnTargetTransformChanged;
        }
        void OnDestroy()
        {
            ClearBattle();
        }
        void Update()
        {
            if (battle != null)
            {
                try
                {
                    var deltaSEC = Time.deltaTime;
                    var intervalMS = deltaSEC * 1000;
                    if (battle.battle != null)
                    {
                        battle.battle.BeginUpdate(intervalMS);
                        battle.battle.Update();
                    }
                    battle.Update(intervalMS);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                if (battle.ModelWrap != null)
                {
                    if (battle.ModelWrap.Active != PreviewConfig.IsShowScene)
                    {
                        battle.ModelWrap.Active = PreviewConfig.IsShowScene;
                    }
                    if (battle.ModelWrap.Active != PreviewConfig.IsShowScene)
                    {
                        battle.ModelWrap.Active = PreviewConfig.IsShowScene;
                    }
                }
                UpdateAttackShape();
            }
        }
        public bool TryProcessMessage(ISerializable data)
        {
            if (data is SceneData sd)
            {
                ClearBattle();
                //UnityBattleFactory.Instance.CleanAssets();
                PreviewProxy.TimeTasks.AddTimeDelayMS(1000, (t) =>
                {
                    StartBattle(sd);
                });
                return true;
            }
            if (Zone != null && Zone.Components.TryGetComponentAs<ZonePreviewComponent>(out var preview))
            {
                if (preview.TryProcessPreviewMessage(data))
                {
                    return true;
                }
            }
            return false;
        }
        public void ClearBattle()
        {
            try
            {
                this.battle?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            this.battle = null;
            this.runtime = null;
        }
        public void StartBattle(SceneData sd = null)
        {
            try
            {
                this.firstFocus = false;
                this.battle = UnityBattleFactory.Instance.CreateBattle();
                if (battle != null)
                {
                    this.runtime = UnityIPC.HostFactory.CreatePreview(UnityIPC.Templates, UnityIPC.SlaveFactory, sd);
                    if (runtime != null)
                    {
                        UnityBattleConfig.ENABLE_BATTLE_DEBUG_GUI = false;
                        var config = new UnityBattleConfig()
                        {
                            EffectLayerName = null,
                            RayCastObjectLayerName = null,
                            RayCastTerrainLayerName = null,
                            Root = gameObject.transform,
                            VoxelTemplateName = RTG.TempVoxel,
                            SpellTemplateName = RTG.TempGizmoz,
                            UnitTemplateName = RTG.TempGizmoz,
                        };
                        this.battle.OnStart += Battle_OnStart;
                        this.battle.OnAddZoneObject += Battle_OnAddZoneObject;
                        this.battle.Init(config, runtime);
                        this.lastSceneData = Zone.SceneData;
                        {
                            //UnityZoneOnGUIRuntime.Init(Templates.Templates);
                            var ongui = gameObject.AddComponent<UnityZoneOnGUIRuntime>();
                            runtime.Layer.GUIRuntime = ongui;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        private void Battle_OnStart(UnityZone zone, LayerZone layer)
        {
            this.firstFocus = false;
            this.lastSceneData = layer.Data;
            if (Zone != null && Zone.Components.TryGetComponentAs<ZonePreviewComponent>(out var preview))
            {
                var center = battle.BattleToUnityWorldPosition(preview.Center);

                Debug.LogWarning("Center is " + center);
                var campos = Vector3.zero + new Vector3(center.x, Math.Min(battle.TerrainW, battle.TerrainH), 0);
                RTG.SetCamera(campos, campos);
                RTG.LookAt(center);
            }
        }
        protected virtual void Battle_RTG_OnTargetTransformChanged(GameObject obj)
        {
            try
            {
                if (obj.TryGetComponent<UnityLayerObjectBeharvior>(out var mono) && mono.zoneObject is UnityZoneUnit unit && unit.layerUnit.AsHost() is InstanceUnit host)
                {
                    var pos = unit.zone.UnityWorldToBattlePosition(obj.transform.localPosition);
                    host.Transport(pos);
                }
            }
            catch { }
        }
        protected virtual void Battle_OnAddZoneObject(UnityLayerObject go)
        {
            try
            {
                if (go is UnityZoneUnit unit)
                {
                    RTG.AddEditorObject(unit.gameObject);
                    var actor = unit.layerUnit.AsHost();
                    actor.OnLaunchSkill += Actor_OnLaunchSkill;
                    actor.OnOverSkill += Actor_OnOverSkill;
                    battle.layer.QueueTask(z =>
                    {
                        if (Zone != null && Zone.Components.TryGetComponentAs<ZonePreviewComponent>(out var preview))
                        {
                            if (preview.Actor != null && preview.Actor.ObjectID == unit.objectID)
                            {
                                if (firstFocus)
                                {
                                    // RTG.LookAt(unit.transform);
                                }
                                else
                                {
                                    firstFocus = true;
                                    var range = preview.Actor.BodySize + 10;
                                    if (preview.Actor.DefaultSkill != null)
                                    {
                                        range = Math.Max(range, preview.Actor.DefaultSkill.AttackRange);
                                    }
                                    RTG.LookAt(unit.transform, true, range);
                                }
                                //Debug.LogWarning("Battle_OnAddZoneObject");
                            }
                        }
                    });
                }
            }
            catch { }
        }


        //----------------------------------------------------------------------------------------------
        #region Attack Shape


        private void Actor_OnLaunchSkill(InstanceUnit obj, EquipSkill skill, StateSkill st)
        {
            var actor = battle.GetObjectAs<UnityZoneUnit>(obj.ObjectID);
            if (actor != null)
            {
                attackShape = InitAttackShape(actor, skill, st);
                attackShapeSkill = st;
            }
        }
        private void Actor_OnOverSkill(InstanceUnit obj, EquipSkill skill, StateSkill st)
        {
            attackShapeSkill = null;
            if (attackShape != null)
            {
                GameObject.Destroy(attackShape.gameObject);
                attackShape = null;
            }
        }


        private AttackShapeGizmos attackShape;
        private StateSkill attackShapeSkill;
        private AttackShapeGizmos InitAttackShape(UnityZoneUnit actor, EquipSkill skill, StateSkill st)
        {
            if (attackShape != null) { Destroy(attackShape.gameObject); attackShape = null; }
            var shape = skill.Data.AttackShape;
            if (st.CurrentAction?.OverrideAttackShape != null)
            {
                shape = st.CurrentAction.OverrideAttackShape;
            }
            attackShapeSkill = st;
            if (shape != null)
            {
                var BodySize = actor.layerUnit.BodyBlockSize;
                var BodyHeight = actor.layerUnit.BodyHeight;
                var current_target = battle.GetObjectAs<UnityZoneUnit>(st.TargetUnitID);
                var shapeObject = new GameObject(shape.AShape.ToString());
                shapeObject.transform.SetParent(actor.transform, false);
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
                    attackShape.SetMaterial(drender.material, Color.red.SetAlpha(0.5f));
                }
                return attackShape;
            }
            return null;
        }
        private void ClearAttackShape()
        {
            attackShapeSkill = null;
            if (attackShape != null)
            {
                GameObject.Destroy(attackShape.gameObject);
                attackShape = null;
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
                if (attackShapeSkill.IsDisposing)
                {
                    attackShapeSkill = null;
                    GameObject.Destroy(attackShape.gameObject);
                    attackShape = null;
                }
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------
        #region GUI

        private Texture2D tex_track;
        private Texture2D tex_frame;
        private Texture2D tex_frame_hi;
        private Texture2D tex_cursor;
        private Texture2D tex_cd;
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
            tex_cd = Proxy.Textures.MakeTexture(GetType(), "tex_cd", 64, 64, Color.white.SetAlpha(0.5f));
        }
        protected override void OnDrawGUI()
        {
            try
            {

                using (var gui = new GUIGraphics())
                {
                    gui.CurrentStype.fontSize = 9;
                    if (Zone != null && Zone.Components.TryGetComponentAs<ZonePreviewComponent>(out var preview))
                    {
                        var actor = preview.Actor;
                        if (actor != null)
                        {
                            var rect = Rect.zero;
                            if (actor.DefaultSkill is SkillTemplate skill)
                            {
                                rect = DrawSkillState(gui, actor, skill, 19);
                            }
                            DrawUnitHUD(gui, actor, 19 + rect.height + 8);
                        }
                        {
                            //                     var mp = Event.current.mousePosition;
                            //                     var stype = new GUIStyle()
                            //                     {
                            //                         alignment = TextAnchor.LowerCenter,
                            //                     };
                            //                     stype.normal.textColor = Color.white;
                            //                     GUI.Label(new Rect(mp.x - 100, mp.y - 420, 200, 400), GUI.tooltip, stype);
                        }
                    }
                    GUIUtils.AutoTooltips();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        protected virtual Rect DrawSkillState(GUIGraphics gui, InstanceUnit actor, SkillTemplate skill, float bottom = 10)
        {
            var SkillData = skill;
            if (SkillData.ActionQueue == null || SkillData.ActionQueue.Count == 0) { return Rect.zero; }
            var max_ms = SkillData.ActionQueue.Max(act => act == null ? 0 : act.TotalTimeMS);
            if (max_ms == 0) { return Rect.zero; }
            var fps = 60;
            if (Templates?.DefaultConfig != null) { fps = Templates.DefaultConfig.SYSTEM_FPS; }
            var interval = 1000 / fps;
            var frameW = 18;
            var frameH = 28;
            var totalW = Screen.width - 20;
            var totalH = SkillData.ActionQueue.Count * frameH + frameH;
            var currentPass = 0L;
            var bounds = new Rect(10, Screen.height - bottom - totalH, totalW, totalH);
            using (var gAnim = gui.BeginGroup(bounds))
            {
                for (int actIndex = 0; actIndex < SkillData.ActionQueue.Count; actIndex++)
                {
                    var act = SkillData.ActionQueue[actIndex];
                    var ay = actIndex * frameH + 1;
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
                        if (actor.CurrentState is StateSkill skillState)
                        {
                            if (skillState.CurrentActionIndex == actIndex)
                            {
                                var crect = new Rect((float)(grect.width * skillState.CurrentActionTimeProgressRate), 8, frameW, grect.height - 8);
                                gui.CurrentStype.normal.background = tex_cursor;
                                GUI.DrawTexture(crect, tex_cursor);
                                currentPass = (long)(skillState.CurrentPassTimeMS);
                            }
                        }
                    }
                    GUI.Box(
                        new Rect(0, totalH - frameH, totalW, frameH),
                        new GUIContent() { text = $"SKILL:{SkillData}  TIME:{currentPass}" },
                        new GUIStyle() { alignment = TextAnchor.MiddleLeft });
                }
                return bounds;
            }
        }

        protected virtual Rect DrawUnitHUD(GUIGraphics gui, InstanceUnit unit, float bottom = 10)
        {
            var totalBounds = new Rect();
            using (var skills = unit.AllocSkillsList())
            {
                var size = 64;
                var totalW = size * skills.Count;
                var bounds = new Rect(10, Screen.height - bottom - size, totalW, size);
                using (var gAnim = gui.BeginGroup(bounds))
                {
                    for (int skIndex = 0; skIndex < skills.Count; skIndex++)
                    {
                        var ss = skills[skIndex];
                        var btnRect = new Rect(skIndex * size + 4, +4, size - 8, size - 8);
                        if (GUIUtils.DrawCoolDownButton(btnRect, new GUIContent()
                        {
                            text = $"{ss.Data.Name}",
                            tooltip = $"{ss.Data}" +
                                 $"  level: {ss.Level}\n" +
                                 $"  CD: {(int)(ss.CDAmount * 100)}%\n" +
                                 $"  action index: {ss.ActionIndex}\n" +
                                 $"  FAR: {ss.FastActionRate}\n" +
                                 $"  FCR: {ss.FastCastRate}\n" +
                                 $"  active state: {ss.ActiveState}\n" +
                                 $"[点击清除技能CD]",
                        }, ss.CDAmount, tex_cd))
                        {
                            unit.ClearSkillCD(ss.ID);
                            if (unit.GetSkillState(ss.ID) is EquipSkill sk)
                            {
                                if (sk.LaunchSkill.AutoLaunch == false)
                                {
                                    unit.LaunchSkill(sk.ID, new DeepCore.GameData.Data.LaunchSkillParam()
                                    {

                                    });
                                }
                            }
                        }
                        //                          if (GUI.Button(btnRect, new GUIContent()
                        //                          {
                        //                              text = $"{ss.Data.Name}",
                        //                              tooltip = $"{ss.Data}" +
                        //                                  $"  level: {ss.Level}\n" +
                        //                                  $"  CD: {(int)(ss.CDAmount * 100)}%\n" +
                        //                                  $"  action index: {ss.ActionIndex}\n" +
                        //                                  $"  FAR: {ss.FastActionRate}\n" +
                        //                                  $"  FCR: {ss.FastCastRate}\n" +
                        //                                  $"  active state: {ss.ActiveState}",
                        //                          })) { unit.ClearSkillCD(ss.ID); }
                        //                          if (ss.IsCD)
                        //                          {
                        //                              btnRect.x += 4;
                        //                              btnRect.y += 4;
                        //                              btnRect.width -= 8;
                        //                              btnRect.height -= 8;
                        //                              float progress = ss.CDAmount; // 0.0 到 1.0 进度
                        //                              float angle = progress * 360f;
                        //                              var center = btnRect.center;
                        //                              var hsize = btnRect.height * progress;
                        //                              GUI.DrawTexture(new Rect(btnRect.x, btnRect.y + btnRect.height - hsize, btnRect.width, hsize), tex_cd);
                        //                          }
                    }
                }
                totalBounds = bounds;
            }

            using (var buffs = unit.AllocBuffsList())
            {
                var size = 32;
                var totalW = size * buffs.Count;
                var bounds = new Rect(10, totalBounds.y - size, totalW, size);
                using (var gAnim = gui.BeginGroup(bounds))
                {
                    for (int buffIndex = 0; buffIndex < buffs.Count; buffIndex++)
                    {
                        var bs = buffs[buffIndex];
                        var btnRect = new Rect(buffIndex * size + 4, +4, size - 8, size - 8);
                        if (GUIUtils.DrawCoolDownButton(btnRect, new GUIContent()
                        {
                            text = $"{bs.Data.Name}",
                            tooltip =
                               $"Buff: {bs.Data}\n" +
                               $"  level: {bs.BuffLevel}\n" +
                               $"  expire: {TimeSpan.FromMilliseconds(bs.ExpireMS)}\n" +
                               $"  overlay level: {bs.OverlayLevel}\n" +
                               $"  is equip: {bs.IsEquip}\n" +
                               $"  sender id: {bs.SenderID}\n" +
                               $"[点击清理BUFF]"
                        }, bs.ProgressAmount, tex_cd))
                        {
                            unit.RemoveBuff(bs);
                        }
                        //                         GUI.Button(btnRect, new GUIContent()
                        //                         {
                        //                             text = $"{bs.Data.Name}",
                        //                             tooltip =
                        //                                 $"Buff: {bs.Data}\n" +
                        //                                 $"  level: {bs.BuffLevel}\n" +
                        //                                 $"  expire: {TimeSpan.FromMilliseconds(bs.ExpireMS)}\n" +
                        //                                 $"  overlay level: {bs.OverlayLevel}\n" +
                        //                                 $"  is equip: {bs.IsEquip}\n" +
                        //                                 $"  sender id: {bs.SenderID}\n"
                        //                         });
                        //                         {
                        //                             btnRect.x += 4;
                        //                             btnRect.y += 4;
                        //                             btnRect.width -= 8;
                        //                             btnRect.height -= 8;
                        //                             float progress = bs.ProgressAmount; // 0.0 到 1.0 进度
                        //                             float angle = progress * 360f;
                        //                             var center = btnRect.center;
                        //                             var hsize = btnRect.height * progress;
                        //                             GUI.DrawTexture(new Rect(btnRect.x, btnRect.y + btnRect.height - hsize, btnRect.width, hsize), tex_cd);
                        //                         }
                    }
                }
                totalBounds.y = bounds.y;
                totalBounds.height += bounds.height;
            }
            using (var cards = unit.AllocCardsList())
            {
                var sizeW = 32;
                var sizeH = 48;
                var totalW = sizeW * cards.Count + 200;
                var bounds = new Rect(10, totalBounds.y - sizeH, totalW, sizeH);
                using (var gAnim = gui.BeginGroup(bounds))
                {
                    for (int buffIndex = 0; buffIndex < cards.Count; buffIndex++)
                    {
                        var bs = cards[buffIndex];
                        var btnRect = new Rect(buffIndex * sizeW + 4, +4, sizeW - 8, sizeH - 8);
                        if (GUI.Button(btnRect, new GUIContent()
                        {
                            text = $"{bs.Card.Name}",
                            tooltip = $"Card: {bs.Card}\n" +
                            $"  level: {bs.Level}\n" +
                            $"[点击清理词缀]"
                        }))
                        {
                            unit.Cartridge.RemoveCardSlot(bs.Card.ID);
                        }
                    }
                    if (cards.Count > 0)
                    {
                        if (GUI.Button(new Rect(cards.Count * sizeW + 4, +4, 100, sizeH - 8), new GUIContent()
                        {
                            text = $"Clear",
                            tooltip = $"[点击清理所有词缀]"
                        }))
                        {
                            unit.Cartridge.ClearCardSlots();
                        }
                    }
                }
                totalBounds.y = bounds.y;
                totalBounds.height += bounds.height;
            }
            return totalBounds;
        }

        #endregion
        //----------------------------------------------------------------------------------------------
    }
}

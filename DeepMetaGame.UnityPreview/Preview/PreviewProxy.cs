using DeepCore;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZonePreview;
using DeepCore.Game3D.Host.ZoneRuntime;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.IO;
using DeepCore.Json;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepCore.Unity3D.AB;
using DeepCore.Unity3D.Impl;
using DeepCore.Unity3D.Impl.OnGUI;
using DeepCore.Xml;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Unity.BattleView;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using static DeepCore.Colors;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public static class PreviewConfig
    {
        public static bool IsShowGizmos = true;
        public static bool IsShowScene = true;
        public static bool IsShowFlag = false;
        public static bool IsRandomTarget = false;
        public static bool IsResetPos = false;
        public static bool IsAutoReplay = true;
        public static void Load()
        {
            Properties.LoadStaticFieldsFromFile(Path.Combine(Application.persistentDataPath, $"{nameof(PreviewConfig)}.save"), typeof(PreviewConfig));
        }
        public static void Save()
        {
            Properties.SaveStaticFieldsToFile(Path.Combine(Application.persistentDataPath, $"{nameof(PreviewConfig)}.save"), typeof(PreviewConfig));
        }
    }

    public class PreviewProxy : UnityIPC, IHostZone
    {
        public static PreviewProxy Proxy { get; private set; }
        public static System.Random RandomN { get; } = new System.Random();
        //public static EditorTemplatesData TemplateDatas { get; } = new EditorTemplatesData();
        //-------------------------------------------------------------------
        public Config CFG => Templates?.Templates?.DefaultConfig;
        public DeepCore.Geometry.Terrain.ITerrainSurface Terrain3D { get; } = new PreviewSurface();
        SingleThreadCollectionPool IZone.ObjectPool => UnityIPC.ObjectPool;
        private BattleDisplay battleDisplay { get; set; }
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        protected override void Awake()
        {
            Proxy = this;
            base.Awake();
            //GUIFactory = new OnGUIFactory(EditorRootDir);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        protected override void Start()
        {
            HandleFromSession += OnMsgReceived;
            base.Start();
        }
        protected override void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (Time.timeScale == 0)
                    {
                        if (lastSpeed == 0) lastSpeed = 1f;
                        Time.timeScale = lastSpeed;
                    }
                    else
                    {
                        lastSpeed = Time.timeScale;
                        if (lastSpeed == 0) lastSpeed = 1f;
                        Time.timeScale = 0;
                    }
                }
                base.Update();
                if (RTG.TempGround != null)
                {
                    RTG.TempGround.gameObject.SetActive(PreviewConfig.IsShowScene);
                }
            }
            catch { }
        }
        protected override void OnUpdate(float deltaSEC)
        {
            base.OnUpdate(deltaSEC);
        }
        protected override void Session_Validate(ISerializable state)
        {
            if (state is PreviewState ovstate)
            {
                PreviewConfig.IsShowScene = ovstate.ShowScene;
                PreviewConfig.IsShowFlag = ovstate.ShowFlag;
                PreviewConfig.IsShowGizmos = ovstate.ShowBody;
            }
            PreviewConfig.Load();
        }
        protected override void Session_Connected(Exception err, object message)
        {
            base.Session_Connected(err, message);
            if (RTG.TempGizmoz != null)
            {
                RTG.TempGizmoz.gameObject.SetActive(false);
            }
            if (RTG.TempHeadText != null)
            {
                RTG.TempHeadText.gameObject.SetActive(false);
            }
            Proxy.RefreshHWND();
            //this.BattleInit();
            this.battleDisplay = this.gameObject.AddComponent<BattleDisplay>();
        }
        //-------------------------------------------------------------------------------------------------------
        private List<IViewResource> resources = new List<IViewResource>();
        public void PlaySound(string soundName, ResourceType soundType, DisplayObject go)
        {
            try
            {
                if (string.IsNullOrEmpty(soundName)) { return; }
                var snd = RTG.LoadResource(soundName, soundType, go);
                snd?.PlaySound(soundType);
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        public override bool TryGetResourceProperties(string resName, out IResourceProperties resProp)
        {
            if (string.IsNullOrEmpty(resName))
            {
                resProp = null;
                return false;
            }
            try
            {
                var editorDir = UnityBattleFactory.Instance.RootPath;
                resProp = null;
                var path = $"{editorDir}/data/{CMD5.CalculateMD5(resName)}.xml";
                if (File.Exists(path))
                {
                    var res = XmlUtil.LoadXMLObject<ResourcePropertiesTuple>(path);
                    if (res != null && res.Properties != null)
                    {
                        resProp = res.Properties;
                        return true;
                    }
                }
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
            }
            return Templates.Templates.ResourcePropertiesMap.PropertiesMap.TryGetValue(resName, out resProp);
        }
        public IViewResource LoadRes(string resName, ResourceType resType, DisplayObject go)
        {
            try
            {
                var res = RTG.LoadResource(resName, resType, go);
                if (res != null)
                {
                    //UnityIPC.RTG.AddEditorObject(res.go);
                    resources.Add(res);
                }
                return res;
            }
            catch (Exception e)
            {
                PLog(e);
            }
            return null;
        }
        public virtual float PlayEffect(IViewResource res, LaunchEffect effect, IViewResource binding = null)
        {
            if (res?.gameObject != null)
            {
                res.PlayEffect(effect.AnimName, effect.IsLoop, effect.TimeScale, binding?.transform);
                var bindPart = effect.BindPartName;
                var bindBody = effect.BindBody;
                if (bindBody && binding != null)
                {
                    res.BindBody(binding, bindPart);
                }
                if (effect.ScaleToBodySize != 0)
                {
                    res.transform.localScale = new Vector3(effect.ScaleToBodySize, effect.ScaleToBodySize, effect.ScaleToBodySize);
                }
                var effectTimeMS = effect.EffectTimeMS;
                if (effectTimeMS > 0)
                {
                    TimeTasks.AddTimeDelayMS(effectTimeMS, (t) =>
                    {
                        res.Dispose();
                    });
                    return effectTimeMS;
                }
                else if (effectTimeMS == 0 && res.gameObject.TryGetParticleDurationMS(out var durationMS, out var loop))
                {
                    TimeTasks.AddTimeDelayMS(durationMS, (t) =>
                    {
                        res.Dispose();
                    });
                    return durationMS;
                }
                else
                {
                    TimeTasks.AddTimeDelayMS(1000, (t) =>
                    {
                        res.Dispose();
                    });
                    return 1000;
                }
            }
            return 0;
        }
        public virtual float PlayEffect(IViewResource res, int effectTimeMS = 0, IViewResource binding = null)
        {
            if (res?.gameObject != null)
            {
                if (binding != null)
                {
                    res.BindBody(binding);
                }
                res.PlayEffect(null, false, 1f, binding?.transform);
                if (effectTimeMS > 0)
                {
                    TimeTasks.AddTimeDelayMS(effectTimeMS, (t) =>
                    {
                        res.Dispose();
                    });
                    return effectTimeMS;
                }
                else
                {
                    if (res.TryGetDurationMS(out var durationMS, out var loop))
                    {
                        TimeTasks.AddTimeDelayMS(durationMS, (t) =>
                        {
                            res.Dispose();
                        });
                        return durationMS;
                    }
                    else if (res.gameObject.TryGetParticleDurationMS(out durationMS, out loop))
                    {
                        TimeTasks.AddTimeDelayMS(durationMS, (t) =>
                        {
                            res.Dispose();
                        });
                        return durationMS;
                    }
                    else
                    {
                        TimeTasks.AddTimeDelayMS(1000, (t) =>
                        {
                            res.Dispose();
                        });
                        return 1000;
                    }
                }
            }
            return 0;
        }
        public TimeTaskMS<ST> AddTimeTask<ST>(float intervalMS, float delayMS, int repeat, ST st, TickHandler<ST> handler)
        {
            return UnityIPC.TimeTasks.AddTimeTask<ST>(intervalMS, delayMS, repeat, st, handler);
        }
        protected virtual void Cleanup()
        {
            RTG.TargetObject = null;
            foreach (var v in gameObject.GetComponentsInChildren<PreviewObject>(true))
            {
                v.Dispose();
            }
            foreach (var res in resources)
            {
                res.Dispose();
            }
            resources.Clear();
        }
        //-------------------------------------------------------------------------------------------------------
        private float lastSpeed = 1f;
        private ISerializable lastData;
        private PreviewObject lastFocus;
        private UnitInfo lastUnit;
        private SkillTemplate lastSkill;
        private BuffTemplate lastBuff;
        //private string lastJson;
        //-------------------------------------------------------------------------------------------------------

        protected virtual void OnMsgReceived(ISerializable data)
        {

            try
            {
                if (data != null)
                {
                    //var newJson = XmlUtil.ObjectToXmlString(data);
                    //if (lastJson != newJson)
                    {
                        Templates.Templates.Put(data);
                        if (battleDisplay != null)
                        {
                            if (battleDisplay.TryProcessMessage(data))
                            {
                                Cleanup();
                                return;
                            }
                        }
                        Cleanup();
                        lastFocus = Show(data);
                    }
                    lastData = data;
                    //lastJson = newJson;
                }
                else
                {
                    Cleanup();
                }
            }
            catch (Exception e)
            {
                PLog(e);
            }
        }
        //-------------------------------------------------------------------------------------------------------
        #region Unity Show

        //         public async Task<T> CreateDisplayAsync<T, V>(string name, UnityEngine.Vector3 pos, V data) where T : PreviewObject<V>
        //         {
        //             var go = new GameObject(name);
        //             var to = go.AddComponent<T>();
        //             go.transform.SetParent(this.transform, false);
        //             go.transform.position = pos;
        //             await to.InitAsync(data);
        //             return to;
        //         }

        private void Replay()
        {
            var list = gameObject.GetComponentsInChildren<PreviewObject>(true);
            foreach (var v in list)
            {
                v.Replay();
            }
        }

        private PreviewObject Show(ISerializable data)
        {
            PLog($"Show {data}");
            if (data is PreviewUpdate preview)
            {
                return Show(preview);
            }
            else if (data is UnitInfo unit)
            {
                return Show(unit);
            }
            else if (data is ItemTemplate item)
            {
                return Show(item);
            }
            else if (data is SpellTemplate spell)
            {
                return Show(spell);
            }
            else if (data is BuffTemplate buff)
            {
                return Show(buff);
            }
            else if (data is SkillTemplate skill)
            {
                return Show(skill);
            }
            else if (data is BattleUITemplate ui)
            {
                return Show(ui);
            }
            else if (data is PreviewResource res)
            {
                return Show(res);
            }
            else if (data is PreviewResourceList resList)
            {
                return Show(resList);
            }
            else
            {
                return null;
            }
        }



        private PreviewObject Show(PreviewUpdate preview)
        {
            if (preview.Template is UnitInfo unit)
            {
                lastUnit = unit;
                if (preview.Relation is SkillTemplate _skill)
                {
                    lastSkill = _skill;
                    var to = CreateDisplay<UnitSkillDisplay>(unit.Name);
                    to.Init(new(unit, _skill));
                    if (preview.Focus)
                    {
                        RTG?.LookAt(to.transform);
                    }
                    return to;
                }
                else if (preview.Relation is BuffTemplate buff)
                {
                    lastBuff = buff;
                    var to = CreateDisplay<UnitBuffDisplay>(unit.Name);
                    to.Init(new(unit, buff));
                    if (preview.Focus)
                    {
                        RTG?.LookAt(to.transform);
                    }
                    return to;
                }
                else if (preview.Relation is UnitAttachmentAbility attachmentAbility)
                {
                    var to = CreateDisplay<UnitAttachmentsDisplay>(unit.Name);
                    to.Init(unit);
                    if (preview.Focus)
                    {
                        RTG?.LookAt(to.transform);
                    }
                    return to;
                }
                else
                {
                    var to = Show(preview.Template);
                    if (preview.Focus)
                    {
                        RTG?.LookAt(to.transform);
                    }
                    return to;
                }
            }
            if (preview.Template is SkillTemplate skill)
            {
                lastSkill = skill;
                if (preview.Relation is UnitInfo _unit)
                {
                    lastUnit = _unit;
                    var to = CreateDisplay<UnitSkillDisplay>(_unit.Name);
                    to.Init(new(_unit, skill));
                    if (preview.Focus)
                    {
                        RTG?.LookAt(to.transform);
                    }
                    return to;
                }
            }
            {
                var to = Show(preview.Template);
                if (preview.Focus)
                {
                    RTG?.LookAt(to.transform);
                }
                return to;
            }
        }
        private PreviewObject Show(UnitInfo unit)
        {
            lastUnit = unit;
            var to = CreateDisplay<UnitDisplay>(unit.Name);
            to.Init(unit);
            return to;
        }
        private PreviewObject Show(BuffTemplate buff)
        {
            lastBuff = buff;
            if (lastUnit != null)
            {
                var to = CreateDisplay<UnitBuffDisplay>(buff.Name);
                to.Init(new(lastUnit, buff));
                return to;
            }
            else
            {
                var to = CreateDisplay<BuffDisplay>(buff.Name);
                to.Init(buff);
                return to;
            }
        }
        private PreviewObject Show(SkillTemplate skill)
        {
            lastSkill = skill;
            if (lastUnit != null)
            {
                var to = CreateDisplay<UnitSkillDisplay>(skill.Name);
                to.Init(new(lastUnit, skill));
                return to;
            }
            else
            {
                var to = CreateDisplay<SkillDisplay>(skill.Name);
                to.Init(skill);
                return to;
            }
        }
        private PreviewObject Show(SpellTemplate spell)
        {
            if (lastUnit != null)
            {
                if (lastSkill != null)
                {
                    var to = CreateDisplay<UnitSkillDisplay>(lastSkill.Name);
                    to.Init(new(lastUnit, lastSkill));
                    return to;
                }
                if (lastBuff != null)
                {
                    var to = CreateDisplay<UnitBuffDisplay>(lastBuff.Name);
                    to.Init(new(lastUnit, lastBuff));
                    return to;
                }
            }
            {
                var to2 = CreateDisplay<SpellDisplay>(spell.Name);
                to2.Init(spell);
                return to2;
            }
        }
        private PreviewObject Show(ItemTemplate item)
        {
            var to = CreateDisplay<ItemDisplay>(item.Name);
            to.Init(item);
            return to;
        }
        private PreviewObject Show(BattleUITemplate ui)
        {
            var to = CreateDisplay<TemplateBattleUIDisplay>(ui.Name);
            to.Init(ui);
            return to;
        }

        private PreviewObject Show(PreviewResource res)
        {
            if (res.PropertiesData != null)
            {
                Templates.Templates.ResourcePropertiesMap.PropertiesMap[res.ResID] = res.PropertiesData;
            }
            var to = CreateDisplay<ResourceDisplay>($"{res.ResID}");
            to.Init(res);
            if (res.Focus)
            {
                RTG?.LookAt(to.transform);
            }
            return to;
        }
        private PreviewObject Show(PreviewResourceList resList)
        {
            var to = CreateDisplay<ResourceListDisplay>($"{resList}");
            to.Init(resList);
            return to;
        }

        public class PreviewSurface : DeepCore.Geometry.Terrain.ITerrainSurface
        {
            public int XCount => 128;
            public int YCount => 128;
            public float TotalSizeX => 128;
            public float TotalSizeY => 128;
            public bool TryGetVoxelLayerByPos(in DeepCore.Geometry.Vector3 pos, out float upward, out float top)
            {
                upward = 0f;
                top = 100f;
                return true;
            }
            public bool TryMoveSpellOnFloor(ref DeepCore.Geometry.Vector3 pos, float direction, float distance)
            {
                DeepCore.Geometry.VectorHelper.MovePolar(ref pos, direction, distance);
                return true;
            }
        }
        #endregion
        //---------------------------------------------------------------------------------------------------------------------------
        #region Battle Show

        #endregion
        //---------------------------------------------------------------------------------------------------------------------------

        protected override void OnGUI()
        {
            try
            {
                bool ispause = Time.timeScale == 0;
                GUIUtils.DrawGrid(new Vector2(0, Screen.height), new Vector2(45, -24), 100, 1, new DrawGridActionRect[]
                {
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsShowGizmos, "边框"))
                         {
                             PreviewConfig.Save();
                         }
                     },
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsShowScene, "场景"))
                         {
                             PreviewConfig.Save();
                         }
                     },
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsShowFlag, "FLAG"))
                         {
                             PreviewConfig.Save();
                         }
                     },
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsResetPos, "重置"))
                         {
                             PreviewConfig.Save();
                             Cleanup();
                             Show(lastData);
                         }
                     },
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsRandomTarget, "随机"))
                         {
                             PreviewConfig.Save();
                             Cleanup();
                             Show(lastData);
                         }
                     },
                     (rect) =>
                     {
                         if (GUI.Button(rect, new GUIContent()
                         {
                             text = $"重播",
                             tooltip = "Replay"
                         }))
                         {
                             if (lastSpeed == 0) lastSpeed = 1f;
                             Time.timeScale = lastSpeed;
                             Replay();
                         }
                     },
                     (rect) =>
                     {
                         rect.width *= 2;
                         if (GUI.Button(rect, new GUIContent()
                         {
                             text = $"播放/暂停",
                             tooltip = ispause ? "Resume" : "Play"
                         }))
                         {
                             if (Time.timeScale == 0)
                             {
                                 if (lastSpeed == 0) lastSpeed = 1f;
                                 Time.timeScale = lastSpeed;
                             }
                             else
                             {
                                 lastSpeed = Time.timeScale;
                                 if (lastSpeed == 0) lastSpeed = 1f;
                                 Time.timeScale = 0;
                             }
                         }
                     },
                     (rect) => { },
                     (rect) =>
                     {
                         if (GUI.Button(rect, new GUIContent() { text = $"速度-", tooltip = "Speed Down" }))
                         {
                             if (Time.timeScale > 1)
                             {
                                 Time.timeScale -= 1;
                             }
                             else
                             {
                                 Time.timeScale /= 2f;
                             }
                             lastSpeed = Time.timeScale;
                         }
                     },
                     (rect) =>
                     {
                         if (GUI.Button(rect, new GUIContent() { text = $"速度+", tooltip = "Speed UP" }))
                         {
                             if (Time.timeScale >= 1)
                             {
                                 Time.timeScale += 1;
                             }
                             else
                             {
                                 Time.timeScale *= 2f;
                             }
                             lastSpeed = Time.timeScale;
                         }
                     },
                     (rect) => { },
                     (rect) =>
                     {
                         rect.width *= 2;
                         if (GUI.Button(rect, "RESET"))
                         {
                             Time.timeScale = lastSpeed = 1;
                             Cleanup();
                             UnitSkillDisplay.ClearTargetPos();
                             var rst = Show(lastData);
                             if (rst is PreviewObject t)
                             {
                                 RTG.LookAt(t.transform);
                                 RTG.TargetObject = t.gameObject;
                                 RTG.LookAt(t.transform);
                             }
                             UnitSkillDisplay.ClearTargetPos();

                         }
                     },
                     (rect) => { },
                     (rect) =>
                     {
                         if (GUIUtils.Toggle(rect, ref PreviewConfig.IsAutoReplay, "循环"))
                         {
                             Cleanup();
                             Show(lastData);
                         }
                     },
                     (rect) =>
                     {
                           var style = new GUIStyle(GUI.skin.label);
                           {
                                style.alignment = TextAnchor.LowerLeft;
                                style.normal.textColor = Color.white;
                                style.normal.background = Proxy.Textures.MakeTexture(GetType(), "txt_status", 64, 64, Color.black.SetAlpha(0.8f));
                           }
                           rect.width = Screen.width - (rect.x + rect.width);
                           var spd = Time.timeScale >= 1f ? Time.timeScale.ToString() : $"1/{1f / Time.timeScale}";
                           var obj = (RTG?.TargetObject);
                           var msg_spd = $"{(ispause ? "Paused" : $"{spd}X")}";
                           var msg_obj = $"{((obj != null) ? ("{" + DockingOffset.FromVectorOffset(obj.transform.position.ToGeometry()) + "} " + obj.name) : "")}";
                           var msg = $"|  {msg_spd}  |  {msg_obj}";
                           GUI.Label(rect, new GUIContent(){ text = msg }, style);
                     },
                });

                UnityBattleConfig.ENABLE_BATTLE_GIZMOS = PreviewConfig.IsShowGizmos;
                UnityBattleConfig.ENABLE_BATTLE_GIZMOS_FLAGS = PreviewConfig.IsShowFlag;
            }
            catch { }
        }
    }


}

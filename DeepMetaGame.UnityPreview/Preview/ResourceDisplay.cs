using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.ZoneEditor.Prewview;
using UnityEngine;
using UnityEngine.XR;

namespace DeepMetaGame.Unity.Preview.Preview
{
    public class ResourceDisplay : PreviewObject<PreviewResource>
    {
        private float deadTime;
        private ParticleSystem[] pss;
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoInit(PreviewResource res)
        {
            var resid = LoadRes(res.ResID, res.ResType);
            if (resid != null)
            {
                //                 if (resid.go.TryGetComponentsInChildren<MeshFilter>(out var meshs))
                //                 {
                //                     foreach (var mesh in meshs)
                //                     {
                //                         RTG.AddEditorObject(mesh.gameObject);
                // //                         if (!mesh.gameObject.TryGetComponent<MeshCollider>(out var collider))
                // //                         {
                // //                             try
                // //                             {
                // //                                 collider = mesh.gameObject.AddComponent<MeshCollider>();
                // //                             }
                // //                             catch { }
                // //                         }
                //                     }
                //                 }
                deadTime = PlayEffect(resid);
            }

            var bounds = gameObject.CalculateRendererBounds();
            this.BodyHeight = bounds.max.y;
            this.BodySize = Mathf.Max(bounds.max.x, bounds.max.z);

            RTG.TargetObject = gameObject;
            //LookAt(transform.position + Vector3.back);
            //OnInitGUI();
            //RTG.LookAt(transform);
        }
        protected override void DoReplay()
        {
            if (MainRes != null)
            {
                deadTime = PlayEffect(MainRes);
            }
        }
        protected override void DoUpdate()
        {
            if (MainRes != null && PassTimeMS > deadTime)
            {
                if (MainRes.resType.HasFlag(DeepMetaGame.Data.ResourceType.Sound_All))
                {

                }
                else
                {
                    Replay();
                }
            }
        }
        public float PlayEffect(IViewResource res, int effectTimeMS = 0, IViewResource binding = null)
        {
            //res?.PlaySound();
            if (res?.gameObject != null)
            {
                res.gameObject.PlayParticle();
                if (effectTimeMS > 0)
                {
                    return effectTimeMS;
                }
                else if (effectTimeMS == 0 && res.gameObject.TryGetParticleDurationMS(out var durationMS, out var loop))
                {
                    return durationMS;
                }
                else
                {
                    return 1000;
                }
            }
            res?.PlayAnim();
            return 0;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------


#if FALSE
        private Texture2D tex_copy;
        private Texture2D tex_play;
        private Vector2 scrollPositionAnims;
        private Vector2 scrollPositionParts;
        protected override void OnInitGUI(GUICanvas canvas)
        {
            this.tex_copy = Proxy.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_common_19.png");
            this.tex_play = Proxy.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_simpleshape_23.png");

        }

        protected override void OnDrawGUI()
        {
            try
            {
                using (var gui = new GUIGraphics())
                {
                    gui.CurrentStype.fontSize = 9;

                    if (MainRes != null)
                    {
                        OnGUIAnims();
                        OnGUIParts();
                    }
                    GUIUtils.AutoTooltips();
                }
            }
            catch (Exception ex)
            {
                PLog(ex);
            }
            //panelAnim?.Visit();
        }

        private void OnGUIAnims()
        {
            var viewW = 0f;
            var viewH = 0f;
            OnGUICalcAnims(ref viewW, ref viewH);
            var formRect = new Rect(0, 100, viewW + 20, Screen.height - 160);
            scrollPositionAnims = GUI.BeginScrollView(formRect, scrollPositionAnims, new Rect(0, 0, viewW, viewH));
            try
            {

                var sx = 0;
                var sy = 0;
                if (MainRes.go.TryGetComponentsInChildren<Animation>(out var animations))
                {
                    foreach (var anim in animations)
                    {
                        GUI.TextField(new Rect(sx, sy, 200, 20), $"Animation:{anim.name}");
                        sy += 20;
                        foreach (AnimationState state in anim)
                        {
                            if (GUIUtils.Button(new Rect(sx, sy, 20, 20), new GUIContent() { image = tex_play, tooltip = "Copy Text" }))
                            {
                                anim.enabled = true;
                                anim.wrapMode = WrapMode.Loop;
                                anim.Play(state.name);
                                GUIUtility.systemCopyBuffer = state.name;
                            }
                            GUI.TextField(new Rect(sx + 20, sy, 200, 20), $"{state.name} {state.wrapMode}");
                            sy += 20;
                        }
                    }
                }
                if (MainRes.go.TryGetComponentsInChildren<Animator>(out var animators))
                {
                    foreach (var anim in animators)
                    {
                        GUI.TextField(new Rect(sx, sy, 200, 20), $"Animator:{anim.name}");
                        if (anim?.runtimeAnimatorController?.animationClips != null)
                        {
                            sy += 20;
                            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                            {
                                if (GUIUtils.Button(new Rect(sx, sy, 20, 20), new GUIContent() { image = tex_play, tooltip = "Copy Text" }))
                                {
                                    //clip.wrapMode = WrapMode.Loop;
                                    anim.enabled = true;
                                    anim.applyRootMotion = false;
                                    anim.Play(clip.name);
                                    GUIUtility.systemCopyBuffer = clip.name;
                                }
                                GUI.TextField(new Rect(sx + 20, sy, 200, 20), $"{clip.name} {clip.wrapMode}");
                                sy += 20;
                            }
                        }
                    }
                }
                if (formRect.Contains(Input.mousePosition))
                {
                    Input.ResetInputAxes();
                }
            }
            finally
            {
                GUI.EndScrollView();
            }
        }
        private void OnGUICalcAnims(ref float sx, ref float sy)
        {
            if (MainRes.go.TryGetComponentsInChildren<Animation>(out var animations))
            {
                foreach (var anim in animations)
                {
                    sy += 20;
                    foreach (AnimationState state in anim)
                    {
                        sy += 20;
                    }
                }
            }
            if (MainRes.go.TryGetComponentsInChildren<Animator>(out var animators))
            {
                foreach (var anim in animators)
                {
                    sy += 20;
                    if (anim?.runtimeAnimatorController?.animationClips != null)
                    {
                        foreach (var clip in anim.runtimeAnimatorController.animationClips)
                        {
                            sy += 20;
                        }
                    }

                }
            }
            sx = 200 + 20 + 20;
        }

        private void OnGUIParts()
        {
            var viewW = 0f;
            var viewH = 0f;
            OnGUICalcParts(1, ref viewW, ref viewH, MainRes.transform);
            viewW = viewW + 200;
            var formRect = new Rect(Screen.width - viewW - 20, 100, viewW + 20, Screen.height - 160);
            scrollPositionParts = GUI.BeginScrollView(formRect, scrollPositionParts, new Rect(0, 0, viewW, viewH));
            try
            {
                var sx = 20f;
                var sy = 0f;
                OnGUIPart(1, sx, ref sy, MainRes.transform);
                if (formRect.Contains(Input.mousePosition))
                {
                    Input.ResetInputAxes();
                }
            }
            finally
            {
                GUI.EndScrollView();
            }
        }
        private void OnGUICalcParts(int deep, ref float sx, ref float sy, Transform part)
        {
            var sw = 20;
            sy += 20;
            sx = Math.Max(deep * sw, sx);
            for (var i = 0; i < part.childCount; i++)
            {
                var child = part.GetChild(i);
                OnGUICalcParts(deep + 1, ref sx, ref sy, child);
            }
        }
        private void OnGUIPart(int deep, float sx, ref float sy, Transform part)
        {
            if (GUIUtils.Button(new Rect(sx - 20, sy, 20, 20), new GUIContent() { image = tex_copy, tooltip = "Copy Text" }))
            {
                RTG.TargetObject = part.gameObject;
                GUIUtility.systemCopyBuffer = part.gameObject.name;
            }
            GUI.TextField(new Rect(sx, sy, 200, 20), part.gameObject.name);
            sy += 20;
            for (var i = 0; i < part.childCount; i++)
            {
                var child = part.GetChild(i);
                OnGUIPart(deep + 1, sx + 20, ref sy, child);
            }
        }
    
#else
        protected override void OnInitGUI(GUICanvas canvas)
        {
            if (MainRes?.gameObject)
            {
                {
                    var animWindow = new ResourceAnimateWindow(MainRes);
                    animWindow.Position = new Vector2(0, 100);
                    //attachWindow.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
                    animWindow.AnchorPadding = new Padding(0, 100, 0, 60);
                    RootCanvas.AddChild(animWindow);
                }
                {
                    var partWindow = new ResourcePartWindow(MainRes);
                    partWindow.Position = new Vector2(Screen.width - partWindow.Width, 100);
                    //partWindow.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                    partWindow.AnchorPadding = new Padding(0, 100, 0, 60);
                    RootCanvas.AddChild(partWindow);
                }
            }
        }
        protected override void OnDrawGUI()
        {
            base.OnDrawGUI();
            if (MainRes?.gameObject)
            {
                var go = MainRes.gameObject;
                var totalW = Screen.width - 20;
                var totalH = Screen.height - 40;
                using (var gui = new GUIGraphics())
                {
                    gui.CurrentStype.fontSize = 12;
                    using (var gAnim = gui.BeginGroup(new Rect(10, 0, totalW, totalH)))
                    {
                        var frameW = 200;
                        var frameH = 28;
                        var sy = gAnim.rect.height - frameH - frameH;
                        var style = new GUIStyle(GUI.skin.button);
                        style.fontSize = 12;
                        if (go.TryGetParticleDurationMS(out var timeMS, out var isLoop))
                        {
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Duration : {timeMS}(MS) Loop:{isLoop}", style))
                            {
                                GUIUtility.systemCopyBuffer = $"{timeMS}";
                                GUIUtility.systemCopyBuffer = $"{timeMS}";
                            }
                            sy -= frameH;
                        }
                        sy -= 4;
                        if ((MainRes.resType & ResourceType.Sound_All) != 0)
                        {
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Stop All Sound", style))
                            {
                                UnityBattleFactory.Audio.StopSound();
                            }
                            sy -= frameH;
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Play As Sound_Effect", style))
                            {
                                MainRes.PlaySound(ResourceType.Sound_Effect);
                            }
                            sy -= frameH;
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Play As Sound_BGM", style))
                            {
                                MainRes.PlaySound(ResourceType.Sound_BGM);
                            }
                            sy -= frameH;
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Play As Sound_Ambient", style))
                            {
                                MainRes.PlaySound(ResourceType.Sound_Ambient);
                            }
                            sy -= frameH;
                            if (GUI.Button(new Rect(0, sy, frameW, frameH - 2), $"Play As Sound_UI", style))
                            {
                                MainRes.PlaySound(ResourceType.Sound_UI);
                            }
                            sy -= frameH;
                        }
                    }
                }
            }
        }
#endif
    }


    public class ResourceAnimateWindow : PreviewWindow
    {
        public ResourceAnimateWindow(IViewResource wrap)
        {
            this.Text = "模型动画";
            var res = wrap.gameObject;
            var panel = new GUIPanel();
            panel.Dock = DockStyle.Fill;
            var sx = 0;
            var sy = 0;
            var tex_copy = Proxy.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_common_19.png");
            var tex_play = Proxy.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_simpleshape_23.png");
            if (res.TryGetComponentsInChildren<Animation>(out var animations))
            {
                foreach (var anim in animations)
                {
                    var head = new GUILabel()
                    {
                        Bounds = new Rect(sx, sy, 180, 20),
                        Text = $"Animation:{anim.name}",
                    };
                    panel.AddChild(head);
                    sy += 20;
                    foreach (AnimationState state in anim)
                    {
                        var btn = new GUIButton()
                        {
                            Bounds = new Rect(sx, sy, 20, 20),
                            Image = tex_play,
                            Tooltip = "复制文本到剪贴板",
                        };
                        btn.Click += new Action<GUIButton>(btn =>
                        {
                            anim.enabled = true;
                            anim.wrapMode = WrapMode.Loop;
                            anim.Play(state.name);
                            GUIUtility.systemCopyBuffer = state.name;
                        });
                        panel.AddChild(btn);
                        var txt = new GUITextField()
                        {
                            Bounds = new Rect(sx + 20, sy, 100, 20),
                            Text = $"{state.name}",
                        };
                        panel.AddChild(txt);
                        sy += 20;
                    }
                }
            }
            if (res.TryGetComponentsInChildren<Animator>(out var animators))
            {
                foreach (var anim in animators)
                {
                    var head = new GUILabel()
                    {
                        Bounds = new Rect(sx, sy, 160, 20),
                        Text = $"Animator:{anim.name}",
                    };
                    panel.AddChild(head);
                    sy += 20;
                    if (anim?.runtimeAnimatorController?.animationClips != null)
                    {
                        foreach (var clip in anim.runtimeAnimatorController.animationClips)
                        {
                            var btn = new GUIButton()
                            {
                                Bounds = new Rect(sx, sy, 20, 20),
                                Image = tex_play,
                                Tooltip = "复制文本到剪贴板",
                            };
                            btn.Click += new Action<GUIButton>(btn =>
                            {
                                anim.enabled = true;
                                anim.applyRootMotion = false;
                                anim.Play(clip.name);
                                GUIUtility.systemCopyBuffer = clip.name;
                            });
                            panel.AddChild(btn);
                            var txt = new GUITextField()
                            {
                                Bounds = new Rect(sx + 20, sy, 100, 20),
                                Text = $"{clip.name}",
                            };
                            panel.AddChild(txt);
                            sy += 20;
                        }
                    }
                }
            }
            var anims = new List<AnimInfo>();
            if (wrap.TryListAnims(anims))
            {
                var title = new GUILabel()
                {
                    Bounds = new Rect(sx, sy, 160, 20),
                    Text = $"ListAnims",
                };
                panel.AddChild(title);
                sy += 20;
                foreach (var anim in anims)
                {
                    var head = new GUITextField()
                    {
                        Bounds = new Rect(sx, sy, 160, 20),
                        Text = $"{anim.Name}",
                    };
                    panel.AddChild(head);
                    var btn = new GUIButton()
                    {
                        Bounds = new Rect(sx + 160, sy, 20, 20),
                        Image = tex_play,
                        Tooltip = "复制文本到剪贴板",
                    };
                    btn.Click += new Action<GUIButton>(btn =>
                    {
                        wrap.PlayAnim(anim.Action);
                        GUIUtility.systemCopyBuffer = anim.Action;
                    });
                    panel.AddChild(btn);
                    sy += 20;
                }
            }
            this.Bounds = new Rect(0, 100, 200, Math.Min(sy + 60, Screen.height - 160));
            this.AddChild(panel);
        }

    }
    public class ResourcePartWindow : PreviewWindow
    {
        public ResourcePartWindow(IViewResource wrap)
        {
            this.Text = "模型部件";
            var res = wrap.gameObject;
            var viewW = 0f;
            var viewH = 0f;
            CalcParts(1, ref viewW, ref viewH, res.transform);
            viewW = viewW + 200;
            this.Bounds = new Rect(0, 0, Math.Min(viewW + 20, 360), Math.Min(viewH + 60, Screen.height - 160));
            var panel = new GUIPanel();
            panel.Dock = DockStyle.Fill;
            var sx = 20f;
            var sy = 0f;
            AddPart(panel, 1, sx, ref sy, res.transform);
            this.AddChild(panel);
        }

        private void CalcParts(int deep, ref float sx, ref float sy, Transform part)
        {
            var sw = 20;
            sy += 20;
            sx = Math.Max(deep * sw, sx);
            for (var i = 0; i < part.childCount; i++)
            {
                var child = part.GetChild(i);
                CalcParts(deep + 1, ref sx, ref sy, child);
            }
        }
        private void AddPart(GUIPanel panel, int deep, float sx, ref float sy, Transform part)
        {
            var tex_copy = Proxy.Textures.MakeAssemblyTexture(this.GetType().Assembly, "icon_common_19.png");
            var btn = new GUIButton()
            {
                Bounds = new Rect(sx - 20, sy, 20, 20),
                Content = new GUIContent() { image = tex_copy, tooltip = "复制文本到剪贴板" },
            };
            btn.Click += new Action<GUIButton>(btn =>
            {
                RTG.TargetObject = part.gameObject;
                GUIUtility.systemCopyBuffer = part.gameObject.name;
            });
            panel.AddChild(btn);
            var text = new GUIToggle()
            {
                Bounds = new Rect(sx, sy, 200, 20),
                Text = part.gameObject.name,
                Checked = part.gameObject.activeSelf,
                //                 Anchor = AnchorStyles.LeftRight,
                //                 AnchorPadding = new Padding(20, 0, 0, 0),
            };
            text.CheckChanged += new Action<GUIToggle, bool>((btn, chk) =>
            {
                part.gameObject.SetActive(chk);
            });
            panel.AddChild(text);
            sy += 20;
            for (var i = 0; i < part.childCount; i++)
            {
                var child = part.GetChild(i);
                AddPart(panel, deep + 1, sx + 20, ref sy, child);
            }
        }
    }
}

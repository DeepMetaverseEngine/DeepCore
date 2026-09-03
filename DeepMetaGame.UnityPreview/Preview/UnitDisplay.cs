using DeepCore;
using DeepCore.Game3D.Host.Instance.Abilities;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity3D;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using UnityEngine;
using static DeepGame3D.Unity.BattleView.UnityZoneUnit;

namespace DeepMetaGame.Unity.Preview.Preview
{
    //---------------------------------------------------------------------------------------------------------------
    public class UnitDisplay : PreviewObject<UnitInfo>, IPreviewUnit
    {
        public UnitInfo UnitData => Data;
        public PreviewObject Preview => this;
        public bool AutoFocus = true;
        protected override void Awake()
        {
            base.Awake();
            RTG.AddEditorObject(gameObject);
        }
        protected override void DoDestory()
        {
            foreach (var wrap in overrideModels)
            {
                wrap.Dispose();
            }
            base.DoDestory();
        }
        protected override void DoInit(UnitInfo unit)
        {
            BodySize = unit.BodySize;
            BodyHeight = unit.BodyHeight;
            this.defineActions = PreviewProxy.Templates?.Templates?.DefaultUnitActionDefinition?.ActionMap;
            if (unit.Abilities.TryGetComponentAs<UnitResourceAbility>(out var u_res))
            {
                var res = LoadRes(u_res.FileName, DeepMetaGame.Data.ResourceType.Object);
                if (res != null)
                {
                    res.transform.localScale = Vector3.one * u_res.BodyScale;
                    res.TryListAnims(actionMap, unit);
                    if (UnityBattleFactory.Resource.TryGetSpine(res.gameObject, out var spine))
                    {
                        spine.initialSkinName = u_res.SkinName;
                        spine.SetAvatar(u_res.SkinAvatar);
                    }
                }
                if (u_res.OverrideActionMap != null)
                {
                    this.defineActions = u_res.OverrideActionMap.ActionMap;
                }
            }
            if (this.AutoFocus)
            {
                RTG.TargetObject = gameObject;
                RTG.LookAt(transform);
            }
            //this.LookAt(transform.position + new UnityEngine.Vector3(0, 0, -100));
        }
        protected override void DoUpdate()
        {
            base.DoUpdate();
            playingState?.Update(base.IntervalMS);
        }
        public PreviewObject DockingParent { get; private set; }
        public DockingOffset DockingOffset { get; private set; }
        public void SetDockingParent(PreviewObject parent, DockingOffset offset)
        {
            DockingParent = parent;
            DockingOffset = offset;
            if (DockingParent != null && DockingOffset != null)
            {
                var pos = DockingParent.Position;
                if (DockingOffset.Radius != 0)
                {
                    DeepCore.Geometry.VectorHelper.MovePolar(ref pos, DockingParent.Direction + DockingOffset.Angle, DockingOffset.Radius);
                }
                pos.Z += DockingOffset.Z;
                Position = pos;
                if (DockingOffset.SolidFaceAngle.HasValue)
                {
                    this.Direction = (DockingParent.Direction + DockingOffset.SolidFaceAngle.Value);
                }
            }
        }

        private DefinedActionStatus playingState;
        internal void PlayAnim(string st)
        {
            for (int i = overrideModels.Count - 1; i >= 0; --i)
            {
                var append = overrideModels[i];
                append.wrap?.PlayAnim(st);
            }
            MainRes?.PlayAnim(st);
        }
        public void PlayDefineState(UnitActionDefinitionMap.UnitAction act)
        {
            playingState?.Dispose();
            playingState = new DefinedActionStatus(this, act);
            playingState.Start();
        }
        //--------------------------------------------------------------------------------------------------------------------------

        private Texture2D tex_track;
        private Texture2D tex_frame;
        private Texture2D tex_frame_hi;
        private Texture2D tex_cursor;
        private Texture2D tex_black;
        private Texture2D tex_gray;
        private List<AnimInfo> actionMap = new List<AnimInfo>();
        private List<UnitActionDefinitionMap.UnitAction> defineActions = new List<UnitActionDefinitionMap.UnitAction>();
        protected override void OnInitGUI(GUICanvas canvas)
        {
            tex_track = Proxy.Textures.MakeTexture(GetType(), "tex_track", 64, 64, Color.gray.SetAlpha(0.3f));
            tex_frame = Proxy.Textures.MakeTexture(GetType(), "tex_frame", 64, 64, Color.red.SetAlpha(0.5f));
            tex_frame_hi = Proxy.Textures.MakeTexture(GetType(), "tex_frame_hi", 64, 64, Color.red.SetAlpha(0.8f));
            tex_cursor = Proxy.Textures.MakeTexture(GetType(), "tex_cursor", 64, 64, Color.white.SetAlpha(0.8f));
            tex_black = Proxy.Textures.MakeTexture(GetType(), "tex_black", 64, 64, Color.black.SetAlpha(0.5f));
            tex_gray = Proxy.Textures.MakeTexture(GetType(), "tex_gray", 64, 64, Color.black.SetAlpha(0.1f));

            {
                var partWindow = new UnitActionMapWindow(this);
                partWindow.Position = new Vector2(
                    0,
                    Screen.height - partWindow.Height - 50);
                //partWindow.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                partWindow.AnchorPadding = new Padding(0, 100, 0, 60);
                RootCanvas.AddChild(partWindow);
            }
            {
                var partWindow = new UnitResAnimWindow(this);
                partWindow.Position = new Vector2(
                    Screen.width - partWindow.Width,
                    Screen.height - partWindow.Height - 50);
                //partWindow.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                partWindow.AnchorPadding = new Padding(0, 100, 0, 60);
                RootCanvas.AddChild(partWindow);
            }
        }
        protected override void OnDrawGUI()
        {
            //             try
            //             {
            //                 {
            //                     using (var gui = new GUIGraphics())
            //                     {
            //                         gui.CurrentStype.fontSize = 9;
            //                         var totalW = 200;
            //                         var totalH = actionMap.Count * 30f;
            //                         using (var gAnim = gui.BeginGroup(new Rect(Screen.width - totalW - 20, Screen.height - totalH - 50, totalW, totalH)))
            //                         {
            //                             int i = 0;
            //                             foreach (var act in actionMap)
            //                             {
            //                                 if (GUI.Button(new Rect(0, i * 30, 200, 28), new GUIContent() { text = $"{act}" }))
            //                                 {
            //                                     MainRes?.PlayAnim(act.Action);
            //                                 }
            //                                 i++;
            //                             }
            //                         }
            //                         totalH = defineActions.Count * 30f;
            //                         using (var gAnim = gui.BeginGroup(new Rect(20, Screen.height - totalH - 50, totalW, totalH)))
            //                         {
            //                             int i = 0;
            //                             foreach (var act in defineActions)
            //                             {
            //                                 if (GUI.Button(new Rect(0, i * 30, 200, 28), new GUIContent() { text = $"{act}" }))
            //                                 {
            //                                     playingState?.Dispose();
            //                                     playingState = new DefinedActionStatus(this, act);
            //                                     playingState.Start();
            //                                 }
            //                                 i++;
            //                             }
            //                         }
            //                         {
            //                             GUIUtils.AutoTooltips();
            //                         }
            //                     }
            //                 }
            // 
            //             }
            //             catch (Exception e)
            //             {
            //                 PLog(e);
            //             }
        }
        //---------------------------------------------------------------------------------------------------------------
        public class UnitResAnimWindow : PreviewWindow
        {
            public UnitResAnimWindow(UnitDisplay res)
            {
                this.Text = "模型动画";
                var panel = new GUIPanel();
                panel.Dock = DockStyle.Fill;
                var sx = 0;
                var sy = 0;
                {
                    foreach (var anim in res.actionMap)
                    {
                        var btn = new GUIButton()
                        {
                            Bounds = new Rect(sx, sy, 180, 20),
                            Text = $"{anim}",
                        };
                        btn.Click += new Action<GUIButton>(btn =>
                        {
                            res.MainRes?.PlayAnim(anim.Action);
                        });
                        panel.AddChild(btn);
                        sy += 20;
                    }
                }
                this.Bounds = new Rect(0, 100, 200, Math.Min(sy + 60, Screen.height - 160));
                this.AddChild(panel);
            }
        }
        public class UnitActionMapWindow : PreviewWindow
        {
            public UnitActionMapWindow(UnitDisplay res)
            {
                this.Text = "定义动画";
                var panel = new GUIPanel();
                panel.Dock = DockStyle.Fill;
                var sx = 0;
                var sy = 0;
                {
                    foreach (var anim in res.defineActions)
                    {
                        var btn = new GUIButton()
                        {
                            Bounds = new Rect(sx, sy, 180, 20),
                            Text = $"{anim}",
                        };
                        btn.Click += new Action<GUIButton>(btn =>
                        {
                            res.PlayDefineState(anim);
                        });
                        panel.AddChild(btn);
                        sy += 20;
                    }
                }
                this.Bounds = new Rect(0, 100, 200, Math.Min(sy + 60, Screen.height - 160));
                this.AddChild(panel);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------
        public class DefinedActionStatus : Disposable
        {
            public UnitDisplay Owner { get; private set; }
            protected UnitActionDefinitionMap.UnitAction mData;
            protected readonly Queue<UnitActionDefinitionMap.UnitActionKeyFrame> mActionQueue = new Queue<UnitActionDefinitionMap.UnitActionKeyFrame>();
            protected UnitActionDefinitionMap.UnitActionKeyFrame mCurrentAction;
            protected double mCurrentOverTime;
            protected double mCurrentPassTime;
            protected AppendModelWrap mCustomModel;
            //--------------------------------------------------------
            public DefinedActionStatus(UnitDisplay owner, UnitActionDefinitionMap.UnitAction data)
            {
                this.Owner = owner;
                this.mData = data;
            }
            public void Start()
            {
                if (!string.IsNullOrEmpty(mData.CustomResource))
                {
                    this.mCustomModel = this.Owner.AppendModel(mData.CustomResource, mData.CustomResourceOverride);
                }
                mActionQueue.Clear();
                foreach (var act in mData.ActionQueue)
                {
                    this.mActionQueue.Enqueue(act);
                }
                mCurrentPassTime = 0;
                NextAction();
            }
            public void Update(float deltaMS)
            {
                this.mCurrentPassTime += (deltaMS);
                if (mActionQueue.Count > 0 && mCurrentAction != null)
                {
                    if (mCurrentPassTime >= mCurrentOverTime)
                    {
                        mCurrentPassTime = 0;
                        NextAction();
                    }
                }
            }
            protected override void Disposing()
            {
                Owner.RemoveModel(mCustomModel);
            }

            protected virtual void NextAction()
            {
                if (mActionQueue.Count > 0)
                {
                    mCurrentAction = mActionQueue.Dequeue();
                    if (mCurrentAction != null)
                    {
                        //                         if (mCurrentAction.TimeMS == 0)
                        //                         {
                        //                             if (Owner.MainRes != null && Owner.MainRes.TryGetAnimatorStateDuriationMS(mCurrentAction.ActionName, out var timeMS))
                        //                             {
                        //                                 this.mCurrentOverTime = timeMS;
                        //                             }
                        //                         }
                        //                         else
                        {
                            this.mCurrentOverTime = mCurrentAction.TimeMS;
                        }
                        Owner.PlayAnim(mCurrentAction.ActionName);
                    }
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------
        public class AppendModelWrap : Disposable
        {
            public UnitDisplay owner { get; private set; }
            public string name { get; private set; }
            public bool overrideBody { get; private set; }
            public IViewResource wrap { get; private set; }
            public AppendModelWrap Init(UnitDisplay unit, string name, bool overrideBody = false)
            {
                this.owner = unit;
                this.name = name;
                this.overrideBody = overrideBody;
                this.wrap = unit.LoadRes(name, DeepMetaGame.Data.ResourceType.Object_Effect);
                return this;
            }
            protected override void Disposing()
            {
                wrap?.Dispose();
                wrap = null;
                name = null;
                owner = null;
            }
        }
        //---------------------------------------------------------------------------------------------------------------

        private List<AppendModelWrap> overrideModels = new();

        /// <summary>
        /// 变身
        /// </summary>
        /// <param name="name"></param>
        /// <param name="overrideBody">是否覆盖掉之前的模型</param>
        /// <returns></returns>
        public AppendModelWrap AppendModel(string name, bool overrideBody = false)
        {
            if (overrideBody)
            {
                HideStack();
            }
            var res = new AppendModelWrap();
            res.Init(this, name, overrideBody);
            overrideModels.Add(res);
            ResetStack();
            return res;
        }
        public bool RemoveModel(AppendModelWrap model)
        {
            if (model != null && overrideModels.Remove(model))
            {
                model.Dispose();
                ResetStack();
                return true;
            }
            return false;
        }
        private void HideStack()
        {
            if (MainRes != null && MainRes.transform)
            {
                MainRes.transform.gameObject.SetActive(false);
                for (int i = overrideModels.Count - 1; i >= 0; --i)
                {
                    var append = overrideModels[i];
                    if (append.wrap != null && append.wrap.transform)
                    {
                        append.wrap.transform.gameObject.SetActive(false);
                    }
                }
            }
        }
        private void ResetStack()
        {
            if (overrideModels.Count > 0)
            {
                for (int i = overrideModels.Count - 1; i >= 0; --i)
                {
                    var append = overrideModels[i];
                    if (append.wrap != null && append.wrap.transform)
                    {
                        append.wrap.transform.gameObject.SetActive(true);
                        if (append.overrideBody)
                        {
                            return;
                        }
                    }
                }
            }
            if (MainRes != null && MainRes.transform)
            {
                MainRes.transform.gameObject.SetActive(true);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------
    }
    //---------------------------------------------------------------------------------------------------------------
    public class UnitAttachmentsDisplay : UnitDisplay
    {
        private HashMap<UnitAttachment, UnitDisplay> attachments;
        private UnitAttachmentAbility ability;
        protected override void Awake()
        {
            base.Awake();
            attachments = new();
        }
        protected override void DoInit(UnitInfo unit)
        {
            base.DoInit(unit);
            if (unit.Abilities.TryGetComponentAs<UnitAttachmentAbility>(out ability))
            {
                if (ability.UnitDockings != null)
                {
                    foreach (var attachment in ability.UnitDockings)
                    {
                        if (Templates.TryGetUnit(attachment.UnitTemplateID, out var attachInfo))
                        {
                            var to = IPC.CreateDisplay<UnitDisplay>(attachInfo.Name);
                            to.AutoFocus = false;
                            to.UserTag = attachment;
                            to.Init(attachInfo);
                            to.SetDockingParent(this, attachment.ToDockingOffset());
                            this.attachments.Add(attachment, to);
                        }
                    }
                }
            }
        }
        protected override void OnInitGUI(GUICanvas canvas)
        {
            {
                var attachWindow = new UnitAttachmentWindow(Data, ability);
                attachWindow.Position = new Vector2(0, 100);
                //attachWindow.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
                attachWindow.AnchorPadding = new Padding(0, 100, 0, 60);
                attachWindow.OnAttachmentClick += new Action<GUIButton, UnitAttachment>((btn, attach) =>
                {
                    if (this.attachments.TryGetValue(attach, out var part))
                    {
                        RTG.TargetObject = part.gameObject;
                    }
                });
                RootCanvas.AddChild(attachWindow);
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

    //---------------------------------------------------------------------------------------------------------------

    public class UnitAttachmentWindow : PreviewWindow
    {
        public UnitAttachmentWindow(UnitInfo unit, UnitAttachmentAbility ability)
        {
            this.Text = "单位挂载";
            var panel = new GUIPanel();
            panel.Dock = DockStyle.Fill;
            var sx = 0f; var sy = 0f;
            if (ability.UnitDockings != null)
            {
                var attachments = ability.UnitDockings;
                panel.AddChild(new GUILabel() { Bounds = new Rect(0, sy, 100, 20), Text = "挂载单位" });
                sy += 20;
                for (int i = 0; i < attachments.Count; i++)
                {
                    var attach = attachments[i];
                    if (PreviewProxy.Templates.Templates.TryGetUnit(attach.UnitTemplateID, out var attachInfo))
                    {
                        var btn = new GUIButton()
                        {
                            Bounds = new Rect(sx, sy, 160, 20),
                            Text = attachInfo.ToString(),
                            Anchor = AnchorStyles.LeftRight,
                            AnchorPadding = new Padding(0, 0, 0, 0),
                        };
                        btn.UserTag = attach;
                        btn.Click += new Action<GUIButton>(btn =>
                        {
                            OnAttachmentClick?.Invoke(btn, attach);
                        });
                        panel.AddChild(btn);
                        sy += 20;
                    }
                }
            }
            this.Bounds = new Rect(0, 0, 200, Math.Min(sy + 40, Screen.height - 160));
            this.AddChild(panel);
        }
        public event Action<GUIButton, UnitAttachment> OnAttachmentClick;
    }

}

using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using DeepCore.Unity3D.UGUIEditor.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeepCore.Unity3D.UGUIEditor
{
    public partial class UIComponent : DisplayNode
    {
        private UILayout mLayout = null;
        private bool mDisable = false;

        protected UILayoutGraphics mCurrentLayout;
        protected UIComponentMeta mOwnerXML;

        public UIComponent(string name = null)
            : base(name)
        {
        }

        //----------------------------------------------------------------------------------------------------------------------

        public UIEditor Editor { get; private set; }
        public string EditName { get; private set; }
        public string EditType { get; private set; }

        public UILayoutGraphics Graphics { get { return mCurrentLayout; } }
        public UIComponentMeta MetaData { get { return mOwnerXML; } }

        public UILayout Layout
        {
            get { return mLayout; }
            set
            {
                mLayout = value;
                if (value != null)
                {
                    if (this.mCurrentLayout == null)
                    {
                        this.mCurrentLayout = GenLayoutGraphics();
                        SetGrayInternal(IsGray, false);
                    }
                }
            }
        }

        private UERoot mUERoot;
        public UERoot RootComponent
        {
            get
            {
                if (this is UERoot)
                {
                    mUERoot = this as UERoot;
                    return mUERoot;
                }
                else if (mUERoot != null)
                {
                    return mUERoot;
                }
                else
                {
                    var parent = Parent;
                    while (parent != null)
                    {
                        if (parent is UERoot)
                        {
                            mUERoot = parent as UERoot;
                            return mUERoot;
                        }
                        parent = parent.Parent;
                    }
                }
                return mUERoot;
            }
        }
        
        public bool Disable
        {
            set { mDisable = value; }
            get { return mDisable; }
        }
        public UILayout LayoutDisable { get; set; }

        //----------------------------------------------------------------------------------------------------------------------
        
        public override DisplayNode Clone()
        {
            if (mOwnerXML != null)
            {
                return Editor.CreateFromMeta(mOwnerXML);
            }
            return null;
        }

        /// <summary>
        /// 触发了OnPointerDown事件
        /// </summary>
        private float mLongPressSec = 1.0f;
        public bool IsLongClick
        {
            get; private set;
        }
        public float mPressDownSec;
        public delegate void LongPressHandle(DisplayNode sender,float pressSec);
        public LongPressHandle event_LongPoniterDown;
        public LongPressHandle event_LongPoniterDownStep;
        public LongPressHandle event_LongPoniterClick;

        public event LongPressHandle LongPoniterDown { add { event_LongPoniterDown += value; } remove { event_LongPoniterDown -= value; } }
        public event LongPressHandle LongPoniterDownStep { add { event_LongPoniterDownStep += value; } remove { event_LongPoniterDownStep -= value; } }
        public event LongPressHandle LongPoniterClick { add { event_LongPoniterClick += value; } remove { event_LongPoniterClick -= value; } }

        protected virtual void OnLongPointerDown(float pressSec) { }
        protected virtual void OnLongPointerClick(float pressSec) { }
        protected virtual void OnLongPointerDownStep(float pressSec) { }

        public float LongPressSecond
        {
            get
            {
                return mLongPressSec;
            }
            set
            {
                mLongPressSec = value;
            }
        }
        private void CheckLongPressDown()
        {
            if (IsPressed)
            {
                mPressDownSec = mPressDownSec + Time.deltaTime;
                if (mPressDownSec >= mLongPressSec)
                {
                    if (!IsLongClick)
                    {
                        IsLongClick = true;
                        OnLongPointerDown(mPressDownSec);
                        if (event_LongPoniterDown != null)
                        {
                            event_LongPoniterDown.Invoke(this,mPressDownSec);
                        }
                    }
                    OnLongPointerDownStep(mPressDownSec);
                    if (event_LongPoniterDownStep != null)
                    {
                        event_LongPoniterDownStep.Invoke(this,mPressDownSec);
                    }
                }
            }
        }
        protected override void OnPointerClick(PointerEventData e)
        {
            base.OnPointerClick(e);
            if (IsLongClick)
            {
                OnLongPointerClick(mPressDownSec);
                if(event_LongPoniterClick != null)
                {
                    event_LongPoniterClick.Invoke(this, mPressDownSec);
                }
            }
        }

        protected override void OnPointerDown(PointerEventData e)
        {
            base.OnPointerDown(e);
            mPressDownSec = 0;
            IsLongClick = false;
        }

        protected override void OnPointerUp(PointerEventData e)
        {
            base.OnPointerUp(e);
        }


        protected override void OnDispose()
        {
            base.OnDispose();
            this.event_LongPoniterDown = null;
            this.event_LongPoniterDownStep = null;
            this.event_LongPoniterClick = null;
        }

        /// <summary>
        /// 根据编辑器名字搜寻子节点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="edit_name"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        public T FindChildByEditName<T>(string edit_name, bool recursive = true) where T : UIComponent
        {
            return FindChildAs<T>((child) =>
            {
                return (edit_name == child.EditName);
            },
            recursive);
        }


        public UIComponent FindChildByEditName(string edit_name, bool recursive = true)
        {
            return FindChildByEditName<UIComponent>(edit_name, recursive);
        }

        //----------------------------------------------------------------------------------------------------------------------

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (mCurrentLayout != null)
            {
                this.OnUpdateLayout();
            }
            CheckLongPressDown();
        }
        protected virtual UILayoutGraphics GenLayoutGraphics()
        {
            var comp = mGameObject.AddComponent<UILayoutGraphics>();
            comp.enabled = false;
            return comp;
        }

        public void ForceUpdateLayout()
        {
            OnUpdateLayout();
        }

        protected virtual void OnUpdateLayout()
        {
            mCurrentLayout.UpdateSprite();
            mCurrentLayout.Alpha = this.RealAlpha;
            if (Enable)
            {
                mCurrentLayout.SetCurrentLayout(Layout);
            }
            else
            {
                if (LayoutDisable != null)
                {
                    mCurrentLayout.SetCurrentLayout(LayoutDisable);
                }
                else
                {
                    mCurrentLayout.SetCurrentLayout(Layout);
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------

        #region _解析XML_

        protected virtual void AddEditorComopnent(UIComponent c)
        {
            this.AddChild(c);
        }

        internal void DecodeFromXML(UIEditor.Decoder editor, UIComponentMeta e)
        {
            this.Editor = editor.editor;
            this.EditType = e.ClassName;
            this.mOwnerXML = e;
            this.DecodeBegin(editor, e);
            this.DecodeFields(editor, e);
            this.DecodeAttributes(editor, e);
            this.DecodeChilds(editor, e);
            this.DecodeEnd(editor, e);
            if (mCurrentLayout != null)
            {
                this.OnUpdateLayout();
            }
        }

        protected virtual void DecodeAttributes(UIEditor.Decoder editor, UIComponentMeta e)
        {
            if (!string.IsNullOrEmpty(e.Attributes))
            {
                foreach (var entry in e.GetAttributsMap())
                {
                    this.SetAttribute(entry.Key, entry.Value);
                    if (entry.Key == "angles")
                    {
                        float x;
                        float y;
                        float z;
                        string[] rt = entry.Value.Split(',');
                        if (rt.Length >= 3)
                        {
                            float.TryParse(rt[0], out x);
                            float.TryParse(rt[1], out y);
                            float.TryParse(rt[2], out z);
                            this.Transform.localEulerAngles = new Vector3(x, y, z);
                        }

                    }
                    else if (entry.Key == "offset")
                    {
                        float x;
                        float y;
                        string[] rt = entry.Value.Split(',');
                        if (rt.Length >= 2)
                        {
                            float.TryParse(rt[0], out x);
                            float.TryParse(rt[1], out y);
                            {
                                var pos = this.Transform.localPosition;
                                this.Transform.localPosition =new Vector3(pos.x + x, pos.y+y,pos.z);
                            }
                          

                        }
                    }
                }
            }
        }

        protected virtual void DecodeChilds(UIEditor.Decoder editor, UIComponentMeta e)
        {
            if (e.Childs != null)
            {
                int len = e.Childs.Count;
                for (int i = 0; i < len; ++i)
                {
                    UIComponentMeta child = e.Childs[i];
                    UIComponent cui = editor.CreateFromMeta(child);
                    if (cui != null)
                    {
                        AddEditorComopnent(cui);
                    }
                }
            }
        }

        protected virtual void DecodeBegin(UIEditor.Decoder editor, UIComponentMeta e)
        {

        }

        protected virtual void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {

        }

        protected virtual void DecodeFields(UIEditor.Decoder editor, UIComponentMeta e)
        {
            this.Bounds2D = new Rect(e.X, e.Y, e.Width, e.Height);
            this.Visible = e.Visible;
            this.EditName = e.EditorName;
            //this.Name = string.Format("{0} - {1}", this.EditName, this.EditType);
            this.Name = this.EditName;
            this.UserData = e.UserData;
            this.UserTag = e.UserTag;

            this.Layout = editor.CreateLayout(e.Layout);
            this.LayoutDisable = editor.CreateLayout(e.DisableLayout);

            this.Enable = e.Enable;
            this.EnableChildren = e.EnableChilds;

            if (e.Z != 0)
            {
                var pos = this.Transform.localPosition;
                pos.z = e.Z;
                this.Transform.localPosition = pos;
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------------------------

    }
}

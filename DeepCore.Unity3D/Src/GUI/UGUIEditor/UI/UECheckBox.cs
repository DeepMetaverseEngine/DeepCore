using DeepCore.GUI.Data;
using DeepCore.Unity3D.UGUI;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UECheckBox : UETextComponent
    {
        protected bool mIsChecked;

        protected UGUI.ImageSprite mImageUnchecked;
        protected UGUI.ImageSprite mImageChecked;

        protected Vector2 mImageTextOffset = new Vector2();
        protected ImageAnchor mImageTextAnchor = ImageAnchor.C_C;

        public UECheckBox(bool use_bitmap) : base(use_bitmap)
        {
            base.Enable = true;
            base.EnableChildren = false;
            base.IsInteractive = true;
            this.PointerClick += EventHandler_PointerClick;
        }
        public UECheckBox() : this(UIEditor.GlobalUseBitmapText)
        {
        }

        private void EventHandler_PointerClick(DisplayNode sender, UnityEngine.EventSystems.PointerEventData e)
        {
            this.mIsChecked = !mIsChecked;
        }

        //----------------------------------------------------------------------------------------------
        public virtual bool IsChecked
        {
            get { return mIsChecked; }
            set { this.mIsChecked = value; }
        }
        //----------------------------------------------------------------------------------------------

        public ImageAnchor ImageTextAnchor
        {
            get { return mImageTextAnchor; }
            set { this.mImageTextAnchor = value; }
        }
        public Vector2 ImageTextOffset
        {
            get { return mImageTextOffset; }
            set { this.mImageTextOffset = value; }
        }

        //----------------------------------------------------------------------------------------------
        protected override void OnStart()
        {
            if (mTextSprite == null)
            {
                if (mUseBitmapFont)
                {
                    mTextSprite = new BitmapTextSprite("bitmap_text");
                }
                else
                {
                    mTextSprite = new TextSprite("bitmap_text");
                }
            }
            base.OnStart();
        }

        protected override void OnUpdateLayout()
        {
            base.OnUpdateLayout();
            if (IsChecked)
            {
                if (mImageChecked != null)
                {
                    mImageChecked.Visible = true;
                    UIUtils.AdjustAnchor(mImageTextAnchor, this, mImageChecked, mImageTextOffset);
                }
                if (mImageUnchecked != null)
                {
                    mImageUnchecked.Visible = false;
                }
            }
            else
            {
                if (mImageUnchecked != null)
                {
                    mImageUnchecked.Visible = true;
                    UIUtils.AdjustAnchor(mImageTextAnchor, this, mImageUnchecked, mImageTextOffset);
                }
                if (mImageChecked != null)
                {
                    mImageChecked.Visible = false;
                }
            }
        }

        protected override void DecodeFields(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeFields(editor, e as UECheckBoxMeta);
            this.Decode_ImageText(editor, e as UECheckBoxMeta);
            this.mIsChecked = (e as UECheckBoxMeta).is_checked;
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.EnableChildren = false;
            this.Enable = true;

            if (UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        private void Decode_ImageText(UIEditor.Decoder editor, UECheckBoxMeta e)
        {
            if (!string.IsNullOrEmpty(e.imageAtlasUnchecked))
            {
                this.mImageUnchecked = editor.editor.ParseImageSpriteFromAtlas(e.imageAtlasUnchecked, Vector2.zero);
                if (mImageUnchecked != null)
                {
                    this.AddChild(mImageUnchecked);
                }
            }
            else if (!string.IsNullOrEmpty(e.imagePathUnchecked))
            {
                this.mImageUnchecked = editor.editor.ParseImageSpriteFromImage(e.imagePathUnchecked, Vector2.zero);
                if (mImageUnchecked != null)
                {
                    this.AddChild(mImageUnchecked);
                }
            }
            if (!string.IsNullOrEmpty(e.imageAtlasChecked))
            {
                this.mImageChecked = editor.editor.ParseImageSpriteFromAtlas(e.imageAtlasChecked, Vector2.zero);
                if (mImageChecked != null)
                {
                    this.AddChild(mImageChecked);
                }
            }
            else if (!string.IsNullOrEmpty(e.imagePathChecked))
            {
                this.mImageChecked = editor.editor.ParseImageSpriteFromImage(e.imagePathChecked, Vector2.zero);
                if (mImageChecked != null)
                {
                    this.AddChild(mImageChecked);
                }
            }
            this.mImageTextAnchor = e.imageAnchor;
            this.mImageTextOffset.x = e.imageOffsetX;
            this.mImageTextOffset.y = e.imageOffsetY;
        }



    }
}

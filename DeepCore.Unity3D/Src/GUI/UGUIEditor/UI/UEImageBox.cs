using DeepCore.GUI.Data;
using DeepCore.Unity3D.Impl;
using UnityEngine;

namespace DeepCore.Unity3D.UGUIEditor.UI
{
    public partial class UEImageBox : UIComponent
    {
        public string UIAssetPath { get; private set; }
        public GameObject AssetObject { get; private set; }
        public UGUI.ImageSprite ImageContent { get => mImageContent; }
        protected UGUI.ImageSprite mImageContent;
 
        protected override void OnDispose()
        {
            if (AssetObject)
            {
                GameObject.DestroyImmediate(AssetObject);
            }
            base.OnDispose();
        }

        public void SetContent(UGUI.ImageSprite spr, float rotate, float scale_x, float scale_y)
        {
            if (mImageContent != spr && mImageContent != null)
            {
                mImageContent.RemoveFromParent();
            }
            var center = new Vector2(0.5f, 0.5f);
            this.mImageContent = spr;
            this.mImageContent.mTransform.anchorMin = center;
            this.mImageContent.mTransform.anchorMax = center;
            this.mImageContent.mTransform.pivot = center;
            this.mImageContent.mTransform.localScale = new Vector2(scale_x / 100f, scale_y / 100f);
            this.mImageContent.mTransform.localRotation = Quaternion.Euler(0f, 0f, -rotate);
            this.AddChild(mImageContent);
        }

        protected override void DecodeEnd(UIEditor.Decoder editor, UIComponentMeta e)
        {
            base.DecodeEnd(editor, e);
            this.Decode_Image(editor, e as UEImageBoxMeta);
            this.EnableChildren = false;
            var tmp = e as UEImageBoxMeta;
            UIAssetPath = tmp.UIAssetPath;
            this.Decode_AssetPath(editor, tmp);

            if(UIEditorMeta.UIEditorRunTime)
            {
                this.Enable = e.Enable;
                this.EnableChildren = e.EnableChilds;
                this.IsInteractive = true;
            }
        }

        protected virtual void Decode_Image(UIEditor.Decoder editor, UEImageBoxMeta e)
        {
            string image_name = e.imagePath;
            string atlas_name = e.imageAtlas;
            
            if (!string.IsNullOrEmpty(atlas_name) && atlas_name.StartsWith("#"))
            {
                var spr = editor.editor.ParseImageSpriteFromAtlas(atlas_name, new Vector2(0.5f, 0.5f));
                if (spr != null)
                {
                    this.SetContent(spr, e.x_rotate, e.x_scaleX, e.x_scaleY);
                }
            }
            else if (!string.IsNullOrEmpty(image_name))
            {
                var spr = editor.editor.ParseImageSpriteFromImage(image_name, new Vector2(0.5f, 0.5f));
                if (spr != null)
                {
                    this.SetContent(spr, e.x_rotate, e.x_scaleX, e.x_scaleY);
                }
            }
        }

        protected virtual void Decode_AssetPath(UIEditor.Decoder editor, UEImageBoxMeta e)
        {
            if (!string.IsNullOrEmpty(UIAssetPath))
            {
                UnityDriver.UnityInstance.CreateAssetObject(UIAssetPath, System.IO.Path.GetFileNameWithoutExtension(UIAssetPath), (o) =>
                {
                    if (o)
                    {
                        AssetObject = o;
                        AssetObject.transform.parent = mGameObject.transform;
                        var rt = AssetObject.GetComponent<RectTransform>();
                        if (rt == null)
                        {
                            throw new System.Exception("UI Asset Not Have RectTransform URL:" + UIAssetPath);
                        }
                        else
                        {
                            rt.transform.parent = this.mTransform.transform;
                            rt.localPosition = Vector3.zero;
                            rt.localScale = Vector3.one;
                            rt.localRotation = Quaternion.identity;
                            rt.anchoredPosition3D = Vector3.zero;
                        }
                    }
                });
            }
        }


    }
}

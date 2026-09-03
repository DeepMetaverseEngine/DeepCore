using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity;
using DeepCore.Unity.ResourceViewer;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepMetaGame.Unity.BattleView
{
    public class UnityZoneItem : UnityZoneObject
    {
        public LayerItem layerItem => layerZoneObject as LayerItem;
        public LayerItem zItem => layerItem;
        public ItemTemplate info => layerItem.Info;
        public UnityZoneItem(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            base.OnInit();
            this.InitResource();
        }
        protected override void OnUpdate(float deltaMS)
        {
            base.OnUpdate(deltaMS);
        }
        protected override void OnDisposing()
        {
            base.OnDisposing();
            this.CleanResource();
        }
        //         protected override GameObject OnCreateGizmos()
        //         {
        //             return BattleGizmos.CreateGizmos(this.info);
        //         }
        //----------------------------------------------------------------------------------------------------------------------------
        #region Resource
        public IItemResourceObject ModelWrap { get; private set; }
        private IAssetLoadingTask assetLoading;
        protected virtual void InitResource()
        {
            //if (!string.IsNullOrEmpty(layerItem.AResource?.FileName))
            {
                this.Retain();
                layerItem.Retain();
                this.assetLoading = UnityBattleFactory.Resource.LoadItemResource(this, static (item, res, err) =>
                {
                    try
                    {
                        if (res != null)
                        {
                            if (item.IsDisposing || item.layerItem == null || item.layerItem.IsDisposing)
                            {
                                res.Dispose();
                                return;
                            }
                            item.ModelWrap = res;
                            //                             var ares = item.layerItem.AResource;
                            //                             {
                            // //                                 var scale = item.layerItem.AResource.BodyScale;
                            // //                                 var bodyH = item.layerObject.BodyHeight;
                            // //                                 var va = ares.BodyVoxelAnchor;
                            // //                                 var space3D = item.Space;
                            // //                                 var v = space3D.BattleToUnityVoxelAnchorOffset(bodyH, va);
                            // //                                 if (res.transform != null)
                            // //                                 {
                            // //                                     res.beginLocalPosition = v;
                            // //                                     res.beginLocalScale = new Vector3(scale, scale, scale);
                            // //                                 }
                            // //                                 else
                            // //                                 {
                            // //                                     Debug.LogWarning($"Item Res …Ë÷√ ß∞‹ transform  = null : {item.layerItem.Info} : {item.layerItem.AResource.FileName}");
                            // //                                 }
                            //                             }
                        }
                        else if (err != null)
                        {
                            Debug.LogError($"Init Item Resource Error : {item.layerItem.Info} : {item.layerItem.AResource.FileName} : {err.Message}");
                        }
                    }
                    finally
                    {
                        item.layerItem.Release();
                        item.Release();
                    }
                });
            }
            if (layerItem.AResource?.BindingEffect != null)
            {
                var effect = layerItem.AResource.BindingEffect;
                parent.BindObjectEffect(this, effect);
            }
        }
        protected override void OnUpdateResource()
        {
            ModelWrap?.UpdateResource(this);
        }
        protected virtual void CleanResource()
        {
            this.assetLoading?.Dispose();
            this.assetLoading = null;
            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------

    }
}

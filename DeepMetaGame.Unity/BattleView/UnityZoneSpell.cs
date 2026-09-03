using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity.ResourceViewer;
using DeepCore.XCSV;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data.Template;
using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UIElements;
using static DeepMetaGame.Data.Template.SpellTemplate;

namespace DeepMetaGame.Unity.BattleView
{
    public class UnityZoneSpell : UnityZoneObject
    {
        public LayerSpell zSpell => layerSpell;
        public LayerSpell layerSpell => layerZoneObject as LayerSpell;
        public SpellTemplate info => layerSpell.Info;
        public UnityZoneSpell(UnityZone zone) : base(zone) { }
        protected override void OnInit()
        {
            base.OnInit();
            InitResource();
            if (!string.IsNullOrEmpty(layerSpell?.Info?.FileNameSpawn))
            {
                parent.PlayEffect(
                layerSpell.Info.FileNameSpawn,
                this.layerObject.Position,
                this.layerObject.Direction,
                layerSpell.Info.FileBodyScale, 0);
            }
            if (layerSpell?.Info?.SpawnEffect != null)
            {
                parent.PlayEffect(
                    layerSpell.Info.SpawnEffect,
                    layerSpell.Position,
                    layerSpell.Direction);
            }
            this.gameObject.name = layerSpell.Info.ToString();
        }
        protected override void OnUpdate(float deltaMS)
        {
            base.OnUpdate(deltaMS);
        }
        protected override void OnDisposing()
        {
            if (!string.IsNullOrEmpty(layerSpell?.Info?.FileNameDestory))
            {
                parent.PlayEffect(
                layerSpell.Info.FileNameDestory,
                this.layerObject.Position,
                this.layerObject.Direction,
                layerSpell.Info.FileBodyScale, 0);
            }
            if (layerSpell?.Info?.DestoryEffect != null)
            {
                parent.PlayEffect(
                    layerSpell.Info.DestoryEffect,
                    layerSpell.Position,
                    layerSpell.Direction);
            }
            CleanResource();
            base.OnDisposing();
        }

        private Vector3? oldPos;
        protected override void UpdatePosition()
        {
            base.UpdatePosition();
            if (oldPos.HasValue)
            {
                if (layerSpell.Info.IsProjectile)
                {
                    if (layerSpell.Info.ResFaceToMotion)
                    {
                        Space.LookAt(this.transform, this.transform.position + (this.transform.position - oldPos.Value));
                    }
                }
                //                 else
                //                 {
                //                     switch (layerSpell.Info.MType)
                //                     {
                //                         case MotionType.Forward:
                //                         case MotionType.Missile:
                //                         case MotionType.SeekerMissile:
                //                             Space.LookAt(this.transform, this.transform.position + (this.transform.position - oldPos.Value));
                //                             break;
                //                         default:
                //                             break;
                //                     }
                //                 }
            }
            oldPos = this.transform.position;
        }

        //         protected override GameObject OnCreateGizmos()
        //         {
        //             return BattleGizmos.CreateGizmos(this.info,
        //                 parent.GetObject(layerSpell.Launcher?.ObjectID)?.transform,
        //                 parent.GetObject(layerSpell.Sender?.ObjectID)?.transform,
        //                 parent.GetObject(layerSpell.Target?.ObjectID)?.transform);
        //         }
        //         protected override void OnUpdateGizmos(GameObject childGizmos)
        //         {
        //             BattleGizmos.UpdateGizmos(childGizmos, info, layerSpell.Distance, layerSpell.BodySize);
        //             base.OnUpdateGizmos(childGizmos);
        //         }


        //----------------------------------------------------------------------------------------------------------------------------
        #region Resource
        public ISpellResourceObject ModelWrap { get; private set; }
        private IAssetLoadingTask assetLoading;
        protected virtual void InitResource()
        {
            //if (!string.IsNullOrEmpty(layerSpell.Info.FileName))
            {
                this.Retain();
                layerSpell.Retain();
                this.assetLoading = UnityBattleFactory.Resource.LoadSpellResource(this, static (spell, res, err) =>
                {
                    try
                    {
                        if (res != null)
                        {
                            if (spell.IsDisposing || spell.layerSpell == null || spell.layerSpell.IsDisposing)
                            {
                                res.Dispose();
                                return;
                            }
                            spell.ModelWrap = res;
                            //                             {
                            // //                                 var scale = spell.layerSpell.ResourceScale;
                            // //                                 if (spell.info.FileBodyScale != 1f && spell.info.FileBodyScale != 0)
                            // //                                 {
                            // //                                     scale *= spell.info.FileBodyScale;
                            // //                                 }
                            // //                                 res.beginLocalScale = new Vector3(scale, scale, scale);
                            //                             }
                        }
                        else if (err != null)
                        {
                            Debug.LogError($"Init Spell Resource Error : {spell.layerSpell.Info} : {spell.layerSpell.Info.FileName} : {err.Message}");
                        }
                    }
                    finally
                    {
                        spell.layerSpell.Release();
                        spell.Release();
                    }
                });
            }
            if (layerSpell.Info.BindingEffect != null)
            {
                parent.BindObjectEffect(this, layerSpell.Info.BindingEffect);
            }
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
        protected override void OnUpdateResource()
        {
            if (ModelWrap != null)
            {
                var res = ModelWrap;
                //                 if (info.FitOwnerScale)
                //                 {
                //                     res.transform.localScale = new UnityEngine.Vector3(ResourceFitSize, BodyHeight, ResourceFitSize);
                //                 }         
                res.UpdateResource(this);
            }
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------

    }
}

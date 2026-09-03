using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using System.Collections.Generic;
using UnityEngine;

namespace DeepGame3D.Unity.BattleView
{
    public partial class UnityZoneUnit : UnityZoneObject
    {
        public LayerUnit zUnit => layerUnit;
        public LayerUnit layerUnit => layerZoneObject as LayerUnit;
        public UnitInfo info => layerUnit.Info;
        //private GizmosCylinder gizmosCylinder;
        private CapsuleCollider capsuleCollider;
        public UnityZoneUnit(UnityZone zone) : base(zone)
        {
            this.capsuleCollider = UnityBattleFactory.Instance.CreateUnitCollider(this);
            //if (Application.isEditor) { this.gizmosCylinder = gameObject.AddComponent<GizmosCylinder>(); }
        }
        protected override void OnInit()
        {
            base.OnInit();
            this.InitActionStatus();
            this.InitResource();
            this.InitBuffs();
        }
        sealed protected override void OnDisposing()
        {
            this.CleanActionStatus();
            this.CleanResource();
            this.CleanBuffs();
            base.OnDisposing();
        }
        protected override void OnUpdate(float deltaMS)
        {
            base.OnUpdate(deltaMS);
            this.UpdateActionStatus(deltaMS);
            this.gameObject.SetActive(layerUnit.IsVisible);
        }
        //         protected override GameObject OnCreateGizmos()
        //         {
        //             return BattleGizmos.CreateGizmos(this.info);
        //         }
        //         protected override void OnUpdateGizmos(GameObject gizmos)
        //         {
        //             BattleGizmos.UpdateGizmos(gizmos, info, 1f);
        //             base.OnUpdateGizmos(gizmos);
        //         }
        //----------------------------------------------------------------------------------------------------------------------------
        #region ResBody
        public UnitResourceBodyAbility AResBody { get; private set; }
        public bool IsPartHeadYaw { get; private set; } = false;
        public bool IsPartHeadPitch { get; private set; } = false;
        public float ResourceScale { get => 1 * layerUnit.BodyScale * layerUnit.ResScale; }
        //public float BaseSpeed { get; private set; }
        private Transform transformHeadYaw;
        private Transform transformHeadPitch;
        protected virtual void InitResBody(IUnitResourceObject res)
        {
            if (AResBody && res != null)
            {
                if (!string.IsNullOrEmpty(AResBody.PartHeadYaw))
                {
                    IsPartHeadYaw = true;
                    transformHeadYaw = res.FindDeep(AResBody.PartHeadYaw);
                }
                if (!string.IsNullOrEmpty(AResBody.PartHeadPitch))
                {
                    IsPartHeadPitch = true;
                    transformHeadPitch = res.FindDeep(AResBody.PartHeadPitch);
                }
            }
        }
        protected override void UpdatePosition()
        {
            if (IsPartHeadYaw)
            {
                // 坦克 移动 //
                this.transform.position = Space.ToUnityWorldPosition(this);
                this.transform.rotation = Space.ToUnityRotation(this, layerUnit.BodyDirection);
                if (transformHeadYaw)
                {
                    this.transformHeadYaw.rotation = Space.ToUnityRotation(this);
                }
                if (transformHeadPitch)
                {

                }
            }
            else
            {
                base.UpdatePosition();
            }
            this.transform.ScaleTo(Vector3.one * ResourceScale, zone.config.ScaleToDiv);
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Resource


        //----------------------------------------------------------------------------------------------------------------------------
        public IUnitResourceObject ModelWrap { get; private set; }
        private IAssetLoadingTask assetLoading;

        protected virtual void InitResource()
        {
            this.AResBody = layerUnit.Info.Abilities.GetComponentAs<UnitResourceBodyAbility>();
            this.Retain();
            this.layerUnit.Retain();
            this.assetLoading = UnityBattleFactory.Resource.LoadUnitResource(this, layerUnit.AResource?.FileName, static (unit, res, err) =>
            {
                try
                {
                    if (res != null)
                    {
                        if (unit.IsDisposing || unit.layerUnit == null || unit.layerUnit.IsDisposing)
                        {
                            res.Dispose();
                            return;
                        }
                        unit.ModelWrap = res;
                        unit.InitResBody(res);
                        unit.CurrentActionStatus?.PlayAnim();
                    }
                    else if (err != null)
                    {
                        Debug.LogError($"Init Unit Resource Error : {unit.layerUnit.Info} : {unit.layerUnit.AResource.FileName} : {err.Message}");
                    }
                }
                finally
                {
                    unit.layerUnit.Release();
                    unit.Release();
                }
            });
            if (capsuleCollider != null)
            {
                capsuleCollider.direction = 1;
                capsuleCollider.height = layerUnit.BodyHeight;
                capsuleCollider.radius = layerUnit.BodyBlockSize;
                capsuleCollider.center = new Vector3(0, layerUnit.BodyHeight * 0.5f, 0);
            }
            if (layerUnit.AResource != null)
            {
                if (layerUnit.AResource.BodyEffect != null)
                {
                    parent.BindObjectEffect(this, layerUnit.AResource.BodyEffect);
                }
            }
        }
        protected override void OnUpdateResource()
        {
            for (int i = overrideModels.Count - 1; i >= 0; --i)
            {
                var append = overrideModels[i];
                if (append.wrap != null)
                {
                    append.wrap.UpdateResource(this);
                    if (append.wrap.gameObject.activeSelf != layerUnit.IsVisible)
                    {
                        append.wrap.gameObject.SetActive(layerUnit.IsVisible);
                    }
                }
            }
            if (ModelWrap != null)
            {
                ModelWrap.UpdateResource(this);
                if (ModelWrap.gameObject.activeSelf != layerUnit.IsVisible)
                {
                    ModelWrap.gameObject.SetActive(layerUnit.IsVisible);
                }
            }
        }
        protected virtual void CleanResource()
        {
            this.assetLoading?.Dispose();
            this.assetLoading = null;
            ModelWrap?.Dispose();
            ModelWrap = null;
            foreach (var wrap in overrideModels)
            {
                wrap.Dispose();
            }
        }
        internal void PlayAnim(UnityActionStatus st)
        {
            for (int i = overrideModels.Count - 1; i >= 0; --i)
            {
                var append = overrideModels[i];
                append.wrap?.PlayAnim(st);
            }
            ModelWrap?.PlayAnim(st);
        }
        internal void StopAnim(UnityActionStatus st)
        {
            for (int i = overrideModels.Count - 1; i >= 0; --i)
            {
                var append = overrideModels[i];
                append.wrap?.StopAnim();
            }
            ModelWrap?.StopAnim();
        }
        //----------------------------------------------------------------------------------------------------------------------------
        public class AppendModelWrap : Recyclable
        {
            public UnityZoneUnit owner { get; private set; }
            public string name { get; private set; }
            public bool overrideBody { get; private set; }
            public IUnitResourceObject wrap { get; private set; }

            private IAssetLoadingTask assetLoading;
            public AppendModelWrap Init(UnityZoneUnit unit, string name, bool overrideBody = false)
            {
                //UnityEngine.Debug.LogWarning($"Init AppendModelWrap : {name}");
                this.owner = unit;
                this.name = name;
                this.overrideBody = overrideBody;
                //this.Retain();
                //unit.Retain();
                //unit.layerUnit.Retain();
                unit.overrideModels.Add(this);
                this.assetLoading = UnityBattleFactory.Resource.LoadUnitResource(unit, name, (unit, res, err) =>
                {
                    try
                    {
                        if (res != null)
                        {
                            if (this.IsDisposing || unit.IsDisposing || unit.layerUnit.IsDisposing)
                            {
                                res.Dispose();
                                return;
                            }
                            wrap = res;
                        }
                        if (err != null)
                        {
                            Debug.LogError($"Init Unit Resource Error : {unit.layerUnit.Info} : {unit.layerUnit.AResource.FileName} : {err.Message}");
                        }
                    }
                    finally
                    {
                        owner.ResetStack(this);
                        //unit.layerUnit.Release();
                        //unit.Release();
                        //this.Release();
                    }
                });
                return this;
            }
            protected override void Disposing()
            {
                this.assetLoading?.Dispose();
                this.assetLoading = null;
                //UnityEngine.Debug.LogWarning($"Disposing AppendModelWrap : {name}");
                wrap?.Dispose();
                this.wrap = null;
                this.name = null;
                this.owner = null;
                this.overrideBody = false;
            }
            protected override void Destructing()
            {

            }
        }
        private List<AppendModelWrap> overrideModels = new();

        /// <summary>
        /// 变身
        /// </summary>
        /// <param name="name"></param>
        /// <param name="overrideBody">是否覆盖掉之前的模型</param>
        /// <returns></returns>
        public AppendModelWrap AppendModel(string name, bool overrideBody = false)
        {
            var res = objectPool.Alloc<AppendModelWrap>().Init(this, name, overrideBody);
            return res;
        }
        public bool RemoveModel(AppendModelWrap model)
        {
            if (model != null && overrideModels.Remove(model))
            {
                model.Dispose();
                ResetStack(null);
                return true;
            }
            return false;
        }
        //         private void HideStack()
        //         {
        //             if (ModelWrap != null && ModelWrap.transform)
        //             {
        //                 ModelWrap.transform.gameObject.SetActive(false);
        //                 for (int i = overrideModels.Count - 1; i >= 0; --i)
        //                 {
        //                     var append = overrideModels[i];
        //                     if (append.wrap != null && append.wrap.transform)
        //                     {
        //                         append.wrap.transform.gameObject.SetActive(false);
        //                     }
        //                 }
        //             }
        //         }
        private void ResetStack(AppendModelWrap current)
        {
            bool overrideBody = false;
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
                            overrideBody = true;
                        }
                    }
                }
            }
            if (ModelWrap != null && ModelWrap.transform)
            {
                ModelWrap.transform.gameObject.SetActive(!overrideBody);
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Events
        protected override void InitObjectEvents()
        {
            base.InitObjectEvents();
            this.layerUnit.OnActionChanged += ZUnit_OnActionChanged;
            this.layerUnit.OnSkillActionStart += ZUnit_OnSkillActionStart;
            this.layerUnit.OnSpeedChanged += ZUnit_OnSpeedChanged;
            this.layerUnit.OnBuffAdded += LayerUnit_OnBuffAdded;
            this.layerUnit.OnBuffRemoved += LayerUnit_OnBuffRemoved;
            this.layerUnit.OnHit += LayerUnit_OnHit;
            this.layerUnit.OnCustomKeyFrame += LayerUnit_OnCustomKeyFrame;
            this.layerUnit.OnMessageReceived += LayerUnit_OnMessageReceived;
        }

        protected override void CleanObjectEvents()
        {
            base.CleanObjectEvents();
            this.layerUnit.OnActionChanged -= ZUnit_OnActionChanged;
            this.layerUnit.OnSkillActionStart -= ZUnit_OnSkillActionStart;
            this.layerUnit.OnSpeedChanged -= ZUnit_OnSpeedChanged;
            this.layerUnit.OnBuffAdded -= LayerUnit_OnBuffAdded;
            this.layerUnit.OnBuffRemoved -= LayerUnit_OnBuffRemoved;
            this.layerUnit.OnHit -= LayerUnit_OnHit;
            this.layerUnit.OnCustomKeyFrame -= LayerUnit_OnCustomKeyFrame;
            this.layerUnit.OnMessageReceived -= LayerUnit_OnMessageReceived;
        }
        protected virtual void ZUnit_OnActionChanged(LayerUnit unit, DeepMetaGame.Data.Misc.UnitActionStatus status, string subst, IRecyclable msg)
        {
            if (status != DeepMetaGame.Data.Misc.UnitActionStatus.Skill)
            {
                this.ChangeAction(status, subst, null, msg);
            }
        }
        protected virtual void ZUnit_OnSkillActionStart(LayerUnit unit, LayerUnit.ISkillAction action)
        {
            this.ChangeAction(DeepMetaGame.Data.Misc.UnitActionStatus.Skill, null, null, action);
        }
        protected virtual void ZUnit_OnSpeedChanged(LayerUnit unit)
        {
            this.ChangeActionSpeed();
        }
        protected virtual void LayerUnit_OnBuffAdded(LayerUnit unit, LayerUnit.BuffState buff)
        {
            DoAddBuff(buff);
        }
        protected virtual void LayerUnit_OnBuffRemoved(LayerUnit unit, LayerUnit.BuffState buff)
        {
            DoRemoveBuff(buff);
        }
        protected virtual void LayerUnit_OnHit(LayerUnit unit, UnitHitArgs damage)
        {
            parent.PlayObjectHitEvent(this, damage);
        }
        protected virtual void LayerUnit_OnCustomKeyFrame(IKeyFrameProperties soundName)
        {
            UnityBattleFactory.Resource.PlayKeyFrame(this.transform, soundName, this);
            //             if (soundName  is customkey)
            //             {
            // 
            //             }
            //             if (soundName.CustomActionType == KeyFrameCustomAction.ActionType.PlaySound)
            //             {
            //                 UnityBattleFactory.Instance.PlaySound(this.transform, soundName.StringParameter);
            //             }
        }

        private void LayerUnit_OnMessageReceived(LayerZoneObject obj, ObjectNotify e)
        {
            if (e is UnitDoActionEvent doAction)
            {
                this.ChangeAction(doAction.Main, doAction.Sub, doAction.ActionName, doAction);
                //AddObjectLog("UnitDoAction: " + obj.Name + " - " + (msg as UnitDoActionEvent).ActionName, obj);
            }

        }


        //         protected virtual void LayerUnit_OnHPChanged(LayerUnit unit, int oldHP, int newHP)
        //         {
        // //             var reduce = newHP - oldHP;
        // //             var color = reduce > 0 ? Color.green : Color.red;
        // //             var text = reduce > 0 ? $"{reduce}" : $"{-reduce}";
        // //             parent.PlayObjectText(this, text, color);
        //         }


        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
    }
}

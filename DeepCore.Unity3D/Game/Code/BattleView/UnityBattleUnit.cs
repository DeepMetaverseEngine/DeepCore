using System;
using System.IO;
using Code.BattleView.MaterialActions;
using Code.BattleView.UnitActionStatuses;
using Code.HUD;
using Code.System.AB;
using Code.System.Pool;
using Code.System.Resource;
using Code.Utility;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Data;
using DeepCore.Unity3D.Impl;
using UnityEngine;
using UnityEngine.Networking;

namespace Code.BattleView
{
    public partial class UnityBattleUnit : UnityBattleObject
    {
        public LayerUnit ZoneUnit => ZoneObject as LayerUnit;
        public WrapGO ModelWrap { get; private set; }
        public Animator Anim;

        protected override void OnInit()
        {
            LoadRes();
            InitActionStatus();

            this.ZoneUnit.OnActionChanged += ZUnit_OnActionChanged;
            this.ZoneUnit.OnSkillActionStart += ZUnit_OnSkillActionStart;

            if (GizmosCylinder && ZoneUnit.Force == 2)
                GizmosCylinder.Color = Color.red;
        }

        private void LoadRes()
        {
            string link = string.Empty;
            string url = ABSystemImpl.Inst.GetResUrl(ZoneUnit.Info.FileName);

            if (!string.IsNullOrEmpty(url))
            {
                var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(url);
                ModelWrap = ResourceSystem.GetWrapGO(url, name, null, Transform);

                if (ModelWrap != null && ModelWrap.GameObject)
                {
                    var force = ModelWrap.GameObject.GetComponentInChildren<ForceUtil>();
                    if (force)
                    {
                        force.Force = ZoneUnit.Force;
                    }
                    
                    Anim = ModelWrap.GameObject.GetComponent<Animator>();
                    if (Math.Abs(ZoneUnit.Info.BodyScale - 1f) > 0.00001)
                    {
                        ModelWrap.Transform.localScale = Vector3.one * ZoneUnit.Info.BodyScale;
                    }
                }
            }

            if (!string.IsNullOrEmpty(link))
            {
                SetLinkTexture(link);
            }
        }

        private async void SetLinkTexture(string link)
        {
            try
            {
                if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    ModelWrap.GameObject.SetActive(false);
                    DownloadHandlerTexture handler = new DownloadHandlerTexture(true);
                    var request = new UnityWebRequest(link);
                    request.downloadHandler = handler;
                    var op = request.SendWebRequest();
                    await op.Async();
                    if (string.IsNullOrEmpty(handler.error))
                    {
                        var renderer = ModelWrap.Transform.FindDeep("Icon").GetComponent<MeshRenderer>();
                        //只能在编辑器模式下用
                        // handler.texture.alphaIsTransparency = true;
                        renderer.material.SetTexture("_MainTex", handler.texture);
                    }
                    else
                    {
                        Debug.LogError("UnityWebRequest " + link);
                    }
                    ModelWrap.GameObject.SetActive(true);
                    if (ZoneUnit.Info.SpawnTimeMS > 0)
                    {
                        var action = System.Pool.ObjectPool<TeleportAction>.Get();
                        action.Init(GameObject, 1f, 0f, ZoneUnit.Info.SpawnTimeMS);
                        DoMaterialAction(action);
                    }
                }
                else
                {
                    var iamge = UnityDriver.UnityInstance.CreateUnityImage(link);
                    if (iamge != null)
                    {
                        var renderer = ModelWrap.Transform.FindDeep("Icon").GetComponent<MeshRenderer>();
                        renderer.material.SetTexture("_MainTex", iamge.Texture2D);
                    }
                    else
                    {
                        Debug.LogError("CreateUnityImage " + link);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void SetLinkTexture(Texture texture)
        {
            var deep = ModelWrap.Transform.FindDeep("Icon");
            Debug.Log($"--------------------- set {deep.name}");
            var renderer = deep.GetComponent<MeshRenderer>();
            renderer.material.SetTexture("_MainTex", texture);
        }

        protected virtual void ZUnit_OnActionChanged(LayerUnit unit, UnitActionStatus status, object msg)
        {
            this.ChangeAction(status);
        }

        protected virtual void ZUnit_OnSkillActionStart(LayerUnit unit, LayerUnit.ISkillAction action)
        {
            if (this.CurrentActionStatus is SkillActionStatus status)
            {
                status.ZUnit_OnSkillActionStart(this, action);
            }
        }

        protected override void OnUpdate(int deltaMS)
        {
#if UNITY_EDITOR
            Vector3 pos = Camera.main.WorldToScreenPoint(GameObject.transform.position + Vector3.up * ZoneUnit.BodyHeight) + Vector3.up * 15;
            pos.z = 0f;
#endif
        }

        protected override void OnClear()
        {
            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }
            this.ZoneUnit.OnActionChanged -= ZUnit_OnActionChanged;
            this.ZoneUnit.OnSkillActionStart -= ZUnit_OnSkillActionStart;
        }

        protected override void Disposing()
        {
            ObjectPool<UnityBattleUnit>.Release(this);
        }
    }
}

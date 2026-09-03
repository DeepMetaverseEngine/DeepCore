using DeepCore.Unity3D.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepCore.Unity3D.UGUI
{
    [DisallowMultipleComponent, ExecuteInEditMode, RequireComponent(typeof(CanvasRenderer)), RequireComponent(typeof(RectTransform))]
    public partial class GrayMaterialModifier : UIBehaviour, IMaterialModifier
    {
        protected Material base_mat;
        protected Material gray_mat;


        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (base_mat != baseMaterial)
            {
                base_mat = baseMaterial;
                if (gray_mat != null)
                {
                    DeepCore.Unity3D.UnityHelper.Destroy(gray_mat);
                    gray_mat = null;
                }
            }
            if (gray_mat == null)
            {
                gray_mat = new Material(baseMaterial);
                gray_mat.CopyPropertiesFromMaterial(baseMaterial);
                gray_mat.SetFloat("_Gray", 1);
                gray_mat.SetPass(0);
            }
            return gray_mat;
        }

        protected override void OnDestroy()
        {
            if (gray_mat != null)
            {
                DeepCore.Unity3D.UnityHelper.Destroy(gray_mat);
            }
            base.OnDestroy();
        }
    }

}

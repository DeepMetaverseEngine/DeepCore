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
    public partial class TextGrayMaterialModifier : GrayMaterialModifier
    {
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            if (gray_mat == null)
            {
                gray_mat = new Material(UnityShaders.MFUGUI_TextGray_Shader);
                gray_mat.CopyPropertiesFromMaterial(baseMaterial);
                gray_mat.SetFloat("_Gray", 1);
                gray_mat.SetPass(0);
            }
            return gray_mat;
        }
        
    }
}

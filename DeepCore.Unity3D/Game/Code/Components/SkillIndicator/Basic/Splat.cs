using System;
using IOGame.Core.Battle.Data;
using UnityEngine;
using Utils;

namespace SkillIndicator.Basic
{
    [Serializable]
    public enum ScalingType
    {
        None,
        LengthAndHeight,
        LengthOnly,
    }
    
    public abstract class Splat : MonoBehaviour
    {
        [SerializeField, InspectorName("Projectors")] 
        public Projector[] mProjector;
        
        [SerializeField, InspectorName("Progress"), Range(0, 1)] 
        protected float mProgress = 0;
        
        [SerializeField, InspectorName("Scale")] 
        protected float mScale = 7;
        
        [SerializeField, InspectorName("Width")] 
        protected float mWidth;

        public abstract ScalingType ScalingType { get; }
        public abstract IOSkillPreWarning.PreWarningType IndicatorType { get; }

        public float Progress
        {
            get => mProgress;
            set
            {
                mProgress = value;
                OnValueChanged();
            }
        }

        public float Scale
        {
            get => mScale;
            set
            {
                mScale = value;
                OnValueChanged();
            }
        }

        public float Width
        {
            get => mWidth;
            set
            {
                mWidth = value;
                OnValueChanged();
            }
        }

        public virtual void OnValueChanged()
        {
            Util.Resize(mProjector, ScalingType, mScale, mWidth);
            UpdateProgress(Progress);
        }

        protected void UpdateProgress(float progress)
        {
            ModifyShader("_Fill", progress);
        }

        protected void ModifyShader(string shaderProp, float progress)
        {
            foreach (var projector in mProjector)
                if (projector.material.HasProperty(shaderProp))
                    projector.material.SetFloat(shaderProp, progress);

        }


        public virtual void Init()
        {
            foreach (var projector in mProjector)
            {
                projector.material = new Material(projector.material);
            }
            transform.localPosition = Vector3.zero;
            name = IndicatorType.ToString();
        }

        public virtual void OnUpdate(Vector3 input, float range) {}
        
        public virtual void OnShow() {}
        
        public virtual void OnHide() {}
        
        
    }
}
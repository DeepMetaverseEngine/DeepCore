using System;
using Code;
using Code.Managers;
using SkillIndicator.Basic;
using UnityEngine;

namespace SkillIndicator.Basic
{
    public abstract class SpellIndicator : Splat
    {
        /// <summary>
        /// 范围指示器
        /// </summary>
        public RangeIndicator RangeIndicator;

        [SerializeField] protected float mRange = 5f;

        public float Range
        {
            get => mRange;
            set
            {
                mRange = value;
                OnChangeRange();
            }
        }

        public override void OnShow()
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            if (RangeIndicator) 
                RangeIndicator.Scale = mRange * 2.1f;
        }

        private void OnChangeRange()
        {
            UpdateSize();
        }

        public void Active()
        {
            gameObject.SetActive(true);
            OnShow();
            if (!RangeIndicator)
                return;
            
            RangeIndicator.gameObject.SetActive(true);
            RangeIndicator.OnShow();
        }

        public void Inactive()
        {
            gameObject.SetActive(false);
            OnHide();
            if (RangeIndicator)
                return;
            
            RangeIndicator.gameObject.SetActive(false);
            RangeIndicator.OnHide();
        }
    }
}
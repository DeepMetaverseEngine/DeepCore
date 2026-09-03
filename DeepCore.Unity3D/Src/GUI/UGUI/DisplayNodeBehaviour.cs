using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity3D.UGUI
{
    public partial class DisplayNodeBehaviour : MonoBehaviour
    {
        public bool IsEnable = false;
        public bool IsEnableChildren = false;
        public bool IsInteractive = false;
        public float Alpha = 1f;
        public bool IsGray = false;
        internal DisplayNode mBinding;
        public DisplayNode Binding { get { return mBinding; } }

        protected virtual void OnDestroy()
        {
            mBinding = null;
        }
    }
}

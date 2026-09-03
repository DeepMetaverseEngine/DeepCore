using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity.Camera
{


    /// 
    /// 将此脚本附加到任意镜头上，可以使其拥有WOW镜头的控制方式
    /// 
    public class FreeCamera2D : BaseCamera
    {
        /// 
        /// 鼠标滚轮拉近拉远速度系数
        /// 
        public float ScrollFactor = 10f;
        public UnityEngine.Camera Camera { get; private set; }
        public virtual void Start()
        {
            Camera = gameObject.GetComponent<UnityEngine.Camera>();
        }
        public virtual void OnEnable()
        {
        }
        public virtual void Update()
        {
            var factor2 = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;
            //滚轮向前：拉近距离；滚轮向后：拉远距离
            var scrollAmount = Input.GetAxis("Mouse ScrollWheel");
            var src = Camera.transform.position;
            src.z += scrollAmount * ScrollFactor * factor2;
            Camera.transform.position = src;
        }
        public override void ResetFromTransform()
        {

        }
    }

}

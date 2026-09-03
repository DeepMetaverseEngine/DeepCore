using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity.Camera
{


    /// 
    /// 将此脚本附加到任意镜头上，可以使其拥有WOW镜头的控制方式
    /// 
    public class WowCamera : BaseCamera
    {
        /// 
        /// 镜头目标
        /// 
        public Transform Target;

        public Vector3 TargetOffset = new Vector3(0, 1f, 0);

        /// 
        /// 镜头离目标的距离
        /// 
        public float Distance = 30.0f;

        /// 
        /// 最大镜头距离
        /// 
        public float MaxDistance = 100f;

        /// 
        /// 鼠标滚轮拉近拉远速度系数
        /// 
        public float ScrollFactor = 5f;

        /// 
        /// 镜头旋转速度比率
        /// 
        public float RotateFactor = 1f;

        /// 
        /// 镜头水平环绕角度
        /// 
        public float HorizontalAngle = 0;

        /// 
        /// 镜头竖直环绕角度
        /// 
        public float VerticalAngle = -45;


        public bool LockHorizontal = false;
        public bool LockVertical = false;

        public Transform CameraTransform { get; protected set; }
        public UnityEngine.Camera Camera { get; protected set; }

        void Start()
        {
            Camera = Camera ?? gameObject.GetComponent<UnityEngine.Camera>();
            CameraTransform = Camera.transform;
            OnStart();
        }

        void Update()
        {
            if (Target && Target.gameObject.activeSelf)
            {
                InternalUpdate(Target);
            }
            OnUpdate();
        }

        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }

        protected virtual void InternalUpdate(Transform target)
        {
            var factor2 = Input.GetKey(KeyCode.LeftShift) ? 2f : 1f;

            //滚轮向前：拉近距离；滚轮向后：拉远距离
            var scrollAmount = Input.GetAxis("Mouse ScrollWheel");
            Distance -= scrollAmount * ScrollFactor * factor2;



            //保证镜头距离合法
            if (Distance < 0)
                Distance = 0;
            else if (Distance > MaxDistance)
                Distance = MaxDistance;



            //按住鼠标左右键移动，镜头随之旋转
            var isMouseLeftButtonDown = Input.GetMouseButton(0);
            var isMouseRightButtonDown = Input.GetMouseButton(1);
            if (isMouseLeftButtonDown || isMouseRightButtonDown)
            {
                var axisX = Input.GetAxis("Mouse X");
                var axisY = Input.GetAxis("Mouse Y");
                if (!LockHorizontal)
                {
                    this.HorizontalAngle += axisX * RotateFactor;
                }
                if (!LockVertical)
                {
                    this.VerticalAngle += axisY * RotateFactor;
                }
                if (!LockHorizontal || !LockVertical)
                {
                    if (isMouseRightButtonDown)
                    {
                        //如果是鼠标右键移动，则旋转人物在水平面上与镜头方向一致                    
                        this.OnTargetRotation(target, Quaternion.Euler(0, HorizontalAngle, 0));
                    }
                }
            }
            else
            {
                //Cursor.lockState = CursorLockMode.None;
            }



            //按镜头距离调整位置和方向
            var rotation = Quaternion.Euler(-VerticalAngle, HorizontalAngle, 0);
            var offset = rotation * Vector3.back * Distance;
            {

            }
            var camSPos = (target.position + TargetOffset) + offset;
            if (isMouseLeftButtonDown || isMouseRightButtonDown)
            {
                CameraTransform.position = camSPos;
                CameraTransform.rotation = rotation;
            }
            else
            {
                var camDPos = new Vector3(camSPos.x, CameraTransform.position.y, camSPos.z);
                camDPos.y += (camSPos.y - camDPos.y) / 4f;
                CameraTransform.position = camDPos;
                CameraTransform.rotation = rotation;
            }
        }

        protected virtual void OnTargetRotation(Transform target, Quaternion rotation)
        {
            target.rotation = Quaternion.Euler(0, HorizontalAngle, 0);
        }

        public override void ResetFromTransform()
        {

        }
    }

}

using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.Simple;
using UnityEngine;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------
    public class SimpleBattleCamera2D : IBattleCamera
    {
        public Camera camera { get; }
        public Transform transform => camera.transform;

        protected WowActorCamera2D baseCamera;
        protected UnityLayerObject focus;
        public SimpleBattleCamera2D(UnityZone zone, Camera camera)
        {
            this.camera = camera;
            this.baseCamera = camera.GetOrAddComponent<WowActorCamera2D>();
        }
        public virtual void Cleanup()
        {

        }
        public virtual void UpdateCamera()
        {
            //                 if (focus != null)
            //                 {
            //                     LookAt(focus.transform);
            //                 }
        }
        public virtual void MoveTo(Transform target)
        {
            var src = this.camera.transform.position;
            var dst = target.transform.position;
            camera.transform.position = new Vector3(dst.x, dst.y, src.z);
        }
        public virtual void LookAt(Transform target)
        {
            var src = this.camera.transform.position;
            var dst = target.transform.position;
            camera.transform.position = new Vector3(dst.x, dst.y, src.z);
        }
        public virtual void BindActor(UnityZoneActor actor)
        {
            this.focus = actor;
            if (focus != null)
            {
                LookAt(focus.transform);
            }
            baseCamera.BindActor(actor.parent);
        }
        public virtual void Focus(UnityLayerObject unit)
        {
            this.focus = unit;
            if (focus != null)
            {
                LookAt(focus.transform);
            }
        }
        public virtual void ResetFromTransform()
        {
            this.baseCamera.ResetFromTransform();
        }
        public virtual void Control(string name)
        {
        }
    }
    //-----------------------------------------------------------------------------------------------------


}


using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepMetaGame.Unity.BattleView.Simple
{

    public class WowFreeCamera : DeepCore.Unity.Camera.FreeCamera
    {
        private UnityZone zone;
        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (zone == null)
            {
                var mono = GameObject.FindObjectOfType<UnityZoneBeharvior>();
                //var mono = gameObject.GetComponentInChildren<UnityBattleZoneBeharvior>();
                if (mono != null) { zone = mono.zone; }
            }
        }
    }


}
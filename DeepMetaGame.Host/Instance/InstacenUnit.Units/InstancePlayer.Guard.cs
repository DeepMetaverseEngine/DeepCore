using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Geometry.Terrain;
using DeepCore.Log;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using static DeepCore.Game3D.Host.Instance.InstancePlayer;
using static DeepCore.Game3D.Host.Instance.InstanceUnit;

namespace DeepCore.Game3D.Host.Instance
{

    /// <summary>
    /// 可自动战斗的可操作单位
    /// </summary>
    public partial class InstancePlayer
    {
        protected UnitComponent mGuard;
        public bool IsGuard { get => mGuard != null; }
        public void SetGuard(bool guard, bool post = false)
        {
            if (guard)
            {
                if (mGuard == null)
                {
                    mGuard = Components.AddComponent(CreateGuard());
                }
            }
            else
            {
                if (mGuard != null)
                {
                    Components.RemoveComponent(mGuard);
                    mGuard = null;
                }
            }
            if (post)
            {
                this.PostEvent(ObjectPool.Alloc<PlayerGuardEvent>().Init(ID, guard));
            }
            ResetAI();
        }



    }

}

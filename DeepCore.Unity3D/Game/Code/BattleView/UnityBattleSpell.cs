using System;
using System.IO;
using Code.System.Pool;
using Code.System.Resource;
using DeepCore.Game3D.Slave.Layer;
using DeepGame3D.Unity.BattleView;
using UnityEngine;

namespace Code.BattleView
{
    public class UnityBattleSpell : UnityBattleObject
    {
        public LayerSpell ZoneSpell => ZoneObject as LayerSpell;
        public WrapGO ModelWrap { get; private set; }
        protected override void OnInit()
        {
            var url = ZoneSpell.Info.FileName;
            if (!string.IsNullOrEmpty(url))
            {
                var name = DeepCore.IO.Resource.GetFileNameWithoutExtension(url);
                ModelWrap = ResourceSystem.GetWrapGO(url, name, null, Transform);
                if (Math.Abs(ZoneSpell.Info.FileBodyScale - 1f) > 0.00001)
                {
                    ModelWrap.Transform.localScale = Vector3.one * ZoneSpell.Info.FileBodyScale;
                }
            }
            
            if (GizmosCylinder)
            {
                GizmosCylinder.Color = Color.magenta;
            }
        }

        protected override void OnUpdate(int deltaMS)
        {
            GameObject.transform.localRotation = ZoneObject.ToUnityRotation();
        }

        protected override void OnClear()
        {
            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }

            Battle.AddEffect(ZoneSpell.Info.FileNameDestory, 2000, 1, ZoneSpell.ToUnityPosition(), ZoneSpell.ToUnityRotation());
        }

        protected override void Disposing()
        {
            ObjectPool<UnityBattleSpell>.Release(this);
        }
    }
}

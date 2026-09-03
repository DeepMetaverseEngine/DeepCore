using System.Collections.Generic;
using Code.System.Resource;
using Code.System.World;
using UnityEngine;

namespace Code.System.WrapGOWatch
{
    public class WrapGOWatchSystemImpl : SingleSystem<WrapGOWatchSystemImpl>
    {
        private HashSet<IWrapGO> _wrapGoes = new HashSet<IWrapGO>();

        protected override void OnUpdate(float deltaTime)
        {
            ForceUpdate();
        }

        protected override void Disposing()
        {
            base.Disposing();
            _wrapGoes = null;
        }

        public void Add(IWrapGO wrap)
        {
            if (wrap == null || !wrap.GameObject)
            {
                Debug.LogError("wrap is null or wrap`s gameObject is null!");
            }
            _wrapGoes.Add(wrap);
        }

        public void Remove(IWrapGO wrap)
        {
            if (wrap == null)
            {
                Debug.LogError("wrap is null!");
                return;
            }
            _wrapGoes.Remove(wrap);
        }

        public IWrapGO Get(GameObject go)
        {
            if (!go) return null;
            foreach (var wrapGO in _wrapGoes)
            {
                if (wrapGO.GameObject == go)
                {
                    return wrapGO;
                }
            }

            return null;
        }

        public void ForceUpdate()
        {
            _wrapGoes.RemoveWhere((wrap) =>
            {
                if (wrap.GameObject) return false;
                wrap.Dispose();
                return true;
            });
        }
    }
}

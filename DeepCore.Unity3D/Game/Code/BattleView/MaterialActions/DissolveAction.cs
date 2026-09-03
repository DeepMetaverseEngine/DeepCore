using UnityEngine;

namespace Code.BattleView.MaterialActions
{
    public sealed class DissolveAction : MaterialAction
    {
        private MeshRenderer[] _renderers;
        private int _durationMaxMS;
        private int _durationMS;
        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");

        public void Init(GameObject go, int durationMS)
        {
            _renderers = go.GetComponentsInChildren<MeshRenderer>();
            _durationMaxMS = durationMS;
            _durationMS = _durationMaxMS;
        }

        protected override void OnUpdate(int deltaMS)
        {
            _durationMS -= deltaMS;
            if (_durationMS <= 0)
            {
                _durationMS = 0;
                IsDone = true;
            }

            var value = Mathf.Lerp(1f, 0f, _durationMS / (_durationMaxMS * 1f));
            foreach (var renderer in _renderers)
            {
                if (renderer)
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetFloat(Dissolve, value);
                    }
                }
            }
        }

        protected override void Disposing()
        {
            System.Pool.ObjectPool<DissolveAction>.Release(this);
        }

        protected override void OnClear()
        {
            _renderers = null;
            _durationMaxMS = 0;
            _durationMS = 0;
        }
    }

}

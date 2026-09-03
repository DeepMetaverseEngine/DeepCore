using UnityEngine;

namespace Code.BattleView.MaterialActions
{
    public sealed class TeleportAction : MaterialAction
    {
        private MeshRenderer[] _renderers;
        private float _from;
        private float _to;
        private int _durationMaxMS;
        private int _durationMS;
        private static readonly int TranProgress = Shader.PropertyToID("_TranProgress");

        public void Init(GameObject go, float from, float to, int durationMS)
        {
            _renderers = go.GetComponentsInChildren<MeshRenderer>();
            _from = from;
            _to = to;
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

            var value = Mathf.Lerp(_to, _from, _durationMS / (_durationMaxMS * 1f));
            foreach (var renderer in _renderers)
            {
                if (renderer)
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetFloat(TranProgress, value);
                    }
                }
            }
        }

        protected override void Disposing()
        {
            System.Pool.ObjectPool<TeleportAction>.Release(this);
        }

        protected override void OnClear()
        {
            _renderers = null;
            _durationMaxMS = 0;
            _durationMS = 0;
        }
    }

}
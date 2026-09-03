using UnityEngine;

namespace Code.BattleView.MaterialActions
{
    public class HitBlinAction : MaterialAction
    {
        private Renderer[] _renderers;
        private int _durationMS;
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");
        private const int MAX_DURATION = 150;

        public void Init(GameObject go)
        {
            _renderers = go.GetComponentsInChildren<MeshRenderer>();
            _durationMS = MAX_DURATION;
        }

        protected override void OnUpdate(int deltaMS)
        {
            _durationMS -= deltaMS;
            if (_durationMS <= 0)
            {
                _durationMS = 0;
                IsDone = true;
            }

            var color = Color.Lerp(Color.white, Color.red, _durationMS / (MAX_DURATION * 1f));
            foreach (var renderer in _renderers)
            {
                if (renderer)
                {
                    foreach (var material in renderer.materials)
                    {
                        material.SetColor(TintColor, color);
                    }
                }
            }
        }

        protected override void Disposing()
        {
            System.Pool.ObjectPool<HitBlinAction>.Release(this);
        }

        protected override void OnClear()
        {
            if (_renderers != null)
            {
                foreach (var renderer in _renderers)
                {
                    if (renderer)
                    {
                        foreach (var material in renderer.materials)
                        {
                            material.SetColor(TintColor, Color.white);
                        }
                    }
                }
            }

            _renderers = null;
            _durationMS = 0;
        }
    }
}
using UnityEngine;

namespace DeepCore.Unity.ResourceViewer
{
    public class EffectReplay : MonoBehaviour
    {
        private bool IsLoop;
        private float DurationMS;
        private float _past;
        private bool _pause;
        public override string ToString()
        {
            return DurationMS.ToString();
        }

        // Start is called before the first frame update
        void Start()
        {
            gameObject.TryGetParticleDurationMS(out DurationMS, out IsLoop);
        }
        // Update is called once per frame
        void Update()
        {
            if (_pause) return;
            if (IsLoop) return;
            _past += Time.deltaTime;
            if (_past * 1000 >= DurationMS + 1000)
            {
                _past = 0f;
                gameObject.SetActive(false);
                gameObject.SetActive(true);
            }
        }

        public void Replay()
        {
            _pause = false;
            foreach (var ps in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                ps.enableEmission = true;
            }
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Pause()
        {
            _pause = true;
            foreach (var ps in gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                ps.enableEmission = false;
            }
        }
    }
}
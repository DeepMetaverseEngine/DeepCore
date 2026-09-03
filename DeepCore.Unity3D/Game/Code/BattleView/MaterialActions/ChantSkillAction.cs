using UnityEngine;

namespace Code.BattleView.MaterialActions
{
    public class ChantSkillAction : MaterialAction
    {
        public GameObject Owner;
        public Transform OwnerTrans;
        public int durationMS;
        public int lifeMS = 1000;
        // 振幅
        private Vector3 amplitude = new(2, 2, 2); 
        // 轴
        private Vector3 axis = new (15, 10, 15);
        // 强度
        private float strength = 1;
        // 频率
        public float frequency = 25f;
        // 衰减
        private float weakenSpeed = 0;
        
        public ChantSkillAction Init(GameObject go, int chant = 1000)
        {
            Owner = go;
            OwnerTrans = go.transform;
            lifeMS = chant;
            return this;
        }
        
        protected override void OnUpdate(int deltaMS)
        {
            durationMS -= deltaMS;
            if (durationMS <= 0)
            {
                durationMS = 0;
                IsDone = true;
            }

            Shake();
        }

        public void Shake()
        {
            //计算位置抖动
            OwnerTrans.localPosition = new Vector3 (
                amplitude.x * Mathf.PerlinNoise(lifeMS, Time.time * frequency) * 2 - 1, 
                amplitude.y * Mathf.PerlinNoise(lifeMS + 1, Time.time * frequency) * 2 - 1,
                amplitude.z * Mathf.PerlinNoise(lifeMS + 2, Time.time * frequency) * 2 - 1
            ) * strength;
        
            //计算角度抖动
            OwnerTrans.localRotation = Quaternion.Euler(new Vector3(
                axis.x * (Mathf.PerlinNoise(lifeMS + 3, Time.time * frequency) * 2 - 1),
                (Mathf.PerlinNoise(lifeMS + 4, Time.time * frequency) * 2 - 1),
                (Mathf.PerlinNoise(lifeMS + 5, Time.time * frequency) * 2 - 1)
            ) * strength);
        
            strength = Mathf.Clamp01(strength - weakenSpeed * Time.deltaTime);
        }
        
        protected override void Disposing()
        {
            System.Pool.ObjectPool<ChantSkillAction>.Release(this);
        }

        protected override void OnClear()
        {
            Owner = null;
            OwnerTrans = null;
            durationMS = 0;
        }
    }
}
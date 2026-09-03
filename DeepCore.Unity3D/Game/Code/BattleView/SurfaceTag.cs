using System;
using UnityEngine;

namespace Code.BattleView
{
    /// <summary>
    /// 可拖拽区域标记脚本
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private BoxCollider Surface;

        private void OnEnable()
        {
            if (Surface == null)
                Surface = GetComponent<BoxCollider>();
        }


        private bool IsPosOnSurface(Vector3 pos)
        {
            bool contains = false;

            if (Surface && Surface.bounds.Contains(pos))
                return true;

            return contains;
        }


        public void Create(Vector3 pos, GameObject prefab, int tid, Action<bool> success)
        {
            if (IsPosOnSurface(pos))
            {
                //todo 放一个卡的内容实例(eg:法术、军队、单位)
                /*if (SampleBattle.Instance)
                {
                    SampleBattle.Instance.SummonUnit(tid, success);
                    return;
                }*/
            }
            success?.Invoke(false);
        }

    }
}
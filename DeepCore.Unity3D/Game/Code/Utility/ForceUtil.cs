using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace Code.Utility
{
    public class ForceUtil : MonoBehaviour
    {
        [SerializeField] private Sprite[] ForceT;
        [SerializeField] private SpriteRenderer Sprite;

        private int force;
        public int Force
        {
            get => force;
            set
            {
                force = value;
                OnForceChange();
            }
        }

        private void OnEnable()
        {
            if (Sprite == null) 
                Sprite = GetComponent<SpriteRenderer>();
        }

        private void OnForceChange()
        {
            if (Sprite == null)
                return;
            if (ForceT.Length <= force)
                return;
            Sprite.sprite = ForceT[force];
        }
    }
}
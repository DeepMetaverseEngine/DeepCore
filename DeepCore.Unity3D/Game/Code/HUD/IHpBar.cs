using UnityEngine;

namespace Code.HUD
{
    public interface IHpBar
    {
        Color Color { get; set; }
        float Percent { get; set; }
        GameObject GameObject { get; }
        Transform Transform { get; }

        void Hide();
        void Show(float percent);
    }
}
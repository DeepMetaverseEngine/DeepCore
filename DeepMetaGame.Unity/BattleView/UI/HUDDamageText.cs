using DeepCore;
using DeepMetaGame.Data.Message;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView.UI
{
    public interface HUDDamageText
    {
        UnitHitArgs HitEvent { get; set; }
        object value { get; set; }
        object status { get; set; }
        float? size { get; set; }
        UnityEngine.Color? color { get; set; }
        float timeMS { get; }


        void Start(object sender, Transform worldPos);
        void Update();

        void Hide();

        void Release();
    }
}

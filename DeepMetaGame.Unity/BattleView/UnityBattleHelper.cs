using DeepCore.GUI;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Misc;
using System;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{

    public static class UnityBattleHelper
    {

        public static void ScaleTo(this Transform transform, in Vector3 dstValue, float div)
        {
            var ss = transform.localScale;
            ss.x += (dstValue.x - ss.x) / div;
            ss.y += (dstValue.y - ss.y) / div;
            ss.z += (dstValue.z - ss.z) / div;
            transform.localScale = ss;
        }

    }
}


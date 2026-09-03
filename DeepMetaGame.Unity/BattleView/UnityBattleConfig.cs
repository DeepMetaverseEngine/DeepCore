using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{
    public class UnityBattleConfig
    {
        public string RayCastObjectLayerName;//RayCast
        public string RayCastTerrainLayerName;//Terrain
        public string EffectLayerName;
        public float RayCastMaxDistance = 1000;

        public float MouseClickDistance = 16f;
        public float ScaleToDiv = 8f;
        public Camera GameCamera { get; set; }
        public Transform Root { get; set; }
        public Transform UIRoot { get; set; }
        public Transform VoxelTemplateName { get; set; }
        public Transform UnitTemplateName { get; set; }
        public Transform SpellTemplateName { get; set; }
        //public AudioSource BGMPlayer { get; set; }

        public static bool ENABLE_BATTLE_GIZMOS = false;
        public static bool ENABLE_BATTLE_GIZMOS_FLAGS = false;
        public static bool ENABLE_BATTLE_DEBUG_GUI = true;
    }
}

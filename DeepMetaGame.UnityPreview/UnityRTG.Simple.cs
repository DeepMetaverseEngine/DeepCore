using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepCore.Unity.ResourceViewer;
using DeepCore.Unity3D.Cell;
using DeepCore.XCSV;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.Preview.Preview;
using System.Security.Cryptography;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{
//     public class SimpleRTGFactory : RTGFactory
//     {
//         public override UnityZoneSpaceTransverter TransHelper { get; } = UnityBattleFactory.Instance?.CreateZoneSpaceTransverter();
// 
// //         protected virtual WrapRes CreateWrapRes(string name, IWrapAssetGO wrap, ResourceType resType)
// //         {
// //             return new WrapRes(name, wrap, resType);
// //         }
//         //---------------------------------------------------------------------------------------------------
//         
//         //---------------------------------------------------------------------------------------------------
//     }


    public abstract class SimpleUnityRTG : UnityRTG
    {
        protected override void Awake()
        {
            base.Awake();
//             if (RTGFactory.Instance == null)
//             {
//                 new SimpleRTGFactory();
//             }
        }
    }


}

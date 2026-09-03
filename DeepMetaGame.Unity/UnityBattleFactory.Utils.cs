using DeepCore;
using DeepCore.Components;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry.Terrain;
using DeepCore.GUI.Input;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepCore.Voxel.Data;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity.BattleView;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static DeepMetaGame.Unity.UnityBattleFactory;
using BKeyCode = DeepCore.GUI.Input.KeyCode;

namespace DeepMetaGame.Unity
{
    
    public static class UnityBattleUtils
    {
        public static void UpdateSpellBones(this ISpellResourceObject res)
        {
            var spell = res.spell;
            var parent = spell.parent;
            var layerSpell = spell.layerSpell;
            switch (layerSpell.Info.BodyShape)
            {
                case SpellTemplate.Shape.LineToStart:
                    if (layerSpell.StartPos != null)
                    {
                        var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                        var p2 = parent.BattleToUnityWorldPosition(layerSpell.StartPos);
                        var t_z2w = parent.transform.localToWorldMatrix;
                        p1 = t_z2w.MultiplyPoint(p1);
                        p2 = t_z2w.MultiplyPoint(p2);
                        if (res.Bone1) { res.Bone1.position = p1; }
                        if (res.Bone2) { res.Bone2.position = p2; }
                    }
                    break;
                case SpellTemplate.Shape.LineToTarget:
                    if (layerSpell.Target != null)
                    {
                        var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                        var p2 = parent.BattleToUnityWorldPosition(layerSpell.Target.Position);
                        var t_z2w = parent.transform.localToWorldMatrix;
                        p1 = t_z2w.MultiplyPoint(p1);
                        p2 = t_z2w.MultiplyPoint(p2);
                        if (res.Bone1) { res.Bone1.position = p1; }
                        if (res.Bone2) { res.Bone2.position = p2; }
                    }
                    break;
                case SpellTemplate.Shape.LineToTargetPos:
                    if (layerSpell.TargetPos != null)
                    {
                        var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                        var p2 = parent.BattleToUnityWorldPosition(layerSpell.TargetPos.Value);
                        var t_z2w = parent.transform.localToWorldMatrix;
                        p1 = t_z2w.MultiplyPoint(p1);
                        p2 = t_z2w.MultiplyPoint(p2);
                        if (res.Bone1) { res.Bone1.position = p1; }
                        if (res.Bone2) { res.Bone2.position = p2; }
                    }
                    break;
                case SpellTemplate.Shape.LineToSender:
                    if (layerSpell.Sender != null)
                    {
                        var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                        var p2 = parent.BattleToUnityWorldPosition(layerSpell.Sender.Position);
                        var t_z2w = parent.transform.localToWorldMatrix;
                        p1 = t_z2w.MultiplyPoint(p1);
                        p2 = t_z2w.MultiplyPoint(p2);
                        if (res.Bone1) { res.Bone1.position = p1; }
                        if (res.Bone2) { res.Bone2.position = p2; }
                    }
                    break;
                case SpellTemplate.Shape.RectStrip:
                case SpellTemplate.Shape.RectStripRay:
                case SpellTemplate.Shape.Strip:
                case SpellTemplate.Shape.StripRay:
                case SpellTemplate.Shape.StripRayTouchEnd:
                case SpellTemplate.Shape.WideStrip:
                    //if (layerSpell.DistancePos.HasValue)
                    {
                        var p1 = parent.BattleToUnityWorldPosition(layerSpell.Position);
                        var p2 = parent.BattleToUnityWorldPosition(layerSpell.DistancePos);
                        var t_z2w = parent.transform.localToWorldMatrix;
                        p1 = t_z2w.MultiplyPoint(p1);
                        p2 = t_z2w.MultiplyPoint(p2);
                        if (res.Bone1) { res.Bone1.position = p1; }
                        if (res.Bone2) { res.Bone2.position = p2; }
                    }
                    break;
            }
        }
    }
}

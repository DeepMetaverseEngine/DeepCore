using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Unity.OnGUI;
using DeepGame3D.Unity.BattleView;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepMetaGame.Unity.BattleView
{
    public class UnityLayerObjectBeharvior : MonoBehaviour
    {
        public UnityLayerObject zoneObject { get; internal set; }
        void LateUpdate()
        {
            zoneObject.UpdateResource();
        }
    }
    public partial class UnityZoneBeharvior : MonoBehaviour
    {
        public UnityZone zone { get; internal set; }
        void LateUpdate()
        {
            zone.UpdateResource();
        }
        private float lastSpeed = 1f;
        protected virtual void OnGUI()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                UnityBattleConfig.ENABLE_BATTLE_DEBUG_GUI = !UnityBattleConfig.ENABLE_BATTLE_DEBUG_GUI;
            }
            if (UnityBattleConfig.ENABLE_BATTLE_DEBUG_GUI)
            {
                if (zone != null)
                {
                    var style = new GUIStyle(GUI.skin.label);
                    {
                        style.alignment = TextAnchor.MiddleLeft;
                        style.normal.textColor = Color.white;
                    }
                    var time = TimeSpan.FromMilliseconds(zone.layer.CurrentServerTimeMS);
                    GUI.Label(new Rect(0, 0, 200, 40), $"Time:{time}", style);
                }
                try
                {
                    bool ispause = Time.timeScale == 0;
                    GUIUtils.DrawGrid(new Vector2(0, Screen.height), new Vector2(64, -24), 100, 1,
                        (rect) =>
                        {
                            UnityBattleConfig.ENABLE_BATTLE_GIZMOS = GUI.Toggle(rect, UnityBattleConfig.ENABLE_BATTLE_GIZMOS,
                                new GUIContent() { text = $"Gizmos", tooltip = "Show Gizmos" }, GUI.skin.toggle);
                        },
                        (rect) =>
                        {
                            if (zone?.ModelWrap != null)
                            {
                                zone.ModelWrap.Active = GUI.Toggle(rect, zone.ModelWrap.Active,
                                    new GUIContent() { text = $"MapRes", tooltip = "Show Scene Resource" }, GUI.skin.toggle);
                            }
                        },
                        (rect) =>
                        {
                            if (zone?.VoxelTerrainObject != null)
                            {
                                zone.VoxelTerrainObject.SetActive(GUI.Toggle(rect, zone.VoxelTerrainObject.activeSelf,
                                    new GUIContent() { text = $"MapVox", tooltip = "Show Scene Voxel" }, GUI.skin.toggle));
                            }
                        },
                        (rect) =>
                        {
                            if (GUI.Button(rect, new GUIContent() { text = $"Reset", tooltip = "Reset" }, GUI.skin.button))
                            {
                                Time.timeScale = 1;
                                lastSpeed = Time.timeScale;
                            }
                        },
                        (rect) =>
                        {
                            if (GUI.Button(rect, new GUIContent()
                            {
                                text = !ispause ? "Resume" : "Play",
                                tooltip = !ispause ? "Resume" : "Play"
                            }, GUI.skin.button))
                            {
                                if (Time.timeScale == 0)
                                {
                                    if (lastSpeed == 0) lastSpeed = 1f;
                                    Time.timeScale = lastSpeed;
                                }
                                else
                                {
                                    lastSpeed = Time.timeScale;
                                    if (lastSpeed == 0) lastSpeed = 1f;
                                    Time.timeScale = 0;
                                }
                            }
                        },
                        (rect) =>
                        {
                            if (GUI.Button(rect, new GUIContent() { text = $"Speed-", tooltip = "Speed Down" }, GUI.skin.button))
                            {
                                if (Time.timeScale > 1)
                                {
                                    Time.timeScale -= 1;
                                }
                                else
                                {
                                    Time.timeScale /= 2f;
                                }
                                lastSpeed = Time.timeScale;
                            }
                        },
                        (rect) =>
                        {
                            if (GUI.Button(rect, new GUIContent() { text = $"Speed+", tooltip = "Speed UP" }, GUI.skin.button))
                            {
                                if (Time.timeScale >= 1)
                                {
                                    Time.timeScale += 1;
                                }
                                else
                                {
                                    Time.timeScale *= 2f;
                                }
                                lastSpeed = Time.timeScale;
                            }
                        },
                        (rect) =>
                        {
                            var style = new GUIStyle(GUI.skin.label);
                            {
                                style.alignment = TextAnchor.MiddleLeft;
                                style.normal.textColor = Color.white;
                            }
                            rect.width += 24 * 4;
                            var spd = Time.timeScale >= 1f ? Time.timeScale.ToString() : $"1/{1f / Time.timeScale}";
                            var msg_spd = $"{(ispause ? "Paused" : $"{spd}X")}";
                            var msg = $"|  {msg_spd}  |";
                            GUI.Label(rect, msg, style);
                        });
                }
                catch { }
            }
        }
    }

    public static class MonoUtils
    {
        public static bool TryGetBattleUnit(this GameObject obj, out InstanceUnit hostUnit)
        {
            if (obj.TryGetComponent<UnityLayerObjectBeharvior>(out var mono) && mono.zoneObject is UnityZoneUnit unit && unit.layerUnit.AsHost() is InstanceUnit host)
            {
                hostUnit = host;
                return true;
            }
            hostUnit = null;
            return false;
        }
    }
}
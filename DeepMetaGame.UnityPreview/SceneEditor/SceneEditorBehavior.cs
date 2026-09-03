using DeepCore;
using DeepMetaGame.Data.ZoneEditor;
using DeepMetaGame.Unity.BattleView;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview.SceneEditor
{
    public class SceneEditorBehavior : MonoBehaviour
    {
        public static SceneEditorProxy Proxy { get => SceneEditorProxy.Proxy; }
        public static UnityRTG RTG { get => UnityRTG.RTG; }
        public static TimeTaskQueue TimeTasks { get => UnityIPC.TimeTasks; }
        public static UnityEditorWorld World { get; set; }
        public static UnityZoneSpaceTransverter TransHelper => DisplayObject.TransHelper;

        //         public static bool IsShowObjectResource = true;
        //         public static bool IsShowObjectGizmos = true;
        //         public static bool IsShowObjectName = true;
        //         public static bool IsShowTerrainVoxel = true;
        //         public static bool IsShowTerrainResource = true;
        //         public static bool IsShowNavMesh = true;
        //         public static bool IsShowUnit = true;
        //         public static bool IsShowItem = true;
        //         public static bool IsShowRegion = true;
        //         public static bool IsShowPoint = true;
        //         public static bool IsShowDecoration = true;
        //         public static bool IsShowArea = true;
        //         public static bool IsShowDocker = true;
        //         public static bool IsGridToSize = false;
        //         public static bool OnlyShowNoneVoxel = false;
        public static SceneEditorStatus VS { get; } = new SceneEditorStatus();
        public static void LookAt(Transform pos)
        {
            RTG?.LookAt(pos);
        }
        public static void LookAt(Vector3 pos)
        {
            RTG?.LookAt(pos);
        }
        public static void PLog(object message)
        {
            UnityIPC.PLog(message);
        }

        public static DeepCore.Geometry.Vector3 UnityWorldToBattlePosition(Vector3 Pos)
        {
            //return new DeepCore.Geometry.Vector3(Pos.x, World.TerrainH - Pos.z, Pos.y);
            return TransHelper.UnityWorldToBattlePosition(World.terrain, Pos);
        }
        public static Vector3 BattleToUnityWorldPosition(in DeepCore.Geometry.Vector3 p)
        {
            //return new Vector3(p.X, p.Z, World.TerrainH - p.Y);
            return TransHelper.BattleToUnityWorldPosition(World.terrain, p);
        }

        public static float UnityToBattleRotation(in Quaternion q)
        {
            //             q.ToAngleAxis(out var angle, out var axis);
            //             return (angle - 90f) / Mathf.Rad2Deg;
            return TransHelper.UnityToBattleRotation(q);
        }
        public static Quaternion BattleToUnityRotation(in float direction)
        {
            //             var radians = direction;
            //             return Quaternion.AngleAxis((radians * Mathf.Rad2Deg) + 90f, Vector3.up);
            return TransHelper.BattleToUnityRotation(direction);
        }

    }
}

using DeepCore.IO;
using DeepCore.Unity.ResourceSnap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace DeepCore.Unity
{
    public static partial class UnityExtensions
    {
        public static GameObject AsGameObject(this object sender)
        {
            if (sender is GameObject go)
            {
                return go;
            }
            if (sender is Component tx)
            {
                return tx.gameObject;
            }
            if (sender is MonoBehaviour unit)
            {
                return unit.gameObject;
            }
            return null;
        }
        public static Transform AsTransform(this object sender)
        {
            return AsGameObject(sender)?.transform;
        }
        public static async Task<AsyncOperation> Async(this AsyncOperation self)
        {
            TaskCompletionSource<AsyncOperation> tcs = new TaskCompletionSource<AsyncOperation>();
            self.completed += (req) =>
            {
                tcs.SetResult(self);
            };
            return await tcs.Task;
        }

        public static LinkedListNode<T> SetValue<T>(this LinkedListNode<T> self, T value)
        {
            self.Value = value;
            return self;
        }

        public static bool IsInCamera(this UnityEngine.Camera camera, Transform transform)
        {
            var mCamera = camera;
            var pos = transform.position;
            //转化为视角坐标
            var viewPos = mCamera.WorldToViewportPoint(pos);
            // z<0代表在相机背后
            if (viewPos.z < 0) return false;
            //太远了！看不到了！
            if (viewPos.z > mCamera.farClipPlane)
                return false;
            // x,y取值在 0~1之外时代表在视角范围外；
            if (viewPos.x < 0 || viewPos.y < 0 || viewPos.x > 1 || viewPos.y > 1) return false;
            return true;
        }

        /*
        public static Vector3 ToUnityPosition(this LayerZoneObject value)
        {
            var p = value.Position;
            return new Vector3(p.X, p.Z, p.Y);
        }

        public static Quaternion ToUnityRotation(this LayerZoneObject self)
        {
            //x为正方向
            var radians = self.Direction;
            return Quaternion.AngleAxis(-radians * Mathf.Rad2Deg + 90, Vector3.up);
        }

        public static Vector3 ToUnityPosition(this AddEffectEvent self)
        {
            return new Vector3(self.X, self.Z, self.Y);
        }

        public static Quaternion ToUnityRotation(this AddEffectEvent self)
        {
            //x为正方向
            var radians = self.direction;
            return Quaternion.AngleAxis(-radians * Mathf.Rad2Deg + 90, Vector3.up);
        }

        public static Vector3 ToUnity(this DeepCore.Geometry.Vector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
        public static Vector2 ToUnity(this DeepCore.Geometry.Vector2 value)
        {
            return new Vector2(value.X, value.Y);
        }
        */
        //--------------------------------------------------------------------------------------------------------------
        #region GameObjects
        public static Transform SetActive(this Transform self, bool active)
        {
            var go = self.gameObject;
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
            return self;
        }
        public static GameObject ActiveSelf(this GameObject self, bool active)
        {
            if (self.activeSelf != active)
            {
                self.SetActive(active);
            }
            return self;
        }
        public static LinkedListNode<T> FindNode<T>(this LinkedList<T> self, Predicate<T> match)
        {
            var node = self.First;
            while (node != null)
            {
                if (match(node.Value))
                {
                    return node;
                }
                node = node.Next;
            }
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------
        private static Transform InternalFindDeep(this Transform self, string name)
        {
            if (self.name.Equals(name)) return self;
            if (self.childCount == 0) return null;
            for (var i = 0; i < self.childCount; i++)
            {
                var sub = self.GetChild(i).InternalFindDeep(name);
                if (sub)
                {
                    return sub;
                }
            }
            return null;
        }
        public static Transform FindDeep(this Transform self, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return name.Contains("/") ? self.Find(name) : self.InternalFindDeep(name);
        }
        public static Transform FindDeep(this GameObject self, string name)
        {
            return self.transform.FindDeep(name);
        }

        //--------------------------------------------------------------------------------------------------------------
        private static Transform InternalFindDeep(this Transform self, Predicate<Transform> action)
        {
            if (action(self)) return self;
            if (self.childCount == 0) return null;
            for (var i = 0; i < self.childCount; i++)
            {
                var sub = self.GetChild(i).InternalFindDeep(action);
                if (sub)
                {
                    return sub;
                }
            }
            return null;
        }
        public static Transform FindDeep(this Transform self, Predicate<Transform> action)
        {
            return self.InternalFindDeep(action);
        }
        public static Transform FindDeep(this GameObject self, Predicate<Transform> action)
        {
            return self.transform.FindDeep(action);
        }
        public static Transform FindDeep(this Transform[] self, Predicate<Transform> action)
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub) return sub;
            }
            return null;
        }
        public static Transform FindDeep(this GameObject[] self, Predicate<Transform> action)
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub) return sub;
            }
            return null;
        }
        //--------------------------------------------------------------------------------------------------------------
        private static T InternalFindDeep<T>(this Transform self, Func<Transform, T> action) where T : class
        {
            var ret = action(self);
            if (ret != null) return ret;
            if (self.childCount == 0) return null;
            for (var i = 0; i < self.childCount; i++)
            {
                var sub = self.GetChild(i).InternalFindDeep<T>(action);
                if (sub != null)
                {
                    return sub;
                }
            }
            return null;
        }
        public static T FindDeep<T>(this Transform self, Func<Transform, T> action) where T : class
        {
            return self.InternalFindDeep<T>(action);
        }
        public static T FindDeep<T>(this GameObject self, Func<Transform, T> action) where T : class
        {
            return self.transform.FindDeep<T>(action);
        }
        public static T FindDeep<T>(this Transform[] self, Func<Transform, T> action) where T : class
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub != null) return sub;
            }
            return null;
        }
        public static T FindDeep<T>(this GameObject[] self, Func<Transform, T> action) where T : class
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub != null) return sub;
            }
            return null;
        }

        public static Transform FindDeep(this Transform self, Func<Transform, bool> action)
        {
            return self.InternalFindDeep(t => action(t) ? t : null);
        }
        public static Transform FindDeep(this GameObject self, Func<Transform, bool> action)
        {
            return self.transform.FindDeep(action);
        }
        public static Transform FindDeep(this Transform[] self, Func<Transform, bool> action)
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub != null) return sub;
            }
            return null;
        }
        public static Transform FindDeep(this GameObject[] self, Func<Transform, bool> action)
        {
            foreach (var t in self)
            {
                var sub = FindDeep(t, action);
                if (sub != null) return sub;
            }
            return null;
        }

        //--------------------------------------------------------------------------------------------------------------
        private static void InternalForEachDeep(this Transform self, Action<Transform> action)
        {
            action(self);
            for (var i = 0; i < self.childCount; i++)
            {
                self.GetChild(i).InternalForEachDeep(action);
            }
        }
        public static void ForEachDeep(this Transform self, Action<Transform> action)
        {
            self.InternalForEachDeep(action);
        }
        public static void ForEachDeep(this GameObject self, Action<Transform> action)
        {
            self.transform.ForEachDeep(action);
        }
        public static void ForEachDeep(this Transform[] self, Action<Transform> action)
        {
            foreach (var t in self)
            {
                ForEachDeep(t, action);
            }
        }
        public static void ForEachDeep(this GameObject[] self, Action<Transform> action)
        {
            foreach (var t in self)
            {
                ForEachDeep(t, action);
            }
        }
        //--------------------------------------------------------------------------------------------------------------


        //--------------------------------------------------------------------------------------------------------------


        public static GameObject Parent(this GameObject self, GameObject go, bool worldPositionStays = false)
        {
            if (self)
            {
                self.transform.Parent(go != null ? go.transform : null, worldPositionStays);
            }
            return self;
        }

        public static Transform Parent(this Transform self, Transform trans, bool worldPositionStays = false)
        {
            if (self)
            {
                self.SetParent(trans, worldPositionStays);
            }
            return self;
        }
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            var t = self.GetComponent<T>();
            if (t == null)
            {
                t = self.AddComponent<T>();
            }
            return t;
        }
        public static bool TryGetComponents<T>(this GameObject self, out T[] com) where T : Component
        {
            com = self.GetComponents<T>();
            return com != null && com.Length > 0;
        }

        //--------------------------------------------------------------------------------------------------------------

        public static bool TryGetComponentInChildren<T>(this GameObject self, out T com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentInChildren<T>(includeInactive);
            return com != null;
        }
        public static bool TryGetComponentInChildren<T>(this Transform self, out T com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentInChildren<T>(includeInactive);
            return com != null;
        }
        public static bool TryGetComponentInParent<T>(this GameObject self, out T com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentInParent<T>(includeInactive);
            return com != null;
        }
        public static bool TryGetComponentInParent<T>(this Transform self, out T com, bool includeInactive = false) where T : Component
        {
            com = self.gameObject.GetComponentInParent<T>(includeInactive);
            return com != null;
        }
        //--------------------------------------------------------------------------------------------------------------
        public static bool TryGetComponentsInChildren<T>(this GameObject self, out T[] com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentsInChildren<T>(includeInactive);
            return com != null && com.Length > 0;
        }
        public static bool TryGetComponentsInChildren<T>(this Transform self, out T[] com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentsInChildren<T>(includeInactive);
            return com != null && com.Length > 0;
        }
        public static bool TryGetComponentsInParent<T>(this GameObject self, out T[] com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentsInParent<T>(includeInactive);
            return com != null && com.Length > 0;
        }
        public static bool TryGetComponentsInParent<T>(this Transform self, out T[] com, bool includeInactive = false) where T : Component
        {
            com = self.GetComponentsInParent<T>(includeInactive);
            return com != null && com.Length > 0;
        }
        //--------------------------------------------------------------------------------------------------------------
        public static bool TryGetComponentsInChildren<T>(this GameObject self, List<T> com, bool includeInactive = false) where T : Component
        {
            self.GetComponentsInChildren<T>(includeInactive, com);
            return com.Count > 0;
        }
        public static bool TryGetComponentsInChildren<T>(this Transform self, List<T> com, bool includeInactive = false) where T : Component
        {
            self.GetComponentsInChildren<T>(includeInactive, com);
            return com.Count > 0;
        }
        public static bool TryGetComponentsInParent<T>(this GameObject self, List<T> com, bool includeInactive = false) where T : Component
        {
            self.GetComponentsInParent<T>(includeInactive, com);
            return com.Count > 0;
        }
        public static bool TryGetComponentsInParent<T>(this Transform self, List<T> com, bool includeInactive = false) where T : Component
        {
            self.GetComponentsInParent<T>(includeInactive, com);
            return com.Count > 0;
        }
        //--------------------------------------------------------------------------------------------------------------
        #endregion
        //--------------------------------------------------------------------------------------------------------------

        public static Vector3 UGUI2World(this UnityEngine.Camera cam, GameObject worldObj, GameObject uiObj)
        {
            Vector3 ptScreen = RectTransformUtility.WorldToScreenPoint(cam, uiObj.transform.position);
            ptScreen.z = 0;
            ptScreen.z = Mathf.Abs(cam.transform.position.z - worldObj.transform.position.z);
            Vector3 ptWorld = cam.ScreenToWorldPoint(ptScreen);
            return ptWorld;
        }

        public static Vector3 World2UGUI(this UnityEngine.Camera cam, GameObject worldObj, GameObject uiObj)
        {
            Vector3 originOff;  // 当前UI系统(0,0)点 相对于屏幕左下角(0, 0)点的偏移量
            originOff = new Vector3(-Screen.width / 2, -Screen.height / 2);
            Vector3 position = cam.WorldToScreenPoint(worldObj.transform.position) + originOff;
            position.z = 0;
            RectTransform rt = uiObj.transform.GetComponent<RectTransform>();
            Vector2 pivot = rt.pivot;
            switch (pivot.x)
            {
                case 1:
                    position.x += rt.sizeDelta.x / 2;
                    break;
                case 0:
                    position.x -= rt.sizeDelta.x / 2;
                    break;
            }
            switch (pivot.y)
            {
                case 1:
                    position.y += rt.sizeDelta.y / 2;
                    break;
                case 0:
                    position.y -= rt.sizeDelta.y / 2;
                    break;
            }
            return position;
        }


        // 
        //         public static async Task<AsyncOperation> Async(this AsyncOperation self)
        //         {
        //             TaskCompletionSource<AsyncOperation> tcs = new TaskCompletionSource<AsyncOperation>();
        //             self.completed += (req) =>
        //             {
        //                 tcs.SetResult(self);
        //             };
        //             return await tcs.Task;
        //         }
        // 
        //         public static Transform SetActive(this Transform self, bool active)
        //         {
        //             var go = self.gameObject;
        //             if (go.activeSelf != active)
        //             {
        //                 go.SetActive(active);
        //             }
        // 
        //             return self;
        //         }
        // 
        //         public static GameObject ActiveSelf(this GameObject self, bool active)
        //         {
        //             if (self.activeSelf != active)
        //             {
        //                 self.SetActive(active);
        //             }
        // 
        //             return self;
        //         }
        // 
        //         public static LinkedListNode<T> SetValue<T>(this LinkedListNode<T> self, T value)
        //         {
        //             self.Value = value;
        //             return self;
        //         }

        /*
        public static Vector3 ToUnityPosition(this LayerZoneObject value)
        {
            var p = value.Position;
            return new Vector3(p.X, p.Z, p.Y);
        }

        public static Quaternion ToUnityRotation(this LayerZoneObject self)
        {
            //x为正方向
            var radians = self.Direction;
            return Quaternion.AngleAxis(-radians * Mathf.Rad2Deg + 90, Vector3.up);
        }

        public static Vector3 ToUnityPosition(this AddEffectEvent self)
        {
            return new Vector3(self.X, self.Z, self.Y);
        }

        public static Quaternion ToUnityRotation(this AddEffectEvent self)
        {
            //x为正方向
            var radians = self.direction;
            return Quaternion.AngleAxis(-radians * Mathf.Rad2Deg + 90, Vector3.up);
        }

        public static Vector3 ToUnity(this DeepCore.Geometry.Vector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
        public static Vector2 ToUnity(this DeepCore.Geometry.Vector2 value)
        {
            return new Vector2(value.X, value.Y);
        }
        */
        // 
        //         public static LinkedListNode<T> FindNode<T>(this LinkedList<T> self, Predicate<T> match)
        //         {
        //             var node = self.First;
        //             while (node != null)
        //             {
        //                 if (match(node.Value))
        //                 {
        //                     return node;
        //                 }
        //                 node = node.Next;
        //             }
        // 
        //             return null;
        //         }
        // 
        //         public static Transform FindDeep(this Transform self, string name)
        //         {
        //             if (string.IsNullOrEmpty(name)) return null;
        //             return name.Contains("/") ? self.Find(name) : self.InternalFindDeep(name);
        //         }
        // 
        //         private static Transform InternalFindDeep(this Transform self, string name)
        //         {
        //             if (self.name.Equals(name)) return self;
        //             if (self.childCount == 0) return null;
        //             for (var i = 0; i < self.childCount; i++)
        //             {
        //                 var sub = self.GetChild(i).InternalFindDeep(name);
        //                 if (sub)
        //                 {
        //                     return sub;
        //                 }
        //             }
        // 
        //             return null;
        //         }
        // 
        //         public static Transform FindDeep(this GameObject self, string name)
        //         {
        //             return self.transform.FindDeep(name);
        //         }
        // 
        //         public static GameObject Parent(this GameObject self, GameObject go, bool worldPositionStays = false)
        //         {
        //             self.transform.Parent(go != null ? go.transform : null, worldPositionStays);
        //             return self;
        //         }
        // 
        //         public static Transform Parent(this Transform self, Transform trans, bool worldPositionStays = false)
        //         {
        //             self.SetParent(trans, worldPositionStays);
        //             return self;
        //         }

        public static void SaveTextureToFile(this Texture2D texture, FileInfo fileName)
        {
            var bytes = texture.EncodeToPNG();
            CFiles.WriteAllBytes(fileName, bytes);
        }
    }
}

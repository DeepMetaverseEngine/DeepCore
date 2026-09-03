using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepCore.Unity3D
{
    public static partial class UnityHelper
    {
        ///Gets existing T component or adds new one if not exists
//         public static T GetOrAddComponent<T>(this GameObject go) where T : Component
//         {
//             return GetOrAddComponent(go, typeof(T)) as T;
//         }

        ///Gets existing T component or adds new one if not exists
        public static T GetOrAddComponent<T>(this Component comp) where T : Component
        {
            return GetOrAddComponent(comp.gameObject, typeof(T)) as T;
        }

        ///Gets existing component or adds new one if not exists
        public static Component GetOrAddComponent(this GameObject go, System.Type type)
        {
            var result = go.GetComponent(type);
            if (result == null)
            {
                result = go.AddComponent(type);
            }

            return result;
        }

        public static float GetAnimationTime(this Animator animator, string stateName)
        {
            if (!animator || string.IsNullOrEmpty(stateName))
            {
                return 0;
            }

            var lastClips = animator.runtimeAnimatorController.animationClips;
            if (lastClips != null)
            {
                return (from clip in lastClips where clip.name.Equals(stateName) select clip.length).FirstOrDefault();
            }

            return 0;
        }

        public static Camera GetFirstRenderCamera(this GameObject go)
        {
            return Camera.allCameras.FirstOrDefault(c => (c.cullingMask & 1 << go.layer) != 0);
        }

        public static Camera[] GetAllRenderCameras(this GameObject go)
        {
            return Camera.allCameras.Where(c => (c.cullingMask & 1 << go.layer) != 0).ToArray();
        }


        public static bool IsObjectExists(Object val)
        {
            return val != null && !val.Equals(null);
        }

        /// <summary>
        /// 递归查找子节点
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="name"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public static Transform FindRecursive(this Transform transform, string name, StringComparison comp)
        {
            //先广度遍历
            foreach (Transform t in transform)
            {
                if (t.name.Equals(name, comp))
                {
                    return t;
                }
            }

            //再深度遍历
            return (from Transform t in transform select FindRecursive(t, name, comp)).FirstOrDefault(tmp => tmp != null);
        }

        public static Transform FindRecursive(this Transform transform, string name)
        {
            //再深度遍历
            return transform.FindRecursive(name, StringComparison.Ordinal);
        }

        public static Rect GetWorldRect(this RectTransform transform)
        {
            var worldCorners = new Vector3[4];
            transform.GetWorldCorners(worldCorners);
            var result = new Rect(
                worldCorners[0].x,
                worldCorners[0].y,
                worldCorners[2].x - worldCorners[0].x,
                worldCorners[2].y - worldCorners[0].y);
            return result;
        }

        public static bool RectOverlaps(this RectTransform self, RectTransform other)
        {
            var t1 = self.GetWorldRect();
            var t2 = other.GetWorldRect();
            return t1.Overlaps(t2);
        }

        public static Transform GetChildAtOrDefault(this Transform transform, int index)
        {
            if (transform.childCount > index)
            {
                return transform.GetChild(index);
            }

            return null;
        }

        public static Transform[] GetChildren(this Transform transform)
        {
            var ret = new Transform[transform.childCount];
            var p = 0;
            foreach (Transform t in transform)
            {
                ret[p++] = t;
            }

            return ret;
        }

        public static string GetStringPath(this Transform go)
        {
            var name = go.name;
            while (go.parent != null)
            {
                go = go.parent;
                name = go.name + "/" + name;
            }

            return name;
        }

        public static List<Transform> GetPath(this Transform go)
        {
            var ret = new List<Transform> { go };
            while (go.parent != null)
            {
                go = go.parent;
                ret.Insert(0, go);
            }

            return ret;
        }


        public static void SetLayer(this GameObject go, int layer)
        {
            go.layer = layer;
            var list = go.GetComponentsInChildren<Transform>(true);
            foreach (var o in list)
            {
                if (o.gameObject.layer != layer)
                {
                    o.gameObject.layer = layer;
                }
            }
        }
        public static void SetLayer(this GameObject go, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            SetLayer(go, layer);
        }

        public static void SetSortingOrder(this GameObject obj, int sortingOrder)
        {
            var renders = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var render in renders)
            {
                render.sortingOrder = sortingOrder;
            }
        }


//         public static Quaternion LogicRad2Quaternion(float direct)
//         {
//             return Quaternion.AngleAxis((direct * Mathf.Rad2Deg) + 90, Vector3.up);
//         }
// 
//         public static float Quaternion2LogicRad(Quaternion rot)
//         {
//             return (rot.eulerAngles.y - 90) / Mathf.Rad2Deg;
//         }

        public static void SetAnimatorFloat(this Animator animator, string name, float normailze = 0)
        {
            animator.SetFloat(name, normailze);
        }

        public static float GetCurrentPlayLayerAnimatorTime(this Animator animator, int layer)
        {
            float time = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            int intTime = Mathf.CeilToInt(time);
            float decimalVal = 1 - (intTime - time);
            return decimalVal;
        }

        public static int GetCurrentPlayLayerAnimatorNameHash(this Animator animator, int layer)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
        }

        public static Color SetAlpha(this Color src, float alpha)
        {
            src.a = alpha;
            return src;
        }

#if UNITY_EDITOR
        private static HashMap<int, string> hash = new HashMap<int, string>();
#else
        private static HashSet<int> hash = new HashSet<int>();
#endif
        public static void Destroy(Object o, float t = 0f)
        {
            if (IsObjectExists(o))
            {
                //                var id = o.GetInstanceID();
                //#if UNITY_EDITOR
                //                if (hash.TryAdd(id, new System.Diagnostics.StackTrace().ToString()))
                //#else
                //                if (hash.Add(id))
                //#endif
                //                {
                //                    Object.Destroy(o, t);
                //                }
                //                else
                //                {
                //#if UNITY_EDITOR
                //                    Debug.LogError("Try to destroy a destroyed object\n" + hash[id], o);
                //#else
                //                    Debug.LogError("Try to destroy a destroyed object\n", o);
                //#endif
                //                }
                Object.Destroy(o, t);
            }
            else
            {
                Debug.LogError("Try to destroy a not exists object", o);
            }
        }

        public static void DestroyImmediate(Object o, bool b = false)
        {
            if (IsObjectExists(o))
            {
                //                var id = o.GetInstanceID();
                //#if UNITY_EDITOR
                //                if (hash.TryAdd(id, new System.Diagnostics.StackTrace().ToString()))
                //#else
                //                if (hash.Add(id))
                //#endif
                //                {
                //                    Object.DestroyImmediate(o, b);
                //                }
                //                else
                //                {
                //                    Debug.LogError("Try to destroy a destroyed object", o);
                //                }
                Object.DestroyImmediate(o, b);
            }
            else
            {
                Debug.LogError("Try to destroy a not exists object", o);
            }
        }

        private static int sIntNextID = 1;
        private static uint sUIntNextID = 1;

        public static int GenIntID()
        {
            var ret = unchecked(sIntNextID++);
            if (ret == 0)
            {
                ret = GenIntID();
            }

            return ret;
        }

        public static uint GenUIntID()
        {
            var ret = unchecked(sUIntNextID++);
            if (ret == 0)
            {
                ret = GenUIntID();
            }

            return ret;
        }
    }
}
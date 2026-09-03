using DeepCore;
using DeepCore.Unity;
using DeepCore.Unity.OnGUI;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Unity.BattleView;
using System.Security.Cryptography;
using UnityEngine;

namespace DeepMetaGame.Unity.Preview
{
    public abstract class PreviewBehavior : MonoBehaviour
    {
        private bool isInitGUI = false;
        private GUICanvas canvas;
        public GUICanvas RootCanvas { get { return canvas; } }
        protected virtual void OnGUI()
        {
            try
            {
                if (!isInitGUI)
                {
                    canvas = new GUICanvas();
                    try
                    {
                        OnInitGUI(canvas);
                    }
                    catch (Exception err)
                    {
                        Debug.LogError(err);
                    }
                    isInitGUI = true;
                }
                canvas.Visit();
                OnDrawGUI();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                if (GUI.changed)
                {
                    Input.ResetInputAxes();
                }
            }
        }
        protected virtual void OnInitGUI(GUICanvas canvas) { }
        protected virtual void OnDrawGUI() { }
    }

    //--------------------------------------------------------------------------------------
    public abstract class DisplayObject : PreviewBehavior, IDisposable
    {
        public static SingleThreadCollectionPool ObjectPool => UnityIPC.ObjectPool;
        //public static RTGFactory Factory => RTGFactory.Instance;
        public static UnityRTG RTG { get => UnityRTG.RTG; }
        public static UnityIPC IPC { get => UnityIPC.IPC; }
        public static UnityZoneSpaceTransverter TransHelper => RTG.TransHelper;
        //--------------------------------------------------------------------------------------
        private bool disposed = false;
        public bool Disposed => disposed;
        //--------------------------------------------------------------------------------------
        public static void PLog(object message)
        {
            UnityIPC.PLog(message);
        }
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                OnDisposing();
                GameObject.Destroy(gameObject);
            }
        }
        protected abstract void OnDisposing();
        //--------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------

    }
    //--------------------------------------------------------------------------------------
    public class DisplayEffect : DisplayObject
    {
        public static DisplayEffect LoadEffect(GameObject parent, LaunchEffect effect, float height = 0)
        {
            return LoadEffect(parent, effect, null, height);
        }
        public static DisplayEffect LoadEffect(GameObject parent, LaunchEffect effect, IViewResource parentRes, float height = 0)
        {
            if (effect != null)
            {
                var ret = new GameObject("Effect_" + effect.Name).AddComponent<DisplayEffect>();
                var res = RTG.LoadResource(effect.Name, ResourceType.Object_Effect, ret);
                if (res != null)
                {
                    if (effect.BindBody)
                    {
                        if (parentRes != null)
                        {
                            res.BindBody(parentRes, effect.BindPartName);
                        }
                        else if (parent.TryGetComponentInChildren<DisplayEffect>(out var parentEffect) && parentEffect.Res != null)
                        {
                            res.BindBody(parentEffect.Res, effect.BindPartName);
                        }
                    }
                    if (effect.ResTransform != null)
                    {
                        var tx = effect.ResTransform;
                        res.transform.localPosition = tx.localPosition.ToUnity();
                        res.transform.localRotation = Quaternion.Euler(tx.localEuler.ToUnity());
                        res.transform.localScale = tx.localScale.ToUnity();
                    }
                    res.PlayEffect(effect.AnimName, effect.IsLoop, 1f, ret.transform);
                }
                {
                    var offset = TransHelper.BattleToUnityVoxelAnchorOffset(height, effect.BodyVoxelAnchor);
                    if (effect.BindingOffsetDistance != 0)
                    {
                        DeepCore.Geometry.Vector3 offset2 = DeepCore.Geometry.VectorHelper.Polar(
                            CMath.ToPI(effect.BindingOffsetAngle360),
                            effect.BindingOffsetDistance);
                        offset2.Z = effect.BindingOffsetZ;
                        offset += TransHelper.BattleToUnityOffset(offset2);
                    }
                    else
                    {
                        DeepCore.Geometry.Vector3 offset2 = new DeepCore.Geometry.Vector3(0, 0, effect.BindingOffsetZ);
                        offset += TransHelper.BattleToUnityOffset(offset2);
                    }
                    {
                        if (effect.ScaleToBodySize != 0)
                        {
                            ret.transform.localScale *= effect.ScaleToBodySize;
                        }
                    }
                    ret.transform.localPosition += (offset);
                    ret.transform.SetParent(parent.transform, false);
                }
                if (effect.SubEffects != null)
                {
                    foreach (var sub in effect.SubEffects)
                    {
                        LoadEffect(parent, sub, parentRes, height);
                    }
                }
                return ret;
            }
            return null;
        }
        public IViewResource Res { get; private set; }
        protected override void OnDisposing()
        {
            try
            {
                Res?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        void LateUpdate()
        {
            try
            {
                Res?.UpdateResource(this.gameObject);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

    }
}

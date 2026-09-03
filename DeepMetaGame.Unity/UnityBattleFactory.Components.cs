using DeepCore;
using DeepCore.Components;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Geometry.Terrain;
using DeepCore.GUI.Display.SceneGraph.GUI;
using DeepCore.GUI.Input;
using DeepCore.Reflection;
using DeepCore.Unity;
using DeepCore.Unity3D;
using DeepCore.Voxel.Data;
using DeepGame3D.Unity.BattleView;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Unity.BattleView;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static DeepMetaGame.Unity.UnityBattleFactory;
using BKeyCode = DeepCore.GUI.Input.KeyCode;

namespace DeepMetaGame.Unity
{
    //-----------------------------------------------------------------------------------------------------------------
    partial class UnityBattleFactory
    {
        protected virtual void InitializeComponents()
        {
            new SimpleAssetsLoaderComponent();
            new SimpleResourceComponent();
            new TextureLoaderComponent();
            new AudioComponent();
            new VoxelComponent();
            new InputComponent();
        }
        public static AssetsLoaderComponent AssetsLoader { get => AssetsLoaderComponent.Instance; }
        public static ResourceComponent Resource { get => ResourceComponent.Instance; }
        public static TextureLoaderComponent TextureLoader { get => TextureLoaderComponent.Instance; }
        public static AudioComponent Audio { get => AudioComponent.Instance; }
        public static VoxelComponent Voxel { get => VoxelComponent.Instance; }
    }
    //-----------------------------------------------------------------------------------------------------------------
    public abstract class BattleFactoryComponent : Disposable, IComponent<UnityBattleFactory>
    {
        public UnityBattleFactory Owner { get; private set; }
        public UnityBattleFactory Factory { get => Owner; }
        //public static SingleThreadCollectionPool ObjectPool => UnityBattleFactory.ObjectPool;
        protected abstract System.Type ComponentType { get; }
        public BattleFactoryComponent()
        {
            UnityBattleFactory.Instance.Components.ReplaceComponent(ComponentType, this, out var old);
        }
        void IComponent<UnityBattleFactory>.InternalAdded(UnityBattleFactory owner)
        {
            this.Owner = owner;
            this.Added(owner);
        }
        void IComponent<UnityBattleFactory>.InternalRemoved(UnityBattleFactory owner)
        {
            this.Dispose();
        }
        internal void CleanAssets() { OnCleanAssets(); }
        internal void LowMemory() { OnLowMemory(); }
        protected abstract void Added(UnityBattleFactory owner);
        protected virtual void OnCleanAssets() { }
        protected virtual void OnLowMemory() { }
        sealed protected override void Disposing()
        {
            var owner = Owner;
            if (owner != null)
            {
                owner.Components.RemoveComponent(this);
            }
            OnDispose(owner);
        }
        protected virtual void OnDispose(UnityBattleFactory owner) { }
    }
    //-----------------------------------------------------------------------------------------------------------------        

    public interface IAssetLoadingTask : IRecyclable
    {
        bool IsAvailable { get; }
        bool IsComplete { get; }
        bool IsCanceled { get; }
        void Cancel();
        public bool IsRunning => IsAvailable && !IsComplete && !IsCanceled;
    }
    public abstract class AssetsLoaderComponent : BattleFactoryComponent
    {
        public static AssetsLoaderComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(AssetsLoaderComponent);
        protected HashMap<string, IWrapAssetsPool> wrapPool = new HashMap<string, IWrapAssetsPool>();
        protected GameObject tempPoolNode;
        protected GameObject wrapPoolNode;
        public AssetsLoaderComponent()
        {
            Instance = this;
            this.tempPoolNode = new GameObject($"TEMP-POOL[{GetType().Name}]");
            GameObject.DontDestroyOnLoad(tempPoolNode);
            this.wrapPoolNode = new GameObject($"WRAP-POOL[{GetType().Name}]");
            GameObject.DontDestroyOnLoad(wrapPoolNode);
        }
        protected internal virtual bool TryStashNode(GameObject obj, string group)
        {
            if (group == "template")
            {
                if (tempPoolNode)
                {
                    obj.transform.SetParent(tempPoolNode.transform, false);
                    return true;
                }
            }
            else
            {
                if (wrapPoolNode)
                {
                    obj.transform.SetParent(wrapPoolNode.transform, false);
                    return true;
                }
            }
            return false;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        protected override void OnDispose(UnityBattleFactory owner)
        {
            base.OnDispose(owner);
            OnCleanAssets();
            GameObject.Destroy(tempPoolNode);
            GameObject.Destroy(wrapPoolNode);
        }
        public void CleanAssets<ST>(ST st, BreakPredicate<ST, IWrapAssetsPool> fuck徐勤)
        {
            var removeArray = wrapPool.ToArray();
            for (int i = removeArray.Length - 1; i >= 0; i--)
            {
                var e = removeArray[i];
                var tempPool = e.Value;
                if (!tempPool.IsLoading && tempPool.ReferenceCount == 0 && fuck徐勤.Invoke(st, tempPool))
                {
                    tempPool.Dispose();
                    wrapPool.Remove(e.Key);
                }
            }
        }
        public void ForEachAssets<ST>(ST st, BreakPredicate<ST, IWrapAssetsPool> fuck徐勤)
        {
            var removeArray = wrapPool.ToArray();
            for (int i = removeArray.Length - 1; i >= 0; i--)
            {
                var e = removeArray[i];
                var tempPool = e.Value;
                fuck徐勤.Invoke(st, tempPool);
            }
        }
        public void CleanAssets(IWrapAssetsPool pool)
        {
            if (wrapPool.Remove(pool.file))
            {
                pool.Dispose();
            }
        }
        protected override void OnCleanAssets()
        {
            foreach (var w in wrapPool.Values)
            {
                w.Dispose();
            }
            wrapPool.Clear();
        }
        protected override void OnLowMemory()
        {
            foreach (var w in wrapPool.Values)
            {
                w.LowMemory();
            }
        }
        protected virtual UnityEngine.Object Instantiate(UnityEngine.Object obj)
        {
            var ret = GameObject.Instantiate(obj);
            return ret;
        }
        protected virtual void DestoryInstance(UnityEngine.Object obj)
        {
            GameObject.Destroy(obj);
        }
        //-----------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 加载纯资产
        /// </summary>
        /// <typeparam name="ST"></typeparam>
        /// <param name="file"></param>
        /// <param name="resType"></param>
        /// <param name="st"></param>
        /// <param name="cb"></param>
        public abstract void LoadAssets<ST>(in string file, ResourceType resType, ST st, LoadAssetsHandler<ST> cb);
        public abstract IAssetsTemplate LoadAssets(in string file, ResourceType resType);

        //-----------------------------------------------------------------------------------------------------------------
        public class AssetLoadingTaskImpl<ST> : Recyclable, IAssetLoadingTask
        {
            private ST st;
            private LoadWrapAssetHandler<ST> handler;
            public bool IsAvailable { get; private set; } = false;
            public bool IsComplete { get; private set; } = false;
            public bool IsCanceled { get; private set; } = false;

            public virtual AssetLoadingTaskImpl<ST> Init(ST st, LoadWrapAssetHandler<ST> handler)
            {
                this.st = st;
                this.handler = handler;
                this.IsAvailable = true;
                return this;
            }
            protected override void Disposing()
            {
                this.Cancel();
                this.st = default;
                this.handler = null;
                this.IsAvailable = false;
                this.IsComplete = false;
                this.IsCanceled = false;
            }

            public void NotValidate()
            {
                this.IsAvailable = false;
                this.Invoke(st, null);
            }
            public void Cancel()
            {
                if (IsComplete || IsCanceled || !IsAvailable) return;
                this.IsCanceled = true;
                this.Invoke(st, null);
            }
            public void Complete(IWrapAssets wrap)
            {
                if (IsComplete || IsCanceled || !IsAvailable) return;
                this.IsComplete = true;
                this.Invoke(st, wrap);
            }
            protected virtual void Invoke(ST st, IWrapAssets wrap)
            {
                try
                {
                    this.handler?.Invoke(st, wrap);
                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);
                }
                this.handler = null;
            }
        }
        protected class AssetLoadingTaskImpl<ST, T> : AssetLoadingTaskImpl<ST> where T : IWrapAssets
        {
            private LoadWrapAssetHandler<ST, T> handler;
            public virtual AssetLoadingTaskImpl<ST, T> Init(ST st, LoadWrapAssetHandler<ST, T> handler)
            {
                base.Init(st, null);
                this.handler = handler;
                return this;
            }
            protected override void Invoke(ST st, IWrapAssets wrap)
            {
                try
                {
                    this.handler?.Invoke(st, wrap as T);
                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);
                }
                this.handler = null;
            }
        }
        //-----------------------------------------------------------------------------------------------------------------
        public IWrapAssets GetAssetObject(in string file, ResourceType resType)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (wrapPool.TryGetValue(file, out var pool))
            {
                pool.Load(true);
                return pool.PopWrap();
            }
            else
            {
                pool = new IWrapAssetsPool(Owner, file, resType);
                wrapPool.Add(file, pool);
                pool.Load(true);
                return pool.PopWrap();
            }
        }
        public IAssetLoadingTask GetAssetObject<ST>(in string file, ResourceType resType, ST st, LoadWrapAssetHandler<ST> cb)
        {
            var task = UnityBattleFactory.ObjectPool.Alloc<AssetLoadingTaskImpl<ST>>().Init(st, cb);
            if (string.IsNullOrEmpty(file))
            {
                task.NotValidate();
                return task;
            }
            if (wrapPool.TryGetValue(file, out var pool))
            {
                if (pool.IsLoading)
                {
                    task.Retain();
                    pool.Listen(task, static (_task, wrap) =>
                    {
                        var task = (AssetLoadingTaskImpl<ST>)_task;
                        try
                        {
                            if (!task.IsCanceled)
                            {
                                task.Complete(wrap.PopWrap());
                            }
                        }
                        finally
                        {
                            task.Release();
                        }
                    });
                }
                else
                {
                    task.Complete(pool.PopWrap());
                }
            }
            else
            {
                pool = new IWrapAssetsPool(Owner, file, resType);
                wrapPool.Add(file, pool);
                pool.Listen(task, static (_task, wrap) =>
                {
                    var task = (AssetLoadingTaskImpl<ST>)_task;
                    try
                    {
                        task.Retain();
                        if (!task.IsCanceled)
                        {
                            task.Complete(wrap.PopWrap());
                        }
                    }
                    finally
                    {
                        task.Release();
                    }
                });
                pool.Load(false);
                return task;
            }
            return task;
        }
        public T GetAssetObjectAs<T>(in string file, ResourceType resType) where T : IWrapAssets
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (wrapPool.TryGetValue(file, out var pool))
            {
                pool.Load(true);
                return pool.PopWrap() as T;
            }
            else
            {
                pool = new IWrapAssetsPool(Owner, file, resType);
                wrapPool.Add(file, pool);
                pool.Load(true);
                return pool.PopWrap() as T;
            }
        }
        public IAssetLoadingTask GetAssetObjectAs<ST, T>(in string file, ResourceType resType, ST st, LoadWrapAssetHandler<ST, T> cb) where T : IWrapAssets
        {
            var task = UnityBattleFactory.ObjectPool.Alloc<AssetLoadingTaskImpl<ST, T>>().Init(st, cb);
            if (string.IsNullOrEmpty(file))
            {
                task.NotValidate();
                return task;
            }
            if (wrapPool.TryGetValue(file, out var pool))
            {
                if (pool.IsLoading)
                {
                    task.Retain();
                    pool.Listen(task, static (t, wrap) =>
                    {
                        var task = (AssetLoadingTaskImpl<ST, T>)t;
                        try
                        {
                            if (!task.IsCanceled)
                            {
                                task.Complete(wrap.PopWrap());
                            }
                        }
                        finally
                        {
                            task.Release();
                        }
                    });
                }
                else
                {
                    task.Complete(pool.PopWrap());
                }
            }
            else
            {
                pool = new IWrapAssetsPool(Owner, file, resType);
                wrapPool.Add(file, pool);
                task.Retain();
                pool.Listen(task, static (t, wrap) =>
                {
                    var task = (AssetLoadingTaskImpl<ST, T>)t;
                    try
                    {
                        if (!task.IsCanceled)
                        {
                            task.Complete(wrap.PopWrap());
                        }
                    }
                    finally
                    {
                        task.Release();
                    }
                });
                pool.Load(false);
                return task;
            }
            return task;
        }
        //-----------------------------------------------------------------------------------------------------------------
        protected virtual IWrapAssets CreateWrapAsset(IWrapAssetsPool owner, UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                return new IWrapAssetsGO(owner, go);
            }
            else
            {
                return new IWrapAssets(owner, obj);
            }
        }
        public class IWrapAssetsPool
        {
            public readonly UnityBattleFactory Factory;
            public readonly string file;
            public readonly ResourceType resType;
            private readonly Queue<(IAssetLoadingTask task, System.Action<IAssetLoadingTask, IWrapAssetsPool> cb)> _onLoad = new();
            private Stack<UnityEngine.Object> _instancing_pool = new Stack<UnityEngine.Object>();
            private IAssetsTemplate _src_assets;
            private bool _isDisposed = false;
            private bool _isLoading = false;
            private bool _isLoaded = false;
            private int _reference_count = 0;
            public int ReferenceCount => _reference_count;
            public bool IsLoading => _isLoading;
            public bool IsDisposing => _isDisposed;
            public IAssetsTemplate AssetsTemplate => _src_assets;
            internal IWrapAssetsPool(UnityBattleFactory owner, string file, ResourceType resType)
            {
                this.Factory = owner;
                this.file = file;
                this.resType = resType;
            }
            public void Listen(IAssetLoadingTask task, System.Action<IAssetLoadingTask, IWrapAssetsPool> cb)
            {
                _onLoad.Enqueue((task, cb));
            }
            internal void Dispose()
            {
                if (_isLoading) return;
                if (_isDisposed) return;
                Disposing();
                _isDisposed = true;
            }
            protected virtual void Disposing()
            {
                foreach (var ins in _instancing_pool)
                {
                    Instance.DestoryInstance(ins);
                }
                _instancing_pool.Clear();
                _src_assets?.Dispose();
                _src_assets = null;
            }
            public void LowMemory()
            {
                if (!_isLoading)
                {
                    foreach (var ins in _instancing_pool)
                    {
                        Instance.DestoryInstance(ins);
                    }
                    _instancing_pool.Clear();
                }
            }
            private void Invoke()
            {
                foreach (var q in _onLoad)
                {
                    try
                    {
                        q.cb?.Invoke(q.task, this);
                    }
                    catch (Exception err)
                    {
                        UnityEngine.Debug.LogError($"On Load Callback Error : {file}");
                        System.ExceptionExt.PrintStackTrace(err);
                    }
                }
                _onLoad.Clear();
            }
            internal void Load(bool sync)
            {
                if (!IsDisposing)
                {
                    if (!_isLoaded && !_isLoading)
                    {
                        this._isLoading = true;
                        if (sync)
                        {
                            var tuple = AssetsLoader.LoadAssets(file, resType);
                            this.OnLoaded(tuple);
                        }
                        else
                        {
                            AssetsLoader.LoadAssets(file, resType, (this), static (st, tuple) =>
                            {
                                st.OnLoaded(tuple);
                            });
                        }
                    }
                }
            }
            internal void OnLoaded(IAssetsTemplate assets)
            {
                this._src_assets = assets;
                this._isLoading = false;
                this._isLoaded = true;
                try
                {
                    if (IsDisposing)
                    {
                        try
                        {
                            this.Invoke();
                        }
                        catch (System.Exception err)
                        {
                            UnityEngine.Debug.LogError($"On Load Callback Error : {file}");
                            System.ExceptionExt.PrintStackTrace(err);
                        }
                        Disposing();
                    }
                    else if (_src_assets != null)
                    {
                        try
                        {
                            if (_src_assets.template is GameObject go && go)
                            {
                                go.SetActive(false);
                                AssetsLoader.TryStashNode(go, "template");
                                //go.transform.SetParent(.WrapPoolNode, false);
                            }
                            else
                            {

                            }
                        }
                        catch (System.Exception err)
                        {
                            System.ExceptionExt.PrintStackTrace(err);
                        }
                        try
                        {
                            this.Invoke();
                        }
                        catch (System.Exception err)
                        {
                            UnityEngine.Debug.LogError($"On Load Callback Error : {file}");
                            System.ExceptionExt.PrintStackTrace(err);
                        }
                    }
                    else
                    {
                        try
                        {
                            this.Invoke();
                        }
                        catch (System.Exception err)
                        {
                            UnityEngine.Debug.LogError($"On Load Callback Error : {file}");
                            System.ExceptionExt.PrintStackTrace(err);
                        }
                    }
                }
                finally
                {
                    _onLoad.Clear();
                }
            }
            internal IWrapAssets PopWrap()
            {
                if (IsDisposing) return null;
                if (_instancing_pool.TryPop(out var old))
                {
                    this._reference_count++;
                    return AssetsLoader.CreateWrapAsset(this, old);
                }
                else if (_src_assets?.template != null)
                {
                    this._reference_count++;
                    var ret = AssetsLoaderComponent.Instance.Instantiate(_src_assets.template);
                    return AssetsLoader.CreateWrapAsset(this, ret);
                }
                else
                {
                    return null;
                }
            }
            internal void Recycle(UnityEngine.Object w)
            {
                this._reference_count--;
                if (!IsDisposing)
                {
                    if (w)
                    {
                        _instancing_pool.Push(w);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Recycle A Null Object {w}");
                    }
                }
                else
                {
                    Instance.DestoryInstance(w);
                }
            }
        }
    }
    //-----------------------------------------------------------------------------------------------------------------
    public abstract class ResourceComponent : BattleFactoryComponent
    {
        public static ResourceComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(ResourceComponent);
        public ResourceComponent()
        {
            Instance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        public virtual bool TryGetSpine(GameObject go, out ISpine spine)
        {
            spine = null;
            return false;
        }
        public virtual void PlayKeyFrame(Transform obj, IKeyFrameProperties keyframe, System.Object owner) { }
        //---------------------------------------------------------------------------------------
        //         public abstract void GetResourceObject(UnityZone zone, BattleResourceLoaderHandler<UnityZone, IZoneResource> cb);
        //         public abstract void GetResourceObject(UnityZoneUnit unit, string resName, BattleResourceLoaderHandler<UnityZoneUnit, IUnitResourceObject> cb);
        //         public abstract void GetResourceObject(UnityZoneSpell spell, BattleResourceLoaderHandler<UnityZoneSpell, ISpellResourceObject> cb);
        //         public abstract void GetResourceObject(UnityZoneItem item, BattleResourceLoaderHandler<UnityZoneItem, IItemResourceObject> cb);
        //         public abstract void GetResourceObject(UnityZoneFlag flag, BattleResourceLoaderHandler<UnityZoneFlag, IFlagResourceObject> cb);
        //         public abstract void GetResourceObject(UnityEffectPlay effect, BattleResourceLoaderHandler<UnityEffectPlay, IEffectResourceObject> cb);
        //-----------------------------------------------------------------------------------------------------
        protected abstract IZoneResource CreateZoneRes(UnityZone zone, IWrapAssetsGO wrap);
        protected abstract IUnitResourceObject CreateUnitRes(UnityZoneUnit unit, IWrapAssetsGO wrap);
        protected abstract IItemResourceObject CreateItemRes(UnityZoneItem item, IWrapAssetsGO wrap);
        protected abstract ISpellResourceObject CreateSpellRes(UnityZoneSpell spell, IWrapAssetsGO wrap);
        protected abstract IEffectResourceObject CreateEffectRes(UnityEffectPlay effect, IWrapAssetsGO wrap);
        protected abstract IFlagResourceObject CreateFlagRes(UnityZoneFlag flag, IWrapAssetsGO wrap);
        //-----------------------------------------------------------------------------------------------------
        protected virtual void CreateResourceObject<TO, TR>(TO obj, IWrapAssets wrap, System.Func<TO, IWrapAssetsGO, TR> createRes, BattleResourceLoaderHandler<TO, TR> cb)
            where TO : IUnityBattleObject
            where TR : IResourceObject
        {
            if (wrap != null && wrap.TemplateObject != null)
            {
                try
                {
                    var res = createRes(obj, wrap as IWrapAssetsGO);
                    cb(obj, res);
                    if (res != null)
                    {
                        res.transform.SetParent(obj.transform, false);
                    }
                }
                catch (System.Exception err)
                {
                    System.ExceptionExt.PrintStackTrace(err);
                    cb(obj, default(TR), err);
                }
            }
            else
            {
                cb(obj, default(TR));
            }
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadSceneResource(UnityZone zone, BattleResourceLoaderHandler<UnityZone, IZoneResource> cb)
        {
            return AssetsLoader.GetAssetObject(zone.layer.Data.FileName, Data.ResourceType.Scene, (zone), (zone, wrap) =>
            {
                if (wrap != null && wrap.TemplateObject != null)
                {
                    try
                    {
                        var res = CreateZoneRes(zone, wrap as IWrapAssetsGO);
                        cb(zone, res);
                    }
                    catch (System.Exception err)
                    {
                        System.ExceptionExt.PrintStackTrace(err);
                        cb(zone, default(IZoneResource), err);
                    }
                }
                else
                {
                    cb(zone, default(IZoneResource));
                }
            });
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadUnitResource(UnityZoneUnit unit, string name, BattleResourceLoaderHandler<UnityZoneUnit, IUnitResourceObject> cb)
        {
            return AssetsLoader.GetAssetObject(name, Data.ResourceType.Object, (this, unit, cb), static (st, wrap) =>
            {
                st.Item1.CreateResourceObject(st.unit, wrap, st.Item1.CreateUnitRes, st.cb);
            });
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadSpellResource(UnityZoneSpell spell, BattleResourceLoaderHandler<UnityZoneSpell, ISpellResourceObject> cb)
        {
            return AssetsLoader.GetAssetObject(spell.layerSpell.Info.FileName, Data.ResourceType.Object, (this, spell, cb), static (st, wrap) =>
            {
                st.Item1.CreateResourceObject(st.spell, wrap, st.Item1.CreateSpellRes, st.cb);
            });
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadItemResource(UnityZoneItem item, BattleResourceLoaderHandler<UnityZoneItem, IItemResourceObject> cb)
        {
            return AssetsLoader.GetAssetObject(item.layerItem.AResource?.FileName, Data.ResourceType.Object, (this, item, cb), static (st, wrap) =>
            {
                st.Item1.CreateResourceObject(st.item, wrap, st.Item1.CreateItemRes, st.cb);
            });
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadFlagResource(UnityZoneFlag flag, BattleResourceLoaderHandler<UnityZoneFlag, IFlagResourceObject> cb)
        {
            return AssetsLoader.GetAssetObject(flag.ResourceName, Data.ResourceType.Object, (this, flag, cb), static (st, wrap) =>
            {
                st.Item1.CreateResourceObject(st.flag, wrap, st.Item1.CreateFlagRes, st.cb);
            });
        }
        //-----------------------------------------------------------------------------------------------------
        public virtual IAssetLoadingTask LoadEffectResource(UnityEffectPlay effect, BattleResourceLoaderHandler<UnityEffectPlay, IEffectResourceObject> cb)
        {
            return AssetsLoader.GetAssetObject(effect.File, Data.ResourceType.Effect, (this, effect, cb), static (st, wrap) =>
            {
                st.Item1.CreateResourceObject(st.effect, wrap, st.Item1.CreateEffectRes, st.cb);
            });
        }

        //-----------------------------------------------------------------------------------------------------



    }
    //-----------------------------------------------------------------------------------------------------------------
    public class TextureLoaderComponent : BattleFactoryComponent
    {
        public static TextureLoaderComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(TextureLoaderComponent);
        private HashMap<string, LoadingTexture> imageCache = new HashMap<string, LoadingTexture>();
        public TextureLoaderComponent()
        {
            Instance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        public class LoadingTexture
        {
            private Texture2D texture;
            private System.Action<Texture2D> onComplete;
            public void Listen(System.Action<Texture2D> cb)
            {
                if (texture != null)
                {
                    cb(texture);
                }
                else
                {
                    onComplete += cb;
                }
            }
            public void Invoke(Texture2D text)
            {
                texture = text;
                onComplete?.Invoke(texture);
                onComplete = null;
            }
        }
        public virtual void GetResourceImage<ST>(in string file, ST st, System.Action<ST, Texture2D> cb)
        {
            if (string.IsNullOrEmpty(file))
            {
                cb(st, null);
                return;
            }
            if (imageCache.TryGetOrNew<LoadingTexture>(file, out var icon))
            {
                icon.Listen((t) => { cb(st, t); });
                return;
            }
            else
            {
                icon.Listen((t) => { cb(st, t); });
                try
                {
                    var path = file;
                    if (path.StartsWith("/"))
                    {
                        path = $"file:///{Owner.RootPath}/{file}";
                    }
                    var www = UnityWebRequestTexture.GetTexture(path);
                    www.SendWebRequest().completed += (a) =>
                    {
                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log($"{path} : {www.error}");
                            icon.Invoke(null);
                        }
                        else
                        {
                            var text = ((DownloadHandlerTexture)www.downloadHandler).texture;
                            icon.Invoke(text);
                        }
                    };
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    cb(st, null);
                    return;
                }
            }
        }
        public virtual void GetResourceSprite<ST>(in string file, ST st, System.Action<ST, Sprite> cb)
        {
            GetResourceImage<ST>(in file, st, (st, text) =>
            {
                if (text == null) cb(st, null);
                else cb(st, Sprite.Create(text, new Rect(0, 0, text.width, text.height), new Vector2(0, 0)));
            });
        }

    }
    //-----------------------------------------------------------------------------------------------------------------
    public class AudioComponent : BattleFactoryComponent
    {
        public static AudioComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(AudioComponent);
        public AudioSource SoundSource { get; set; }
        public virtual bool SoundOn { get; set; }

        protected string lastPlayFile;
        protected PlayingBGM lastPlayBgm;
        public AudioComponent()
        {
            Instance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        public static AudioType GetAudioTypeWithFile(string file)
        {
            if (file.EndsWith(".mp3")) return AudioType.MPEG;
            if (file.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            if (file.EndsWith(".wav")) return AudioType.WAV;
            return AudioType.UNKNOWN;
        }
        public virtual void StopSound() { }

        public virtual System.IDisposable PlayBGM(UnityZone zone, in string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            var path = file;
            if (SoundSource != null)
            {
                var source = SoundSource;
                if (path.StartsWith("/"))
                {
                    if (file != lastPlayFile)
                    {
                        lastPlayFile = file;
                        lastPlayBgm?.Release();
                        path = $"file:///{Owner.RootPath}/{file}";
                        lastPlayBgm = new PlayingBGM(SoundSource, path);
                        return lastPlayBgm;
                    }
                }
            }
            return null;
        }
        public virtual System.IDisposable PlayAmbient(Transform source, ResourceType resType, string file)
        {
            return null;
        }
        public virtual System.IDisposable PlaySound(Transform source, ResourceType resType, in string file, int? durationMS)
        {
            return null;
        }
        public class PlayingBGM : System.IDisposable
        {
            public readonly AudioSource source;
            private AudioClip clip;
            private bool isDisposing = false;
            public PlayingBGM(AudioSource source, string path)
            {
                this.source = source;
                var www = UnityWebRequestMultimedia.GetAudioClip(path, GetAudioTypeWithFile(path));
                www.SendWebRequest().completed += (a) =>
                {
                    if (isDisposing)
                    {
                        if (clip != null) GameObject.Destroy(clip);
                        return;
                    }
                    try
                    {
                        if (www.result != UnityWebRequest.Result.Success)
                        {
                            Debug.Log($"{path} : {www.error}");
                        }
                        else
                        {
                            this.clip = DownloadHandlerAudioClip.GetContent(www);
                            source.clip = clip;
                            source.Play();
                        }
                    }
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                };
            }
            public void Release()
            {
                isDisposing = true;
                try
                {
                    if (clip != null) GameObject.Destroy(clip);
                    source.Stop();
                    source.clip = null;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
            public void Dispose()
            {

            }
        }
    }
    //-----------------------------------------------------------------------------------------------------------------
    public class VoxelComponent : BattleFactoryComponent
    {
        public static VoxelComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(VoxelComponent);
        public VoxelComponent()
        {
            Instance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        public virtual GameObject CreateVoxelTerrain(UnityZone zone, VoxelClientTerrain3D voxel)
        {
            var voxelTemp = zone.config.VoxelTemplateName;
            if (voxelTemp != null)
            {
                var gameObject = new GameObject("VoxelTerrain");
                gameObject.transform.SetParent(zone.childTerrainNode.transform, false);
                if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
                {
                    gameObject.SetLayer(zone.config.RayCastTerrainLayerName);
                }
                var cell_size = voxel.GridCellSize;
                if (voxel.World.Terrain is VoxelTerrain3D terrain)
                {
                    terrain.ForEachLayers(0, (layer, _) =>
                    {
                        var cell_body = CreateVoxelLayerObject(zone, layer, voxelTemp.gameObject);
                        if (cell_body != null)
                        {
                            cell_body.transform.SetParent(gameObject.transform, false);
                        }
                        return false;
                    });
                    StaticBatchingUtility.Combine(gameObject);
                }
                return gameObject;
            }
            return null;
        }
        public virtual GameObject CreateVoxelLayerObject(UnityZone zone, ITerrainLayer _layer, GameObject voxelTemp)
        {
            if (_layer is VoxelLayer layer)
            {
                var temp = voxelTemp;
                if (temp != null)
                {
                    var totalH = zone.battle.Layer.Terrain3D.TotalHeight;
                    var cell_body = UnityEngine.Object.Instantiate(temp);
                    var cell_size = layer.OwnerCell.Terrain.GridCellSize;
                    try
                    {
                        if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
                        {
                            cell_body.SetLayer(zone.config.RayCastTerrainLayerName);
                        }
                        //cell_body.layer = LayerMask.NameToLayer(zone.config.RayCastTerrainLayerName);
                    }
                    catch { }
                    cell_body.name = $"VoxelLayer[{layer.X},{layer.Y},{layer.Layer}]";
                    {
                        cell_body.transform.localPosition = new Vector3(
                          layer.X * cell_size,
                          layer.Downward,
                          totalH - layer.Y * cell_size);
                        cell_body.transform.localScale = new Vector3(cell_size, layer.Height, cell_size);
                    }
                    var cell_meshr = cell_body.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        Colors.DecodeARGB(layer.Color.ARGB, out float r, out var g, out var b, out var a);
                        cell_meshr.material.SetColor("_Color", new Color(r, g, b, a));
                        cell_meshr.material.enableInstancing = true;
                    }
                    cell_body.SetActive(true);
                    return cell_body;
                }
                else
                {
                    var cell_size = layer.OwnerCell.Terrain.GridCellSize;
                    var cell_body = new GameObject($"VoxelLayer[{layer.X},{layer.Y},{layer.Layer}]");
                    try
                    {
                        if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
                        {
                            cell_body.SetLayer(zone.config.RayCastTerrainLayerName);
                        }
                        //cell_body.layer = LayerMask.NameToLayer(zone.config.RayCastTerrainLayerName);
                    }
                    catch { }
                    var cell_mesh = CreateVoxelMesh(layer);
                    var cell_meshf = cell_body.AddComponent<MeshFilter>();
                    cell_meshf.mesh = cell_mesh;
                    var cell_meshr = cell_body.AddComponent<MeshRenderer>();
                    Colors.DecodeARGB(layer.Color.ARGB, out float r, out var g, out var b, out var a);
                    cell_meshr.material.SetColor("_Color", new Color(r, g, b, a));
                    cell_meshr.material.enableInstancing = true;
                    cell_body.transform.localPosition = new Vector3(
                      layer.X * cell_size,
                      layer.Downward,
                      layer.Y * cell_size);

                    return cell_body;
                }
            }
            return null;
        }
        public virtual Mesh CreateVoxelMesh(VoxelLayer layer)
        {
            var w = layer.OwnerCell.Terrain.GridCellSize;
            var h = layer.Height;

            Vector3[] vertices = {
            new Vector3 (0, 0, 0),
            new Vector3 (w, 0, 0),
            new Vector3 (w, h, 0),
            new Vector3 (0, h, 0),
            new Vector3 (0, h, w),
            new Vector3 (w, h, w),
            new Vector3 (w, 0, w),
            new Vector3 (0, 0, w),
            };

            int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
            };

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();

            return mesh;
        }

        public virtual GameObject CreateVoxelUnit(UnityZoneUnit obj)
        {
            var zone = obj.parent;
            if (zone.config.UnitTemplateName != null)
            {
                var layerUnit = obj.layerUnit;
                var temp = zone.config.UnitTemplateName.gameObject;
                if (temp != null)
                {
                    var size = layerUnit.BodyBlockSize;
                    var height = layerUnit.BodyHeight;
                    var cell_body = UnityEngine.Object.Instantiate(temp);
                    try
                    {
                        if (!string.IsNullOrEmpty(zone.config.RayCastObjectLayerName))
                        {
                            cell_body.layer = LayerMask.NameToLayer(zone.config.RayCastObjectLayerName);
                        }
                    }
                    catch { }
                    cell_body.name = $"{layerUnit.DisplayName}";
                    {
                        cell_body.transform.localPosition = Vector3.zero;
                        cell_body.transform.localScale = new Vector3(size * 2, height, size * 2);
                    }
                    var cell_meshr = cell_body.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        cell_meshr.material.SetColor("_Color", Color.yellow);
                    }
                    cell_body.transform.SetParent(obj.gameObject.transform, false);
                    cell_body.SetActive(true);
                    return cell_body;
                }
            }
            return null;
        }

        public virtual GameObject CreateVoxelSpell(UnityZoneSpell obj)
        {
            var zone = obj.parent;
            if (zone.config.SpellTemplateName != null)
            {
                var layerSpell = obj.layerSpell;
                var temp = zone.config.SpellTemplateName.gameObject;
                if (temp != null)
                {
                    var size = layerSpell.Info.BodySize;
                    var height = layerSpell.Info.BodyHeight;
                    var cell_body = UnityEngine.Object.Instantiate(temp);
                    //                     try
                    //                     {
                    //                         cell_body.layer = LayerMask.NameToLayer(zone.RayCastLayerName);
                    //                     }
                    //                     catch { }
                    cell_body.name = $"{layerSpell.DisplayName}";
                    switch (layerSpell.Info.BodyShape)
                    {
                        case SpellTemplate.Shape.Round:
                            cell_body.transform.localPosition = Vector3.zero;
                            cell_body.transform.localScale = new Vector3(size * 2, size * 2, size * 2);
                            break;
                        default:
                            cell_body.transform.localPosition = Vector3.zero;
                            cell_body.transform.localScale = new Vector3(size * 2, size * 2, size * 2);
                            break;
                    }
                    var cell_meshr = cell_body.GetComponentInChildren<MeshRenderer>();
                    if (cell_meshr)
                    {
                        cell_meshr.material.SetColor("_Color", Color.yellow);
                    }
                    cell_body.transform.SetParent(obj.gameObject.transform, false);
                    cell_body.SetActive(true);
                    return cell_body;
                }
            }
            return null;
        }

        public Color DEFAULT_UNIT_GIZMOS_COLOR = Color.blue;
        public Color DEFAULT_UNIT_HIT_GIZMOS_COLOR = Color.yellow;
        public Color DEFAULT_ITEM_GIZMOS_COLOR = Color.green;
        public Color DEFAULT_SPELL_GIZMOS_COLOR = Color.yellow;
        public Color DEFAULT_FLAG_GIZMOS_COLOR = Color.green;

        public virtual GameObject CreateGizmos(UnityLayerObject obj)
        {
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var zone = obj.parent;
                try
                {
                    if (obj is UnityZoneUnit unit)
                    {
                        return unit.gameObject.GetOrAddComponent<UnitGizmos>().Init(unit);
                    }
                    if (obj is UnityZoneItem item)
                    {
                        return item.gameObject.GetOrAddComponent<ItemGizmos>().Init(item);
                    }
                    if (obj is UnityZoneSpell spell)
                    {
                        return spell.gameObject.GetOrAddComponent<SpellGizmos>().Init(spell);
                    }
                    if (obj is UnityLayerRegion region)
                    {
                        return region.gameObject.GetOrAddComponent<RegionGizmos>().Init(region);
                    }
                    if (obj is UnityLayerDecoration decoration)
                    {
                        return decoration.gameObject.GetOrAddComponent<DecorationGizmos>().Init(decoration);
                    }
                    if (obj is UnityLayerPoint point)
                    {
                        return point.gameObject.GetOrAddComponent<PointGizmos>().Init(point);
                    }
                }
                catch (System.Exception err)
                {
                    Debug.LogError(err);
                }
            }
            return null;
        }
    }
    //-----------------------------------------------------------------------------------------------------------------
    public class InputComponent : BattleFactoryComponent
    {
        public static InputComponent Instance { get; private set; }
        sealed protected override System.Type ComponentType => typeof(InputComponent);
        public InputComponent()
        {
            Instance = this;
        }
        protected override void Added(UnityBattleFactory owner)
        {

        }
        public virtual Vector2 MousePosition { get => Input.mousePosition; }
        public virtual bool TryKeyboard(IBattleCamera camera)
        {
            return Input.anyKey || Input.anyKeyDown;
        }
        public virtual bool TryScreenPointToRay(IBattleCamera camera, out Ray ray)
        {
            if (camera.camera != null)
            {
                ray = camera.camera.ScreenPointToRay(Input.mousePosition);
                return true;
            }
            ray = default;
            return false;
        }
        public virtual bool IsMouseDown(out MouseButton mouse) { return InputHelper.IsMouseDown(out mouse); }
        public virtual bool IsMouseUp(out MouseButton mouse) { return InputHelper.IsMouseUp(out mouse); }
        public virtual bool IsMouseHold(out MouseButton mouse) { return InputHelper.IsMouseHold(out mouse); }
        public virtual bool IsMouseMove()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            { return true; }
            return false;
        }
        public virtual bool IsKeyDown(out BKeyCode key) { return InputHelper.IsKeyDown(out key); }
        public virtual bool IsKeyUp(out BKeyCode key) { return InputHelper.IsKeyUp(out key); }

        public virtual bool RayCastObject<T>(UnityZone zone, Ray ray, out RaycastHit hitInfo, out DeepCore.Geometry.Vector3? target, out T obj) where T : UnityLayerObject
        {
            if (!string.IsNullOrEmpty(zone.config.RayCastObjectLayerName))
            {
                var hitMask = LayerMask.GetMask(zone.config.RayCastObjectLayerName);
                if (Physics.Raycast(ray, out hitInfo, zone.config.RayCastMaxDistance, hitMask)) // Voxel Mask 6
                {
                    var trans = hitInfo.transform;
                    target = zone.UnityWorldToBattlePosition(hitInfo.point);
                    obj = null;
                    if (trans.gameObject.TryGetComponentInParent<UnityLayerObjectBeharvior>(out var mono))
                    {
                        if (mono.zoneObject is T t)
                        {
                            obj = t;
                            return true;
                        }
                    }
                    return false;
                }
            }
            else
            {
                if (Physics.Raycast(ray, out hitInfo, zone.config.RayCastMaxDistance)) // Voxel Mask 6
                {
                    var trans = hitInfo.transform;
                    target = zone.UnityWorldToBattlePosition(hitInfo.point);
                    obj = null;
                    if (trans.gameObject.TryGetComponentInParent<UnityLayerObjectBeharvior>(out var mono))
                    {
                        if (mono.zoneObject is T t)
                        {
                            obj = t;
                            return true;
                        }
                    }
                    return false;
                }
            }
            hitInfo = default;
            obj = null;
            target = null;
            return false;
        }
        public virtual bool RayCastTerrain(UnityZone zone, Ray ray, out RaycastHit hitInfo, out DeepCore.Geometry.Vector3? target)
        {
            if (!string.IsNullOrEmpty(zone.config.RayCastTerrainLayerName))
            {
                var hitMask = LayerMask.GetMask(zone.config.RayCastTerrainLayerName);
                if (Physics.Raycast(ray, out hitInfo, zone.config.RayCastMaxDistance, hitMask)) // Voxel Mask 6
                {
                    target = zone.UnityWorldToBattlePosition(hitInfo.point);
                    return true;
                }
            }
            else
            {
                if (Physics.Raycast(ray, out hitInfo, zone.config.RayCastMaxDistance)) // Voxel Mask 6
                {
                    target = zone.UnityWorldToBattlePosition(hitInfo.point);
                    return true;
                }
            }
            hitInfo = default;
            target = null;
            return false;
        }
    }
    //-----------------------------------------------------------------------------------------------------------------

    //-----------------------------------------------------------------------------------------------------------------

}

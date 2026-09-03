using DeepCore;
using DeepCore.Game3D.Host.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Protocol;
using DeepCore.Unity3D;
using DeepMetaGame.Data.ZoneGeometry;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Unity;
using DeepMetaGame.Unity.BattleView;
using DeepMetaGame.Unity.BattleView.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using DeepMetaGame.Data.Helper;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Template;

namespace DeepGame3D.Unity.BattleView
{
    public class UnityZoneObjectPool : BattleObjectPool<UnityZone>
    {
        public UnityZoneObjectPool(UnityZone owner) : base(owner) { }
    }
    public partial class UnityZone : Disposable, IUnityBattleObject
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(UnityZone));
        new public static bool EnableAlloc
        {
            get => Alloc.Enable;
            set
            {
                UnityPoolingObject.EnableAlloc = value;
                UnityZone.Alloc.Enable = value;
            }
        }
        new public static bool VerbosAlloc
        {
            get => Alloc.Verbos;
            set
            {
                UnityPoolingObject.VerbosAlloc = value;
                UnityZone.Alloc.Verbos = value;
            }
        }
        public UnityZoneObjectPool objectPool { get; }
        public UnityBattleConfig config { get; private set; }
        public GameObject gameObject { get; private set; }
        public UnityZoneBeharvior mono { get; private set; }
        public Transform transform { get => gameObject.transform; }
        public GameObject HUDRoot { get; private set; }
        public AbstractBattle battle { get; private set; }
        public LayerZone layer { get => battle.Layer; }
        public TemplateManager templates=>layer?.Templates;
        public UnityZoneSpaceTransverter Space { get; private set; }
        public UnityZone()
        {
            Alloc.RecordConstructor(GetType());
            this.objectPool = new UnityZoneObjectPool(this);
            this.Space = UnityBattleFactory.Instance.CreateZoneSpaceTransverter();
        }
        ~UnityZone()
        {
            Alloc.RecordDestructor(GetType());
        }
        protected override void RecordDisposing()
        {
            Alloc.RecordDispose(GetType());
            base.RecordDisposing();
        }
        public virtual UnityZone Init(UnityBattleConfig cfg, AbstractBattle battle)
        {
            this.config = cfg;
            this.HUDRoot = cfg.UIRoot?.gameObject;
            this.gameObject = new GameObject($"UnityZone:{battle.GetType().Name}");
            if (cfg.Root)
            {
                this.gameObject.transform.SetParent(cfg.Root.transform, false);
            }
            this.mono = this.gameObject.AddComponent<UnityZoneBeharvior>();
            this.mono.zone = this;
            this.battle = battle;
            this.InitZoneEvents();
            this.InitObjects(gameObject);
            this.battle.Start();
            return this;
        }
        public virtual void LowMemory()
        {
            if (IsDisposing) return;
            this.objectPool.LowMemory();
        }
        protected override void Disposing()
        {
            try
            {
                this.OnDispose?.Invoke(this);
            }
            catch (Exception e) { Debug.LogError(e); }
            try
            {
                this.CleanZoneEvents();
                this.ClearUpdateables();
                this.CleanLayerResource();
                this.CleanObjects();
                this.CleanCamera();
            }
            catch (Exception e) { Debug.LogError(e); }
            try
            {
                this.battle.Dispose();
            }
            catch (Exception e) { Debug.LogError(e); }
            try
            {
                this.objectPool.Dispose();
            }
            catch (Exception e) { Debug.LogError(e); }
            UnityEngine.Object.Destroy(gameObject); 
            gameObject = null;
        }
        public float CurrentDeltaTimeMS { get; private set; } = 0;
        public void Update(float deltaTimeMS)
        {
            if (IsDisposing) return;
            try
            {
                if (battle != null && !battle.IsDisposing)
                {
                    if (battle.Pause)
                    {
                        deltaTimeMS = 0;
                    }
                    this.CurrentDeltaTimeMS = deltaTimeMS;
                    UpdateObjects();
                    UpdateUpdateables();
                    UpdateKeyPress();
                    UpdateCamera();
                    //UpdateGizmos();
                }
            }
            finally
            {
                objectPool.UpdateRecycle();
            }
        }
//         protected virtual void UpdateGizmos()
//         {
//             foreach (var bv in _battleObjects.Values)
//             {
//                 bv.UpdateGizmos();
//             }
//             foreach (var bv in _battleFlags.Values)
//             {
//                 bv.UpdateGizmos();
//             }
//         }
        protected virtual void UpdateKeyPress()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                HUD.ALWAYS_SHOW_HP_BAR = !HUD.ALWAYS_SHOW_HP_BAR;
            }
        }
        protected virtual void UpdateObjects()
        {
            foreach (var bv in _battleFlags.Values)
            {
                bv.Update(this.CurrentDeltaTimeMS);
            }
            foreach (var bv in _battleObjects.Values)
            {
                if (Actor == null || Actor != bv)
                {
                    bv.Update(this.CurrentDeltaTimeMS);
                }
            }
            if (Actor != null)
            {
                Actor.Update(this.CurrentDeltaTimeMS);
            }
        }
        internal protected virtual void UpdateResource()
        {
            ModelWrap?.UpdateResource();
            //             foreach (var bv in _battleFlags.Values)
            //             {
            //                 bv.UpdateResource();
            //             }
            //             foreach (var bv in _battleObjects.Values)
            //             {
            //                 if (Actor == null || Actor != bv)
            //                 {
            //                     bv.UpdateResource();
            //                 }
            //             }
            //             if (Actor != null)
            //             {
            //                 Actor.UpdateResource();
            //             }
        }
        //----------------------------------------------------------------------------------------------------------------------------
        #region Resource
        public readonly TerrainInfo terrain = new TerrainInfo();
        public float TerrainH { get => terrain.TerrainH; private set => terrain.TerrainH = value; }
        public float TerrainW { get => terrain.TerrainW; private set => terrain.TerrainW = value; }
        public Light DefaultLight { get; set; }
        public IZoneResource ModelWrap { get; private set; }

        protected IAssetLoadingTask assetLoading;
        private IDisposable bgm;
        protected virtual void InitLayerResource(LayerZone layer)
        {
            DefaultLight = GameObject.FindObjectOfType<Light>();
            //if (!string.IsNullOrWhiteSpace(layer.Data.FileName))
            {
                this.assetLoading = UnityBattleFactory.Resource.LoadSceneResource(this, static (zone, res, err) =>
                {
                    if (zone.IsDisposing)
                    {
                        res?.Dispose();
                        return;
                    }
                    zone.ModelWrap = res;
                    if (err != null)
                    {
                        Debug.LogError($"Load Scene Resource Error : {zone.layer.Data} : {zone.layer.Data.FileName} : {err.Message}");
                    }

                    if (zone.OnLoadSceneResource != null)
                    {
                        zone.OnLoadSceneResource.Invoke(zone);
                    }
                });
            }
        }
        protected virtual void InitLayerBGM(LayerZone layer, string fileName)
        {
            //if (!string.IsNullOrWhiteSpace(layer.Data.BGM))
            {
                try
                {
                    bgm = UnityBattleFactory.Audio.PlayBGM(this, fileName);
                }
                catch (Exception err)
                {
                    Debug.LogError($"PlayBGM Error : {layer.Data} : {fileName} : {err.Message}");
                }
            }
        }
        protected virtual void InitLayerVoxelTerrain(LayerZone layer)
        {
            {
                if (string.IsNullOrEmpty(layer.Data.FileName) || Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    if (layer.Terrain3D is VoxelClientTerrain3D voxel)
                    {
                        var go = UnityBattleFactory.Voxel.CreateVoxelTerrain(this, voxel);
                        if (go)
                        {
                            if (!string.IsNullOrEmpty(layer.Data.FileName))
                            {
                                go.SetActive(false);
                            }
                            go.transform.localPosition = new Vector3(
                                  voxel.ResourceStartX,
                                  0,
                                  voxel.ResourceStartY);
                            VoxelTerrainObject = go;
                        }
                    }
                }
            }
        }
        protected virtual void CleanLayerResource()
        {
            this.assetLoading?.Dispose();
            this.assetLoading = null;
            if (ModelWrap != null)
            {
                ModelWrap.Dispose();
                ModelWrap = null;
            }
            this.bgm?.Dispose();
        }
        public GameObject VoxelTerrainObject { get; private set; }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region ZoneEvents

        public delegate void OnAddZoneObjectHandler(UnityLayerObject go);
        public delegate void OnAddZonePlayerHandler(UnityZoneActor go);
        public delegate void OnRemoveZoneObjectHandler(UnityLayerObject go);
        public delegate void UnityZoneHandler(UnityZone zone);
        public delegate void UnityZoneLayerHandler(UnityZone zone, LayerZone layer);


        public event OnAddZoneObjectHandler OnAddZoneObject;
        public event OnAddZonePlayerHandler OnAddZonePlayer;
        public event OnRemoveZoneObjectHandler OnRemoveZoneObject;
        public event UnityZoneLayerHandler OnStart;
        public event UnityZoneLayerHandler OnStop;
        public event UnityZoneHandler OnDispose;
        public event UnityZoneHandler OnLoadSceneResource;

        private HashMap<Type, List<Action<BattleNotify>>> _zoneEvens = new HashMap<Type, List<Action<BattleNotify>>>();

        protected virtual void InitZoneEvents()
        {
            this.battle.Layer.LayerInit += Layer_LayerInit;
            this.battle.Layer.ObjectEnter += Layer_ObjectEnter;
            this.battle.Layer.ObjectLeave += Layer_ObjectLeave;
            this.battle.Layer.OnChangeBGM += Layer_OnChangeBGM;
            this.battle.Layer.MessageReceived += Layer_MessageReceived;
            this.battle.Layer.LayerDispose += Layer_LayerDispose;
            this.battle.OnError += Battle_OnError;
            this.battle.OnPauseChanged += Battle_OnPauseChanged;
            ListenZoneEvent<AddEffectEvent>(ZoneEvent_AddEffectEvent);
        }
        protected virtual void CleanZoneEvents()
        {
            this.OnStart = null;
            this.OnDispose = null;
            this.OnLoadSceneResource = null;
            this.OnAddZoneObject = null;
            this.OnAddZonePlayer = null;
            this.OnRemoveZoneObject = null;
            if (this.battle.Layer != null)
            {
                this.battle.Layer.LayerInit -= Layer_LayerInit;
                this.battle.Layer.ObjectEnter -= Layer_ObjectEnter;
                this.battle.Layer.ObjectLeave -= Layer_ObjectLeave;
                this.battle.Layer.OnChangeBGM -= Layer_OnChangeBGM;
                this.battle.Layer.MessageReceived -= Layer_MessageReceived;
            }
            _zoneEvens.Clear();
        }

        public void ListenZoneEvent<T>(Action<T> action) where T : BattleNotify
        {
            var type = typeof(T);
            _zoneEvens.TryGetOrCreate(type, out var outVal, t => new List<Action<BattleNotify>>());
            {
                outVal.Add(e => action(e as T));
            }
        }
        private void Layer_LayerInit(LayerZone layer)
        {
            TerrainH = layer.Terrain3D.TotalHeight;
            TerrainW = layer.Terrain3D.TotalWidth;
            terrain.ResX = layer.Terrain3D.ResourceStartX;
            terrain.ResY = layer.Terrain3D.ResourceStartY;

            // init flags
            InitFlags();
            // init voxel terrain
            InitLayerVoxelTerrain(layer);
            // init scene resource
            InitLayerResource(layer);
            // bgm
            InitLayerBGM(layer, layer.Data.BGM);
            // init camera
            InitCamera();

            this.OnStart?.Invoke(this, layer);
            UnityBattleFactory.Instance.BeginBattle(this, layer.Data);
        }
        private void Layer_LayerDispose(LayerZone layer)
        {
            this.OnStop?.Invoke(this, layer);
            UnityBattleFactory.Instance.EndBattle(this);
        }
        private void Layer_OnChangeBGM(LayerZone layer, string filename)
        {
            // bgm            
            InitLayerBGM(layer, filename);
        }
        private void Layer_ObjectEnter(LayerZone layer, LayerZoneObject obj)
        {
            //Debug.Log($"Layer_ObjectEnter {obj.Name}");
            var go = AllocObject(obj);

            if (go != null)
            {
                _battleObjects.Add(obj.ObjectID, go);
                if (go is UnityZoneActor actor)
                {
                    this.Actor = actor;
                }
                go.Init(obj, childObjectsNode);
                if (obj is LayerUnit unit)
                {
                    if (!string.IsNullOrEmpty(config.RayCastObjectLayerName))
                    {
                        go.gameObject.SetLayer(LayerMask.NameToLayer(config.RayCastObjectLayerName));
                    }
                }
                else if (obj is LayerItem item)
                {
                    if (!string.IsNullOrEmpty(config.RayCastObjectLayerName))
                    {
                        go.gameObject.SetLayer(LayerMask.NameToLayer(config.RayCastObjectLayerName));
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(config.EffectLayerName))
                    {
                        go.gameObject.SetLayer(LayerMask.NameToLayer(config.EffectLayerName));
                    }
                }
                if (go is UnityZoneActor player)
                {
                    this.OnInitActorCamera(player);
                    this.OnAddZonePlayer?.Invoke(player);
                }
                OnAddZoneObject?.Invoke(go);

            }
        }
        private void Layer_ObjectLeave(LayerZone layer, LayerZoneObject obj)
        {
            //Debug.Log($"Layer_ObjectLeave {obj.Name}");
            if (_battleObjects.TryRemove(obj.ObjectID, out var body))
            {
                OnRemoveZoneObject?.Invoke(body);
                if (body == this.Actor)
                {
                    this.Actor = null;
                }
                body.Leave();
            }
        }
        private void Layer_MessageReceived(LayerZone layer, IBattleMessage msg)
        {
            if (msg is BattleNotify)
            {
                if (_zoneEvens.TryGetValue(msg.GetType(), out var action))
                {
                    foreach (var a in action)
                    {
                        a(msg as BattleNotify);
                    }
                }
            }
        }
        private void Battle_OnPauseChanged(AbstractBattle bc, bool pause)
        {
            foreach (var bv in _battleObjects.Values)
            {
                bv.PauseChanged(pause);
            }
        }
        private void Battle_OnError(AbstractBattle error, Exception err)
        {
            Debug.LogError($"Battle Error : {layer} : {err.Message} : {err.StackTrace}");
        }
        private void ZoneEvent_AddEffectEvent(AddEffectEvent ev)
        {
            PlayZoneEffect(ev);
        }


        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Objects
        //----------------------------------------------------------------------------------------------------------------------------
        private UnityZoneObject AllocObject(LayerZoneObject obj)
        {
            if (IsDisposing) return null;
            if (obj is LayerPlayer player)
            {
                return objectPool.AllocOrCreateAutoRelease((this, obj), static (st, pool) =>
                {
                    var zobj = UnityBattleFactory.Instance.CreateZoneObject(st.Item1, st.Item2) as UnityZoneActor;
                    return zobj;
                });
            }
            else if (obj is LayerUnit unit)
            {
                return objectPool.AllocOrCreateAutoRelease((this, obj), static (st, pool) =>
                {
                    var zobj = UnityBattleFactory.Instance.CreateZoneObject(st.Item1, st.Item2) as UnityZoneUnit;
                    return zobj;
                });
            }
            else if (obj is LayerSpell spell)
            {
                return objectPool.AllocOrCreateAutoRelease((this, obj), static (st, pool) =>
                {
                    var zobj = UnityBattleFactory.Instance.CreateZoneObject(st.Item1, st.Item2) as UnityZoneSpell;
                    return zobj;
                });
            }
            else if (obj is LayerItem item)
            {
                return objectPool.AllocOrCreateAutoRelease((this, obj), static (st, pool) =>
                {
                    var zobj = UnityBattleFactory.Instance.CreateZoneObject(st.Item1, st.Item2) as UnityZoneItem;
                    return zobj;
                });
            }
            throw new System.Exception($"Unknow layer object : {obj.GetType()}");
        }
        //----------------------------------------------------------------------------------------------------------------------------

        public GameObject childObjectsNode { get; private set; }
        public GameObject childFlagsNode { get; private set; }
        public GameObject childTerrainNode { get; private set; }
        public GameObject childEffectsNode { get; private set; }
        public GameObject childAudioNode { get; private set; }
        public UnityZoneActor Actor { get; private set; }
        private readonly HashMap<uint, UnityZoneObject> _battleObjects = new HashMap<uint, UnityZoneObject>();
        private readonly HashMap<string, UnityZoneFlag> _battleFlags = new HashMap<string, UnityZoneFlag>();
        public virtual void SetParentNode(UnityLayerObject gameObject, Transform parent)
        {
            gameObject.transform.SetParent(parent, false);
        }
        private void InitObjects(GameObject go)
        {
            this.childObjectsNode = new GameObject("objects");
            this.childFlagsNode = new GameObject("flags");
            this.childTerrainNode = new GameObject("terrain");
            this.childEffectsNode = new GameObject("effects");
            this.childAudioNode = new GameObject("audio");
            this.childObjectsNode.transform.SetParent(go.transform, false);
            this.childFlagsNode.transform.SetParent(go.transform, false);
            this.childTerrainNode.transform.SetParent(go.transform, false);
            this.childEffectsNode.transform.SetParent(go.transform, false);
            this.childAudioNode.transform.SetParent(go.transform, false);
        }

        private void InitFlags()
        {
            {
                layer.ForEachFlags(this, (UnityZone st, LayerFlag flag) =>
                {
                    var viewflag = UnityBattleFactory.Instance.CreateZoneFlag(this, flag);
                    if (viewflag != null)
                    {
                        _battleFlags.Add(flag.Name, viewflag);
                        viewflag.Init(flag, childFlagsNode);
                        OnAddZoneObject?.Invoke(viewflag);
                    }
                    return false;
                });
            }
        }

        protected virtual void CleanObjects()
        {
            Actor = null;
            foreach (var bv in _battleObjects.Values)
            {
                bv.Dispose();
                //this.objectPool.Release(bv);
            }
            foreach (var bv in _battleFlags.Values)
            {
                bv.Dispose();
            }
            _battleObjects.Clear();
            _battleFlags.Clear();
        }

        public UnityZoneObject GetObject(uint? objID)
        {
            if (objID.HasValue)
            {
                return _battleObjects.Get(objID.Value);
            }
            return null;
        }
        public bool TryGetObject(uint? objID, out UnityZoneObject obj)
        {
            if (objID.HasValue)
            {
                return _battleObjects.TryGetValue(objID.Value, out obj);
            }
            obj = null;
            return false;
        }
        public T GetObjectAs<T>(uint? objID) where T : UnityZoneObject
        {
            if (objID.HasValue)
                return _battleObjects.Get(objID.Value) as T;
            return null;
        }
        public bool TryGetObjectAs<T>(uint? objID, out T ret) where T : UnityZoneObject
        {
            if (objID.HasValue)
            {
                if (_battleObjects.TryGetValue(objID.Value, out var obj) && obj is T t)
                {
                    ret = t;
                    return true;
                }
            }
            ret = default;
            return false;
        }

        public UnityZoneObject GetObjectByName(string name)
        {
            foreach (var u in _battleObjects.Values)
            {
                if (name.Equals(u.layerZoneObject.Name))
                {
                    return u;
                }
            }
            return null;
        }
        public T GetObjectByNameAs<T>(string name) where T : UnityZoneObject
        {
            return GetObjectByName(name) as T;
        }


        public UnityZoneFlag GetFlag(string name)
        {
            return _battleFlags.Get(name);
        }
        public T GetFlagAs<T>(string name) where T : UnityZoneFlag
        {
            return _battleFlags.Get(name) as T;
        }


        public void ForEachZoneObjects(Action<UnityZoneObject> action)
        {
            foreach (var o in _battleObjects.Values)
            {
                action(o);
            }
        }
        public bool ForEachZoneObjects(BreakPredicate<UnityZoneObject> action)
        {
            foreach (var o in _battleObjects.Values)
            {
                if (action(o)) { return true; }
            }
            return false;
        }
        public void ForEachLayerFlags(Action<UnityZoneFlag> action)
        {
            foreach (var o in _battleFlags.Values)
            {
                action(o);
            }
        }
        public bool ForEachLayerFlags(BreakPredicate<UnityZoneFlag> action)
        {
            foreach (var o in _battleFlags.Values)
            {
                if (action(o)) { return true; }
            }
            return false;
        }
        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
        #region Transform

        public DeepCore.Geometry.Vector3 UnityWorldToBattlePosition(UnityEngine.Vector3 Pos)
        {
            return Space.UnityWorldToBattlePosition(terrain, Pos);
            //return terrain.UnityWorldToBattlePosition(Pos);
        }
        public Vector3 BattleToUnityWorldPosition(in DeepCore.Geometry.Vector3 p)
        {
            return Space.BattleToUnityWorldPosition(terrain, p);
        }
        public Quaternion BattleToUnityRotation(in float direction)
        {
            return Space.BattleToUnityRotation(direction);
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------------------------
    }
}
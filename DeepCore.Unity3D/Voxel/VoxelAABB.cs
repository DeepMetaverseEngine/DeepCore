using DeepCore.Unity.Camera;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.Unity3D.Voxel
{
    public class VoxelAABB : MonoBehaviour
    {
        [SerializeField]
        public Vector2 boundsWH = new Vector2(100, 100);
        [SerializeField]
        public float boundsTop = 100;
        [SerializeField]
        public float boundsBottom = -10;

        [SerializeField]
        public Color m_DebugLineColor = Color.yellow;
        [SerializeField]
        public Transform m_DebugCube;
        [SerializeField]
        public bool m_FreeCameraEnabled = false;

        [SerializeField] public float width = .5f;
        [SerializeField] public float minHeight = .25f;
        [SerializeField] public float rayWidth = .4f;

        [SerializeField] public bool useMeshColor = true;
        [SerializeField] public bool useNavMesh = true;
        [SerializeField] public bool useBoxCast = true;
        [SerializeField] public bool splitVoxels = false;
        [SerializeField] public bool singleLayer = false;
        [SerializeField] public bool autoBindMeshCollider = false;
        [SerializeField] public bool onlyMeshCollider = true;
        [SerializeField] public float raycastLimit = 20000;


        [SerializeField] public string[] ignoreLayers;
        [SerializeField] public string[] includeLayers;

        [SerializeField]
        [SerializeReference]
        public LayerTuple[] layerMap = new LayerTuple[]
        {
           new ("Default", Color.green ),
           new ("NavLayer", Color.green ),
           new ("Water", Color.blue),
           new ("Ignore Raycast", Color.black ),
           new ("Tree", Color.red ),
        };

        [Serializable]
        public class LayerTuple
        {
            [SerializeField] public string LayerName;
            [SerializeField] public Color LayerColor;
            public LayerTuple() { }
            public LayerTuple(string layerName, Color layerColor)
            {
                LayerName = layerName;
                LayerColor = layerColor;
            }
        }







        public Color color { get => m_DebugLineColor; }
        public Vector2 StartPoint()
        {
            var center = this.transform.position;
            return new Vector2(
                center.x - (boundsWH.x * 0.5f),
                center.z - (boundsWH.y * 0.5f));
        }
        public float GetMinLevel()
        {
            return transform.position.y + boundsBottom;
        }

        private Bounds GetBounds()
        {
            var c = transform.position;
            var h = (boundsTop - boundsBottom);
            var sy = c.y + boundsBottom;
            return new Bounds()
            {
                center = new(
                    c.x,
                    sy + h / 2,
                    c.z),
                extents = new(
                    boundsWH.x / 2,
                    h / 2,
                    boundsWH.y / 2)
            };
        }
        void OnDrawGizmos()
        {
            var b = this.GetBounds();
            Gizmos.color = m_DebugLineColor;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        private Camera camControl;
        private List<GameObject> lastRayBall = new List<GameObject>();

        void Start()
        {
            if (Application.isEditor && m_FreeCameraEnabled)
            {
                if (Camera.main == null)
                {
                    var camObj = new GameObject("Main Camera");
                    camObj.transform.parent = this.transform;
                    this.camControl = camObj.AddComponent<Camera>();
                    var free = camControl.gameObject.AddComponent<FreeCamera>();
                }
                else
                {
                    this.camControl = Camera.main;
                    var free = camControl.gameObject.AddComponent<FreeCamera>();
                }
            }
        }

        void Update()
        {
            if (Application.isEditor && m_FreeCameraEnabled)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    foreach (var ball in lastRayBall)
                    {
                        GameObject.Destroy(ball);
                    }
                    lastRayBall.Clear();

                    var ray = camControl.ScreenPointToRay(Input.mousePosition);
                    var hits = Physics.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, -5);
                    if (hits.Length > 0)
                    {
                        Array.Sort(hits, (a, b) => CMath.GetDirect(b.point.y - a.point.y));
                        var hitc = hits[0];
                        {
                            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            ball.transform.position = hitc.point;
                            ball.transform.localScale = Vector3.one * 0.2f;
                            lastRayBall.Add(ball);
                        }
                        var voxel = false;
                        var min = hitc.point;
                        var max = hitc.point;
                        {
                            hits = Physics.RaycastAll(hitc.point + new Vector3(0, -1000, 0), Vector3.up, 2000, -5, QueryTriggerInteraction.Collide);
                            foreach (var hit in hits)
                            {
                                voxel = true;
                                min.y = Math.Min(min.y, hit.point.y);
                                max.y = Math.Max(max.y, hit.point.y);
                            }
                            hits = Physics.RaycastAll(hitc.point + new Vector3(0, 1000, 0), Vector3.down, 2000, -5, QueryTriggerInteraction.Collide);
                            foreach (var hit in hits)
                            {
                                voxel = true;
                                min.y = Math.Min(min.y, hit.point.y);
                                max.y = Math.Max(max.y, hit.point.y);
                            }
                        }
                        if (voxel)
                        {
                            var ball = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            ball.transform.position = min + ((max - min) / 2f);
                            ball.transform.localScale = new Vector3(1, max.y - min.y, 1);
                            lastRayBall.Add(ball);
                        }
                    }
                }
            }
        }


    }

    public class VoxelTest : MonoBehaviour { }
    public class VoxelTracer : MonoBehaviour
    {
        public Transform hitTarget;
        public int iX;
        public int iY;
        public Color Color;
        public float Upward;
        public float Downward;
        public bool BaseLine;
    }


}

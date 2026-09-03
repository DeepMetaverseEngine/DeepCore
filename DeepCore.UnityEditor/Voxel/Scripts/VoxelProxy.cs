
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepCore.UnityEditor.Voxel
{
    public class VoxelProxy : MonoBehaviour
    {
        public TempVoxel[] voxels;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnMouseDown()
        {
            VoxelEditor.Instance.OnMouseDown();
        }

        public void OnMouseUp()
        {
            VoxelEditor.Instance.OnMouseUp();
        }

    }

    [Serializable]
    public class TempVoxel
    {
        public int x;
        public int y;
        public float upward;
        public float downward;
        //黑色 未部署导航网格 
        //绿色 行走导航网格 
        //红色 不可行走导航网格
        //蓝色 水
        public uint color;
        public int layer;
        public int hIndex;
        public int vIndex;
        public bool isDirty = false;
        public GameObject bindObj;
    }

}
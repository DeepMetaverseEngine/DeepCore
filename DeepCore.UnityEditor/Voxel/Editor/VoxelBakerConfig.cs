using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.UnityEditor.Voxel
{
    public class VoxelBakerConfig
    {
        public float width = .5f;
        public float minLevel = 0;
        public float minHeight = .25f;
        public bool useMeshColor = true;
        public bool useNavMesh = true;
        public bool useBoxCast = true;
        public bool autoBindMeshCollider = true;
        public float raycastLimit = 2000;

        public string ignoreLayers = "Ignore Raycast";
        public string layerBaseLine = "BASE_VOXEL";

        public string layerNavLayer = "NavLayer";
        public string layerWater = "Water";
        public string layerDummyLayer = "DummyLayer";
        public string layerNotWalkable = "Not Walkable";
    }

}

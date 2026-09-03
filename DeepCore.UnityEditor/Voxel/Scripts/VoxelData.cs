using System.Collections;
using System.Collections.Generic;
using DeepCore.IO;

namespace DeepCore.UnityEditor.Voxel
{

    public class VoxelLayer
    {
        public float upward;
        public float downward;
        //public float length;
        //黑色 未部署导航网格 
        //绿色 行走导航网格 
        //红色 不可行走导航网格
        //蓝色 水
        public uint color;
        //public bool isDirty = false;
        public bool baseline = false;
    }

    public class VoxelData
    {
        public float size;
        public int xLength;
        public int yLength;
        public float minX;
        public float minY;
        public float maxX;
        public float maxY;
        public VoxelLayer[,][] voxels;
    }

    public class VoxelMetaExternalizableFactory : MessageFactoryGenerator
    {
        public const int VoxelMeta_TypeID = 1;
        public VoxelMetaExternalizableFactory()
        {
            this.RegistExternalizable(typeof(VoxelDataMeta), VoxelMeta_TypeID);
        }
    }

    public class VoxelLayerMeta : IExternalizable
    {
        public int length;
        public float upward;
        public float downward;
        public uint color;

        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutS32(length);
            output.PutF32(upward);
            output.PutF32(downward);
            output.PutU32(color);
        }

        public virtual void ReadExternal(IInputStream input)
        {
            length = input.GetS32();
            upward = input.GetF32();
            downward = input.GetF32();
            color = input.GetU32();
        }
    }

    public class VoxelDataMeta : IExternalizable
    {
        public float size;
        public int xLength;
        public int yLength;
        public VoxelLayerMeta[,][] voxels;

        public virtual void WriteExternal(IOutputStream output)
        {
            output.PutF32(size);
            output.PutS32(xLength);
            output.PutS32(yLength);
            for (int i = 0; i < xLength; i++)
            {
                for (int j = 0; j < yLength; j++)
                {
                    var voxelTmp = voxels[i, j];
                    for (int l = 0; l < voxelTmp.Length; l++)
                    {
                        voxelTmp[l].WriteExternal(output);
                    }
                }
            }
        }

        public virtual void ReadExternal(IInputStream input)
        {
            size = input.GetF32();
            xLength = input.GetS32();
            yLength = input.GetS32();
            voxels = new VoxelLayerMeta[xLength, yLength][];
            for (int i = 0; i < xLength; i++)
            {
                for (int j = 0; j < yLength; j++)
                {
                    var voxelTmp = new VoxelLayerMeta();
                    voxelTmp.ReadExternal(input);
                    voxels[i, j] = new VoxelLayerMeta[voxelTmp.length];
                    voxels[i, j][0] = new VoxelLayerMeta();
                    voxels[i, j][0] = voxelTmp;

                    if (voxelTmp.length > 1)
                    {
                        for (int l = 1; l < voxelTmp.length; l++)
                        {
                            voxels[i, j][l] = new VoxelLayerMeta();
                            voxels[i, j][l].ReadExternal(input);
                        }
                    }
                }
            }
        }
    }

}
using DeepCore.IO;
using DeepCore.Voxel.Data;
using DeepCore.Voxel.Extensions.MagicaVoxel;
using System;
using System.IO;

namespace DeepTools.Voxel
{
    public static  class VoxelLoader
    {
        
        public static bool TryPickVoxelAsVoxelWorld(FileInfo file, out VoxelWorld world)
        {
            try
            {
                if (file.Exists)
                {
                    using (var fs = file.OpenRead())
                    {
                        return TryPickVoxelAsVoxelWorld(new InputStream(fs), out world);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
            world = null;
            return false;
        }
        public static bool TryPickVoxelAsVoxelWorld(InputStream fs, out VoxelWorld world)
        {
            try
            {
                if (fs.TryPickFileHeadASCII(MagicaVoxelFile.FILE_HEAD))
                {
                    var vox = MagicaVoxelFile.Load(fs);
                    world = vox.ConvertMagicaVoxelFileToVoxelWorld();
                    return true;
                }
                else if (fs.TryPickFileHead(VoxelWorld.FILE_HEAD))
                {
                    world = VoxelWorld.LoadFromStream(fs);
                    return true;
                }
            }
            catch (Exception ex)
            {
                ex.PrintStackTrace();
            }
            world = null;
            return false;
        }
        
    }
}

using DeepCore.IO;
using System;
using System.IO;
using System.IO.Compression;

namespace DeepCore.Voxel.Data
{
    public class VoxelStream
    {
        public static bool IsGZip(ref BitSet32 flags)
        {
            return flags.Get(0);
        }
        public static void SetGZip(ref BitSet32 flags, bool value)
        {
            flags.Set(0, value);
        }

        public static void Load(InputStream inputT, byte[] fileHead, out BitSet32 flags, Action<InputStream> load)
        {
            if (!inputT.TryLoadFileHead(fileHead))
            {
                throw new Exception("Bad File Head");
            }
            using (inputT.BeginGetHeadLength())
            {
                flags = inputT.GetStruct<BitSet32>();
                using (new BeginLoadPosition(inputT.GetStream()))
                {
                    if (IsGZip(ref flags))
                    {
                        using (var gz = new CachedGZipStream(inputT.GetStream(), CompressionMode.Decompress))
                        {
                            using (var input = new InputStream(gz, inputT.Factory))
                            {
                                load(input);
                            }
                        }
                    }
                    else
                    {
                        load(inputT);
                    }
                }
            }
        }
        public static void Save(OutputStream outputT, byte[] fileHead, BitSet32 flags, Action<OutputStream> save)
        {
            outputT.SaveFileHead(fileHead);
            using (outputT.BeginPutHeadLength())
            {
                outputT.PutStruct(flags);
                using (new BeginSavePosition(outputT.GetStream()))
                {
                    if (IsGZip(ref flags))
                    {
                        using (var gz = new CachedGZipStream(outputT.GetStream(), CompressionMode.Compress))
                        {
                            using (var output = new OutputStream(gz, outputT.Factory))
                            {
                                save(output);
                            }
                            gz.Flush();
                        }
                    }
                    else
                    {
                        save(outputT);
                    }
                }
            }
            outputT.GetStream().Flush();
        }

    }


}

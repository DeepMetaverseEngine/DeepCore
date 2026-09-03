using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Xml;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace DeepCore.Voxel.Data
{
    //     public class VoxelLayer
    //     {
    //         public float upward;
    //         public float downward;
    //         //public float length;
    //         //黑色 未部署导航网格 
    //         //绿色 行走导航网格 
    //         //红色 不可行走导航网格
    //         //蓝色 水
    //         public uint color;
    //         //public bool isDirty = false;
    //         public bool baseline = false;
    //     }
    //     public class VoxelData
    //     {
    //         public float size;
    //         public int xLength;
    //         public int yLength;
    //         public float minX;
    //         public float minY;
    //         public float maxX;
    //         public float maxY;
    //         public VoxelLayer[,][] voxels;
    //     }

    public class VoxelNodeData
    {
        /// <summary>
        /// 体素Flag值ARGB
        /// </summary>
        public uint Color;
        public float Upward;
        public float Downward;
        public bool BaseLine = false;
        public float Height { get => Upward - Downward; }
        [XmlSerializable(XmlProperty.NoSerialize)] public object state { get; set; } = null;
    }

    public class VoxelTerrainData
    {
        public float GridSize;
        public float MinHeight;
        public int XLength;
        public int YLength;
        public float MinX;
        public float MinY;
        public float MaxX;
        public float MaxY;
        public VoxelNodeData[,][] Grids;

        public int XCount { get => Grids.GetArrayRanges()[0]; }
        public int YCount { get => Grids.GetArrayRanges()[1]; }
        public float TotalX => XCount * GridSize;
        public float TotalY => YCount * GridSize;

        public void GetLength(out int xcount, out int ycount)
        {
            var range = Grids.GetArrayRanges();
            xcount = range[0];
            ycount = range[1];
        }
        public VoxelTerrainData Clip(int sx, int sy, int wc, int hc)
        {
            var ret = new VoxelTerrainData();
            ret.GridSize = this.GridSize;
            ret.Grids = new VoxelNodeData[wc, hc][];
            CUtils.ArrayCopy2D(this.Grids, sx, sy, ret.Grids, 0, 0, wc, hc);
            return ret;
        }

        public delegate void ForEachVoxelDelegate(int x, int y, int layerIndex, VoxelNodeData data);
        public void ForEachVoxelNodes(ForEachVoxelDelegate action)
        {
            for (int x = 0; x < this.XCount; x++)
            {
                for (int y = 0; y < this.YCount; y++)
                {
                    var layers = this.Grids[x, y];
                    for (int i = 0; i < layers.Length; i++)
                    {
                        var layer = layers[i];
                        action(x, y, i, layer);
                    }
                }
            }
        }

        public static uint FromARGB(int a, int r, int g, int b)
        {
            return (((uint)a & 0xFF) << 24) | (((uint)r & 0xFF) << 16) | (((uint)g & 0xFF) << 8) | (((uint)b & 0xFF));
        }
        public static uint FromRGB(int r, int g, int b)
        {
            return (((uint)0xFF000000)) | (((uint)r & 0xFF) << 16) | (((uint)g & 0xFF) << 8) | (((uint)b & 0xFF));
        }


        private static void ReadKeyValue<T>(string line, out string key, out T value)
        {
            var kv = line.Split('=');
            key = kv[0];
            value = Parser.StringToObject<T>(kv[1]);
        }


        public static VoxelBuildConfig CreateVoxelBuildConfig(VoxelTerrainData tdata = null)
        {
            if (tdata != null)
            {
                return new VoxelBuildConfig()
                {
                    VoxelMinHeight = tdata.MinHeight,
                    VoxelMinDistance = Math.Max(tdata.MinHeight, 1f),
                    StepIntercept = Math.Min(tdata.MinHeight, tdata.GridSize),
                };
            }
            return new VoxelBuildConfig();
        }

        public static VoxelTerrainData LoadFromXML(XmlDocument doc)
        {
            var ser = new XmlSerializer(false);
            ser.OnTrySetField += OnTrySetField;
            var data = ser.XmlToObject<VoxelTerrainData>(doc);
            return data;
            bool OnTrySetField(XmlSerializer ser, object data, XmlElement e)
            {
                if (data is VoxelTerrainData tdata)
                {
                    if (e.Name == "size")
                    {
                        tdata.GridSize = ser.DecodeFromXml<float>(e);
                        return true;
                    }
                    else if (e.Name == "voxels")
                    {
                        tdata.Grids = ser.DecodeFromXml<VoxelNodeData[,][]>(e);
                        return true;
                    }
                }
                else if (data is VoxelNodeData ndata)
                {
                    if (e.Name == "downward")
                    {
                        ndata.Downward = ser.DecodeFromXml<float>(e);
                        return true;
                    }
                    else if (e.Name == "upward")
                    {
                        ndata.Upward = ser.DecodeFromXml<float>(e);
                        return true;
                    }
                    else if (e.Name == "color")
                    {
                        ndata.Color = ser.DecodeFromXml<uint>(e);
                        return true;
                    }
                    else if (e.Name == "baseline")
                    {
                        ndata.BaseLine = ser.DecodeFromXml<bool>(e);
                        return true;
                    }
                }
                return false;
            }
        }


        public static VoxelTerrainData LoadFromText(System.IO.TextReader reader)
        {
            var data = new VoxelTerrainData();
            ReadKeyValue<float>(reader.ReadLine(), out var key, out data.GridSize);
            ReadKeyValue<int>(reader.ReadLine(), out key, out data.XLength);
            ReadKeyValue<int>(reader.ReadLine(), out key, out data.YLength);
            ReadKeyValue<float>(reader.ReadLine(), out key, out data.MinX);
            ReadKeyValue<float>(reader.ReadLine(), out key, out data.MinY);
            ReadKeyValue<float>(reader.ReadLine(), out key, out data.MaxX);
            ReadKeyValue<float>(reader.ReadLine(), out key, out data.MaxY);
            ReadKeyValue<float>(reader.ReadLine(), out key, out data.MinHeight);
            data.Grids = new VoxelNodeData[data.XLength, data.YLength][];
            for (int x = 0; x < data.XCount; x++)
            {
                for (int y = 0; y < data.YCount; y++)
                {
                    ReadKeyValue<int>(reader.ReadLine(), out key, out var count);
                    data.Grids[x, y] = new VoxelNodeData[count];
                    for (int i = 0; i < count; i++)
                    {
                        var layer = new VoxelNodeData();
                        ReadKeyValue<string>(reader.ReadLine(), out key, out var layerLine);
                        var line = layerLine.Split(',');
                        layer.Color = Parser.StringToObject<uint>(line[0]);
                        layer.Upward = Parser.StringToObject<float>(line[1]);
                        layer.Downward = Parser.StringToObject<float>(line[2]);
                        layer.BaseLine = Parser.StringToObject<bool>(line[3]);
                        data.Grids[x, y][i] = layer;
                    }
                }
            }
            return data;
        }
        public static bool SaveToText(VoxelTerrainData data, System.IO.TextWriter output, BreakPredicate<string, float> progress = null)
        {
            output.Append(nameof(GridSize)).Append("=").Append(data.GridSize).AppendLine();
            output.Append(nameof(XLength)).Append("=").Append(data.XCount).AppendLine();
            output.Append(nameof(YLength)).Append("=").Append(data.YCount).AppendLine();
            output.Append(nameof(MinX)).Append("=").Append(data.MinX).AppendLine();
            output.Append(nameof(MinY)).Append("=").Append(data.MinY).AppendLine();
            output.Append(nameof(MaxX)).Append("=").Append(data.MaxX).AppendLine();
            output.Append(nameof(MaxY)).Append("=").Append(data.MaxY).AppendLine();
            output.Append(nameof(MinHeight)).Append("=").Append(data.MinHeight).AppendLine();
            progress?.Invoke("", 0);
            float total = data.XCount * data.YCount;
            float count = 0;
            for (int x = 0; x < data.XCount; x++)
            {
                for (int y = 0; y < data.YCount; y++)
                {
                    output.Append(nameof(Grids)).Append($"[{x},{y}]").Append("=").Append(data.Grids[x, y].Length).AppendLine();
                    for (int i = 0; i < data.Grids[x, y].Length; i++)
                    {
                        var layer = data.Grids[x, y][i];
                        output.Append(nameof(Grids)).Append($"[{x},{y}][{i}]").Append("=");
                        output
                            .Append($"0x{layer.Color.ToString("X8")}").Append(",")
                            .Append(layer.Upward).Append(",")
                            .Append(layer.Downward).Append(",")
                            .Append(layer.BaseLine);
                        output.AppendLine();
                    }
                    count += 1f;
                    if (progress != null && progress.Invoke($"[{x},{y}]", count / total))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /*
        public static VoxelTerrainData LoadFromXML(XmlDocument doc)
        {
            var ser = new XmlSerializer(false);
            ser.OnTrySetField += OnTrySetField;
            var data = ser.XmlToObject<VoxelTerrainData>(doc);
            return data;
        }
  

        private static bool OnTrySetField(XmlSerializer ser, object data, XmlElement e)
        {
            if (data is VoxelTerrainData tdata)
            {
                if (e.Name == "size")
                {
                    tdata.GridSize = ser.DecodeFromXml<float>(e);
                    return true;
                }
                else if (e.Name == "voxels")
                {
                    tdata.Grids = ser.DecodeFromXml<VoxelNodeData[,][]>(e);
                    return true;
                }
            }
            else if (data is VoxelNodeData ndata)
            {
                if (e.Name == "downward")
                {
                    ndata.Downward = ser.DecodeFromXml<float>(e);
                    return true;
                }
                else if (e.Name == "upward")
                {
                    ndata.Upward = ser.DecodeFromXml<float>(e);
                    return true;
                }
                else if (e.Name == "color")
                {
                    ndata.Color = ser.DecodeFromXml<uint>(e);
                    return true;
                }
            }
            return false;
        }
        */

    }
}

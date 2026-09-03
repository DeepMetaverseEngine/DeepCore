using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data;
using DeepMetaGame.Data.Helper;
using System;
using System.Collections.Generic;
using static FastNoise.FastNoiseLite;

namespace FastNoise;

public class FastNoiseConfig
{
    //--------------------------------------------------------------------------
    [Desc(category: "1.General", desc: "要生成的噪声类型")] public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    [Desc(category: "1.General", desc: "噪声种子")] public int seed = 1337;
    [Desc(category: "1.General", desc: "噪声频率")] public float frequency = 0.01f;
    [Desc(category: "1.General", desc: "3D 噪声的旋转类型")] public RotationType3D rotationType3D = RotationType3D.None;
    //--------------------------------------------------------------------------
    [Desc(category: "2.Fractal", desc: "要生成的分形噪声类型")] public FractalType fractalType = FractalType.None;
    [Desc(category: "2.Fractal", desc: "分形噪声的倍频层数量（octaves）"), DependOnProperty(nameof(IsFractal))] public int fractalOctaves = 3;
    [Desc(category: "2.Fractal", desc: "分形噪声的倍频层间隔（lacunarity）"), DependOnProperty(nameof(IsFractal))] public float fractalLacunarity = 2.0f;
    [Desc(category: "2.Fractal", desc: "分形噪声的增益（gain）"), DependOnProperty(nameof(IsFractal))] public float fractalGain = 0.5f;
    [Desc(category: "2.Fractal", desc: "分形噪声的权重强度（用于加权倍频层）"), DependOnProperty(nameof(IsFractal))] public float fractalWeightedStrength = 0.0f;
    [Desc(category: "2.Fractal", desc: "分形 PingPong 效果的强度"), DependOnProperty(nameof(IsFractal))] public float fractalPingPongStrength = 2.0f;
    //--------------------------------------------------------------------------
    [Desc(category: "3.Cellular", desc: "细胞噪声使用的距离函数"), DependOnProperty(nameof(IsCellular))] public CellularDistanceFunction cellularDistanceFunction = CellularDistanceFunction.EuclideanSq;
    [Desc(category: "3.Cellular", desc: "细胞噪声的返回类型"), DependOnProperty(nameof(IsCellular))] public CellularReturnType cellularReturnType = CellularReturnType.Distance;
    [Desc(category: "3.Cellular", desc: "细胞噪声的扰动强度（jitter），影响点在格点上的偏移"), DependOnProperty(nameof(IsCellular))] public float cellularJitter = 1f;
    //--------------------------------------------------------------------------
    [Desc(category: "4.DomainWarp", desc: "DomainWarp 使用的扭曲算法")] public DomainWarpType domainWarpType = DomainWarpType.OpenSimplex2;
    [Desc(category: "4.DomainWarp", desc: "DomainWarp 的最大扭曲幅度（振幅）")] public float domainWarpAmp = 1;
    //--------------------------------------------------------------------------
    [Desc(category: "5.BoundingBox", desc: "噪声网格在 X 方向的大小")] public int sizeX = 256;
    [Desc(category: "5.BoundingBox", desc: "噪声网格在 Y 方向的大小")] public int sizeY = 256;
    [Desc(category: "5.BoundingBox", desc: "噪声网格最小值")] public float noiseTexMin = -1;
    [Desc(category: "5.BoundingBox", desc: "噪声网格最大值")] public float noiseTexMax = 10f;
    //--------------------------------------------------------------------------
    [Desc(category: "5.Preview", desc: "最小切线，用于海平面")] public float clampMin = -1;
    [Desc(category: "5.Preview", desc: "最大切线，用于山谷高地")] public float clampMax = 10f;
    [Desc(category: "5.Preview", desc: "视图缩放比例")] public float viewScale = 1f;
    [Desc(category: "5.Preview", desc: "种植稀疏度，越大越密集")] public float layerSparseness = 1f;
    //--------------------------------------------------------------------------
    public bool IsFractal => fractalType != FractalType.None;
    public bool IsCellular => noiseType == NoiseType.Cellular;
    //--------------------------------------------------------------------------
    public void Setup(FastNoiseLite noise)
    {
        noise.SetNoiseType(noiseType);
        noise.SetRotationType3D(rotationType3D);
        noise.SetSeed(seed);
        noise.SetFrequency(frequency);

        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(fractalOctaves);
        noise.SetFractalLacunarity(fractalLacunarity);
        noise.SetFractalGain(fractalGain);
        noise.SetFractalWeightedStrength(fractalWeightedStrength);
        noise.SetFractalPingPongStrength(fractalPingPongStrength);

        noise.SetCellularDistanceFunction(cellularDistanceFunction);
        noise.SetCellularReturnType(cellularReturnType);
        noise.SetCellularJitter(cellularJitter);

        noise.SetDomainWarpType(domainWarpType);
        noise.SetDomainWarpAmp(domainWarpAmp);
    }
    public float[,] GetColors(FastNoiseLite noise)
    {
        var mesh = new float[sizeX, sizeY];
        var cfg = this;
        for (int x = 0; x < cfg.sizeX; x++)
        {
            for (int y = 0; y < cfg.sizeY; y++)
            {
                var noiseV = noise.GetNoise(x, y); // -1 ~ 1
                noiseV = (noiseV + 1) / 2f; // 0 ~ 1
                var c = CMath.Clamp(noiseV * 255, 0, 255);
                mesh[x, y] = c;
            }
        }
        return mesh;
    }
    public float[,] GetMatrix(FastNoiseLite noise)
    {
        var mesh = new float[sizeX, sizeY];
        var cfg = this;
        var height = noiseTexMax - noiseTexMin;
        for (int x = 0; x < cfg.sizeX; x++)
        {
            for (int y = 0; y < cfg.sizeY; y++)
            {
                var noiseV = noise.GetNoise(x, y); // -1 ~ 1
                noiseV = (noiseV + 1) / 2f; // 0 ~ 1
                var c = (noiseV * height) + noiseTexMin;
                mesh[x, y] = c;
            }
        }
        return mesh;
    }
    public float[,] GetViewMatrix(FastNoiseLite noise)
    {
        var mesh = new float[sizeX, sizeY];
        var cfg = this;
        var height = noiseTexMax - noiseTexMin;
        for (int x = 0; x < cfg.sizeX; x++)
        {
            for (int y = 0; y < cfg.sizeY; y++)
            {
                var noiseV = noise.GetNoise(x, y); // -1 ~ 1
                noiseV = (noiseV + 1) / 2f; // 0 ~ 1
                var c = (noiseV * height) + noiseTexMin;
                mesh[x, y] = Math.Clamp(c, clampMin, clampMax) * viewScale;
            }
        }
        return mesh;
    }
    //--------------------------------------------------------------------------
}

public interface INoiseLayer
{
    string Name { get; }
    [Desc("最小高度")] float MinZ { get; }
    [Desc("最大高度")] float MaxZ { get; }
    [Desc("权重")] float Weight { get; }
    [Desc("最大数量(0表示无限)")] int MaxCount { get; }
    [Desc("尺寸")] float Size { get; }
    [Desc("间隔")] float Interval { get; }
    [Desc("地表")] bool Ground { get; }
}

public class NoiseGenerator
{
    protected List<INoiseLayer> layers = new();
    protected List<INoiseLayer> grounds = new();
    protected WeightDropList<INoiseLayer> layerGen = new WeightDropList<INoiseLayer>();
    protected FastNoiseLite noise = new FastNoiseLite();
    public FastNoiseLite Noise { get => noise; }
    public IReadOnlyList<INoiseLayer> Grounds => this.grounds;
    public IReadOnlyList<INoiseLayer> Layers => this.layers;
    public NoiseGenerator()
    {
    }
    public void Cleanup()
    {
        this.layers.Clear();
        this.grounds.Clear();
    }
    public void AddLayers(IEnumerable<INoiseLayer> layers)
    {
        foreach (var layer in layers)
        {
            AddLayer(layer);
        }
    }
    public void AddLayer(INoiseLayer layer)
    {
        if (layer.Ground)
        {
            this.grounds.Add(layer);
        }
        else
        {
            this.layers.Add(layer);
        }
    }
    protected virtual bool TryGetLayer(Random random, HashMap<INoiseLayer, int> layerCount, float height, out INoiseLayer layer)
    {
        if (layers.Count > 0)
        {
            layerGen.Clear();
            foreach (var _layer in layers)
            {
                if (height >= _layer.MinZ && height < _layer.MaxZ)
                {
                    layerGen.AddItem(_layer, _layer.Weight);
                }
            }
            if (layerGen.TryDropOnce(random, out var ret, layerCount, static (layerCount, ret) =>
            {

                if (ret.MaxCount > 0 && layerCount.TryGetValue(ret, out var exists) && exists >= ret.MaxCount)
                {
                    return true;
                }
                return false;
            }))
            {
                if (layerCount.TryGetValue(ret, out var count))
                {
                    layerCount[ret] = count + 1;
                }
                else
                {
                    layerCount[ret] = 1;
                }
                layer = ret;
                return true;
            }
        }
        layer = null;
        return false;
    }
    public float[,] GenMap(Random random, FastNoiseConfig cfg, GenMapLayer onLayerGenerated)
    {
        var layerCount = new HashMap<INoiseLayer, int>();
        cfg.Setup(noise);
        var matrix = cfg.GetMatrix(noise);
        for (int x = 0; x < cfg.sizeX; x++)
        {
            for (int y = 0; y < cfg.sizeY; y++)
            {
                if (random.NextDouble() > cfg.layerSparseness)
                {
                    onLayerGenerated.Invoke(x, y, matrix[x, y], null);
                    continue;
                }
                var n = matrix[x, y];
                if (TryGetLayer(random, layerCount, n, out var layer))
                {
                }
                onLayerGenerated.Invoke(x, y, n, layer);
            }
        }
        return matrix;
    }
    public INoiseLayer GenGround(Random random)
    {
       return random.GetRandomInList(grounds);
    }
    public delegate void GenMapLayer(int x, int y, float n, INoiseLayer layer);
}

public class NoiseGenerator<T> : NoiseGenerator where T : class, INoiseLayer
{
    new public IReadOnlyList<T> Grounds => base.Grounds.ConvertAll(t => (t as T));
    new public IReadOnlyList<T> Layers => base.Layers.ConvertAll(t => (t as T));
    public float[,] GenMapAs(Random random, FastNoiseConfig cfg, GenMapLayerAs onLayerGenerated)
    {
        return base.GenMap(random, cfg, (x, y, n, layer) =>
        {
            onLayerGenerated.Invoke(x, y, n, layer as T);
        });
    }
    public T GenGroundAs(Random random)
    {
        return random.GetRandomInList(grounds) as T;
    }
    public delegate void GenMapLayerAs(int x, int y, float n, T layer);
}

public struct AltitudeLine
{
    public float Min;
    public float Max;
    public uint ColorARGB;
    public AltitudeLine(float min, float max, uint argb)
    {
        this.Min = min;
        this.Max = max;
        this.ColorARGB = argb;
    }
}

public static class AltitudeLines
{
    static List<AltitudeLine> lineColors = new([
        new AltitudeLine(float.MinValue,0, Colors.ARGB.Blue),//低洼地/海盆,<0,#ACDF87 或水体色,浅绿 / 蓝绿,低于海平面的陆地或沿海低地
        new AltitudeLine(0, .2f,  (0xFF74A860)),//平原 / 低地,0−200,#74A860,深绿色,植被茂密的平原、三角洲
        new AltitudeLine(.2f, .5f,  (0xFF9BC27A)),//丘陵 / 盆地,200−500,#9BC27A,浅绿色,缓坡、低矮丘陵
        new AltitudeLine(.5f, 1f, (0xFFFEDD83)),//低山 / 高原边,500−1000,#FEDD83,浅黄色,地势开始抬升，植被减少
        new AltitudeLine(1, 2, (0xFFEBB15B)),//中山,1000−2000,#EBB15B,金黄 / 浅褐,明显山地，海拔较高
        new AltitudeLine(2, 3,  (0xFFC68744)),//高山,2000−3000,#C68744,黄褐色,高海拔山脉，岩石裸露
        new AltitudeLine(3, 5,  (0xFFA06434)),//极高山,3000−5000,#A06434,深咖啡色,荒漠高山、高寒地带
        new AltitudeLine(5, float.MaxValue,  (0xFFFFFFFF)),//雪线以上,>5000,#FFFFFF,纯白色,终年积雪、冰川顶峰
    ]);
    public static AltitudeLine GetLineColor(float height)
    {
        foreach (var lc in lineColors)
        {
            if (height >= lc.Min && height < lc.Max)
            {
                return lc;
            }
        }
        return lineColors[0];
    }
}

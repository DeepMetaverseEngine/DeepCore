using DeepCore.Geometry.Terrain;
using DeepCore.GUI.Data;
using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace DeepCore.GUI.Cell
{
    [MessageType(Constants.MSG_HEADER)]
    public class CPJFileSet : IExternalizable
    {
        public String ImageType;
        public bool ImageTile;
        public bool ImageGroup;
        public HashMap<String, ImagesSet> ImgTable = new HashMap<String, ImagesSet>();
        public HashMap<String, SpriteSet> SprTable = new HashMap<String, SpriteSet>();
        public HashMap<String, MapSet> MapTable = new HashMap<String, MapSet>();
        public HashMap<String, WorldSet> WorldTable = new HashMap<String, WorldSet>();
        public void ReadExternal(IInputStream input)
        {
            var data = this;
            data.ImageType = input.GetUTF();
            data.ImageTile = input.GetBool();
            data.ImageGroup = input.GetBool();
            data.ImgTable = input.GetMap<DeepCore.HashMap<string, DeepCore.GUI.Cell.ImagesSet>, string, DeepCore.GUI.Cell.ImagesSet>(static (i) => i.GetUTF(), static (i) => i.GetObj<DeepCore.GUI.Cell.ImagesSet>(), data.ImgTable);
            data.SprTable = input.GetMap<DeepCore.HashMap<string, DeepCore.GUI.Cell.SpriteSet>, string, DeepCore.GUI.Cell.SpriteSet>(static (i) => i.GetUTF(), static (i) => i.GetObj<DeepCore.GUI.Cell.SpriteSet>(), data.SprTable);
            data.MapTable = input.GetMap<DeepCore.HashMap<string, DeepCore.GUI.Cell.MapSet>, string, DeepCore.GUI.Cell.MapSet>(static (i) => i.GetUTF(), static (i) => i.GetObj<DeepCore.GUI.Cell.MapSet>(), data.MapTable);
            data.WorldTable = input.GetMap<DeepCore.HashMap<string, DeepCore.GUI.Cell.WorldSet>, string, DeepCore.GUI.Cell.WorldSet>(static (i) => i.GetUTF(), static (i) => i.GetObj<DeepCore.GUI.Cell.WorldSet>(), data.WorldTable);

        }
        public void WriteExternal(IOutputStream output)
        {
            var data = this;
            output.PutUTF(data.ImageType);
            output.PutBool(data.ImageTile);
            output.PutBool(data.ImageGroup);
            output.PutMap(data.ImgTable, static (o, v) => o.PutUTF(v), static (o, v) => o.PutObj(v));
            output.PutMap(data.SprTable, static (o, v) => o.PutUTF(v), static (o, v) => o.PutObj(v));
            output.PutMap(data.MapTable, static (o, v) => o.PutUTF(v), static (o, v) => o.PutObj(v));
            output.PutMap(data.WorldTable, static (o, v) => o.PutUTF(v), static (o, v) => o.PutObj(v));
        }
    }


    public abstract class SetObject : IExternalizable
    {
        public int Index;
        public String Name;

        public virtual void ReadExternal(IInputStream input)
        {
            var data = this;
            data.Index = input.GetS32();
            data.Name = input.GetUTF();
        }
        public virtual void WriteExternal(IOutputStream output)
        {
            var data = this;
            output.PutS32(data.Index);
            output.PutUTF(data.Name);
        }

    }


    [MessageType(Constants.MSG_HEADER + 1)]
    public class ImagesSet : SetObject
    {
        public int Count => Clips.Length;
        //         public int[] ClipsX;
        //         public int[] ClipsY;
        //         public int[] ClipsW;
        //         public int[] ClipsH;
        public String[] ClipsKey;
        public Clip[] Clips;
        public struct Clip
        {
            public int ClipX;
            public int ClipY;
            public int ClipW;
            public int ClipH;
        }

        public String Extention;
        public bool IsTiles;
        public String ImageInfo;

        public int TotalW;
        public int TotalH;
        public int SplitSize;

        /**String*/
        public String AppendData;

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var data = this;
            //data.Count = input.GetS32();
            //             data.ClipsX = input.GetArray<int>(static (i) => i.GetS32(), data.ClipsX);
            //             data.ClipsY = input.GetArray<int>(static (i) => i.GetS32(), data.ClipsY);
            //             data.ClipsW = input.GetArray<int>(static (i) => i.GetS32(), data.ClipsW);
            //             data.ClipsH = input.GetArray<int>(static (i) => i.GetS32(), data.ClipsH);
            data.Clips = input.GetArray(static (i) => i.GetStruct<Clip>(), data.Clips);
            data.ClipsKey = input.GetArray<string>(static (i) => i.GetUTF(), data.ClipsKey);
            data.Extention = input.GetUTF();
            data.IsTiles = input.GetBool();
            data.ImageInfo = input.GetUTF();
            data.TotalW = input.GetS32();
            data.TotalH = input.GetS32();
            data.SplitSize = input.GetS32();
            data.AppendData = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            var data = this;
            //output.PutS32(data.Count);
            //             output.PutArray(data.ClipsX, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.ClipsY, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.ClipsW, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.ClipsH, static (o, v) => o.PutS32(v));
            output.PutArray(data.Clips, static (o, v) => o.PutStruct(v));
            output.PutArray(data.ClipsKey, static (o, v) => o.PutUTF(v));
            output.PutUTF(data.Extention);
            output.PutBool(data.IsTiles);
            output.PutUTF(data.ImageInfo);
            output.PutS32(data.TotalW);
            output.PutS32(data.TotalH);
            output.PutS32(data.SplitSize);
            output.PutUTF(data.AppendData);
        }

        public ImagesSet()
        {
        }
        public ImagesSet(int index, String name)
        {
            this.Index = index;
            this.Name = name;
        }
        public int getIndex()
        {
            return Index;
        }

        public String getName()
        {
            return Name;
        }

        public int getCount()
        {
            return Count;
        }

        public int getClipX(int i)
        {
            return Clips[i].ClipX;
        }

        public int getClipY(int i)
        {
            return Clips[i].ClipY;
        }

        public int getClipW(int i)
        {
            return Clips[i].ClipW;
        }
        public int getClipH(int i)
        {
            return Clips[i].ClipH;
        }

        public Clip getClip(int i)
        {
            return Clips[i];
        }

        public String getClipKey(int i)
        {
            return ClipsKey[i];
        }

        public bool TryGetClip(int i, out Clip clip, out string key)
        {
            clip = Clips[i];
            key = ClipsKey[i];
            if (clip.ClipW == 0 || clip.ClipH == 0)
            {
                return false;
            }
            return true;
        }
    }


    [MessageType(Constants.MSG_HEADER + 2)]
    public class MapSet : SetObject
    {

        public String ImagesName;

        public int XCount;
        public int YCount;
        public int CellW;
        public int CellH;
        public int LayerCount;

        //         public BlockType[] BlocksType;
        //         public int[] BlocksMask;
        //         public int[] BlocksX1;
        //         public int[] BlocksY1;
        //         public int[] BlocksX2;
        //         public int[] BlocksY2;
        //         public int[] BlocksW;
        //         public int[] BlocksH;
        public MapBlock[] Blocks;
        public struct MapBlock
        {
            public BlockType BlockType;
            public int Mask;
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public int W;
            public int H;
        }

        /** [layer][y][x] */
        //         public int[,,] TerrainTile;
        //         public Trans[,,] TerrainFlip;
        //         public int[,,] TerrainFlag;
        public MapTile[,,] Terrain;
        public struct MapTile
        {
            public int TerrainTile;
            public Trans TerrainFlip;
            public int TerrainFlag;
        }

        /**String*/
        public String AppendData;


        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var data = this;
            data.ImagesName = input.GetUTF();
            data.XCount = input.GetS32();
            data.YCount = input.GetS32();
            data.CellW = input.GetS32();
            data.CellH = input.GetS32();
            data.LayerCount = input.GetS32();
            data.Blocks = input.GetArray<MapBlock>(static (i) => i.GetStruct<MapBlock>(), data.Blocks);
            //             data.BlocksType = input.GetArray<DeepCore.GUI.Cell.BlockType>(static (i) => i.GetEnum<DeepCore.GUI.Cell.BlockType>(), data.BlocksType);
            //             data.BlocksMask = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksMask);
            //             data.BlocksX1 = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksX1);
            //             data.BlocksY1 = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksY1);
            //             data.BlocksX2 = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksX2);
            //             data.BlocksY2 = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksY2);
            //             data.BlocksW = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksW);
            //             data.BlocksH = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksH);
            data.Terrain = input.GetArray<MapTile>(static (i) => i.GetStruct<MapTile>(), data.Terrain);
            //             data.TerrainTile = input.GetArray<int>(static (i) => i.GetS32(), data.TerrainTile);
            //             data.TerrainFlip = input.GetArray<Trans>(static (i) => i.GetEnum<Trans>(), data.TerrainFlip);
            //             data.TerrainFlag = input.GetArray<int>(static (i) => i.GetS32(), data.TerrainFlag);
            data.AppendData = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            var data = this;
            output.PutUTF(data.ImagesName);
            output.PutS32(data.XCount);
            output.PutS32(data.YCount);
            output.PutS32(data.CellW);
            output.PutS32(data.CellH);
            output.PutS32(data.LayerCount);
            //             output.PutArray(data.BlocksType, static (o, v) => o.PutEnum(v));
            //             output.PutArray(data.BlocksMask, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksX1, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksY1, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksX2, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksY2, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksW, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksH, static (o, v) => o.PutS32(v));
            output.PutArray(data.Blocks, static (o, v) => o.PutStruct(v));
            //             output.PutArray(data.TerrainTile, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.TerrainFlip, static (o, v) => o.PutEnum(v));
            //             output.PutArray(data.TerrainFlag, static (o, v) => o.PutS32(v));
            output.PutArray(data.Terrain, static (o, v) => o.PutStruct(v));
            output.PutUTF(data.AppendData);
        }

        public MapSet()
        {
        }
        public MapSet(int index, String name)
        {
            this.Index = index;
            this.Name = name;
        }


        public int getIndex()
        {
            return Index;
        }

        public String getName()
        {
            return Name;
        }

        public MapTile GetMapCell(int layer, int x, int y)
        {
            return Terrain[layer, y, x];
        }
    }


    [MessageType(Constants.MSG_HEADER + 3)]
    public class SpriteSet : SetObject
    {
        public String ImagesName;

        public bool ComplexMode = false;
        public int FPS = 0;
        //         public float[] PartX;
        //         public float[] PartY;
        //         public float[] PartZ;
        //         public int[] PartTileID;
        //         public Trans[] PartTileTrans;
        //         public float[] PartAlpha;
        //         public float[] PartRotate;
        //         public float[] PartScaleX;
        //         public float[] PartScaleY;
        //         public float[] PartAnchorX;
        //         public float[] PartAnchorY;
        public struct Part
        {
            public int PartTileID;
            public float PartX;
            public float PartY;
            public float PartZ;
            public Trans PartTileTrans;
            public float PartAlpha;
            public float PartRotate;
            public float PartScaleX;
            public float PartScaleY;
            public float PartAnchorX;
            public float PartAnchorY;
        }
        public Part[] Parts;
        public short[][] FrameParts;

        //         public int[] BlocksMask;
        //         public float[] BlocksX1;
        //         public float[] BlocksY1;
        //         public float[] BlocksW;
        //         public float[] BlocksH;
        public struct Block
        {
            public int BlockMask;
            public float BlockX1;
            public float BlockY1;
            public float BlockW;
            public float BlockH;
        }
        public Block[] Blocks;
        public short[][] FrameBlocks;

        public int AnimateCount => Animates.Length;
        //public String[] AnimateNames;

        public struct Frame
        {
            public short FramePartIndex;
            public short FrameCDMapIndex;
            public short FrameCDAtkIndex;
            public short FrameCDDefIndex;
            public short FrameCDExtIndex;
            public float FrameAlpha;
        }
        public class Animate
        {
            public string Name;
            public Frame[] Frames;
            public String[] FramesData;
        }
        public Animate[] Animates;

        //         public short[][] FrameAnimate;
        //         public short[][] FrameCDMap;
        //         public short[][] FrameCDAtk;
        //         public short[][] FrameCDDef;
        //         public short[][] FrameCDExt;
        //         public float[][] FrameAlpha;
        //         public String[][] FrameDatas;

        /**String*/
        public String AppendData;

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var data = this;
            data.ImagesName = input.GetUTF();
            data.ComplexMode = input.GetBool();
            data.FPS = input.GetS32();
            //             data.PartX = input.GetArray<float>(static (i) => i.GetF32(), data.PartX);
            //             data.PartY = input.GetArray<float>(static (i) => i.GetF32(), data.PartY);
            //             data.PartZ = input.GetArray<float>(static (i) => i.GetF32(), data.PartZ);
            //             data.PartTileID = input.GetArray<int>(static (i) => i.GetS32(), data.PartTileID);
            //             data.PartTileTrans = input.GetArray<DeepCore.GUI.Data.Trans>(static (i) => i.GetEnum<DeepCore.GUI.Data.Trans>(), data.PartTileTrans);
            //             data.PartAlpha = input.GetArray<float>(static (i) => i.GetF32(), data.PartAlpha);
            //             data.PartRotate = input.GetArray<float>(static (i) => i.GetF32(), data.PartRotate);
            //             data.PartScaleX = input.GetArray<float>(static (i) => i.GetF32(), data.PartScaleX);
            //             data.PartScaleY = input.GetArray<float>(static (i) => i.GetF32(), data.PartScaleY);
            //             data.PartAnchorX = input.GetArray<float>(static (i) => i.GetF32(), data.PartAnchorX);
            //             data.PartAnchorY = input.GetArray<float>(static (i) => i.GetF32(), data.PartAnchorY);
            data.Parts = input.GetArray<Part>(static (i) => i.GetStruct<Part>(), data.Parts);
            data.FrameParts = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameParts);
            //             data.BlocksMask = input.GetArray<int>(static (i) => i.GetS32(), data.BlocksMask);
            //             data.BlocksX1 = input.GetArray<float>(static (i) => i.GetF32(), data.BlocksX1);
            //             data.BlocksY1 = input.GetArray<float>(static (i) => i.GetF32(), data.BlocksY1);
            //             data.BlocksW = input.GetArray<float>(static (i) => i.GetF32(), data.BlocksW);
            //             data.BlocksH = input.GetArray<float>(static (i) => i.GetF32(), data.BlocksH);
            data.Blocks = input.GetArray<Block>(static (i) => i.GetStruct<Block>(), data.Blocks);
            data.FrameBlocks = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameBlocks);
            //data.AnimateCount = input.GetS32();
            //             data.AnimateNames = input.GetArray<string>(static (i) => i.GetUTF(), data.AnimateNames);
            //             data.FrameAnimate = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameAnimate);
            //             data.FrameCDMap = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameCDMap);
            //             data.FrameCDAtk = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameCDAtk);
            //             data.FrameCDDef = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameCDDef);
            //             data.FrameCDExt = input.GetArray<System.Int16[]>(static (i) => i.GetArray<short>(static (i) => i.GetS16()), data.FrameCDExt);
            //             data.FrameAlpha = input.GetArray<System.Single[]>(static (i) => i.GetArray<float>(static (i) => i.GetF32()), data.FrameAlpha);
            //             data.FrameDatas = input.GetArray<System.String[]>(static (i) => i.GetArray<string>(static (i) => i.GetUTF()), data.FrameDatas);
            data.Animates = input.GetArray<Animate>(static (i) =>
            {
                return new Animate()
                {
                    Name = i.GetUTF(),
                    Frames = i.GetArray<Frame>(static i => i.GetStruct<Frame>()),
                    FramesData = i.GetUTFArray(),
                };
            }, data.Animates);
            data.AppendData = input.GetUTF();
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            var data = this;
            output.PutUTF(data.ImagesName);
            output.PutBool(data.ComplexMode);
            output.PutS32(data.FPS);
            //             output.PutArray(data.PartX, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartY, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartZ, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartTileID, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.PartTileTrans, static (o, v) => o.PutEnum(v));
            //             output.PutArray(data.PartAlpha, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartRotate, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartScaleX, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartScaleY, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartAnchorX, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.PartAnchorY, static (o, v) => o.PutF32(v));
            output.PutArray(data.Parts, static (o, v) => o.PutStruct(v));
            output.PutArray(data.FrameParts, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.BlocksMask, static (o, v) => o.PutS32(v));
            //             output.PutArray(data.BlocksX1, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.BlocksY1, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.BlocksW, static (o, v) => o.PutF32(v));
            //             output.PutArray(data.BlocksH, static (o, v) => o.PutF32(v));
            output.PutArray(data.Blocks, static (o, v) => o.PutStruct(v));
            output.PutArray(data.FrameBlocks, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //output.PutS32(data.AnimateCount);
            //             output.PutArray(data.AnimateNames, static (o, v) => o.PutUTF(v));
            //             output.PutArray(data.FrameAnimate, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.FrameCDMap, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.FrameCDAtk, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.FrameCDDef, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.FrameCDExt, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutS16(v)));
            //             output.PutArray(data.FrameAlpha, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutF32(v)));
            //             output.PutArray(data.FrameDatas, static (o, v1) => o.PutArray(v1, static (o, v) => o.PutUTF(v)));
            output.PutArray(data.Animates, static (o, v) =>
            {
                o.PutUTF(v.Name);
                o.PutArray(v.Frames, static (o, v) => o.PutStruct(v));
                o.PutUTFArray(v.FramesData);
            });
            output.PutUTF(data.AppendData);
        }
        //----------------------------------------------------------------------------------------------------------
        public bool TryGetAnimateIndex(string name, out int anim)
        {
            for (int i = 0; i < AnimateCount; i++)
            {
                if (Animates[i].Name == name)
                {
                    anim = i;
                    return true;
                }
            }
            anim = -1;
            return false;
        }
        public bool TryGetAnimateName(int anim, out string name)
        {
            if (Animates != null && anim >= 0 && anim < Animates.Length)
            {
                name = Animates[anim].Name;
                return true;
            }
            name = null;
            return false;
        }
        //----------------------------------------------------------------------------------------------------------


        public delegate void ForEachFramesDelegate<ST>(ST st, Animate anim, Frame frame, Part part);

        public void ForEachFrames<ST>(ST st, ForEachFramesDelegate<ST> func)
        {
            for (int anim = 0; anim < AnimateCount; anim++)
            {
                var tanim = this.Animates[anim];
                int frameCount = tanim.Frames.Length;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    var tframe = this.Animates[anim].Frames[frame];
                    var pindex = tframe.FramePartIndex;
                    for (int i = FrameParts[pindex].Length - 1; i >= 0; --i)
                    {
                        var part = FrameParts[pindex][i];
                        var tpart = Parts[part];
                        func(st, tanim, tframe, tpart);
                    }
                }
            }
        }
        public int ForEachParts<ST>(ST st, int anim, int frame, ForEachFramesDelegate<ST> func)
        {
            int ret = 0;
            if (anim >= 0 && anim < AnimateCount)
            {
                var tanim = this.Animates[anim];
                int frameCount = tanim.Frames.Length;
                if (frame >= 0 && frame < frameCount)
                {
                    var tframe = this.Animates[anim].Frames[frame];
                    var pindex = tframe.FramePartIndex;
                    for (int i = FrameParts[pindex].Length - 1; i >= 0; --i)
                    {
                        var part = FrameParts[pindex][i];
                        var tpart = Parts[part];
                        func(st, tanim, tframe, tpart);
                        ret++;
                    }
                }
            }
            return ret;
        }
        public int GetParts(int anim, int frame, List<Part> func)
        {
            return ForEachParts(func, anim, frame, static (func, tanim, tframe, tpart) => func.Add(tpart));
        }

        public int GetFrameCount(int anim)
        {
            if (anim >= 0 && anim < AnimateCount)
            {
                return Animates[anim].Frames.Length;
            }
            return 0;
        }
        public int GetPartCount(int anim, int frame)
        {
            int ret = 0;
            if (anim >= 0 && anim < AnimateCount)
            {
                int frameCount = Animates[anim].Frames.Length;
                if (frame >= 0 && frame < frameCount)
                {
                    int index = Animates[anim].Frames[frame].FramePartIndex;
                    return FrameParts[index].Length;
                }
            }
            return ret;
        }
        public int GetMaxPartCount()
        {
            var ret = 0;
            for (int anim = 0; anim < AnimateCount; anim++)
            {
                int frameCount = Animates[anim].Frames.Length;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    int index = Animates[anim].Frames[frame].FramePartIndex;
                    ret = Math.Max(ret, FrameParts[index].Length);
                }
            }
            return ret;
        }

        //----------------------------------------------------------------------------------------------------------
        public SpriteSet()
        {
        }
        public SpriteSet(int index, String name)
        {
            this.Index = index;
            this.Name = name;
        }


        public int getIndex()
        {
            return Index;
        }

        public String getName()
        {
            return Name;
        }

        //         public int getPartImageIndex(int anim, int frame, int subpart)
        //         {
        //             return PartTileID[FrameParts[FrameAnimate[anim][frame]][subpart]];
        //         }
        // 
        //         public Trans getPartTrans(int anim, int frame, int subpart)
        //         {
        //             return PartTileTrans[FrameParts[FrameAnimate[anim][frame]][subpart]];
        //         }
        // 
        //         public float getPartX(int anim, int frame, int subpart)
        //         {
        //             return PartX[FrameParts[FrameAnimate[anim][frame]][subpart]];
        //         }
        // 
        //         public float getPartY(int anim, int frame, int subpart)
        //         {
        //             return PartY[FrameParts[FrameAnimate[anim][frame]][subpart]];
        //         }

    }


    [MessageType(Constants.MSG_HEADER + 4)]
    public class WorldSet : SetObject, IAfterExternalizable
    {
        public int GridXCount;
        public int GridYCount;
        public int GridW;
        public int GridH;
        public int Width;
        public int Height;

        public HashMap<int, SpriteObject> Sprs = new HashMap<int, SpriteObject>();
        public HashMap<int, MapObject> Maps = new HashMap<int, MapObject>();
        public HashMap<int, ImageObject> Imgs = new HashMap<int, ImageObject>();

        public HashMap<int, WaypointObject> WayPoints = new HashMap<int, WaypointObject>();
        public HashMap<int, RegionObject> Regions = new HashMap<int, RegionObject>();
        public HashMap<int, EventObject> Events = new HashMap<int, EventObject>();

        public String Data;
        /// <summary>
        /// y, x
        /// </summary>
        public int[,] Terrian;

        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            var data = this;
            data.GridXCount = input.GetS32();
            data.GridYCount = input.GetS32();
            data.GridW = input.GetS32();
            data.GridH = input.GetS32();
            data.Width = input.GetS32();
            data.Height = input.GetS32();
            data.Sprs = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.SpriteObject>, int, DeepCore.GUI.Cell.WorldSet.SpriteObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.SpriteObject>(), data.Sprs);
            data.Maps = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.MapObject>, int, DeepCore.GUI.Cell.WorldSet.MapObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.MapObject>(), data.Maps);
            data.Imgs = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.ImageObject>, int, DeepCore.GUI.Cell.WorldSet.ImageObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.ImageObject>(), data.Imgs);
            data.WayPoints = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.WaypointObject>, int, DeepCore.GUI.Cell.WorldSet.WaypointObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.WaypointObject>(), data.WayPoints);
            data.Regions = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.RegionObject>, int, DeepCore.GUI.Cell.WorldSet.RegionObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.RegionObject>(), data.Regions);
            data.Events = input.GetMap<DeepCore.HashMap<int, DeepCore.GUI.Cell.WorldSet.EventObject>, int, DeepCore.GUI.Cell.WorldSet.EventObject>(static (i) => i.GetS32(), static (i) => i.GetRawData<DeepCore.GUI.Cell.WorldSet.EventObject>(), data.Events);
            data.Data = input.GetUTF();
            data.Terrian = input.GetArray<int>(static (i) => i.GetS32(), data.Terrian);
        }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            var data = this;
            output.PutS32(data.GridXCount);
            output.PutS32(data.GridYCount);
            output.PutS32(data.GridW);
            output.PutS32(data.GridH);
            output.PutS32(data.Width);
            output.PutS32(data.Height);
            output.PutMap(data.Sprs, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutMap(data.Maps, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutMap(data.Imgs, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutMap(data.WayPoints, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutMap(data.Regions, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutMap(data.Events, static (o, v) => o.PutS32(v), static (o, v) => o.PutRawData(v));
            output.PutUTF(data.Data);
            output.PutArray(data.Terrian, static (o, v) => o.PutS32(v));
        }
        public void AfterWrite(IOutputStream output)
        {

        }
        public void AfterRead(IInputStream input)
        {
            foreach (var wp in WayPoints)
            {
                foreach (var next in wp.Value.Nexts.Keys.ToArray())
                {
                    wp.Value.Nexts.Put(next, WayPoints.Get(next));
                }
            }
        }

        public WorldSet()
        {
        }
        public WorldSet(int index, String name)
        {
            this.Index = index;
            this.Name = name;
        }


        public int getIndex()
        {
            return Index;
        }

        public String getName()
        {
            return Name;
        }

        public int getTerrainCell(int grid_x, int grid_y)
        {
            return Terrian[grid_x, grid_y];
        }


        public abstract class WorldObject : IExternalizable
        {
            public int Index;
            public int X;
            public int Y;
            public String Data;
            public virtual void ReadExternal(IInputStream input)
            {
                Index = input.GetS32();
                X = input.GetS32();
                Y = input.GetS32();
                Data = input.GetUTF();
            }
            public virtual void WriteExternal(IOutputStream output)
            {
                output.PutS32(Index);
                output.PutS32(X);
                output.PutS32(Y);
                output.PutUTF(Data);
            }
        }



        [MessageType(Constants.MSG_HEADER + 0x101)]
        public class MapObject : WorldObject
        {
            public String UnitName;
            public String MapID;
            public String ImagesID;
            public int Priority;
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                UnitName = input.GetUTF();
                MapID = input.GetUTF();
                ImagesID = input.GetUTF();
                Priority = input.GetS32();
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutUTF(UnitName);
                output.PutUTF(MapID);
                output.PutUTF(ImagesID);
                output.PutS32(Priority);
            }
        }

        [MessageType(Constants.MSG_HEADER + 0x102)]
        public class SpriteObject : WorldObject
        {
            public String UnitName;
            public String SprID;
            public String ImagesID;
            public int Anim;
            public int Frame;
            public int Priority;
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                UnitName = input.GetUTF();
                SprID = input.GetUTF();
                ImagesID = input.GetUTF();
                Anim = input.GetS32();
                Frame = input.GetS32();
                Priority = input.GetS32();
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutUTF(UnitName);
                output.PutUTF(SprID);
                output.PutUTF(ImagesID);
                output.PutS32(Anim);
                output.PutS32(Frame);
                output.PutS32(Priority);
            }
        }




        [MessageType(Constants.MSG_HEADER + 0x103)]
        public class ImageObject : WorldObject
        {
            public String UnitName;
            public String ImagesID;
            public int TileID;
            public AlignmentStyle ImgAnchor;
            public Trans ImgTrans;
            public int Priority;
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                UnitName = input.GetUTF();
                ImagesID = input.GetUTF();
                TileID = input.GetS32();
                ImgAnchor = input.GetEnum<AlignmentStyle>();
                ImgTrans = input.GetEnum<Trans>();
                Priority = input.GetS32();
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutUTF(UnitName);
                output.PutUTF(ImagesID);
                output.PutS32(TileID);
                output.PutEnum(ImgAnchor);
                output.PutEnum(ImgTrans);
                output.PutS32(Priority);
            }
        }


        [MessageType(Constants.MSG_HEADER + 0x104)]
        public class WaypointObject : WorldObject
        {
            public HashMap<int, WaypointObject> Nexts { get; set; } = new HashMap<int, WaypointObject>();
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                var nexts = input.GetArray<int>(static (i) => i.GetS32());
                foreach (var n in nexts)
                {
                    this.Nexts.Put(n, null);
                }
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutArray(this.Nexts.Keys.ToArray(), static (o, v) => o.PutS32(v));
            }
        }

        [MessageType(Constants.MSG_HEADER + 0x105)]
        public class RegionObject : WorldObject
        {
            public int W;
            public int H;
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                W = input.GetS32();
                H = input.GetS32();
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutS32(W);
                output.PutS32(H);
            }
        }

        [MessageType(Constants.MSG_HEADER + 0x106)]
        public class EventObject : WorldObject
        {
            public long ID;
            public String EventName;
            public String EventFile;
            public override void ReadExternal(IInputStream input)
            {
                base.ReadExternal(input);
                ID = input.GetS64();
                EventName = input.GetUTF();
                EventFile = input.GetUTF();
            }
            public override void WriteExternal(IOutputStream output)
            {
                base.WriteExternal(output);
                output.PutS64(ID);
                output.PutUTF(EventName);
                output.PutUTF(EventFile);
            }
        }
    }


}

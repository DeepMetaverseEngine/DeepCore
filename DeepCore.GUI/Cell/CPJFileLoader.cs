using DeepCore.GUI.Data;
using DeepCore.IO;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DeepCore.GUI.Cell
{
    public class CPJCodec : MessageFactoryGenerator
    {
        public CPJCodec()
        {
            RegistExternalizable<CPJFileSet>();
            RegistExternalizable<ImagesSet>();
            RegistExternalizable<MapSet>();
            RegistExternalizable<SpriteSet>();
            RegistExternalizable<WorldSet>();
        }
    }
    public static class CPJFileLoader
    {
        //-------------------------------------------------------------------------------------
        public static CPJFileSet LoadXML(string file)
        {
            if (Resource.TryOpenStream(file, out var stream))
            {
                return LoadXML(stream);
            }
            return null;
        }
        public static CPJFileSet LoadXML(Stream stream)
        {
            var doc = XmlUtil.LoadXML(stream);
            return LoadXML(doc);
        }
        public static CPJFileSet LoadXML(XmlDocument doc)
        {
            var cpj = new CPJFileSet();
            XmlElement element = doc.DocumentElement;
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("IMAGE_TYPE"))
                    {
                        cpj.ImageType = e.InnerText.Trim();
                    }
                    else if (e.Name.Equals("IMAGE_TILE"))
                    {
                        cpj.ImageTile = Boolean.Parse(e.InnerText.Trim());
                    }
                    else if (e.Name.Equals("IMAGE_GROUP"))
                    {
                        cpj.ImageGroup = Boolean.Parse(e.InnerText.Trim());
                    }
                    else if (e.Name.Equals("level"))
                    {
                        initLevel(cpj, e);
                    }
                    else if (e.Name.Equals("resource"))
                    {
                        initResource(cpj, e);
                    }
                }
            }
            return cpj;
        }

        //-------------------------------------------------------------------------------------
        public static IExternalizableFactory Codec { get; set; } = new CPJCodec();
        public static CPJFileSet LoadBin(string file, IExternalizableFactory codec = null)
        {
            codec = codec ?? Codec;
            if (Resource.TryOpenStream(file, out var stream))
            {
                return LoadBin(stream, codec);
            }
            return null;
        }
        public static CPJFileSet LoadBin(byte[] bytes, IExternalizableFactory codec = null)
        {
            codec = codec ?? Codec;
            using (var output = new System.IO.MemoryStream(bytes))
            {
                return LoadBin(output, codec);
            }
        }
        public static CPJFileSet LoadBin(Stream stream, IExternalizableFactory codec = null)
        {
            codec = codec ?? Codec;
            using (var output = new InputStream(stream, codec))
            {
                return LoadBin(output);
            }
        }
        public static CPJFileSet LoadBin(InputStream stream)
        {
            return stream.GetObj<CPJFileSet>();
        }

        public static void SaveToBin(CPJFileSet file, Stream stream, IExternalizableFactory codec = null)
        {
            codec = codec ?? Codec;
            using (var output = new OutputStream(stream, codec))
            {
                SaveToBin(file, output);
            }
            stream.Flush();
        }
        public static byte[] SaveToBin(CPJFileSet file, IExternalizableFactory codec = null)
        {
            codec = codec ?? Codec;
            using (var ms = new System.IO.MemoryStream())
            {
                SaveToBin(file, ms, codec);
                return ms.ToArray();
            }
        }
        public static void SaveToBin(CPJFileSet file, OutputStream output)
        {
            output.PutObj(file);
        }

        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------
        #region XML Decode 
        private static void initResource(CPJFileSet cpj, XmlElement resource)
        {
            foreach (XmlNode node in resource.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("images"))
                    {
                        ImagesSet im = initImages(cpj, e);
                        cpj.ImgTable.Add(im.Name, im);
                    }
                    else if (e.Name.Equals("map"))
                    {
                        MapSet ms = initMap(cpj, e);
                        cpj.MapTable.Add(ms.Name, ms);
                    }
                    else if (e.Name.Equals("sprite"))
                    {
                        SpriteSet ss = initSprite(cpj, e);
                        cpj.SprTable.Add(ss.Name, ss);
                    }
                }
            }
        }
        private static ImagesSet initImages(CPJFileSet cpj, XmlElement images)
        {
            ImagesSet set = new ImagesSet(
                    Parser.ParseInt(images.Attributes["index"].Value),
                    images.Attributes["name"].Value);

            //             set.Count = Parser.ParseInt(images.Attributes[("size")].Value);
            //             set.ClipsX = new int[set.Count];
            //             set.ClipsY = new int[set.Count];
            //             set.ClipsW = new int[set.Count];
            //             set.ClipsH = new int[set.Count];
            var Count = Parser.ParseInt(images.Attributes[("size")].Value);
            set.Clips = new ImagesSet.Clip[Count];
            set.ClipsKey = new String[set.Count];

            String output_file = images.Attributes["output_file"].Value;
            String output_type = images.Attributes["output_type"].Value;
            if (!string.IsNullOrEmpty(output_file))
            {
                set.Extention = output_file;
            }
            else
            {
                set.Extention = cpj.ImageType;
            }
            if (output_type.Contains("tile"))
            {
                set.IsTiles = true;
            }
            else
            {
                set.IsTiles = cpj.ImageTile;
            }

            if (images.HasAttribute("all_width"))
            {
                set.TotalW = Parser.ParseInt(images.Attributes["all_width"].Value);
            }
            if (images.HasAttribute("all_height"))
            {
                set.TotalH = Parser.ParseInt(images.Attributes["all_height"].Value);
            }
            if (images.HasAttribute("total_split"))
            {
                set.SplitSize = Parser.ParseInt(images.Attributes["total_split"].Value);
            }

            foreach (XmlNode node in images.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("clip"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        //                         set.ClipsX[index] = Parser.ParseInt(e.Attributes["x"].Value);
                        //                         set.ClipsY[index] = Parser.ParseInt(e.Attributes["y"].Value);
                        //                         set.ClipsW[index] = Parser.ParseInt(e.Attributes["width"].Value);
                        //                         set.ClipsH[index] = Parser.ParseInt(e.Attributes["height"].Value);
                        set.Clips[index] = new ImagesSet.Clip()
                        {
                            ClipX = Parser.ParseInt(e.Attributes["x"].Value),
                            ClipY = Parser.ParseInt(e.Attributes["y"].Value),
                            ClipW = Parser.ParseInt(e.Attributes["width"].Value),
                            ClipH = Parser.ParseInt(e.Attributes["height"].Value),
                        };
                        set.ClipsKey[index] = e.Attributes["data"].Value;
                    }
                    else if (e.Name.Equals("ImageInfo"))
                    {
                        set.ImageInfo = e.Value;
                    }
                    else if (e.Name.Equals("Append"))
                    {
                        set.AppendData = GetArray1DLines(e.Attributes["data"].Value);
                    }
                }
            }

            return set;
        }

        private static MapSet initMap(CPJFileSet cpj, XmlElement map)
        {
            MapSet set = new MapSet(
                    Parser.ParseInt(map.Attributes["index"].Value),
                    map.Attributes["name"].Value);

            set.ImagesName = map.Attributes["images_name"].Value;
            set.XCount = Parser.ParseInt(map.Attributes["xcount"].Value);
            set.YCount = Parser.ParseInt(map.Attributes["ycount"].Value);
            set.CellW = Parser.ParseInt(map.Attributes["cellw"].Value);
            set.CellH = Parser.ParseInt(map.Attributes["cellh"].Value);
            set.LayerCount = Parser.ParseInt(map.Attributes["layer_count"].Value);
            int cdCount = Parser.ParseInt(map.Attributes["cd_part_count"].Value);

            //             set.BlocksType = new BlockType[cdCount];
            //             set.BlocksMask = new int[cdCount];
            //             set.BlocksX1 = new int[cdCount];
            //             set.BlocksY1 = new int[cdCount];
            //             set.BlocksX2 = new int[cdCount];
            //             set.BlocksY2 = new int[cdCount];
            //             set.BlocksW = new int[cdCount];
            //             set.BlocksH = new int[cdCount];
            set.Blocks = new MapSet.MapBlock[cdCount];

            //             set.TerrainTile = new int[set.LayerCount, set.YCount, set.XCount];
            //             set.TerrainFlip = new int[set.LayerCount, set.YCount, set.XCount];
            //             set.TerrainFlag = new int[set.LayerCount, set.YCount, set.XCount];
            set.Terrain = new MapSet.MapTile[set.LayerCount, set.YCount, set.XCount];

            foreach (XmlNode node in map.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("cd_part"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        //                         set.BlocksType[index] = "rect".Equals(e.Attributes["type"].Value) ? BlockType.CD_TYPE_RECT : BlockType.CD_TYPE_LINE;
                        //                         set.BlocksMask[index] = Parser.ParseInt(e.Attributes["mask"].Value);
                        //                         set.BlocksX1[index] = Parser.ParseInt(e.Attributes["x1"].Value);
                        //                         set.BlocksY1[index] = Parser.ParseInt(e.Attributes["y1"].Value);
                        //                         set.BlocksX2[index] = Parser.ParseInt(e.Attributes["x2"].Value);
                        //                         set.BlocksY2[index] = Parser.ParseInt(e.Attributes["y2"].Value);
                        //                         set.BlocksW[index] = Parser.ParseInt(e.Attributes["width"].Value);
                        //                         set.BlocksH[index] = Parser.ParseInt(e.Attributes["height"].Value);
                        set.Blocks[index] = new MapSet.MapBlock()
                        {
                            BlockType = "rect".Equals(e.Attributes["type"].Value) ? BlockType.CD_TYPE_RECT : BlockType.CD_TYPE_LINE,
                            Mask = Parser.ParseInt(e.Attributes["mask"].Value),
                            X1 = Parser.ParseInt(e.Attributes["x1"].Value),
                            Y1 = Parser.ParseInt(e.Attributes["y1"].Value),
                            X2 = Parser.ParseInt(e.Attributes["x2"].Value),
                            Y2 = Parser.ParseInt(e.Attributes["y2"].Value),
                            W = Parser.ParseInt(e.Attributes["width"].Value),
                            H = Parser.ParseInt(e.Attributes["height"].Value),
                        };
                    }
                    else if (e.Name.Equals("layer"))
                    {
                        int layerIndex = Parser.ParseInt(e.Attributes["index"].Value);
                        String[] tile_matrix = GetArray2D(e.Attributes["tile_matrix"].Value);
                        String[] flip_matrix = GetArray2D(e.Attributes["flip_matrix"].Value);
                        String[] flag_matrix = GetArray2D(e.Attributes["flag_matrix"].Value);
                        for (int y = 0; y < set.YCount; y++)
                        {
                            String[] tline = tile_matrix[y].Split(',');
                            String[] fline = flip_matrix[y].Split(',');
                            String[] cline = flag_matrix[y].Split(',');
                            for (int x = 0; x < set.XCount; x++)
                            {
                                //                                 set.TerrainTile[layerIndex, y, x] = Parser.ParseInt(tline[x]);
                                //                                 set.TerrainFlip[layerIndex, y, x] = Parser.ParseInt(fline[x]);
                                //                                 set.TerrainFlag[layerIndex, y, x] = Parser.ParseInt(cline[x]);
                                set.Terrain[layerIndex, y, x] = new MapSet.MapTile()
                                {
                                    TerrainTile = Parser.ParseInt(tline[x]),
                                    TerrainFlip = (Trans)Parser.ParseInt(fline[x]),
                                    TerrainFlag = Parser.ParseInt(cline[x]),
                                };
                            }
                        }
                    }
                    else if (e.Name.Equals("Append"))
                    {
                        set.AppendData = GetArray1DLines(e.Attributes["data"].Value);
                    }
                }
            }

            return set;
        }

        private static SpriteSet initSprite(CPJFileSet cpj, XmlElement sprite)
        {
            SpriteSet set = new SpriteSet(
                    Parser.ParseInt(sprite.Attributes["index"].Value),
                    sprite.Attributes["name"].Value);

            set.ImagesName = sprite.Attributes["images_name"].Value;
            if (sprite.HasAttribute("complexMode"))
            {
                set.ComplexMode = Boolean.Parse(sprite.Attributes["complexMode"].Value);
            }
            if (sprite.HasAttribute("fps"))
            {
                set.FPS = Parser.ParseInt(sprite.Attributes["fps"].Value);
            }
            int scenePartCount = Parser.ParseInt(sprite.Attributes["scene_part_count"].Value);
            int sceneFrameCount = Parser.ParseInt(sprite.Attributes["scene_frame_count"].Value);
            int cdCount = Parser.ParseInt(sprite.Attributes["cd_part_count"].Value);
            int collidesCount = Parser.ParseInt(sprite.Attributes["cd_frame_count"].Value);
            int animateCount = Parser.ParseInt(sprite.Attributes["animate_count"].Value);

            //             set.PartX = new float[scenePartCount];
            //             set.PartY = new float[scenePartCount];
            //             set.PartZ = new float[scenePartCount];
            //             set.PartTileID = new int[scenePartCount];
            //             set.PartTileTrans = new Trans[scenePartCount];
            //             set.PartAlpha = new float[scenePartCount];
            //             set.PartRotate = new float[scenePartCount];
            //             set.PartScaleX = new float[scenePartCount];
            //             set.PartScaleY = new float[scenePartCount];
            //             set.PartAnchorX = new float[scenePartCount];
            //             set.PartAnchorY = new float[scenePartCount];
            set.Parts = new SpriteSet.Part[scenePartCount];
            set.FrameParts = new short[sceneFrameCount][];

            //             set.BlocksMask = new int[cdCount];
            //             set.BlocksX1 = new float[cdCount];
            //             set.BlocksY1 = new float[cdCount];
            //             set.BlocksW = new float[cdCount];
            //             set.BlocksH = new float[cdCount];
            set.Blocks = new SpriteSet.Block[cdCount];
            set.FrameBlocks = new short[collidesCount][];

            //             set.AnimateCount = animateCount;
            //             set.AnimateNames = new String[animateCount];
            //             set.FrameAnimate = new short[animateCount][];
            //             set.FrameAlpha = new float[animateCount][];
            //             set.FrameCDMap = new short[animateCount][];
            //             set.FrameCDAtk = new short[animateCount][];
            //             set.FrameCDDef = new short[animateCount][];
            //             set.FrameCDExt = new short[animateCount][];
            //             set.FrameDatas = new String[animateCount][];
            set.Animates = new SpriteSet.Animate[animateCount];

            //NodeList list = sprite.getChildNodes();

            foreach (XmlNode node in sprite.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("scene_part"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        //                         set.PartTileID[index] = Parser.ParseInt(e.Attributes["tile"].Value);
                        //                         set.PartX[index] = Parser.ParseFloat(e.Attributes["x"].Value);
                        //                         set.PartY[index] = Parser.ParseFloat(e.Attributes["y"].Value);
                        //                         set.PartZ[index] = Parser.ParseFloat(e.Attributes["z"].Value);
                        //                         set.PartTileTrans[index] = (Trans)Parser.ParseByte(e.Attributes["trans"].Value);
                        //                         set.PartAlpha[index] = Parser.ParseFloat(e.Attributes["alpha"].Value);
                        //                         set.PartRotate[index] = Parser.ParseFloat(e.Attributes["rotate"].Value);
                        //                         set.PartScaleX[index] = Parser.ParseFloat(e.Attributes["scaleX"].Value);
                        //                         set.PartScaleY[index] = Parser.ParseFloat(e.Attributes["scaleY"].Value);
                        //                         if (e.HasAttribute("anchorX"))
                        //                         {
                        //                             set.PartAnchorX[index] = Parser.ParseFloat(e.Attributes["anchorX"].Value);
                        //                             set.PartAnchorY[index] = Parser.ParseFloat(e.Attributes["anchorY"].Value);
                        //                         }
                        set.Parts[index] = new SpriteSet.Part()
                        {
                            PartTileID = Parser.ParseInt(e.Attributes["tile"].Value),
                            PartX = Parser.ParseFloat(e.Attributes["x"].Value),
                            PartY = Parser.ParseFloat(e.Attributes["y"].Value),
                            PartZ = Parser.ParseFloat(e.Attributes["z"].Value),
                            PartTileTrans = (Trans)Parser.ParseByte(e.Attributes["trans"].Value),
                            PartAlpha = Parser.ParseFloat(e.Attributes["alpha"].Value),
                            PartRotate = Parser.ParseFloat(e.Attributes["rotate"].Value),
                            PartScaleX = Parser.ParseFloat(e.Attributes["scaleX"].Value),
                            PartScaleY = Parser.ParseFloat(e.Attributes["scaleY"].Value),
                        };
                        if (e.HasAttribute("anchorX"))
                        {
                            set.Parts[index].PartAnchorX = Parser.ParseFloat(e.Attributes["anchorX"].Value);
                            set.Parts[index].PartAnchorY = Parser.ParseFloat(e.Attributes["anchorY"].Value);
                        }
                    }
                    else if (e.Name.Equals("scene_frame"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        int frameCount = Parser.ParseInt(e.Attributes["data_size"].Value);
                        set.FrameParts[index] = new short[frameCount];
                        if (frameCount > 0)
                        {
                            String[] data = e.Attributes["data"].Value.Split(',');
                            for (int f = 0; f < frameCount; f++)
                            {
                                set.FrameParts[index][f] = Parser.ParseShort(data[f]);
                            }
                        }
                    }
                    else if (e.Name.Equals("cd_part"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        //                         set.BlocksMask[index] = Parser.ParseInt(e.Attributes["mask"].Value);
                        //                         set.BlocksX1[index] = Parser.ParseFloat(e.Attributes["x1"].Value);
                        //                         set.BlocksY1[index] = Parser.ParseFloat(e.Attributes["y1"].Value);
                        //                         set.BlocksW[index] = Parser.ParseFloat(e.Attributes["width"].Value);
                        //                         set.BlocksH[index] = Parser.ParseFloat(e.Attributes["height"].Value);
                        set.Blocks[index] = new SpriteSet.Block()
                        {
                            BlockMask = Parser.ParseInt(e.Attributes["mask"].Value),
                            BlockX1 = Parser.ParseFloat(e.Attributes["x1"].Value),
                            BlockY1 = Parser.ParseFloat(e.Attributes["y1"].Value),
                            BlockW = Parser.ParseFloat(e.Attributes["width"].Value),
                            BlockH = Parser.ParseFloat(e.Attributes["height"].Value),
                        };
                    }
                    else if (e.Name.Equals("cd_frame"))
                    {
                        int index = Parser.ParseInt(e.Attributes["index"].Value);
                        int frameCount = Parser.ParseInt(e.Attributes["data_size"].Value);
                        set.FrameBlocks[index] = new short[frameCount];
                        if (frameCount > 0)
                        {
                            String[] data = e.Attributes["data"].Value.Split(',');
                            for (int f = 0; f < frameCount; f++)
                            {
                                set.FrameBlocks[index][f] = Parser.ParseShort(data[f]);
                            }
                        }
                    }
                    else if (e.Name.Equals("frames"))
                    {
                        var AnimateNamesReader = new DeepCore.IO.TextInputStream(
                            new StringReader(e.Attributes["names"].Value), null);
                        String[] frame_counts = e.Attributes["counts"].Value.Split(',');
                        String[] frame_animate = GetArray2D(e.Attributes["animates"].Value);
                        String[] frame_cd_map = GetArray2D(e.Attributes["cd_map"].Value);
                        String[] frame_cd_atk = GetArray2D(e.Attributes["cd_atk"].Value);
                        String[] frame_cd_def = GetArray2D(e.Attributes["cd_def"].Value);
                        String[] frame_cd_ext = GetArray2D(e.Attributes["cd_ext"].Value);
                        String[] frame_alpha = GetArray2D(e.Attributes["alpha"].Value);

                        for (int i = 0; i < animateCount; i++)
                        {
                            var anim = set.Animates[i] = new SpriteSet.Animate();
                            //set.AnimateNames[i] = AnimateNamesReader.GetUTF();
                            anim.Name = AnimateNamesReader.GetUTF();
                            int frameCount = Parser.ParseInt(frame_counts[i]);
                            String[] animate = frame_animate[i].Split(',');
                            String[] cd_map = frame_cd_map[i].Split(',');
                            String[] cd_atk = frame_cd_atk[i].Split(',');
                            String[] cd_def = frame_cd_def[i].Split(',');
                            String[] cd_ext = frame_cd_ext[i].Split(',');
                            String[] alpha = frame_alpha[i].Split(',');

                            //                             set.FrameAnimate[i] = new short[frameCount];
                            //                             set.FrameCDMap[i] = new short[frameCount];
                            //                             set.FrameCDAtk[i] = new short[frameCount];
                            //                             set.FrameCDDef[i] = new short[frameCount];
                            //                             set.FrameCDExt[i] = new short[frameCount];
                            //                             set.FrameAlpha[i] = new float[frameCount];
                            anim.Frames = new SpriteSet.Frame[frameCount];

                            for (int f = 0; f < frameCount; f++)
                            {
                                //                                 set.FrameAnimate[i][f] = Parser.ParseShort(animate[f]);
                                //                                 set.FrameCDMap[i][f] = Parser.ParseShort(cd_map[f]);
                                //                                 set.FrameCDAtk[i][f] = Parser.ParseShort(cd_atk[f]);
                                //                                 set.FrameCDDef[i][f] = Parser.ParseShort(cd_def[f]);
                                //                                 set.FrameCDExt[i][f] = Parser.ParseShort(cd_ext[f]);
                                //                                 set.FrameAlpha[i][f] = Parser.ParseFloat(alpha[f]);
                                anim.Frames[f] = new SpriteSet.Frame()
                                {
                                    FramePartIndex = Parser.ParseShort(animate[f]),
                                    FrameCDMapIndex = Parser.ParseShort(cd_map[f]),
                                    FrameCDAtkIndex = Parser.ParseShort(cd_atk[f]),
                                    FrameCDDefIndex = Parser.ParseShort(cd_def[f]),
                                    FrameCDExtIndex = Parser.ParseShort(cd_ext[f]),
                                    FrameAlpha = Parser.ParseFloat(alpha[f]),
                                };
                            }
                        }

                        if (e.HasAttribute("fdata"))
                        {
                            String[] frame_datas = GetArray2D(e.Attributes["fdata"].Value);
                            for (int i = 0; i < animateCount; i++)
                            {
                                var anim = set.Animates[i];
                                int frameCount = Parser.ParseInt(frame_counts[i]);
                                var frameDataReader = new DeepCore.IO.TextInputStream(
                                    new StringReader(frame_datas[i]));
                                //set.FrameDatas[i] = new String[frameCount];
                                set.Animates[i].FramesData = new string[frameCount];
                                for (int f = 0; f < frameCount; f++)
                                {
                                    //set.FrameDatas[i][f] = frameDataReader.GetUTF();
                                    anim.FramesData[f] = frameDataReader.GetUTF();
                                }
                            }
                        }
                    }
                    else if (e.Name.Equals("Append"))
                    {
                        set.AppendData = GetArray1DLines(e.Attributes["data"].Value);
                    }
                }
            }

            return set;
        }

        private static void initLevel(CPJFileSet cpj, XmlElement level)
        {
            foreach (XmlNode node in level.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("world"))
                    {
                        WorldSet ws = initWorld(cpj, e);
                        cpj.WorldTable.Add(ws.Name, ws);
                    }
                }
            }
        }


        private static WorldSet initWorld(CPJFileSet cpj, XmlElement world)
        {
            WorldSet set = new WorldSet(
                    Parser.ParseInt(world.Attributes["index"].Value),
                    world.Attributes["name"].Value);

            set.GridXCount = Parser.ParseInt(world.Attributes["grid_x_count"].Value);
            set.GridYCount = Parser.ParseInt(world.Attributes["grid_y_count"].Value);
            set.GridW = Parser.ParseInt(world.Attributes["grid_w"].Value);
            set.GridH = Parser.ParseInt(world.Attributes["grid_h"].Value);
            set.Width = Parser.ParseInt(world.Attributes["width"].Value);
            set.Height = Parser.ParseInt(world.Attributes["height"].Value);

            //		int maps_count	= int.Parse(world.Attributes["unit_count_map"]);
            //		int sprs_count	= int.Parse(world.Attributes["unit_count_sprite"]);
            //		int wpss_count	= int.Parse(world.Attributes["waypoint_count"]);
            //		int wrss_count	= int.Parse(world.Attributes["region_count"]);

            set.Data = GetArray1DLines(world.Attributes["data"].Value);

            int terrains_count = set.GridXCount * set.GridYCount;
            set.Terrian = new int[set.GridXCount, set.GridYCount];
            String[] terrains = world.Attributes["terrain"].Value.Split(',');
            for (int i = 0; i < terrains_count; i++)
            {
                int x = i / set.GridYCount;
                int y = i % set.GridYCount;
                set.Terrian[x, y] = Parser.ParseInt(terrains[i]);
            }

            //NodeList list = world.getChildNodes();

            foreach (XmlNode node in world.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("unit_map"))
                    {
                        WorldSet.MapObject map = new WorldSet.MapObject();
                        map.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        map.UnitName = e.Attributes["map_name"].Value;
                        map.MapID = e.Attributes["id"].Value;
                        map.X = Parser.ParseInt(e.Attributes["x"].Value);
                        map.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        try
                        {
                            map.Priority = Parser.ParseInt(e.Attributes["priority"].Value);
                        }
                        catch { }
                        map.ImagesID = e.Attributes["images"].Value;
                        map.Data = GetArray1DLines(e.Attributes["map_data"].Value);
                        set.Maps.Add(map.Index, map);
                    }
                    else if (e.Name.Equals("unit_sprite"))
                    {
                        WorldSet.SpriteObject spr = new WorldSet.SpriteObject();
                        spr.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        spr.UnitName = e.Attributes["spr_name"].Value;
                        spr.SprID = e.Attributes["id"].Value;
                        spr.Anim = Parser.ParseInt(e.Attributes["animate_id"].Value);
                        spr.Frame = Parser.ParseInt(e.Attributes["frame_id"].Value);
                        spr.X = Parser.ParseInt(e.Attributes["x"].Value);
                        spr.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        try
                        {
                            spr.Priority = Parser.ParseInt(e.Attributes["priority"].Value);
                        }
                        catch { }
                        spr.ImagesID = e.Attributes["images"].Value;
                        spr.Data = GetArray1DLines(e.Attributes["spr_data"].Value);
                        set.Sprs.Add(spr.Index, spr);
                    }
                    else if (e.Name.Equals("unit_image"))
                    {
                        WorldSet.ImageObject img = new WorldSet.ImageObject();
                        img.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        img.UnitName = e.Attributes["img_name"].Value;
                        img.ImagesID = e.Attributes["id"].Value;
                        img.TileID = Parser.ParseInt(e.Attributes["tile_id"].Value);
                        //img.ImgAnchor = (AlignmentStyle)Enum.Parse(typeof(AlignmentStyle), e.Attributes["anchor"].Value);
                        //img.ImgTrans = (Trans)Enum.Parse(typeof(Trans), e.Attributes["trans"].Value);
                        Enum.TryParse<AlignmentStyle>(e.Attributes["anchor"].Value, out img.ImgAnchor);
                        Enum.TryParse<Trans>(e.Attributes["trans"].Value, out img.ImgTrans);
                        img.X = Parser.ParseInt(e.Attributes["x"].Value);
                        img.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        try
                        {
                            img.Priority = Parser.ParseInt(e.Attributes["priority"].Value);
                        }
                        catch { }
                        img.Data = GetArray1DLines(e.Attributes["img_data"].Value);
                        set.Imgs.Add(img.Index, img);
                    }
                    else if (e.Name.Equals("waypoint"))
                    {
                        WorldSet.WaypointObject wp = new WorldSet.WaypointObject();
                        wp.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        wp.X = Parser.ParseInt(e.Attributes["x"].Value);
                        wp.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        wp.Data = GetArray1DLines(e.Attributes["path_data"].Value);
                        set.WayPoints.Add(wp.Index, wp);
                    }
                    else if (e.Name.Equals("region"))
                    {
                        WorldSet.RegionObject wr = new WorldSet.RegionObject();
                        wr.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        wr.X = Parser.ParseInt(e.Attributes["x"].Value);
                        wr.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        wr.W = Parser.ParseInt(e.Attributes["width"].Value);
                        wr.H = Parser.ParseInt(e.Attributes["height"].Value);
                        wr.Data = GetArray1DLines(e.Attributes["region_data"].Value);
                        set.Regions.Add(wr.Index, wr);
                    }
                    else if (e.Name.Equals("event"))
                    {
                        WorldSet.EventObject ev = new WorldSet.EventObject();
                        ev.Index = Parser.ParseInt(e.Attributes["index"].Value);
                        ev.ID = Parser.ParseInt(e.Attributes["id"].Value);
                        ev.X = Parser.ParseInt(e.Attributes["x"].Value);
                        ev.Y = Parser.ParseInt(e.Attributes["y"].Value);
                        ev.EventName = e.Attributes["event_name"].Value;
                        ev.EventFile = e.Attributes["event_file"].Value;
                        ev.Data = e.Attributes["event_data"].Value;
                        set.Events.Add(ev.Index, ev);
                    }
                }
            }

            foreach (XmlNode node in world.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement e = (XmlElement)node;
                    if (e.Name.Equals("waypoint_link"))
                    {
                        int start = Parser.ParseInt(e.Attributes["start"].Value);
                        int end = Parser.ParseInt(e.Attributes["end"].Value);
                        set.WayPoints.Get(start).Nexts.Put(end, set.WayPoints.Get(end));
                    }
                }
            }

            return set;
        }

        /**
         * input "{1234},{5678}"
         * return [1234][5678]
         */
        public static String[] GetArray2D(String text)
        {
            text = text.Replace('{', ' ');
            String[] texts = text.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = texts.Length - 1; i >= 0; --i)
            {
                texts[i] = texts[i].Trim();
            }
            return texts;
        }

        /**
         * input 3,123,4,5678
         * return [123] [5678]
         * @param text
         * @return
         */
        public static String[] GetArray1D(String text)
        {
            var reader = new DeepCore.IO.TextInputStream(new StringReader(text), null);
            List<String> list = new List<String>();
            try
            {
                String line = reader.GetUTF();
                while (!string.IsNullOrEmpty(line))
                {
                    list.Add(line);
                    line = reader.GetUTF();
                }
            }
            catch { }
            return list.ToArray();
        }

        /**
         * input 3,123,4,5678
         * return [123] [5678]
         * @param text
         * @return
         */
        public static String GetArray1DLines(String text)
        {
            var reader = new DeepCore.IO.TextInputStream(new StringReader(text), null);
            StringBuilder ret = new StringBuilder();
            try
            {
                String line = reader.GetUTF();
                while (!string.IsNullOrEmpty(line))
                {
                    ret.Append(line + "\n");
                    line = reader.GetUTF();
                }
            }
            catch { }
            return ret.ToString();
        }
        #endregion
        //-------------------------------------------------------------------------------------

    }
}

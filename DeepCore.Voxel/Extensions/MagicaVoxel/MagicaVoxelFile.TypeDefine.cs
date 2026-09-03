using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeepCore.Voxel.Extensions.MagicaVoxel
{
    /// <summary>
    /// MagicaVoxel.vox File Format [10/18/2016]
    /// </summary>
    public partial class MagicaVoxelFile
    {
        public const string FILE_HEAD = "VOX ";
        public const string FILE_EXT = ".vox";
        static private HashMap<string, Type> CHUNK_TYPES = new HashMap<string, Type>();
        static MagicaVoxelFile()
        {
            var type = typeof(MagicaVoxelFile);
            var types = type.GetNestedTypes();
            foreach (var t in types)
            {
                if (PropertyUtil.TryGetAttribute<DescAttribute>(t, out var desc))
                {
                    CHUNK_TYPES.Add(desc.Desc, t);
                }
            }
        }
        public struct Model
        {
            public ChunkSize Size;
            public ChunkXYZI XYZI;
        }
        public struct Cube
        {
            public byte X;
            public byte Y;
            public byte Z;
            public byte ColorIndex;
            public override string ToString()
            {
                return $"[X:{X} Y:{Y} Z:{Z} C:{ColorIndex}]";
            }
        }
        public struct Color
        {
            public byte R;
            public byte G;
            public byte B;
            public byte A;
            public override string ToString()
            {
                return $"[R:{R} G:{G} B:{B} A:{A}]";
            }
            public uint RGBA
            {
                get => Colors.EncodeRGBA(R, G, B, A);
                set { Colors.DecodeRGBA(value, out R, out G, out B, out A); }
            }
            public uint ARGB
            {
                get => Colors.EncodeARGB(R, G, B, A);
                set { Colors.DecodeARGB(value, out R, out G, out B, out A); }
            }
        }
        public enum MaterialType : int
        {
            _diffuse = 0,
            _metal = 1,
            _glass = 2,
            _emit = 3,
        }
        public struct Rotation
        {
            public float
                M11, M12, M13,
                M21, M22, M23,
                M31, M32, M33;
            public override string ToString()
            {
                return $"\n[{M11} {M12} {M13}]\n[{M21} {M22} {M23}]\n[{M31} {M32} {M33}]";
            }
            public static Rotation Identy = new Rotation()
            {
                M11 = 1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
            };
        }
        public struct Translation
        {
            public int X, Y, Z;
            public override string ToString()
            {
                return $"[{X} {Y} {Z}]";
            }
            public static Translation Zero = new Translation() { X = 0, Y = 0, Z = 0 };
        }
        public struct Frame
        {
            public Properties FrameAttributes;
            public Rotation Rotation;
            public Translation Translation;
        }
        public struct ShapeModel
        {
            public int ModelID;
            public Properties NodeAttributes;
            public override string ToString()
            {
                return $"[ModelID:{ModelID}]";
            }
        }
        public struct VisitCube
        {
            public int X;
            public int Y;
            public int Z;
            public byte ColorIndex;
            public Model Model;
        }
        public struct VisitModel
        {
            public int X1, Y1, Z1, X2, Y2, Z2;
        }

        public class SceneGraph
        {
            public static Matrix IdentyMatrix = Matrix.Identity;
            public ChunkTransformNode Transform { get; private set; }
            public ChunkGroupNode Group { get; private set; }
            public ChunkShapeNode Shape { get; private set; }
            public Model[] ShapeModels { get; private set; }
            public SceneGraph[] GroupChilds { get; private set; }
            public void ForEachModels(Action<VisitModel> action)
            {
                var matrix = IdentyMatrix;
                ForEachModels(this, matrix, action);
            }
            private static void ForEachModels(SceneGraph currentNode, Geometry.Matrix matrix, Action<VisitModel> action)
            {
                var t = currentNode.Transform.Translation;
                var r = currentNode.Transform.Rotation;
                var mtrans = Matrix.CreateTranslation(new Vector3(t.X, t.Y, t.Z));
                var mrotate = new Matrix(
                               r.M11, r.M21, r.M31, 0.00f,
                               r.M12, r.M22, r.M32, 0.00f,
                               r.M13, r.M23, r.M33, 0.00f,
                               0.00f, 0.00f, 0.00f, 1.00f);
                matrix = matrix * mtrans;
                if (currentNode.ShapeModels != null)
                {
                    var group_pos = matrix.Translation;
                    foreach (var sp in currentNode.ShapeModels)
                    {
                        var sx = (sp.Size.SizeX / 2f);
                        var sy = (sp.Size.SizeY / 2f);
                        var sz = (sp.Size.SizeZ / 2f);
                        var local_pos1 = new Vector3(-sx, -sy, -sz);
                        var local_pos2 = new Vector3(+sx, +sy, +sz);
                        var trans_pos1 = Vector3.Transform(local_pos1, mrotate);
                        var trans_pos2 = Vector3.Transform(local_pos2, mrotate);
                        var vc = new VisitModel()
                        {
                            X1 = (int)(trans_pos1.X + group_pos.X),
                            Y1 = (int)(trans_pos1.Y + group_pos.Y),
                            Z1 = (int)(trans_pos1.Z + group_pos.Z),
                            X2 = (int)(trans_pos2.X + group_pos.X),
                            Y2 = (int)(trans_pos2.Y + group_pos.Y),
                            Z2 = (int)(trans_pos2.Z + group_pos.Z),
                        };
                        action(vc);
                    }
                }
                else if (currentNode.GroupChilds != null)
                {
                    foreach (var child in currentNode.GroupChilds)
                    {
                        ForEachModels(child, matrix * mrotate, action);
                    }
                }
            }

            public void ForEachVoxels(Action<VisitCube> action)
            {
                var matrix = IdentyMatrix;
                ForEachVoxels(this, matrix, action);
            }
            private static void ForEachVoxels(SceneGraph currentNode, Geometry.Matrix matrix, Action<VisitCube> action)
            {
                var t = currentNode.Transform.Translation;
                var r = currentNode.Transform.Rotation;
                var mtrans = Matrix.CreateTranslation(new Vector3(t.X, t.Y, t.Z));
                var mrotate = new Matrix(
                               r.M11, r.M21, r.M31, 0.00f,
                               r.M12, r.M22, r.M32, 0.00f,
                               r.M13, r.M23, r.M33, 0.00f,
                               0.00f, 0.00f, 0.00f, 1.00f);
                matrix = matrix * mtrans;
                if (currentNode.ShapeModels != null)
                {
                    var group_pos = matrix.Translation;
                    foreach (var sp in currentNode.ShapeModels)
                    {
                        var sx = (int)(sp.Size.SizeX / 2f);
                        var sy = (int)(sp.Size.SizeY / 2f);
                        var sz = (int)(sp.Size.SizeZ / 2f);
                        foreach (var c in sp.XYZI.Voxels)
                        {
                            var local_pos = new Vector3(
                                c.X - sx + 0.5f,
                                c.Y - sy + 0.5f,
                                c.Z - sz + 0.5f);
                            var trans_pos = Vector3.Transform(local_pos, mrotate);
                            var vc = new VisitCube()
                            {
                                X = (int)(trans_pos.X + group_pos.X - 0.5f),
                                Y = (int)(trans_pos.Y + group_pos.Y - 0.5f),
                                Z = (int)(trans_pos.Z + group_pos.Z - 0.5f),
                                ColorIndex = c.ColorIndex,
                                Model = sp,
                            };
                            action(vc);
                        }
                    }
                }
                else if (currentNode.GroupChilds != null)
                {
                    foreach (var child in currentNode.GroupChilds)
                    {
                        ForEachVoxels(child, matrix * mrotate, action);
                    }
                }
            }
            public static SceneGraph InitSceneGraph(ChunkMain main)
            {
                var exts = main.Extensions;
                if (exts == null) return null;
                var map = new HashMap<int, ISceneGraphNode>();
                foreach (var ext in exts)
                {
                    if (ext is ISceneGraphNode node)
                    {
                        map.Add(node.NodeID, node);
                    }
                }
                if (map.TryGetValue(0, out var root))
                {
                    if (root is ChunkTransformNode rootTrans)
                    {
                        main.Owner.TotalVoxelCount = 0;
                        return CreateSceneGraph(main, rootTrans, map);
                    }
                }
                return null;
            }
            private static SceneGraph CreateSceneGraph(ChunkMain main, ChunkTransformNode root, HashMap<int, ISceneGraphNode> exts)
            {
                main.Owner.TotalVoxelCount = 0;
                var sroot = new SceneGraph();
                sroot.Transform = root;
                if (exts.TryGetValue(root.ChildNodeID, out var gameobject))
                {
                    if (gameobject is ChunkGroupNode group)
                    {
                        sroot.Group = group;
                        if (sroot.Group.NumOfChildren > 0)
                        {
                            var childs = new List<SceneGraph>(sroot.Group.NumOfChildren);
                            foreach (var childID in sroot.Group.ChildrenID)
                            {
                                if (exts.TryGetValue(childID, out var child))
                                {
                                    if (child is ChunkTransformNode childTrans)
                                    {
                                        var schild = CreateSceneGraph(main, childTrans, exts);
                                        childs.Add(schild);
                                    }
                                    else
                                    {
                                        log.Error($"Unknow Child Transform : {child}");
                                    }
                                }
                                else
                                {
                                    log.Error($"Unknow Child Transform NodeID : {childID}");
                                }
                            }
                            sroot.GroupChilds = childs.ToArray();
                        }
                    }
                    else if (gameobject is ChunkShapeNode shape)
                    {
                        sroot.Shape = shape;
                        sroot.ShapeModels = new Model[shape.NumOfModels];
                        for (int i = 0; i < shape.NumOfModels; i++)
                        {
                            var sm = shape.Models[i];
                            sroot.ShapeModels[i] = main.Models[sm.ModelID];
                            main.Owner.TotalVoxelCount += sroot.ShapeModels[i].XYZI.NumVoxels;
                        }
                    }
                    else
                    {
                        log.Error($"Unknow GameObject : {gameobject}");
                    }
                }
                else
                {
                    log.Error($"Unknow Transform NodeID : {root.ChildNodeID}");
                }
                return sroot;
            }
        }
        public interface ISceneGraphNode
        {
            int NodeID { get; }
        }
    }
}

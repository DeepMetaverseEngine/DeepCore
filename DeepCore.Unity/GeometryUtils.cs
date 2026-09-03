using DeepCore.GUI.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class GeometryUtils
{
    public static Vector3 ToUnity(this DeepCore.Geometry.Vector3 value)
    {
        return new Vector3(value.X, value.Y, value.Z);
    }
    public static Vector2 ToUnity(this DeepCore.Geometry.Vector2 value)
    {
        return new Vector2(value.X, value.Y);
    }
    public static Rect ToUnity(this DeepCore.Geometry.RectangleF value)
    {
        return new Rect(value.X, value.Y, value.Width, value.Height);
    }

    public static TextAnchor ToTextAnchor(this AlignmentStyle style)
    {
        switch (style)
        {
            case AlignmentStyle.TopLeft: return TextAnchor.UpperLeft;
            case AlignmentStyle.TopCenter: return TextAnchor.UpperCenter;
            case AlignmentStyle.TopRight: return TextAnchor.UpperRight;

            case AlignmentStyle.MiddleLeft: return TextAnchor.MiddleLeft;
            case AlignmentStyle.MiddleCenter: return TextAnchor.MiddleCenter;
            case AlignmentStyle.MiddleRight: return TextAnchor.MiddleRight;

            case AlignmentStyle.BottomLeft: return TextAnchor.LowerLeft;
            case AlignmentStyle.BottomCenter: return TextAnchor.LowerCenter;
            case AlignmentStyle.BottomRight: return TextAnchor.LowerRight;
        }
        return TextAnchor.MiddleCenter;
    }




    public static Color ToUnityColor(this DeepCore.GUI.Display.Color value)
    {
        return new Color(value.R, value.G, value.B, value.A);
    }

    public static Color ToUnityColor(this DeepCore.Geometry.Vector4 value)
    {
        return new Color(value.X, value.Y, value.Z, value.W);
    }

    public static DeepCore.Geometry.Vector3 ToGeometry(this Vector3 value)
    {
        return new DeepCore.Geometry.Vector3(value.x, value.y, value.z);
    }
    public static DeepCore.Geometry.Vector2 ToGeometry(this Vector2 value)
    {
        return new DeepCore.Geometry.Vector2(value.x, value.y);
    }

    public static Vector3 VoxelToUnity(this Vector3 pos)
    {
        return new Vector3(pos.x, pos.z, -pos.y);
    }
    public static Vector3 UnityToVoxel(this Vector3 pos)
    {
        return new Vector3(pos.x, pos.z, -pos.y);
    }


    public static Vector3[] ToUnity(this DeepCore.Geometry.Vector3[] array)
    {
        return Array.ConvertAll(array, value => new UnityEngine.Vector3(value.X, value.Y, value.Z));
    }
    public static Vector2[] ToUnity(this DeepCore.Geometry.Vector2[] array)
    {
        return Array.ConvertAll(array, value => new UnityEngine.Vector2(value.X, value.Y));
    }
    public static List<Vector3> ToUnity(this List<DeepCore.Geometry.Vector3> array)
    {
        return array.ConvertAll(value => new Vector3(value.X, value.Y, value.Z));
    }
    public static List<Vector2> ToUnity(this List<DeepCore.Geometry.Vector2> array)
    {
        return array.ConvertAll(value => new Vector2(value.X, value.Y));
    }

    public static Vector3[] ToUnityArray(this List<DeepCore.Geometry.Vector3> array)
    {
        return array.ConvertAll(value => new Vector3(value.X, value.Y, value.Z)).ToArray();
    }
    public static Vector2[] ToUnityArray(this List<DeepCore.Geometry.Vector2> array)
    {
        return array.ConvertAll(value => new Vector2(value.X, value.Y)).ToArray();
    }
}


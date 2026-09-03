using Battlehub;
using Battlehub.RTCommon;
using Battlehub.RTHandles;
using DeepCore.Unity;
using DeepMetaGame.Unity.Preview;
using DeepMetaGame.Unity.Preview.SceneEditor;
using UnityEngine;
//using Gizmos = Popcron.Gizmos;


public class SimpleEditorRTH : EditorRTG
{
    #region IGizmos

    public override void GizmosDrawLine(Vector3 p1, Vector3 p2)
    {
//         var color = this.GizmosColor;
//         var mtx = this.GizmosMatrix;
//         Gizmos.Line(mtx.GetPosition() + p1, mtx.GetPosition() + p2, color);
    }
    public override void GizmosDrawCube(Vector3 center, Vector3 size)
    {
//         var color = this.GizmosColor;
//         var mtx = this.GizmosMatrix;
//         Gizmos.Cube(mtx.GetPosition() + center, mtx.rotation, size, color);
    }
    #endregion
}

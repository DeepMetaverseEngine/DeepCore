using DeepCore.Game3D.Host.Helper;
using DeepCore.Game3D.Host.Instance.Components;
using DeepCore.Reflection;
using DeepMetaGame.Data.ZoneEditor;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Host.Instance
{
    public partial class ZoneWayPoint : InstanceFlag
    {
        new public PointData EditorData { get => base.EditorData as PointData; }
        public PointData Data { get => this.EditorData as PointData; }
        public override float BodySize { get { return Data.Radius; } }
        public override float Direction { get { return Data.Direction; } }




        public ZoneWayPoint(InstanceZone zone, PointData data)
            : base(zone, data)
        {
        }

        //         public ZoneWayPoint GetNext()
        //         {
        //             if (mNexts.Count > 0)
        //             {
        //                 return mNexts[0];
        //             }
        //             return null;
        //         }
        // 
        //         public ZoneWayPoint GetTail()
        //         {
        //             if (mNexts.Count == 0)
        //             {
        //                 return this;
        //             }
        //             ZoneWayPoint wp = GetNext();
        //             while (true)
        //             {
        //                 ZoneWayPoint next = wp.GetNext();
        //                 if (next != null)
        //                 {
        //                     wp = next;
        //                 }
        //                 else
        //                 {
        //                     break;
        //                 }
        //             }
        //             return wp;
        //         }

    }


}

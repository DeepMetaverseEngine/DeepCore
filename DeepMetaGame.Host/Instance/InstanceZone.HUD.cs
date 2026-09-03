using DeepCore.Game3D.Host.ZoneEditor.EventTrigger;
using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepMetaGame.Data.Message;
using DeepMetaGame.Data.Message.UI;
using DeepMetaGame.Data.Template;
using System;

namespace DeepCore.Game3D.Host.Instance
{
    partial class InstanceZone
    {
        public InstanceUnit DraggingUnit { get; protected set; }
        protected Vector3 draggingUnitOffset;
        protected virtual void ProcessHUDMessage(UIInteractiveAction act)
        {

            if (act is MouseDownAction mouseDown)
            {
                if (mouseDown.raycast != null)
                {
                    var ray = mouseDown.raycast;
                    var unit = GetUnit(mouseDown.raycast.HitObjectID);
                    if (TryBeginDrag(mouseDown, unit))
                    {
                        unit.Retain();
                        this.DraggingUnit = unit;
                        this.draggingUnitOffset = DeepCore.Geometry.RayCast.RayPlaneIntersection(
                            ray.origin, ray.normal,
                            unit.Position,
                            DeepCore.Geometry.Vector3.UnitZ) - unit.Position;
                    }
                }
            }
            else if (act is MouseMoveAction mouseMove)
            {
                if (mouseMove.raycast != null)
                {
                    var ray = mouseMove.raycast;
                    if (DraggingUnit != null && TryDragging(mouseMove, DraggingUnit))
                    {
                        var newOffset = DeepCore.Geometry.RayCast.RayPlaneIntersection(
                            ray.origin, ray.normal,
                            DraggingUnit.Position,
                            DeepCore.Geometry.Vector3.UnitZ);
                        var offset = newOffset - draggingUnitOffset;
                        DraggingUnit.Transport(offset);
                    }
                }
            }
            else if (act is MouseUpAction mouseUp)
            {
                this.DraggingUnit?.Release();
                this.DraggingUnit = null;
            }
        }

        protected virtual bool TryBeginDrag(MouseDownAction mouseDown, InstanceUnit unit) { return unit != null && unit.Info.Abilities.TryGetComponentAs<UnitDragAndDropAbility>(out var dnd); }
        protected virtual bool TryDragging(MouseMoveAction mouseMove, InstanceUnit unit) { return true; }

    }
}


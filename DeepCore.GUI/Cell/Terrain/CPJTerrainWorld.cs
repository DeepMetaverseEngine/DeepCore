using DeepCore.Astar;
using DeepCore.Concurrent;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepCore.IO;
using DeepCore.Space;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DeepCore.GUI.Cell
{
    
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------
    public class CPJTerrainWorld :  ITerrainWorld
    {
        public CPJTerrainWorld(MapSet map) { }

        protected override void Disposing()
        {
            throw new NotImplementedException();
        }


        public ITerrain Terrain => throw new NotImplementedException();

        public ITerrainAstar PathFinder => throw new NotImplementedException();

        public ITerrainAgent CreateAgent()
        {
            throw new NotImplementedException();
        }

        public ITerrainAgent CreateAgent(Vector3 pos)
        {
            throw new NotImplementedException();
        }

        public ITerrainAgent CreateAgent(ITerrainBlock pos)
        {
            throw new NotImplementedException();
        }
        public ITerrainBlock FindNearRandomMoveableNode(Random random, ITerrainAgent src, float radius)
        {
            throw new NotImplementedException();
        }

        public ITerrainBlock FindNearRandomMoveableNode(Random random, ITerrainBlock src, float radius)
        {
            throw new NotImplementedException();
        }

        public ITerrainBlock FindNearRandomMoveableNode(Random random, ref Vector3 src, float radius)
        {
            throw new NotImplementedException();
        }

        public ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ITerrainAgent src, float width, float height)
        {
            throw new NotImplementedException();
        }

        public ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ITerrainBlock src, float width, float height)
        {
            throw new NotImplementedException();
        }

        public ITerrainBlock FindNearRandomMoveableNodeRect(Random random, ref Vector3 src, float width, float height)
        {
            throw new NotImplementedException();
        }
    }
    //-------------------------------------------------------------------------------------------------------------------
    public class CPJAgent : ITerrainAgent
    {
        public ITerrainWorld World => throw new NotImplementedException();

        public ITerrain Terrain => throw new NotImplementedException();

        public ITerrainBlock CurrentLayer => throw new NotImplementedException();

        public Vector3 Position => throw new NotImplementedException();

        public float X => throw new NotImplementedException();

        public float Y => throw new NotImplementedException();

        public float Z => throw new NotImplementedException();

        public float? LandAirDistance => throw new NotImplementedException();

        public bool IsInTheAir => throw new NotImplementedException();

        public float Height { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float SpeedZ { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float Gravity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool MoveKeepInColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event ITerrainAgent.LayerChanged OnLayerChanged;
        public event ITerrainAgent.BumpHead OnBumpHead;
        public event ITerrainAgent.FallenDown OnFallenDown;

        public ITerrainAgent Clone()
        {
            throw new NotImplementedException();
        }

        public void EnterWorld(ITerrainWorld world)
        {
            throw new NotImplementedException();
        }

        public void Fly(float zoffset)
        {
            throw new NotImplementedException();
        }

        public void FlyTo(float dz)
        {
            throw new NotImplementedException();
        }

        public void Jump(float speed)
        {
            throw new NotImplementedException();
        }

        public void LeaveWorld()
        {
            throw new NotImplementedException();
        }

        public AgentMoveResult MoveLinearTo2D(Vector3 dst, out ITerrainBlock touched)
        {
            throw new NotImplementedException();
        }

        public void MoveOffsetNoTouch(Vector2 offset)
        {
            throw new NotImplementedException();
        }

        public void Transport(ITerrainBlock layer)
        {
            throw new NotImplementedException();
        }

        public void Transport(in Vector3 pos)
        {
            throw new NotImplementedException();
        }

        public void Transport(in Vector3 pos, ITerrainBlock layer)
        {
            throw new NotImplementedException();
        }

        public AgentMoveResult TryMoveLerp(float direction, float step, bool land)
        {
            throw new NotImplementedException();
        }

        public AgentMoveResult TryMoveOffset(Vector2 offset, bool land)
        {
            throw new NotImplementedException();
        }

        public AgentMoveResult TryMoveTo(Vector3 target, float step, bool land)
        {
            throw new NotImplementedException();
        }

        public AgentMoveResult TryMoveToPath(ref ITerrainWayPoint path, float step, bool land)
        {
            throw new NotImplementedException();
        }

        public void Update(float intervalMS)
        {
            throw new NotImplementedException();
        }
    }
    //-------------------------------------------------------------------------------------------------------------------
}

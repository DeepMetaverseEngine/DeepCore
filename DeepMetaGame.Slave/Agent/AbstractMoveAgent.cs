namespace DeepCore.Game3D.Slave.Agent
{
    public abstract class AbstractMoveAgent : AbstractAgent
    {
        public abstract ILayerWayPoint WayPoints { get; }
        public abstract bool IsFinish { get; }

    }
}

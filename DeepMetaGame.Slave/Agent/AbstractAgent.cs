using DeepCore.Game3D.Slave.Layer;

namespace DeepCore.Game3D.Slave.Agent
{
    public abstract class AbstractAgent
    {
        private LayerPlayer m_Object;
        private bool m_Disposed = false;
        public LayerPlayer Owner { get { return m_Object; } }
        public LayerZone Layer { get { return m_Object.Parent; } }
        public object Tag { get; set; }
        //--------------------------------------------------------------------------------------
        internal void InternalInit(LayerPlayer a)
        {
            m_Object = a;
            OnInit(a);
        }
        internal void InternalStart()
        {
            if (m_OnStart != null) { m_OnStart.Invoke(this); }
        }
        internal void InternalEnd()
        {
            if (m_OnEnd != null) { m_OnEnd.Invoke(this); }
        }
        internal void InternalBeginUpdate(float intervalMS)
        {
            this.BeginUpdate(intervalMS);
        }
        internal void InternalEndUpdate(float intervalMS)
        {
            this.EndUpdate(intervalMS);
        }

        //--------------------------------------------------------------------------------------

        public delegate void OnStartHandler(AbstractAgent agent);
        public delegate void OnEndHandler(AbstractAgent agent);

        private OnStartHandler m_OnStart;
        private OnEndHandler m_OnEnd;

        public event OnStartHandler OnStart { add { m_OnStart += value; } remove { m_OnStart -= value; } }
        public event OnEndHandler OnEnd { add { m_OnEnd += value; } remove { m_OnEnd -= value; } }

        //--------------------------------------------------------------------------------------
        /// <summary>
        /// Agent 是否已结束
        /// </summary>
        public abstract bool IsEnd { get; }

        /// <summary>
        /// 是否允许多个同类实体
        /// </summary>
        public abstract bool IsDuplicate { get; }

        protected virtual void OnInit(LayerPlayer actor) { }
        protected virtual void OnDispose() { }
        protected virtual void BeginUpdate(float intervalMS) { }
        protected virtual void EndUpdate(float intervalMS) { }

        internal void Dispose()
        {
            if (m_Disposed == false)
            {
                this.m_Disposed = true;
                this.m_OnEnd = null;
                this.m_OnStart = null;
                this.OnDispose();
                m_Object = null;
            }
        }

        //--------------------------------------------------------------------------------------
    }
}

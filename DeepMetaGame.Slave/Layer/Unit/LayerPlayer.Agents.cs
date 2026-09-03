using DeepCore.Game3D.Slave.Agent;
using System;
using System.Collections.Generic;

namespace DeepCore.Game3D.Slave.Layer
{

    public partial class LayerPlayer
    {

        private List<AbstractAgent> mAgents = new List<AbstractAgent>(1);
        private List<AbstractAgent> mUpdateAgents = new List<AbstractAgent>(1);

        public int AgentCount { get { return mAgents.Count; } }

        public T AddAgentAs<T>(T agent) where T : AbstractAgent
        {
            return AddAgent((AbstractAgent)agent) as T;
        }
        public AbstractAgent AddAgent(AbstractAgent agent)
        {
            if (mAgents.Contains(agent))
            {
                return null;
            }
            if (!agent.IsDuplicate)
            {
                for (int i = mAgents.Count - 1; i >= 0; --i)
                {
                    if (agent.GetType() == mAgents[i].GetType())
                    {
                        mAgents.RemoveAt(i);
                    }
                }
            }
            mAgents.Add(agent);
            agent.InternalInit(this);
            Parent.TaskQueue.Enqueue(agent, static (z, a) =>
            {
                a.InternalStart();
            });
            return agent;
        }
        public bool RemoveAgent(AbstractAgent agent)
        {
            if (mAgents.Remove(agent))
            {
                Parent.TaskQueue.Enqueue(agent, static (z, a) =>
                {
                    a.InternalEnd();
                    a.Dispose();
                });
                return true;
            }
            return false;
        }
        public T GetAgentByType<T>() where T : AbstractAgent
        {
            for (int i = 0; i < mAgents.Count; i++)
            {
                if (mAgents[i] is T)
                {
                    return mAgents[i] as T;
                }
            }
            return null;
        }
        public void ForEachAgent(Action<AbstractAgent> action)
        {
            mUpdateAgents.AddRange(mAgents);
            try
            {
                mUpdateAgents.ForEach(action);
            }
            finally
            {
                mUpdateAgents.Clear();
            }
        }
        private void MAgents_OnBeginUpdate(AbstractAgent obj)
        {
            if (obj.IsEnd)
            {
                RemoveAgent(obj);
            }
            else
            {
                obj.InternalBeginUpdate(Parent.CurrentIntervalMS);
            }
        }
        private void MAgents_OnEndUpdate(AbstractAgent obj)
        {
            if (obj.IsEnd)
            {
                RemoveAgent(obj);
            }
            else
            {
                obj.InternalEndUpdate(Parent.CurrentIntervalMS);
            }
        }

        private void clearAgents()
        {
            foreach (var agt in mAgents)
            {
                agt.InternalEnd();
                agt.Dispose();
            }
            mAgents.Clear();
        }
    }
}




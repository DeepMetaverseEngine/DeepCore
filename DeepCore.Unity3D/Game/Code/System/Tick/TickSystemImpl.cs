using System.Collections.Generic;
using Code.System.Pool;
using Code.System.World;

namespace Code.System.Tick
{
    internal class TickSystemImpl : SingleSystem<TickSystemImpl>
    {
        private Dictionary<long, LinkedListNode<Ticker>> _snap = new Dictionary<long, LinkedListNode<Ticker>>();
        private LinkedList<Ticker> _datas = new LinkedList<Ticker>();
        /// <summary>
        /// 新加入的下一帧计时
        /// </summary>
        private LinkedList<Ticker> _temps = new LinkedList<Ticker>();

        protected override void Disposing()
        {
            _snap = null;
            _datas = null;
            _temps = null;
        }

        protected override void OnUpdate(float deltaTime)
        {
            var node = _datas.First;
            while (node != null)
            {
                var tmp = node;
                node = node.Next;

                var data = tmp.Value;
                data.PastTime += deltaTime;
                    
                if (!(data.PastTime >= data.IntervalTime)) continue;
                data.Callback.Invoke(data.Serial, data.Index);
                data.PastTime -= data.IntervalTime;
                data.Index++;
                
                if (data.Count <= 0 || data.Index < data.Count) continue;
                _snap.Remove(data.Serial);
                node = tmp.Next;
                tmp.List.Remove(tmp);
                tmp.Value.Dispose();
            }

            node = _temps.First;
            while (node != null)
            {
                var tmp = node;
                node = node.Next;
                tmp.List.Remove(tmp);
                _datas.AddLast(tmp);
            }
        }

        public long Tick(float intervalTime, TickSystem.TickHandler callback, int count = 1)
        {
            if (intervalTime <= 0 && count == 0)
            {
                callback.Invoke(0, 0);
                return 0;
            }
        
            var ticker = ObjectPool<Ticker>.Get();
            ticker.Serial = WorldSystem.GenerateSerial();
            ticker.IntervalTime = intervalTime;
            ticker.Callback = callback;
            ticker.Count = count;
            
            var node = new LinkedListNode<Ticker>(ticker);
            if (ticker.IntervalTime <= 0)
            {
                _datas.AddLast(ticker);
            }
            else
            {
                _temps.AddLast(node);
            }
            _snap.Add(ticker.Serial, node);
            return ticker.Serial;
        }

        public void TickCancel(long serial)
        {
            if (!_snap.TryGetValue(serial, out var node)) return;
            _snap.Remove(node.Value.Serial);
            node.List?.Remove(node);
        }
    }
}

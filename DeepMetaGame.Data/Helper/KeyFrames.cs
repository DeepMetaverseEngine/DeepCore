using DeepCore;
using DeepMetaGame.Data.Misc;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Helper
{
    public class PopupKeyFrames<K> where K : IKeyFrame
    {
        private readonly List<K> list = new List<K>();

        public int Count
        {
            get { return list.Count; }
        }

        public PopupKeyFrames()
        {
        }
        public void AddRange(IEnumerable<K> frames)
        {
            if (frames != null)
            {
                list.AddRange(frames);
                list.Sort(Compare);
            }
        }
        public void Add(K frame)
        {
            list.Add(frame);
            list.Sort(Compare);
        }
        public void Sort()
        {
            list.Sort(Compare);
        }
        public void Clear()
        {
            list.Clear();
        }
        /// <summary>
        /// 取出所有到时间的关键帧
        /// </summary>
        /// <param name="passTimeMS"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public int PopKeyFrames(double passTimeMS, List<K> ret = null)
        {
            int count = 0;
            while (list.Count > 0)
            {
                int i = list.Count - 1;
                K kf = list[i]; 
                if (kf == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (kf.FrameMS <= passTimeMS)
                {
                    list.RemoveAt(i);
                    if (ret != null) { ret.Add(kf); }
                    count++;
                    continue;
                }
                else
                {
                    return count;
                }
            }
            return count;
        }
        public int DoKeyFrames<ST>(double passTimeMS, ST st, System.Action<ST, K> cb)
        {
            int count = 0;
            while (list.Count > 0)
            {
                int i = list.Count - 1;
                K kf = list[i];
                if (kf == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                if (kf.FrameMS <= passTimeMS)
                {
                    list.RemoveAt(i);
                    cb(st, kf);
                    count++;
                    continue;
                }
                else
                {
                    return count;
                }
            }
            return count;
        }

        static public int Compare(K x, K y)
        {
            return y.FrameMS - x.FrameMS;
        }

    }
}

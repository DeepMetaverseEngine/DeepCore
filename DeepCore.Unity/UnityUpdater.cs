using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeepCore.Unity
{
    public class UnityInterval
    {
        public double PassTimeMS { get => (passTime * 1000); }
        public float IntervalMS { get => interval; }

        private double startTime;
        private double lastTime;
        private double passTime;
        private float interval = 0;
        public UnityInterval()
        {
            ResetTime();
        }
        public void ResetTime()
        {
            this.startTime = Time.timeAsDouble;
            this.interval = 0;
            this.passTime = 0;
            this.lastTime = 0;
        }
        public float UpdateTime()
        {
            this.lastTime = passTime;
            this.passTime = Time.timeAsDouble - startTime;
            this.interval = (float)((passTime - lastTime) * 1000);
            return interval;
        }
    }
}

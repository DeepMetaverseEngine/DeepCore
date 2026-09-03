
using DeepCore.GUI.Data;
using DeepCore.GUI.Display.Node;
using DeepCore.GUI.Gemo;
using System;

namespace DeepCore.GUI.Display.Action
{
    public class Transitions
    {
        public const int LINEAR = 0;
        public const int EASE_IN = 1;
        public const int EASE_OUT =2;
        public const int EASE_IN_OUT = 3;
        public const int EASE_OUT_IN = 4;
        public const int EASE_IN_BACK = 5;
        public const int EASE_OUT_BACK = 6;
        public const int EASE_IN_OUT_BACK = 7;
        public const int EASE_OUT_IN_BACK = 8;
        public const int EASE_IN_ELASTIC = 9;
        public const int EASE_OUT_ELASTIC = 10;
        public const int EASE_IN_OUT_ELASTIC = 11;
        public const int EASE_OUT_IN_ELASTIC =12;
        public const int EASE_IN_BOUNCE = 13;
        public const int EASE_OUT_BOUNCE = 14;
        public const int EASE_IN_OUT_BOUNCE = 15;
        public const int EASE_OUT_IN_BOUNCE = 16;

        public static float GetTransitionValue(int type, float value)
        {
            switch (type)
            {
                case LINEAR: return Linear(value);
                case EASE_IN: return EaseIn(value);
                case EASE_OUT: return EaseOut(value);
                case EASE_IN_OUT: return EaseInOut(value);
                case EASE_OUT_IN: return EaseOutIn(value);
                case EASE_IN_BACK: return EaseInBack(value);
                case EASE_OUT_BACK: return EaseOutBack(value);
                case EASE_IN_OUT_BACK: return EaseInOutBack(value);
                case EASE_OUT_IN_BACK: return EaseOutInBack(value);
                case EASE_IN_ELASTIC: return EaseInElastic(value);
                case EASE_OUT_ELASTIC: return EaseOutElastic(value);
                case EASE_IN_OUT_ELASTIC: return EaseInOutElastic(value);
                case EASE_OUT_IN_ELASTIC: return EaseOutInElastic(value);
                case EASE_IN_BOUNCE: return EaseInBounce(value);
                case EASE_OUT_BOUNCE: return EaseOutBounce(value);
                case EASE_IN_OUT_BOUNCE: return EaseInOutBounce(value);
                case EASE_OUT_IN_BOUNCE: return EaseOutInBounce(value);
                default: return value;
            }
        }

        protected static float Linear(float ratio)
        {
            return ratio;
        }

        protected static float EaseIn(float ratio)
        {
            return ratio * ratio * ratio;
        }

        protected static float EaseOut(float ratio)
        {
            float invRatio = ratio - 1.0f;
            return invRatio * invRatio * invRatio + 1;
        }

        protected static float EaseInOut(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseIn(ratio); }
            else { return 0.5f * EaseIn((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseOutIn(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseOut(ratio); }
            else { return 0.5f * EaseOut((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseInBack(float ratio)
        {
            float s = 1.70158f;
            return (float)(Math.Pow(ratio, 2) * ((s + 1.0) * ratio - s));
        }

        protected static float EaseOutBack(float ratio)
        {
            float invRatio = ratio - 1.0f;
            float s = 1.70158f;
            return (float)Math.Pow(invRatio, 2) * ((s + 1.0f) * invRatio + s) + 1.0f;

        }

        protected static float EaseInOutBack(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseInBack(ratio); }
            else { return 0.5f * EaseOutBack((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseOutInBack(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseOutBack(ratio); }
            else { return 0.5f * EaseInBack((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseInElastic(float ratio)
        {
            if (ratio == 0 || ratio == 1) return ratio;
            else
            {
                float p = 0.3f;
                float s = p / 4.0f;
                float invRatio = ratio - 1;
                return (float)(-1.0 * Math.Pow(2.0, 10.0 * invRatio) * Math.Sin((invRatio - s) * (2.0 * Math.PI) / p));
            }
        }

        protected static float EaseOutElastic(float ratio)
        {
            if (ratio == 0 || ratio == 1) return ratio;
            else
            {
                float p = 0.3f;
                float s = p / 4.0f;
                return (float)(Math.Pow(2.0, -10.0 * ratio) * Math.Sin((ratio - s) * (2.0 * Math.PI) / p) + 1);
            }
        }

        protected static float EaseInOutElastic(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseInElastic(ratio); }
            else { return 0.5f * EaseOutElastic((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseOutInElastic(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseOutElastic(ratio); }
            else { return 0.5f * EaseInElastic((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseInBounce(float ratio)
        {
            return 1.0f - EaseOutBounce(1.0f - ratio);
        }

        protected static float EaseOutBounce(float ratio)
        {
            float s = 7.5625f;
            float p = 2.75f;
            float l;
            if (ratio < (1.0 / p))
            {
                l = (float)(s * Math.Pow(ratio, 2));
            }
            else
            {
                if (ratio < (2.0 / p))
                {
                    ratio -= 1.5f / p;
                    l = (float)(s * Math.Pow(ratio, 2) + 0.75f);
                }
                else
                {
                    if (ratio < 2.5f / p)
                    {
                        ratio -= 2.25f / p;
                        l = (float)(s * Math.Pow(ratio, 2) + 0.9375f);
                    }
                    else
                    {
                        ratio -= 2.625f / p;
                        l = (float)(s * Math.Pow(ratio, 2) + 0.984375f);
                    }
                }
            }
            return l;
        }

        protected static float EaseInOutBounce(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseInBounce(ratio); }
            else { return 0.5f * EaseOutBounce((ratio - 0.5f) * 2.0f) + 0.5f; }
        }

        protected static float EaseOutInBounce(float ratio)
        {
            if (ratio < 0.5f) { return 0.5f * EaseOutBounce(ratio); }
            else { return 0.5f * EaseInBounce((ratio - 0.5f) * 2.0f) + 0.5f; }

        }
    }
}


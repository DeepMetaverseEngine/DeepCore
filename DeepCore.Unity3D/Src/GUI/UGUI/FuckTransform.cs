using System;
using System.Collections.Generic;
using System.Text;
using DeepCore;
using DeepCore.Components;
using DeepCore.Concurrent;
using DeepCore.Reflection;
using DeepCore.Unity3D.UGUIAction;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeepCore.Unity3D.Src.GUI.UGUI
{
    public class FuckTransform
    {
        private readonly RectTransform mTransform;
        private Vector3 localPosition;
        private Vector2 sizeDelta;
        private Vector2 localScale;

        public FuckTransform(RectTransform t)
        {
            this.mTransform = t;
            this.localPosition = mTransform.localPosition;
            this.sizeDelta = mTransform.sizeDelta;
            this.localScale = mTransform.localScale;
        }

        public float X
        {
            get { return this.localPosition.x; }
            set
            {
                if (this.localPosition.x != value)
                {
                    this.localPosition.x = value;
                    this.mTransform.localPosition = this.localPosition;
                }
            }
        }
        public float Y
        {
            get { return this.localPosition.y; }
            set
            {
                if (this.localPosition.y != value)
                {
                    this.localPosition.y = value;
                    this.mTransform.localPosition = this.localPosition;
                }
            }
        }
        public float Width
        {
            get { return this.sizeDelta.x; }
            set
            {
                if (this.sizeDelta.x != value)
                {
                    this.sizeDelta.x = value;
                    this.mTransform.sizeDelta = this.sizeDelta;
                }
            }
        }
        public float Height
        {
            get { return this.sizeDelta.y; }
            set
            {
                if (this.sizeDelta.y != value)
                {
                    this.sizeDelta.y = value;
                    this.mTransform.sizeDelta = this.sizeDelta;
                }
            }
        }

        public Vector2 Position2D
        {
            get
            {
                Vector2 pos = this.localPosition;
                pos.y = -pos.y;
                return pos;
            }
            set
            {
                if (this.localPosition.x != value.x || this.localPosition.y != -value.y)
                {
                    this.localPosition.x = value.x;
                    this.localPosition.y = -value.y;
                    this.mTransform.localPosition = this.localPosition;
                }
            }
        }
        public Vector2 Size2D
        {
            get { return this.sizeDelta; }
            set
            {
                if (this.sizeDelta != value)
                {
                    this.sizeDelta = value;
                    this.mTransform.sizeDelta = this.sizeDelta;
                }
            }
        }
        public Vector2 Scale
        {
            get { return this.localScale; }
            set
            {
                if (this.localScale != value)
                {
                    this.localScale = value;
                    this.mTransform.localScale = this.localScale;
                }
            }
        }

        public Rect Bounds2D
        {
            get
            {
                Rect rect = new Rect();
                rect.position = this.localPosition;
                rect.size = this.sizeDelta;
                rect.y = -rect.y;
                return rect;
            }
            set
            {
                if (this.localPosition.x != value.x || this.localPosition.y != -value.y)
                {
                    this.localPosition.x = value.x;
                    this.localPosition.y = -value.y;
                    this.mTransform.localPosition = this.localPosition;
                }
                if (this.sizeDelta != value.size)
                {
                    this.sizeDelta = value.size;
                    this.mTransform.sizeDelta = this.sizeDelta;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.Meta.Layout
{
    public class MetaObjectList<V> : MetaObjectContainer<V> where V : MetaObject
    {
        protected readonly List<V> children = new List<V>();
        override public int NumChildren { get { return children.Count; } }
        public override IEnumerable<MetaObject> Children => children;

        protected override void CollectionClearChildren()
        {
            children.Clear();
        }
        protected override bool CollectionRemoveChild(MetaObject c)
        {
            return children.Remove(c as V);
        }
        internal void InternalAddChild(V child, int index)
        {
            base.InternalAddChild(child, c =>
            {
                if (children.Count == index)
                {
                    children.Insert(index, c as V);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
        internal void InternalRemoveChild(V child, bool dispose)
        {
            InternalRemoveChild(child, c => children.Remove(c as V), dispose);
        }


        public void AddChildAt(V child, int index)
        {
            if (child == null || index < 0)
            {
                return;
            }
            if (child.Parent == this)
            {
                SetChildIndex(child, index);
            }
            else
            {
                InternalAddChild(child, index);
            }
        }

        public void AddChild(V child)
        {
            AddChildAt(child, NumChildren);
        }



        public bool RemoveChild(V child, bool dispose = true)
        {
            int result = children.IndexOf(child);
            if (result != -1)
            {
                RemoveChildAt(result, dispose);
                return true;
            }
            return false;
        }

        public void RemoveChildAt(int index, bool dispose = true)
        {
            if (index >= 0 && index < children.Count)
            {
                var child = children[index];
                if (child.parent == this)
                {
                    InternalRemoveChild(child, dispose);
                }
            }
            else
            {
                throw new Exception("RemoveChild Error :: mChildren Out of Bounds");
            }
        }

        public void RemoveChildren(int beginIndex, int endIndex, bool dispose = true)
        {
            if (endIndex < 0 || endIndex >= NumChildren)
                endIndex = NumChildren - 1;

            for (int i = beginIndex; i <= endIndex; ++i)
                RemoveChildAt(beginIndex, dispose);
        }

        public void SetChildIndex(V child, int index)
        {
            int oldIndex = GetChildIndex(child);
            if (oldIndex == -1)
            {
                //LogError("SetChildIndex Error: oldIndex = -1");
                return;
            }
            //logic list.
            children.RemoveAt(oldIndex);
            if (index > children.Count)
            {
                index = children.Count;
            }
            children.Insert(index, child);
        }

        public int GetChildIndex(V child)
        {
            if (child == null)
            {
                //LogError("UIBase GetChildIndex() child == null");
                return -1;
            }
            return children.IndexOf(child);
        }

        public V GetChildAt(int index)
        {
            if (index >= 0 && index < NumChildren)
                return children[index];
            else
                throw new Exception("Invalid child index");
        }

        public void SwapChildren(V child1, V child2)
        {
            int index1 = GetChildIndex(child1);
            int index2 = GetChildIndex(child2);
            if (index1 == -1 || index2 == -1)
                throw new Exception("Not a child of this container");
            SwapChildrenAt(index1, index2);
        }

        public void SwapChildrenAt(int index1, int index2)
        {
            var child1 = GetChildAt(index1);
            var child2 = GetChildAt(index2);

            if (child1 != null && child2 != null)
            {
                children[index1] = child2;
                children[index2] = child1;
            }
        }

    }
}

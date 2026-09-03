using DeepCore.Geometry;
using DeepCore.GUI.Data;
using DeepCore.GUI.SceneGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepCore.GUI.Display.SceneGraph.GUI
{
    public abstract class UEListView<T> : UEDisplayNode<T> where T : UEListMeta
    {
        public UILayout LayoutItem { get; protected set; }
        //--------------------------------------------------------------------
        protected UEListView(UIFactory editor, T e) : base(editor, e)
        {
        }
        protected override void DoDecodeFields()
        {
            base.DoDecodeFields();
            this.LayoutItem = Editor.CreateLayout(Meta.ItemLayout);
            this.AutoRelease(this.LayoutItem);
        }
        //--------------------------------------------------------------------
        protected override void OnDrawBegin(GraphicsArgs args)
        {
            base.OnDrawBegin(args);
            var count = itemList.Count;
            if (IsEditor)
            {
                count = 10;
            }
            if (count > 0)
            {
                var itemSize = Meta.ItemSize + Meta.ItemMargin.CutSize;
                var contentBounds = new RectangleF(Vector2.Zero, itemSize);
                var parentBounds = Meta.ContentPadding.Cut(this.LocalBounds);
                switch (Meta.ContentOrientation)
                {
                    case ListOrientation.Horizontal:
                        contentBounds.Width = itemSize.X * count;
                        if (Meta.ItemSizeToFit)
                        {
                            itemSize.Y = contentBounds.Height = parentBounds.Height;
                        }
                        break;
                    case ListOrientation.Vertical:
                        contentBounds.Height = itemSize.Y * count;
                        if (Meta.ItemSizeToFit)
                        {
                            itemSize.X = contentBounds.Width = parentBounds.Width;
                        }
                        break;
                    case ListOrientation.None:
                        contentBounds.Size = GraphicsUtils.MeasureItems(count, itemSize, parentBounds.Width);
                        break;
                }
                contentBounds = Meta.ContentAlign.GetAlignmentBounds(parentBounds, contentBounds.Size);
                DrawItems(args, count, in itemSize, in contentBounds);
            }
        }
        protected virtual void DrawItems(GraphicsArgs args, int count, in Vector2 itemSize, in RectangleF contentBounds)
        {
            var step = itemSize;
            switch (Meta.ContentOrientation)
            {
                case ListOrientation.Horizontal: step.Y = 0; break;
                case ListOrientation.Vertical: step.X = 0; break;
                case ListOrientation.None: step.Y = 0; break;
            }
            var itemBounds = new RectangleF(contentBounds.Location, itemSize);
            for (int i = 0; i < count; i++)
            {
                var item = (i < itemList.Count) ? itemList[i] : null;
                var ibounds = Meta.ItemMargin.Cut(itemBounds);
                if (item != null)
                {
                    DrawItem(args, item, ibounds);
                }
                else
                {
                    DrawDummy(args, ibounds);
                }
                itemBounds.Location += step;
                if (Meta.ContentOrientation == ListOrientation.None)
                {
                    if (itemBounds.Location.X + itemBounds.Width > contentBounds.Right)
                    {
                        itemBounds.Location = new Vector2(0, itemBounds.Location.Y + itemSize.Y);
                    }
                }
            }
        }
        protected virtual void DrawDummy(GraphicsArgs args, in RectangleF itemBounds)
        {
            if (Rect.Bounds.Contains(itemBounds))
            {
                LayoutItem?.Render(args.Graphics, itemBounds);
                if (Meta.ItemTextStyle != null) { args.Graphics.SetColor(Meta.ItemTextStyle.TextColor); }
                args.Graphics.DrawRect(itemBounds);
            }
        }
        protected virtual void DrawItem(GraphicsArgs args, ListItem item, in RectangleF itemBounds)
        {
            LayoutItem?.Render(args.Graphics, itemBounds);
            if (item.Icon?.Image != null)
            {
                args.Graphics.BeginImage(item.Icon.Image);
                args.Graphics.DrawImageZoom(itemBounds);
            }
            if (Meta.ItemTextStyle != null) { args.Graphics.SetColor(Meta.ItemTextStyle.TextColor); }
            args.Graphics.DrawString(item.Text, itemBounds, Meta.ItemAlign);
        }
        //--------------------------------------------------------------------
        #region ListItem
        private List<ListItem> itemList = new List<ListItem>();
        public IReadOnlyList<ListItem> Items { get => itemList; }
        public int ItemCount { get => itemList.Count; }
        public void AddItemRange(IEnumerable<ListItem> items)
        {
            itemList.AddRange(items);
        }
        public void AddItemRange(IEnumerable items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }
        public void AddItem(ListItem item)
        {
            itemList.Add(item);
        }
        public ListItem AddItem(object item)
        {
            if (item is ListItem titem)
            {
                itemList.Add(titem);
                return titem;
            }
            else
            {
                titem = new ListItem(item);
                itemList.Add(titem);
                return titem;
            }
        }
        public void InsertItem(int index, ListItem item)
        {
            itemList.Insert(index, item);
        }
        public ListItem InsertItem(int index, object item)
        {
            if (item is ListItem titem)
            {
                itemList.Insert(index, titem);
                return titem;
            }
            else
            {
                titem = new ListItem(item);
                itemList.Insert(index, titem);
                return titem;
            }
        }
        public void RemoveRange(int start, int count)
        {
            itemList.RemoveRange(start, count);
        }
        public bool RemoveItem(ListItem item)
        {
            return itemList.Remove(item);
        }
        public void RemoveAt(int index)
        {
            itemList.RemoveAt(index);
        }
        public void ClearItems()
        {
            itemList.Clear();
        }
        public void SortItem(Comparison<ListItem> comparison = null)
        {
            if (comparison == null)
                itemList.Sort();
            else
                itemList.Sort(comparison);
        }
        #endregion
    }

    //--------------------------------------------------------------------
    public class ListItem
    {
        public string Text;
        public string ToolTips;
        public UIResourceImage Icon;
        public object Tag;
        public ListItem() { }
        public ListItem(object item)
        {
            this.Text = $"{item}";
            this.Tag = item;
        }
    }
    //--------------------------------------------------------------------


    [UEInstance(typeof(UETextListMeta))]
    public class UETextList : UEListView<UETextListMeta>
    {
        public UETextList(UIFactory editor, UETextListMeta e) : base(editor, e)
        {
            if (e.Items != null)
            {
                base.AddItemRange(e.Items);
            }
        }
        public override string GetTextValue()
        {
            return string.Empty;
        }
    }
}

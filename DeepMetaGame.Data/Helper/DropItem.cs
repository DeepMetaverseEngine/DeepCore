using DeepCore;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Helper
{
    public class DropItemAgent : BattleAutoRecycle
    {
        private ITerrainAgent mRandomPos;
        protected override void Disposing()
        {

        }
        public virtual DeepCore.Geometry.Vector3 GetDropPos(ITerrainWorld terrain, Random random, in DeepCore.Geometry.Vector3 src, float range)
        {
            CMath.RandomPosInRound(random, src.X, src.Y, range, out var dst_X, out var dst_Y);
            var dst = new DeepCore.Geometry.Vector3(dst_X, dst_Y, src.Z);
            if (mRandomPos == null)
            {
                this.mRandomPos = terrain.CreateAgent();
                this.mRandomPos.EnterWorld(terrain);
            }
            this.mRandomPos.Transport(src);
            this.mRandomPos.MoveLinearTo2D(dst, out var touched);
            if (terrain.Terrain.TryGetVoxelLayerByPos(mRandomPos.Position, out var layer))
            {
                dst = mRandomPos.Position;
                dst.Z = layer.Upward;
            }
            return dst;
        }
    }
    public class DropItemGenerator : DropItemAgent
    {
        class DropList
        {
            private readonly float AllPct;
            private readonly DropItem[] Items;

            public DropList(List<DropItem> items)
            {
                Items = new DropItem[items.Count];
                float total = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    Items[i] = items[i];
                    total += items[i].DropPercent;
                }
                AllPct = total;
            }

            public DropItem DropOnce(Random random)
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    int r = random.Next(0, Items.Length);
                    Items[i] = Items[r];
                }
                float seed = (float)(random.NextDouble() * 100f);
                float begin = 0;
                for (int i = 0; i < Items.Length; i++)
                {
                    float end = begin + Items[i].DropPercent;
                    if (begin <= seed && seed <= end)
                    {
                        return Items[i];
                    }
                    begin = end;
                }
                return null;
            }
        }

        private DropList[] Drops;
        private List<KeyValuePair<ItemTemplate, DropItem>> ret;

        public DropItemGenerator(List<DropItemList> drops)
        {
            if (drops != null)
            {
                Drops = new DropList[drops.Count];
                for (int i = 0; i < drops.Count; i++)
                {
                    Drops[i] = new DropList(drops[i].DropItems);
                }
                ret = new List<KeyValuePair<ItemTemplate, DropItem>>(drops.Count);
            }
            else
            {
                Drops = new DropList[0];
                ret = new List<KeyValuePair<ItemTemplate, DropItem>>(0);
            }
        }


        public IEnumerable<KeyValuePair<ItemTemplate, DropItem>> Drop(TemplateManager templates, Random random)
        {
            ret.Clear();
            for (int i = 0; i < Drops.Length; i++)
            {
                DropItem drop = Drops[i].DropOnce(random);
                if (drop != null)
                {
                    ItemTemplate template = templates.GetItem(drop.ItemTemplateID);
                    if (template != null)
                    {
                        for (int c = 0; c < drop.DropCount; c++)
                        {
                            KeyValuePair<ItemTemplate, DropItem> e = new KeyValuePair<ItemTemplate, DropItem>(template, drop);
                            ret.Add(e);
                        }
                    }
                }
            }
            return ret;
        }


    }

}

using System.Collections.Generic;
using System.IO;
using Code.System.Resource;
using Code.System.Tick;
using Code.Utility;
using DeepCore.IO;
using UnityEngine;

namespace Code.BattleView
{
    public partial class UnityBattleObject
    {
        private HashSet<long> _bindEffectSerials = new HashSet<long>();

        public long AddEffect(string file, string bindPart, int durationMS, float scale)
        {
            if (!string.IsNullOrEmpty(file))
            {
                var part = Transform.FindDeep(bindPart);
                if (!part)
                {
                    part = Transform;
                }
                var name = Resource.GetFileNameWithoutExtension(file);
                var wrapGO = ResourceSystem.GetWrapGO(file, name);
                if (wrapGO.GameObject)
                {
                    wrapGO.Transform.SetParent(part, false);
                    wrapGO.Transform.localScale *= scale;
                    var serial = TickSystem.Tick(durationMS / 1000f, (s, index) =>
                    {
                        wrapGO.Dispose();
                    });
                    _bindEffectSerials.Add(serial);
                }
            }

            return 0;
        }

        public void RemoveEffect(long serial)
        { 
            _bindEffectSerials.Remove(serial);
            TickSystem.TickCancel(serial);
        }
    }
}

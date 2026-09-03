using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCore.GUI.Data
{
    public static class GUIMetaUtils
    {
        public static void ForEachMeta<ST>(this UEComponentMeta meta, ST st, ForEachAction<ST, UEComponentMeta> action)
        {
            action(st, meta);
            if (meta is UEContainerMeta containerMeta)
            {
                if (containerMeta.Childs != null)
                {
                    foreach (var subMeta in containerMeta.Childs)
                    {
                        ForEachMeta(subMeta, st, action);
                    }
                }
            }
        }
    }
}

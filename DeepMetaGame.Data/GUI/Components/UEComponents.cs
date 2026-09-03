using DeepCore.GUI.Data;
using DeepCore.IO;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using DeepMetaGame.Data.GUI.Meta;

namespace DeepMetaGame.Data.GUI.Components
{
    [MessageType(BattleConstants.UEBindDataMeta)]
    [Desc("Zone数据绑定")]
    public class UEBindDataMeta : IUEComponentMeta, IExternalizable
    {
        public override string ToString()
        {
            return GetType().ToDesc();
        }
        public void ReadExternal(IInputStream input)
        {

        }
        public void WriteExternal(IOutputStream output)
        {

        }
    }
}

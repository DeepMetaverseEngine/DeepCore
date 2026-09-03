using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMetaGame.Data.Misc
{
    public class ResourceMeta
    {
        public float TotalEffectTimeMS;
        public bool TotalEffectLoop;
        public AnimationMeta[] Animates;
    }

    public class AnimationMeta
    {
        public string StateName;
        public float DurationMS;
    }



    [MessageType(BattleConstants.ResourcePropertiesMap)]
    [Expandable]
    public class ResourcePropertiesMap : ISerializable
    {
        /// <summary>
        /// md5 -> tuple
        /// </summary>
        public HashMap<string, IResourceProperties> PropertiesMap = new();
    }

    [MessageType(BattleConstants.ResourcePropertiesTuple)]
    [Expandable]
    public class ResourcePropertiesTuple
    {
        public string ResID;
        public IResourceProperties Properties;
    }
}

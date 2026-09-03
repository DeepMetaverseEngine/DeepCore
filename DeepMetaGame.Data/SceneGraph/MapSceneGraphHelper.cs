using DeepMetaGame.Data.ZoneEditor;
using System.Collections.Generic;

namespace DeepMetaGame.Data.SceneGraph
{
    public static class MapSceneGraphHelper
    {

        public static List<SceneNextLink> GetSceneNextLinks(this SceneData data)
        {
            var ret = new List<SceneNextLink>();
            foreach (var region in data.Regions)
            {
                foreach (var attr in region.Abilities)
                {
                    if (attr is SceneTransportAbilityData tp)
                    {
                        ret.Add(new SceneNextLink()
                        {
                            from_flag_name = region.Name,
                            from_flag_pos = region.Position,
                            to_map_id = tp.NextSceneID,
                            to_flag_name = tp.NextScenePosition,
                        });
                    }
                }
            }
            return ret;
        }

    }
}

using DeepCore;
using DeepCore.Geometry;
using DeepCore.Geometry.Terrain;
using DeepMetaGame.Data.Template;
using DeepMetaGame.Data.ZoneEditor;

namespace DeepMetaGame.Data.ZoneGeometry.Terrain
{

    public class SceneTerrainWorld : Disposable, ITerrainWorld
    {
        public readonly TerrainMap map;
        public readonly TerrainAstar astar;
        public SceneTerrainWorld(SceneData data, TemplateManager templates)
        {
            this.map = new TerrainMap(data, templates);
            this.astar = new TerrainAstar(map);
            this.PathFinder = astar.CreatePathFinder();
        }
        public ITerrain Terrain => map;
        public ITerrainAstar PathFinder { get; }
        public ITerrainAgent CreateAgent()
        {
            return new TerrainAgent();
        }
        public ITerrainAgent CreateAgent(Vector3 pos)
        {
            return new TerrainAgent(pos);
        }
        public ITerrainAgent CreateAgent(ITerrainLayer pos)
        {
            return new TerrainAgent(pos as TerrainLayer);
        }
        protected override void Disposing()
        {
        }
    }
}

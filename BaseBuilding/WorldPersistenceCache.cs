using System.Collections.Generic;
using GTA.Math;

namespace BaseBuilding
{
    public class WorldPersistenceCache
    {
        public WorldPersistenceCache()
        {
            RockPersistence = new List<RockPersistenceInfo>();
            RockSpawnAreas = new List<Vector3>();
        }

        public List<RockPersistenceInfo> RockPersistence { get; set; }
        public List<Vector3> RockSpawnAreas { get; set; }
    }

    public class RockPersistenceInfo
    {
        public string Scene { get; set; }

        public int ResourceId { get; set; }

        public int RockModelId { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public int PersistenceId { get; set; }
    }
}
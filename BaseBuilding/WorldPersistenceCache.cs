using System.Collections.Generic;
using System.Runtime.Serialization;
using GTA.Math;

namespace BaseBuilding
{
    public class WorldPersistenceCache
    {
        public WorldPersistenceCache()
        {
            RockPersistence = new List<RockPersistenceInfo>();
        }

        public List<RockPersistenceInfo> RockPersistence { get; set; }
        public List<Vector3> RockSpawnAreas { get; set; }
    }

    public class RockPersistenceInfo
    {
        public string Scene { get; set; }

        public ResourceDefinition Resource { get; set; }

        public RockModelInfo RockModel { get; set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public int PersistenceId { get; set; }
    }
}
